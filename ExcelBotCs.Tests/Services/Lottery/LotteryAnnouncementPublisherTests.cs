using Discord;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Discord.Records;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The delivery seam (spec §5.6, §5.10; criteria 9, 10, 16 and 20): the assembled post, the dry-run prefix and
///     suppressed mentions, the run-time template fallback plus alert, the posting reconciliation, and the void
///     annotation. Discord is mocked; nothing here touches a real guild.
/// </summary>
public class LotteryAnnouncementPublisherTests
{
	private const ulong MessageId = 555_000_111_222_333_444;
	private const ulong MemberRoleId = 111_111_111_111_111_111;

	private static readonly LotteryWinner Aurora = Winner("2026-11-10", 42, 7001, "Aurora", 25_000_000,
		"Chaotic Mount");

	private static readonly LotteryWinner Siegward = Winner("2026-07-21", 56, null, "Siegward", 80_000_000,
		"Chaotic Mount");

	private static readonly LotteryWinner Elara = Winner("2026-06-17", 99, null, "Elara", 6_700_000);

	private readonly Mock<ILotteryAlertService> _alerts = new();
	private readonly Mock<ILogger<LotteryAnnouncementPublisher>> _logger = new();
	private readonly Mock<IDiscordMessageService> _messages = new();
	private readonly Mock<IUserMessage> _postedMessage = new();
	private readonly Mock<ILotteryPrizePoolRepository> _prizes = new();
	private readonly StubLotterySettingsService _settings;
	private readonly Mock<ILotteryWinnerRepository> _winners = new();

	public LotteryAnnouncementPublisherTests()
	{
		_settings = new StubLotterySettingsService(ValidSettings());
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.ReturnsAsync(_postedMessage.Object);
		_postedMessage.SetupGet(message => message.Id).Returns(MessageId);
		_winners.Setup(x => x.GetLatestAsync(It.IsAny<int>())).ReturnsAsync([Aurora, Siegward, Elara]);
		_winners.Setup(x => x.GetByRoundKeyAsync(It.IsAny<string>())).ReturnsAsync([Aurora]);
		_prizes.Setup(x => x.GetInPoolAsync()).ReturnsAsync([]);
	}

