using Discord;
using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Discord.Records;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The reminder module against a real Mongo, a fake clock and a fake Discord: the opt-in record, the audience
///     selection (opted in, current FC, guesses left), the one-post-per-week window and its restart idempotency, the
///     retry/alert policy, the officer's manual reminder and the admin test send. No test waits for a real Monday.
/// </summary>
[Collection("MongoDB")]
public class LotteryReminderServiceTests : MongoDbTest
{
	private const ulong AdminId = 900000000000000001UL;

	// Monday 2026-11-09 18:00 Europe/Berlin is 17:00 UTC; the default Tuesday 20:00 draw is 19:00 UTC on the 10th.
	private static readonly DateTimeOffset BeforeReminder = TestInstants.Utc("2026-11-09T12:00:00");
	private static readonly DateTimeOffset AtReminder = TestInstants.Utc("2026-11-09T17:00:00");
	private const string NextRoundKey = "2026-11-10";

	private readonly List<Member> _fcMembers = [];
	private readonly List<DiscordChannelMessage> _channelMessages = [];
	private readonly List<(string Text, IReadOnlyCollection<ulong> Mentions)> _posted = [];
	private readonly List<(ulong DiscordId, string Text)> _directMessages = [];

	private ILotteryGuessRepository _guesses = null!;
	private IExtraLotteryGuessRepository _extraGuesses = null!;
	private ILotteryReminderOptinRepository _optIns = null!;
	private LotteryRoundRepository _rounds = null!;
	private FakeTimeProvider _time = null!;
	private StubLotterySettingsService _settings = null!;
	private Mock<IMemberService> _members = null!;
	private Mock<IDiscordMessageService> _messages = null!;
	private FakeLotteryAlertService _alerts = null!;
	private LotteryReminderService _service = null!;