	[Fact]
	public async Task PublishAsync_PostsOnePlainAnnouncementAndReturnsTheMessageId()
	{
		string? posted = null;
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		var result = await BuildPublisher().PublishAsync(Announcement());

		result.MessageId.ShouldBe("555000111222333444");
		posted.ShouldNotBeNull();
		posted.ShouldStartWith($"Hello <@&{MemberRoleId}>,");
		posted.ShouldContain("the winning number was 42! Congratulations to <@7001>! 🎉");
		posted.ShouldContain("They take the 25,000,000 Gil pot plus the Chaotic Mount.");
		posted.ShouldContain("Current pot: 1,000,000 Gil");
		posted.ShouldContain("Last 5 winners:");
		posted.ShouldContain("Full history: https://lottery.test/lottery/history");
		posted.ShouldNotContain("Participants");

		// One post, never the dry-run channel, and never an embed.
		_messages.Verify(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()), Times.Once);
		_messages.Verify(x => x.PostDryRunAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task PublishAsync_DryRun_PrefixesTheMessageAndSuppressesMentions()
	{
		string? posted = null;
		_messages.Setup(x => x.PostDryRunAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		var result = await BuildPublisher().PublishAsync(Announcement(dryRun: true));

		result.MessageId.ShouldBeNull();
		posted.ShouldNotBeNull();
		posted.ShouldStartWith("[DRY RUN] Hello <@&");
		posted.ShouldContain("the winning number was 42!");

		// The dry run goes through the mentions-suppressed seam, never the real announcement post.
		_messages.Verify(x => x.PostDryRunAsync(It.IsAny<string>()), Times.Once);
		_messages.Verify(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task PublishAsync_DryRun_PostsTheSameAnnouncementAsTheLivePost()
	{
		// criterion 20: the dry run is the *same* announcement, only prefixed and quietened. Rendering it twice from
		// one announcement must therefore differ by exactly the prefix.
		string? dryRunText = null;
		string? liveText = null;
		_messages.Setup(x => x.PostDryRunAsync(It.IsAny<string>()))
			.Callback<string>(message => dryRunText = message)
			.ReturnsAsync(_postedMessage.Object);
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(message => liveText = message)
			.ReturnsAsync(_postedMessage.Object);

		await BuildPublisher().PublishAsync(Announcement(dryRun: true));
		await BuildPublisher().PublishAsync(Announcement());

		dryRunText.ShouldNotBeNull();
		liveText.ShouldNotBeNull();
		dryRunText.ShouldBe("[DRY RUN] " + liveText);
	}

	[Fact]
	public async Task PublishAsync_DryRun_ShowsTheSimulatedWinnerInTheLastFive()
	{
		string? posted = null;
		_messages.Setup(x => x.PostDryRunAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		// Nothing was persisted for the preview round, so only history is stored; the simulated winner still has to
		// appear as the first of the five rows, exactly as it will on the real post.
		_winners.Setup(x => x.GetLatestAsync(It.IsAny<int>())).ReturnsAsync([Siegward, Elara]);

		await BuildPublisher().PublishAsync(Announcement(dryRun: true));

		posted.ShouldNotBeNull();
		var rows = posted.Split('\n').Where(line => line.Contains(" | ", StringComparison.Ordinal)).ToList();
		rows[0].ShouldStartWith("Aurora | 42 - 25,000,000 Gil");
		rows[1].ShouldStartWith("Siegward | 56");
		rows[2].ShouldStartWith("Elara | 99");
	}

	[Fact]
	public async Task PublishAsync_WithACustomTemplate_UsesItForTheResultBlock()
	{
		string? posted = null;
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		_settings.Settings = ValidSettings(winTemplate:
			"number {number} was hit by {winners}, who takes {prize}! Next pot {nextPot}.");

		await BuildPublisher().PublishAsync(Announcement());

		posted.ShouldNotBeNull();
		posted.ShouldContain("number 42 was hit by <@7001>, who takes 25,000,000 Gil! Next pot 1,000,000.");
	}

	[Fact]
	public async Task PublishAsync_WithAnInvalidStoredTemplate_FallsBackToTheDefaultAndAlerts()
	{
		string? posted = null;
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		_settings.Settings = ValidSettings(winTemplate: "no placeholders at all");

		await BuildPublisher().PublishAsync(Announcement());

		posted.ShouldNotBeNull();
		posted.ShouldContain("the winning number was 42! Congratulations to <@7001>! 🎉");

		_alerts.Verify(x => x.PublishAsync(
			It.Is<LotteryAlert>(alert => alert.NeedsAdmin
			                             && alert.RoundKey == "2026-11-10"
			                             && alert.Error!.Contains("required placeholder")),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task PublishAsync_WithAnAssembledMessageOverTheLimit_FallsBackToTheDefaultsAndAlerts()
	{
		string? posted = null;
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		// The template alone fits the save-time measurement, but five very long historical names push the real post
		// over Discord's limit.
		var filler = new string('x', 700);
		_settings.Settings = ValidSettings(winTemplate: LotterySettingsDefaults.WinTemplate + "\n" + filler);

		var longNames = Enumerable.Range(0, 5)
			.Select(index => Winner($"2026-0{index + 1}-01", 42, null, new string('n', 120), 1_000_000))
			.ToList();
		_winners.Setup(x => x.GetLatestAsync(It.IsAny<int>())).ReturnsAsync(longNames);

		await BuildPublisher().PublishAsync(Announcement());

		posted.ShouldNotBeNull();
		posted.ShouldNotContain(filler);
		posted.ShouldContain("the winning number was 42! Congratulations to <@7001>! 🎉");

		_alerts.Verify(x => x.PublishAsync(
			It.Is<LotteryAlert>(alert => alert.NeedsAdmin && alert.Error!.Contains("2,000")),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task PublishAsync_WhenTheLotteryChannelIsUnreachable_ThrowsSoTheRunnerRetries()
	{
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.ReturnsAsync((IUserMessage?)null);

		await Should.ThrowAsync<InvalidOperationException>(() =>
			BuildPublisher().PublishAsync(Announcement()));
	}

	[Fact]
	public async Task PublishAsync_ListsTheNewestFiveFromTheWinnerRepository()
	{
		string? posted = null;
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		await BuildPublisher().PublishAsync(Announcement());

		_winners.Verify(x => x.GetLatestAsync(5), Times.Once);

		posted.ShouldNotBeNull();
		var rows = posted.Split('\n').Where(line => line.Contains(" | ", StringComparison.Ordinal)).ToList();
		rows[0].ShouldStartWith("Aurora | 42");
		rows[1].ShouldStartWith("Siegward | 56");
		rows[2].ShouldStartWith("Elara | 99");
	}

	[Fact]
	public async Task FindExistingPostAsync_AdoptsTheRoundsRecentBotMessage()
	{
		var round = RoundWithPostingMarker();
		var rendered = await RenderAsync();

		_messages.Setup(x => x.GetRecentLotteryChannelMessagesAsync(It.IsAny<int>()))
			.ReturnsAsync([new DiscordChannelMessage(MessageId, 1, true, rendered,
				round.PostingStartedAt!.Value.AddSeconds(5))]);

		var existing = await BuildPublisher().FindExistingPostAsync(round);

		existing.ShouldBe("555000111222333444");
	}

	[Fact]
	public async Task FindExistingPostAsync_NeverAdoptsADryRunMessage()
	{
		var round = RoundWithPostingMarker();
		var rendered = await RenderAsync();

		_messages.Setup(x => x.GetRecentLotteryChannelMessagesAsync(It.IsAny<int>()))
			.ReturnsAsync([new DiscordChannelMessage(MessageId, 1, true, "[DRY RUN] " + rendered,
				round.PostingStartedAt!.Value.AddSeconds(5))]);

		(await BuildPublisher().FindExistingPostAsync(round)).ShouldBeNull();
	}

	[Fact]
	public async Task FindExistingPostAsync_IgnoresMessagesFromPeopleAndMessagesOutsideTheWindow()
	{
		var round = RoundWithPostingMarker();
		var rendered = await RenderAsync();
		var startedAt = round.PostingStartedAt!.Value;

		_messages.Setup(x => x.GetRecentLotteryChannelMessagesAsync(It.IsAny<int>()))
			.ReturnsAsync([
				// A member pasting the announcement is not the bot's post.
				new DiscordChannelMessage(1, 2, false, rendered, startedAt.AddSeconds(5)),
				// A previous round's announcement is far before this round's posting marker.
				new DiscordChannelMessage(2, 1, true, rendered, startedAt.AddDays(-7)),
				// And a bot chatter message in the window carries none of the announcement markers.
				new DiscordChannelMessage(3, 1, true, "good morning everyone", startedAt.AddSeconds(10))
			]);

		(await BuildPublisher().FindExistingPostAsync(round)).ShouldBeNull();
	}

	[Fact]
	public async Task FindExistingPostAsync_WithATemplateChangedSinceThePost_StillAdoptsIt()
	{
		var round = RoundWithPostingMarker();
		var rendered = await RenderAsync();

		// The admin edited the winner template after the crashed attempt posted: the result block differs, but the
		// generated sections still identify the message as an announcement.
		_settings.Settings = ValidSettings(winTemplate:
			"number {number} was hit by {winners}, who takes {prize}! Next pot {nextPot}.");

		_messages.Setup(x => x.GetRecentLotteryChannelMessagesAsync(It.IsAny<int>()))
			.ReturnsAsync([new DiscordChannelMessage(MessageId, 1, true, rendered,
				round.PostingStartedAt!.Value.AddSeconds(5))]);

		(await BuildPublisher().FindExistingPostAsync(round)).ShouldBe("555000111222333444");
	}

	[Fact]
	public async Task FindExistingPostAsync_WithoutACandidate_ReturnsNull()
	{
		_messages.Setup(x => x.GetRecentLotteryChannelMessagesAsync(It.IsAny<int>()))
			.ReturnsAsync([]);

		(await BuildPublisher().FindExistingPostAsync(RoundWithPostingMarker())).ShouldBeNull();

		// An empty channel read is not a reason to post twice, but it does mean "no existing post".
		_messages.Verify(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task AnnotateVoidedAsync_PostsTheReasonAndTheAdminLinkIntoTheLotteryChannel()
	{
		string? posted = null;
		_messages.Setup(x => x.PostInLotteryChannelAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.Returns(Task.CompletedTask);

		await BuildPublisher().AnnotateVoidedAsync(RoundWithPostingMarker(), "the draw was wrong");

		posted.ShouldNotBeNull();
		posted.ShouldContain("2026-11-10");
		posted.ShouldContain("the draw was wrong");
		posted.ShouldContain("void");
		posted.ShouldContain("https://lottery.test/lottery/admin");
	}

	private LotteryAnnouncementPublisher BuildPublisher()
	{
		var discord = new DiscordBotOptions
		{
			GuildId = 1,
			LotteryChannel = 2,
			AnnouncementChannel = 3,
			EventsChannel = 4,
			UpcomingRosterChannel = 5,
			HallOfClearsChannel = 6,
			LogChannel = 7,
			RiddlesChannel = 8,
			LotteryMemberRoleId = MemberRoleId,
			LotteryCustomEmojiId = 9,
			AdminRoleIds = [10]
		};

		return new LotteryAnnouncementPublisher(_messages.Object, _settings, _winners.Object, _prizes.Object,
			_alerts.Object, Options.Create(discord),
			Options.Create(new WebBaseUrlOptions { BaseUrl = "https://lottery.test" }), _logger.Object);
	}

	private async Task<string> RenderAsync()
	{
		string? posted = null;
		_messages.Setup(x => x.PostLotteryAnnouncementAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.ReturnsAsync(_postedMessage.Object);

		await BuildPublisher().PublishAsync(Announcement());

		return posted!;
	}

	private static LotteryEffectiveSettings ValidSettings(string? winTemplate = null, string? noWinTemplate = null)
	{
		return TestLotterySettings.Create(
			winTemplate: winTemplate ?? LotterySettingsDefaults.WinTemplate,
			noWinTemplate: noWinTemplate ?? LotterySettingsDefaults.NoWinTemplate);
	}

	private static LotteryAnnouncement Announcement(bool dryRun = false)
	{
		return new LotteryAnnouncement(Round(), [Aurora], 25_000_000, 1_000_000, dryRun);
	}

	private static LotteryRound Round()
	{
		return new LotteryRound
		{
			RoundKey = "2026-11-10",
			ScheduledAt = new DateTime(2026, 11, 10, 19, 0, 0, DateTimeKind.Utc),
			RunAt = new DateTime(2026, 11, 10, 19, 0, 5, DateTimeKind.Utc),
			Status = LotteryRoundStatus.Drawn,
			WinningNumber = 42,
			PotBefore = 25_000_000,
			PotAfter = 1_000_000
		};
	}

	private static LotteryRound RoundWithPostingMarker()
	{
		var round = Round();
		round.PostingStartedAt = new DateTime(2026, 11, 10, 19, 0, 10, DateTimeKind.Utc);
		return round;
	}

	private static LotteryWinner Winner(string roundKey, int guess, ulong? discordId, string displayName, long gilPrize,
		params string[] bonusItems)
	{
		return new LotteryWinner
		{
			RoundKey = roundKey,
			DiscordId = discordId,
			DisplayName = displayName,
			Guess = guess,
			GilPrize = gilPrize,
			BonusItems = bonusItems,
			PayoutStatus = LotteryPayoutStatus.Paid
		};
	}
}