	public LotteryReminderServiceTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_guesses = new LotteryGuessRepository(mongoClient, databaseOptions);
		_extraGuesses = new ExtraLotteryGuessRepository(mongoClient, databaseOptions);
		_optIns = new LotteryReminderOptinRepository(mongoClient, databaseOptions);
		_rounds = new LotteryRoundRepository(mongoClient, databaseOptions);
	}

	protected override async Task OnAfterInitializeAsync()
	{
		await _optIns.EnsureIndexesAsync();
		await _rounds.EnsureIndexesAsync();

		_time = new FakeTimeProvider(BeforeReminder);
		_settings = new StubLotterySettingsService(TestLotterySettings.Create(remindersEnabled: true));

		_members = new Mock<IMemberService>();
		_members.Setup(service => service.GetFcMembers()).ReturnsAsync(() => _fcMembers);

		_messages = new Mock<IDiscordMessageService>();
		_messages.Setup(service => service.GetRecentLotteryChannelMessagesAsync(It.IsAny<int>()))
			.ReturnsAsync(() => (IReadOnlyList<DiscordChannelMessage>)_channelMessages.ToList());
		_messages.Setup(service => service.PostLotteryReminderAsync(It.IsAny<string>(),
				It.IsAny<IReadOnlyCollection<ulong>>()))
			.ReturnsAsync((string text, IReadOnlyCollection<ulong> mentions) =>
			{
				_posted.Add((text, mentions));
				return Mock.Of<IUserMessage>();
			});
		_messages.Setup(service => service.SendLotteryReminderDirectMessageAsync(It.IsAny<ulong>(),
				It.IsAny<string>()))
			.ReturnsAsync((ulong discordId, string text) =>
			{
				_directMessages.Add((discordId, text));
				return true;
			});

		_alerts = new FakeLotteryAlertService();
		BuildService();
	}

	private void BuildService(LotteryStartupCheckService? startupCheck = null)
	{
		var schedules = new LotteryScheduleService(_settings, _rounds, _time);
		_service = new LotteryReminderService(_settings, schedules, _optIns, _rounds, _guesses, _extraGuesses,
			_members.Object, _messages.Object, _alerts, _time,
			Mock.Of<ILogger<LotteryReminderService>>(), startupCheck);
	}

	/// <summary>A startup check whose verdict the test decides; the scheduler reads it once at startup.</summary>
	private sealed class StubStartupCheck(LotteryStartupResult result) : ILotteryStartupCheck
	{
		public Task<LotteryStartupResult> CheckAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(result);
		}
	}

	/// <summary>A startup check that has been constructed but not run: <c>Result</c> is still null.</summary>
	private LotteryStartupCheckService PendingStartupCheck()
	{
		return new LotteryStartupCheckService(new StubStartupCheck(LotteryStartupResult.Healthy),
			NullLogger<LotteryStartupCheckService>.Instance);
	}

	/// <summary>A startup check that has finished degraded, the bad-config startup state of spec §5.10.</summary>
	private async Task<LotteryStartupCheckService> DegradedStartupCheckAsync()
	{
		var service = new LotteryStartupCheckService(
			new StubStartupCheck(LotteryStartupResult.Degraded(["DiscordBot:GuildId is not configured (0)."])),
			NullLogger<LotteryStartupCheckService>.Instance);

		await service.StartAsync(CancellationToken.None);
		await service.Completion;
		return service;
	}

	private void SetFcMembers(params ulong[] discordIds)
	{
		_fcMembers.Clear();
		_fcMembers.AddRange(discordIds.Select(discordId => new Member
		{
			DiscordId = discordId.ToString(),
			DiscordName = $"Member {discordId}",
			Roles = [new MemberRole { IsMember = true }]
		}));
	}

	private async Task OptInAsync(ulong discordId, bool enabled = true)
	{
		await _optIns.SetEnabledAsync(discordId, enabled);
	}

	private async Task UseGuessAsync(ulong discordId, int number)
	{
		await _guesses.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = number });
	}

	private async Task AwardGuessAsync(ulong discordId)
	{
		await _extraGuesses.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId, Reason = "event" });
	}

	#region Opt-in

	[Fact]
	public async Task GetPreference_IsOffForAMemberWhoNeverOptedIn()
	{
		var preference = await _service.GetPreferenceAsync(11);

		preference.Enabled.ShouldBeFalse();
	}

	[Fact]
	public async Task SetPreference_RoundTripsAndIsIdempotent()
	{
		(await _service.SetPreferenceAsync(11, true)).Enabled.ShouldBeTrue();
		(await _service.SetPreferenceAsync(11, true)).Enabled.ShouldBeTrue();
		(await _service.GetPreferenceAsync(11)).Enabled.ShouldBeTrue();

		// One document per Discord id, no matter how often the toggle is saved.
		(await _optIns.GetAsync()).ShouldHaveSingleItem();

		(await _service.SetPreferenceAsync(11, false)).Enabled.ShouldBeFalse();
		(await _service.GetPreferenceAsync(11)).Enabled.ShouldBeFalse();
		(await _optIns.GetAsync()).ShouldHaveSingleItem();
	}

	[Fact]
	public async Task SetPreference_WorksWithoutFcMembership_SoTheOptOutButtonSurvivesLeaving()
	{
		SetFcMembers();

		var preference = await _service.SetPreferenceAsync(11, false);

		preference.Enabled.ShouldBeFalse();
		(await _service.GetPreferenceAsync(11)).Enabled.ShouldBeFalse();
	}

	[Fact]
	public async Task TheOptInIsRetainedThroughLeavingAndRejoining_ButInactiveWhileNotAMember()
	{
		await _service.SetPreferenceAsync(11, true);

		// Left the FC: the record survives but nobody is in the audience.
		SetFcMembers();
		(await _service.GetAudienceAsync()).IsEmpty.ShouldBeTrue();
		(await _service.GetPreferenceAsync(11)).Enabled.ShouldBeTrue();

		// Rejoined: the same record makes them eligible again without opting in a second time.
		SetFcMembers(11);
		var audience = await _service.GetAudienceAsync();
		audience.Recipients.ShouldHaveSingleItem().DiscordId.ShouldBe(11UL);
	}

	#endregion

	#region Audience

	[Fact]
	public async Task GetAudience_OnlyCurrentMembersWhoOptedInAndHaveAGuessLeft()
	{
		await UseGuessAsync(33, 1);
		await UseGuessAsync(44, 2);
		await AwardGuessAsync(11);
		await OptInAsync(11);
		await OptInAsync(22);
		await OptInAsync(33);
		await OptInAsync(44);
		await OptInAsync(66); // opted in but has no member record: a deleted/unknown account.
		SetFcMembers(11, 22, 33, 44, 55);

		var audience = await _service.GetAudienceAsync();

		audience.Recipients.Select(recipient => (recipient.DiscordId, recipient.RemainingGuesses))
			.ShouldBe([(11UL, 2), (22UL, 1)]);
	}

	[Fact]
	public async Task GetAudience_GroupsExactCountsInDescendingOrderAndOrdersMembersById()
	{
		await AwardGuessAsync(300);
		await OptInAsync(300);
		await OptInAsync(200);
		await OptInAsync(100);
		SetFcMembers(300, 200, 100);

		var audience = await _service.GetAudienceAsync();

		audience.Groups.Select(group => group.RemainingGuesses).ShouldBe([2, 1]);
		audience.Groups[0].DiscordIds.ShouldBe([300UL]);
		audience.Groups[1].DiscordIds.ShouldBe([100UL, 200UL]);
	}

	[Fact]
	public async Task GetAudience_SkipsMembersWithNoGuessesLeftAndOptedOutMembers()
	{
		await UseGuessAsync(11, 1);
		await AwardGuessAsync(22);
		await UseGuessAsync(22, 1);
		await UseGuessAsync(22, 2);
		await UseGuessAsync(33, 1);
		await OptInAsync(11);
		await OptInAsync(22);
		await OptInAsync(33, false);
		SetFcMembers(11, 22, 33);

		var audience = await _service.GetAudienceAsync();

		audience.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public async Task GetAudience_IsEmptyWhenNobodyOptedIn()
	{
		SetFcMembers(11, 22);

		var audience = await _service.GetAudienceAsync();

		audience.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public async Task GetAudience_SkipsUnparsableMemberIdsInsteadOfThrowing()
	{
		SetFcMembers();
		_fcMembers.Add(new Member { DiscordId = "not-a-discord-id", Roles = [new MemberRole { IsMember = true }] });
		await OptInAsync(11);

		var audience = await _service.GetAudienceAsync();

		audience.IsEmpty.ShouldBeTrue();
	}

	#endregion

	#region Scheduled tick

	private async Task SeedTwoRecipientsAsync()
	{
		// 11 has an extra guess (2 left), 22 has the base guess (1 left).
		await AwardGuessAsync(11);
		await OptInAsync(11);
		await OptInAsync(22);
		SetFcMembers(11, 22);
	}

	private async Task SeedSkippedNextRoundAsync()
	{
		await _rounds.CreateAsync(new LotteryRound
		{
			RoundKey = NextRoundKey,
			ScheduledAt = TestInstants.UtcTime("2026-11-10T19:00:00"),
			Status = LotteryRoundStatus.Skipped
		});
	}

	[Fact]
	public async Task TickAsync_AtTheReminderInstant_PostsTheOptedInAudienceWithTheExactCopyAndMentions()
	{
		await SeedTwoRecipientsAsync();
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		outcome.RoundKey.ShouldBe(NextRoundKey);
		outcome.Recipients.ShouldBe(2);

		var post = _posted.ShouldHaveSingleItem();
		post.Text.ShouldBe(
			"## Use your lottery guesses before Tuesday 20:00\n" +
			"<2 guesses remaining: <@11>\n" +
			"<1 guess remaining: <@22>\n" +
			"Only members who opted in are pinged. The draw runs Tuesday at 20:00.");
		post.Mentions.ShouldBe([11UL, 22UL]);

		// One channel post with intentional mentions, never a DM per member (spec §5.7).
		_directMessages.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_BeforeTheReminderInstant_IsSilent()
	{
		await SeedTwoRecipientsAsync();
		_time.Set(BeforeReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.OutsideWindow);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_AfterTheWindowCloses_DoesNotPostForTheNextDraw()
	{
		await SeedTwoRecipientsAsync();
		// 18:30 Europe/Berlin on the draw day: the window closed at 17:00 UTC (18:00 Berlin) the day before.
		_time.Set(TestInstants.Utc("2026-11-10T17:30:00"));

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.OutsideWindow);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WhenTheReminderToggleIsOff_IsSilent()
	{
		await SeedTwoRecipientsAsync();
		_settings.Settings = TestLotterySettings.Create(remindersEnabled: false);
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.RemindersDisabled);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WhenTheDeploymentSwitchIsOff_IsSilent()
	{
		await SeedTwoRecipientsAsync();
		_settings.Settings = TestLotterySettings.Create(remindersEnabled: true, automationEnabled: false);
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.AutomationDisabled);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WhenAutomationIsPaused_IsSilent()
	{
		await SeedTwoRecipientsAsync();
		_settings.Settings = TestLotterySettings.Create(remindersEnabled: true, pauseAutomation: true);
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Paused);
		_posted.ShouldBeEmpty();
	}

	// §5.10 and criterion 19: a degraded or still-running startup check disables *all* automation, the reminder
	// included. Before #116 the reminder honoured only the toggles, so a degraded start could still ping the audience.
	[Fact]
	public async Task TickAsync_WhileTheStartupCheckIsStillRunning_DoesNotPost()
	{
		await SeedTwoRecipientsAsync();
		BuildService(PendingStartupCheck());
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.StartupPending);
		outcome.Posted.ShouldBeFalse();
		_posted.ShouldBeEmpty();
		_alerts.Alerts.ShouldBeEmpty("the startup check owns the single alert");
	}

	[Fact]
	public async Task TickAsync_WhenTheStartupCheckIsDegraded_DoesNotPostAndDoesNotAlertAgain()
	{
		await SeedTwoRecipientsAsync();
		BuildService(await DegradedStartupCheckAsync());
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.StartupDegraded);
		outcome.Posted.ShouldBeFalse();
		_posted.ShouldBeEmpty();
		_alerts.Alerts.ShouldBeEmpty("the degraded startup already raised exactly one alert");
	}

	[Fact]
	public async Task RemindNowAsync_WhenTheStartupCheckIsDegraded_StillPostsTheManualReminder()
	{
		// The manual reminder is the documented escape hatch, exactly like the admin's Run now: the startup verdict
		// stops the schedule, not an officer who explicitly asks for a post.
		await SeedTwoRecipientsAsync();
		BuildService(await DegradedStartupCheckAsync());
		_time.Set(AtReminder);

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_posted.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task TickAsync_AfterAHealthyStartupCheck_PostsTheReminder()
	{
		await SeedTwoRecipientsAsync();
		var startup = PendingStartupCheck();
		await startup.StartAsync(CancellationToken.None);
		await startup.Completion;
		BuildService(startup);
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_posted.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task TickAsync_WhenNobodyOptedIn_IsSilent()
	{
		SetFcMembers(11);
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.NobodyOptedIn);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WhenNobodyIsEligible_IsSilent()
	{
		await OptInAsync(11);
		await UseGuessAsync(11, 1);
		SetFcMembers(11);
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.NobodyEligible);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WhenTheNextRoundWasSkipped_IsSilent()
	{
		await SeedTwoRecipientsAsync();
		await SeedSkippedNextRoundAsync();
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Skipped);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_TwiceInTheSameProcess_PostsOnlyOnce()
	{
		await SeedTwoRecipientsAsync();
		_time.Set(AtReminder);

		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		var second = await _service.TickAsync();

		second.Disposition.ShouldBe(LotteryReminderDisposition.AlreadySent);
		_posted.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task TickAsync_AfterARestartInsideTheWindow_AdoptsTheExistingPostInsteadOfPostingAgain()
	{
		await SeedTwoRecipientsAsync();
		_time.Set(AtReminder);
		_channelMessages.Add(new DiscordChannelMessage(123, 999, true,
			"## Use your lottery guesses before Tuesday 20:00\n<1 guess remaining: <@11>",
			AtReminder.AddHours(2)));

		// A fresh instance has no in-memory memory; the channel is the durable idempotency marker.
		BuildService();
		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.AlreadySent);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_IgnoresASimilarMessageFromAnEarlierWeek()
	{
		await SeedTwoRecipientsAsync();
		_time.Set(AtReminder);
		_channelMessages.Add(new DiscordChannelMessage(123, 999, true,
			"## Use your lottery guesses before Tuesday 20:00",
			AtReminder.AddDays(-7)));

		BuildService();
		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_posted.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task TickAsync_FollowsARescheduledRoundsNewDayAndTime()
	{
		await SeedTwoRecipientsAsync();
		await _rounds.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-11-11",
			// Wednesday 20:30 Europe/Berlin (CET, +1).
			ScheduledAt = TestInstants.UtcTime("2026-11-11T19:30:00"),
			Status = LotteryRoundStatus.Scheduled
		});
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		var post = _posted.ShouldHaveSingleItem();
		post.Text.ShouldStartWith("## Use your lottery guesses before Wednesday 20:30");
		post.Text.ShouldEndWith("Only members who opted in are pinged. The draw runs Wednesday at 20:30.");
	}

	[Fact]
	public async Task TickAsync_WhenTheAudienceIsEmptyButTheRoundIsSkipped_ReportsTheSkippedRound()
	{
		// A skipped round is silent even when there would be an audience, so the officer's manual reminder still
		// knows why the scheduled post did not happen.
		await SeedTwoRecipientsAsync();
		await SeedSkippedNextRoundAsync();
		_time.Set(AtReminder.AddHours(-3));

		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.Skipped);
	}

	#endregion

	#region Failure, retry and alert

	private int _postAttempts;

	private void FailPosts()
	{
		_messages.Setup(service => service.PostLotteryReminderAsync(It.IsAny<string>(),
				It.IsAny<IReadOnlyCollection<ulong>>()))
			.ReturnsAsync((string _, IReadOnlyCollection<ulong> _) =>
			{
				_postAttempts++;
				throw new InvalidOperationException("Discord is down");
			});
	}

	[Fact]
	public async Task TickAsync_WhenThePostFails_RetriesThreeTimesOverFifteenMinutesAndAlertsOnce()
	{
		await SeedTwoRecipientsAsync();
		FailPosts();
		_time.Set(AtReminder);

		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.Retrying);

		// Before the recorded retry instant the tick waits instead of trying again.
		_time.Advance(TimeSpan.FromMinutes(4));
		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.WaitingForRetry);
		_postAttempts.ShouldBe(1);

		_time.Advance(TimeSpan.FromMinutes(1));
		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.Retrying);
		_postAttempts.ShouldBe(2);

		_time.Advance(TimeSpan.FromMinutes(10));
		var exhausted = await _service.TickAsync();

		exhausted.Disposition.ShouldBe(LotteryReminderDisposition.Failed);
		_postAttempts.ShouldBe(3);
		var alert = _alerts.Alerts.ShouldHaveSingleItem();
		alert.RoundKey.ShouldBe(NextRoundKey);
		alert.Attempts.ShouldBe(3);
		alert.NeedsAdmin.ShouldBeTrue();
		alert.Title.ShouldContain("reminder");
		alert.Error.ShouldBe("Discord is down");

		// The exhausted round is not retried or alerted again.
		_time.Advance(TimeSpan.FromMinutes(1));
		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.Failed);
		_postAttempts.ShouldBe(3);
		_alerts.Alerts.Count.ShouldBe(1);
	}

	[Fact]
	public async Task TickAsync_WhenTheChannelIsUnreachable_TreatsTheNullPostAsAFailure()
	{
		await SeedTwoRecipientsAsync();
		_messages.Setup(service => service.PostLotteryReminderAsync(It.IsAny<string>(),
				It.IsAny<IReadOnlyCollection<ulong>>()))
			.ReturnsAsync((IUserMessage?)null);
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Retrying);
		outcome.Message.ShouldContain("attempt 1 of 3");
	}

	[Fact]
	public async Task TickAsync_WhenARetrySucceeds_PostsOnceAndAlertsNobody()
	{
		await SeedTwoRecipientsAsync();
		FailPosts();
		_time.Set(AtReminder);
		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.Retrying);

		_messages.Setup(service => service.PostLotteryReminderAsync(It.IsAny<string>(),
				It.IsAny<IReadOnlyCollection<ulong>>()))
			.ReturnsAsync((string text, IReadOnlyCollection<ulong> mentions) =>
			{
				_posted.Add((text, mentions));
				return Mock.Of<IUserMessage>();
			});
		_time.Advance(TimeSpan.FromMinutes(5));

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_posted.ShouldHaveSingleItem();
		_alerts.Alerts.ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WhenTheDuplicateCheckCannotReadTheChannel_StillPosts()
	{
		await SeedTwoRecipientsAsync();
		_messages.Setup(service => service.GetRecentLotteryChannelMessagesAsync(It.IsAny<int>()))
			.ThrowsAsync(new InvalidOperationException("channel read failed"));
		_time.Set(AtReminder);

		var outcome = await _service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_posted.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task TickAsync_WhenSettingsCannotBeRead_ReturnsAFailedOutcomeInsteadOfThrowing()
	{
		var brokenSettings = new Mock<ILotterySettingsService>();
		brokenSettings.Setup(service => service.GetEffectiveAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("database down"));
		var schedules = new LotteryScheduleService(_settings, _rounds, _time);
		var service = new LotteryReminderService(brokenSettings.Object, schedules, _optIns, _rounds, _guesses,
			_extraGuesses, _members.Object, _messages.Object, _alerts, _time,
			Mock.Of<ILogger<LotteryReminderService>>());

		var outcome = await service.TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Failed);
		outcome.Message.ShouldContain("database down");
	}

	#endregion

	#region Opt-out button semantics

	[Fact]
	public async Task SetPreferenceOff_ChangesOnlyTheCallersRecord()
	{
		await OptInAsync(11);
		await OptInAsync(22);

		await _service.SetPreferenceAsync(11, false);

		(await _service.GetPreferenceAsync(11)).Enabled.ShouldBeFalse();
		(await _service.GetPreferenceAsync(22)).Enabled.ShouldBeTrue();
	}

	[Fact]
	public async Task SetPreferenceOff_IsIdempotentAndWorksForACallerWhoNeverOptedIn()
	{
		await _service.SetPreferenceAsync(11, false);

		var again = await _service.SetPreferenceAsync(11, false);

		again.Enabled.ShouldBeFalse();
		(await _optIns.GetAsync()).ShouldHaveSingleItem().Enabled.ShouldBeFalse();
	}

	#endregion

	#region Manual reminder

	[Fact]
	public async Task RemindNowAsync_PostsTheSameSpecCopyAsTheScheduledReminder()
	{
		await SeedTwoRecipientsAsync();
		_time.Set(BeforeReminder);

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		outcome.Recipients.ShouldBe(2);
		var post = _posted.ShouldHaveSingleItem();
		post.Text.ShouldBe(
			"## Use your lottery guesses before Tuesday 20:00\n" +
			"<2 guesses remaining: <@11>\n" +
			"<1 guess remaining: <@22>\n" +
			"Only members who opted in are pinged. The draw runs Tuesday at 20:00.");
		post.Mentions.ShouldBe([11UL, 22UL]);
	}

	[Fact]
	public async Task RemindNowAsync_IgnoresTheReminderToggleAndTheDeliveryWindow()
	{
		await SeedTwoRecipientsAsync();
		_settings.Settings = TestLotterySettings.Create(remindersEnabled: false);
		_time.Set(BeforeReminder);

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_posted.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task RemindNowAsync_IgnoresAnAlreadyPostedScheduledReminder()
	{
		await SeedTwoRecipientsAsync();
		_time.Set(AtReminder);
		(await _service.TickAsync()).Disposition.ShouldBe(LotteryReminderDisposition.Sent);

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_posted.Count.ShouldBe(2);
	}

	[Fact]
	public async Task RemindNowAsync_WhenNobodyIsEligible_DoesNotPostAndExplainsWhy()
	{
		await OptInAsync(11);
		await UseGuessAsync(11, 1);
		SetFcMembers(11);

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.NobodyEligible);
		outcome.Message.ShouldContain("unused lottery guesses");
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task RemindNowAsync_WhenNobodyOptedIn_DoesNotPost()
	{
		SetFcMembers(11, 22);

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.NobodyOptedIn);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task RemindNowAsync_WhenTheNextRoundWasSkipped_DoesNotPost()
	{
		await SeedTwoRecipientsAsync();
		await SeedSkippedNextRoundAsync();

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Skipped);
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task RemindNowAsync_WhenThePostFails_AlertsOnceAndDoesNotRetry()
	{
		await SeedTwoRecipientsAsync();
		FailPosts();

		var outcome = await _service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Failed);
		_postAttempts.ShouldBe(1);
		_alerts.Alerts.ShouldHaveSingleItem().NeedsAdmin.ShouldBeTrue();
	}

	[Fact]
	public async Task RemindNowAsync_WhenSettingsCannotBeRead_ReturnsAFailedOutcome()
	{
		var brokenSettings = new Mock<ILotterySettingsService>();
		brokenSettings.Setup(service => service.GetEffectiveAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("database down"));
		// The manual reminder bypasses the toggles, but it still reads the schedule, which reads the settings.
		var schedules = new LotteryScheduleService(brokenSettings.Object, _rounds, _time);
		var service = new LotteryReminderService(brokenSettings.Object, schedules, _optIns, _rounds, _guesses,
			_extraGuesses, _members.Object, _messages.Object, _alerts, _time,
			Mock.Of<ILogger<LotteryReminderService>>());

		var outcome = await service.RemindNowAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Failed);
	}

	#endregion

	#region Admin test send

	[Fact]
	public async Task SendTestAsync_DeliversTheCopyToTheRequestingAdminOnly_NeverToTheChannel()
	{
		await SeedTwoRecipientsAsync();

		var outcome = await _service.SendTestAsync(AdminId);

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		var message = _directMessages.ShouldHaveSingleItem();
		message.DiscordId.ShouldBe(AdminId);
		message.Text.ShouldBe(
			"## Use your lottery guesses before Tuesday 20:00\n" +
			"<2 guesses remaining: <@11>\n" +
			"<1 guess remaining: <@22>\n" +
			"Only members who opted in are pinged. The draw runs Tuesday at 20:00.");
		_posted.ShouldBeEmpty();
	}

	[Fact]
	public async Task SendTestAsync_IgnoresTheDeliveryWindowAndTheToggles()
	{
		await SeedTwoRecipientsAsync();
		_settings.Settings = TestLotterySettings.Create(remindersEnabled: false);
		_time.Set(BeforeReminder);

		var outcome = await _service.SendTestAsync(AdminId);

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Sent);
		_directMessages.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task SendTestAsync_WhenNobodyIsEligible_SendsNothingAndSaysWhy()
	{
		await OptInAsync(11);
		await UseGuessAsync(11, 1);
		SetFcMembers(11);

		var outcome = await _service.SendTestAsync(AdminId);

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.NobodyEligible);
		_directMessages.ShouldBeEmpty();
	}

	[Fact]
	public async Task SendTestAsync_WhenTheAdminCannotBeReached_ReturnsFailed()
	{
		await SeedTwoRecipientsAsync();
		_messages.Setup(service => service.SendLotteryReminderDirectMessageAsync(It.IsAny<ulong>(),
				It.IsAny<string>()))
			.ReturnsAsync(false);

		var outcome = await _service.SendTestAsync(AdminId);

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.Failed);
		_directMessages.ShouldBeEmpty();
	}

	#endregion
}
