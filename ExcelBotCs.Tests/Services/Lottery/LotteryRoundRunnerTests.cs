using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The round engine against a real Mongo and a fake clock: the full round with and without a winner, the double
///     claim, restart resume, catch-up selection, skip, void, dry run and the retry after a draw or announcement
///     failure. The hosted scheduler is stripped by <see cref="Utils.IntegrationTestBase" />, so these tests invoke the
///     runner directly, exactly as the scheduler does.
/// </summary>
[Collection("MongoDB")]
public class LotteryRoundRunnerTests : MongoDbTest
{
	private const long BasePot = 1_000_000;
	private const long Increment = 1_000_000;

	private static readonly DateTimeOffset DefaultNow = TestInstants.Utc("2026-11-10T19:00:00");

	private ILotteryGuessRepository _guesses = null!;
	private IExtraLotteryGuessRepository _extraGuesses = null!;
	private LotteryEntryRepository _entries = null!;
	private LotteryRoundRepository _rounds = null!;
	private LotteryWinnerRepository _winners = null!;
	private LotteryPotLedgerRepository _ledger = null!;
	private LotteryAwardRepository _awards = null!;
	private FlakyLotteryEntryRepository _flakyEntries = null!;

	private FakeTimeProvider _time = null!;
	private StubLotterySettingsService _settings = null!;
	private FakeLotteryNumberGenerator _numbers = null!;
	private FakeLotteryAnnouncementPublisher _publisher = null!;
	private FakeLotteryAlertService _alerts = null!;
	private Mock<IMemberService> _members = null!;
	private LotteryPotLedgerService _pot = null!;
	private LotteryRoundRunner _runner = null!;

	public LotteryRoundRunnerTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_guesses = new LotteryGuessRepository(mongoClient, databaseOptions);
		_extraGuesses = new ExtraLotteryGuessRepository(mongoClient, databaseOptions);
		_entries = new LotteryEntryRepository(mongoClient, databaseOptions);
		_rounds = new LotteryRoundRepository(mongoClient, databaseOptions);
		_winners = new LotteryWinnerRepository(mongoClient, databaseOptions);
		_ledger = new LotteryPotLedgerRepository(mongoClient, databaseOptions);
		_awards = new LotteryAwardRepository(mongoClient, databaseOptions);
		_flakyEntries = new FlakyLotteryEntryRepository(_entries);
	}

	protected override async Task OnAfterInitializeAsync()
	{
		// Production creates these through the index migration. The unique RoundKey and IdempotencyKey indexes are
		// load-bearing for idempotency, so the tests mirror the deployed state instead of racing without them.
		await _rounds.EnsureIndexesAsync();
		await _entries.EnsureIndexesAsync();
		await _winners.EnsureIndexesAsync();
		await _ledger.EnsureIndexesAsync();
		await _awards.EnsureIndexesAsync();

		_time = new FakeTimeProvider(DefaultNow);
		_settings = new StubLotterySettingsService(TestLotterySettings.Create(basePot: BasePot,
			weeklyIncrement: Increment));
		_numbers = new FakeLotteryNumberGenerator();
		_publisher = new FakeLotteryAnnouncementPublisher();
		_alerts = new FakeLotteryAlertService();
		_members = new Mock<IMemberService>();
		_members.Setup(service => service.GetByDiscordIds(It.IsAny<IEnumerable<ulong>>()))
			.ReturnsAsync((IEnumerable<ulong> ids) => ids
				.Select(id => new Member { DiscordId = id.ToString(), DiscordName = $"Member {id}" })
				.ToList());

		BuildRunner();
	}

	private void BuildRunner(ILotteryEntryRepository? entries = null)
	{
		_pot = new LotteryPotLedgerService(_ledger, Mock.Of<ILogger<LotteryPotLedgerService>>());
		_runner = new LotteryRoundRunner(_settings, _rounds, entries ?? _flakyEntries, _winners, _pot, _guesses,
			_extraGuesses, _members.Object, _numbers, _publisher, _time, Mock.Of<ILogger<LotteryRoundRunner>>(),
			alerts: _alerts);
	}

	#region Scheduled round

	[Fact]
	public async Task TickAsync_OnTheRunDayWithAWinner_DrawsSplitsAndCompletes()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		var winnerId = 7001UL;
		var otherId = 7002UL;
		await SeedGuessAsync(winnerId, 50);
		await SeedGuessAsync(otherId, 17);
		_numbers.DefaultNumber = 50;

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.Ran);

		var round = await ReadRoundAsync("2026-11-10");
		round.ShouldNotBeNull();
		round!.Status.ShouldBe(LotteryRoundStatus.Completed);
		round.WinningNumber.ShouldBe(50);
		round.PotBefore.ShouldBe(BasePot);
		round.PotAfter.ShouldBe(BasePot);
		round.RunAt.ShouldNotBeNull();

		// The guess set is frozen into lottery_entries before the number is chosen.
		var entries = await _entries.GetByRoundKeyAsync("2026-11-10");
		entries.Count.ShouldBe(2);

		var winner = (await _winners.GetByRoundKeyAsync("2026-11-10")).ShouldHaveSingleItem();
		winner.DiscordId.ShouldBe(winnerId);
		winner.DisplayName.ShouldBe("Member 7001");
		winner.Guess.ShouldBe(50);
		winner.GilPrize.ShouldBe(BasePot);
		winner.PayoutStatus.ShouldBe(LotteryPayoutStatus.Pending);
		winner.BonusItems.ShouldBeEmpty();

		// A win: the prize is debited and the pot returns to the base.
		await AssertLedgerAsync("2026-11-10", -BasePot, [LotteryPotLedgerType.Payout, LotteryPotLedgerType.Reset]);
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot);

		// The live guesses are cleared only once the round is completed.
		(await _guesses.GetAsync()).ShouldBeEmpty();

		_publisher.Published.ShouldHaveSingleItem();
		_publisher.Published[0].DryRun.ShouldBeFalse();
		_publisher.Published[0].Winners.ShouldHaveSingleItem();
		_publisher.Published[0].Round.WinningNumber.ShouldBe(50);
	}

	[Fact]
	public async Task TickAsync_WithoutAWinner_GrowsThePotByTheIncrement()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 17);
		_numbers.DefaultNumber = 50;

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.Ran);

		var round = await ReadRoundAsync("2026-11-10");
		round!.Status.ShouldBe(LotteryRoundStatus.Completed);
		round.PotBefore.ShouldBe(BasePot);
		round.PotAfter.ShouldBe(BasePot + Increment);
		(await _winners.GetByRoundKeyAsync("2026-11-10")).ShouldBeEmpty();

		await AssertLedgerAsync("2026-11-10", Increment, [LotteryPotLedgerType.WeeklyIncrement]);
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot + Increment);

		// No winner still clears the guesses: the round completed.
		(await _guesses.GetAsync()).ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WithZeroGuesses_StillDrawsAndNoOneWins()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		_numbers.DefaultNumber = 42;

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.Ran);
		var round = await ReadRoundAsync("2026-11-10");
		round!.WinningNumber.ShouldBe(42);
		round.Status.ShouldBe(LotteryRoundStatus.Completed);
		(await _winners.GetByRoundKeyAsync("2026-11-10")).ShouldBeEmpty();
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot + Increment);
	}

	[Fact]
	public async Task TickAsync_ASecondTickForTheSameSlot_DoesNotDrawAgain()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_numbers.DefaultNumber = 50;

		var first = await _runner.TickAsync();
		var second = await _runner.TickAsync();

		first.Action.ShouldBe(LotteryRunDisposition.Ran);
		second.Action.ShouldBe(LotteryTickAction.Nothing);
		_numbers.Draws.ShouldBe(1);
		_publisher.Published.Count.ShouldBe(1);

		var rounds = await _rounds.GetLatestAsync(10);
		rounds.Count(round => round.RoundKey == "2026-11-10").ShouldBe(1);
	}

	[Fact]
	public async Task TickAsync_WithAnOddPotAndTwoWinners_GivesTheRemainderToTheEarliestEntry()
	{
		await SeedCompletedRoundAsync("2026-11-03");

		// A top-up makes the pot 1,000,001 Gil: it does not divide evenly between two winners.
		await _pot.EnsureBalanceAsync(BasePot);
		await _pot.AppendAsync(null, LotteryPotLedgerType.Correction, 1, BasePot + 1, "test top-up", null,
			"test:odd-pot");

		var earlyId = 7100UL;
		var lateId = 7101UL;
		await SeedGuessAsync(lateId, 50, "2026-11-01T12:00:00");
		await SeedGuessAsync(earlyId, 50, "2026-11-01T10:00:00");
		_numbers.DefaultNumber = 50;

		await _runner.TickAsync();

		var winners = await _winners.GetByRoundKeyAsync("2026-11-10");
		winners.Count.ShouldBe(2);
		winners.Single(winner => winner.DiscordId == earlyId).GilPrize.ShouldBe(500_001);
		winners.Single(winner => winner.DiscordId == lateId).GilPrize.ShouldBe(500_000);
		winners.Sum(winner => winner.GilPrize).ShouldBe(BasePot + 1);

		// The payout debits exactly the pot at the draw, then the reset returns the base.
		var payout = await _ledger.GetByIdempotencyKeyAsync("2026-11-10:payout");
		payout!.Amount.ShouldBe(-(BasePot + 1));
		var reset = await _ledger.GetByIdempotencyKeyAsync("2026-11-10:reset");
		reset!.Amount.ShouldBe(BasePot);

		var round = await ReadRoundAsync("2026-11-10");
		round!.PotAfter.ShouldBe(BasePot);
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot);
	}

	[Fact]
	public async Task TickAsync_AWinnerWhoLeftTheFc_StillWinsWithTheStoredName()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		var winnerId = 7200UL;
		await SeedGuessAsync(winnerId, 50);
		_numbers.DefaultNumber = 50;
		_members.Setup(service => service.GetByDiscordIds(It.IsAny<IEnumerable<ulong>>()))
			.ReturnsAsync((IEnumerable<ulong> ids) => ids
				.Select(id => new Member { DiscordId = id.ToString(), DiscordName = "Gone Member" })
				.ToList());

		await _runner.TickAsync();

		var winner = (await _winners.GetByRoundKeyAsync("2026-11-10")).ShouldHaveSingleItem();
		winner.DisplayName.ShouldBe("Gone Member");
		winner.PayoutStatus.ShouldBe(LotteryPayoutStatus.Pending);
	}

	#endregion

	#region Manual run and claims

	[Fact]
	public async Task RunNowAsync_WhenTheDueRoundAlreadyRan_ReturnsAlreadyRun()
	{
		await SeedCompletedRoundAsync("2026-11-10");

		var result = await _runner.RunNowAsync(9001UL);

		result.Disposition.ShouldBe(LotteryRunDisposition.AlreadyRun);
		_numbers.Draws.ShouldBe(0);
		(await _rounds.GetAsync()).ShouldHaveSingleItem();
	}

	[Fact]
	public async Task RunNowAsync_WhenTheDueRoundWasNeverScheduled_RunsItWithTheAdminAsActor()
	{
		_numbers.DefaultNumber = 50;
		await SeedGuessAsync(7001UL, 50);

		var result = await _runner.RunNowAsync(9001UL);

		result.Disposition.ShouldBe(LotteryRunDisposition.Ran);
		var round = await ReadRoundAsync("2026-11-10");
		round!.Status.ShouldBe(LotteryRoundStatus.Completed);
		round.WinningNumber.ShouldBe(50);
	}

	[Fact]
	public async Task RunNowAsync_TwiceInParallel_DrawsOnceAndRejectsTheSecondClaim()
	{
		_numbers.DefaultNumber = 50;
		await SeedGuessAsync(7001UL, 50);

		var results = await Task.WhenAll(
			_runner.RunNowAsync(9001UL),
			_runner.RunNowAsync(9002UL));

		results.Count(result => result.Disposition == LotteryRunDisposition.Ran).ShouldBe(1);
		results.Count(result => result.Disposition != LotteryRunDisposition.Ran).ShouldBe(1);
		_numbers.Draws.ShouldBe(1);
		(await _rounds.GetAsync()).ShouldHaveSingleItem();
		(await _winners.GetByRoundKeyAsync("2026-11-10")).ShouldHaveSingleItem();
	}

	[Fact]
	public async Task RunNowAsync_WhileAFailedRoundBlocks_ReturnsBlocked()
	{
		await CreateRoundAsync("2026-11-03", LotteryRoundStatus.Failed, LotteryFailedStep.Announcement, attempts: 7);

		var result = await _runner.RunNowAsync(9001UL);

		result.Disposition.ShouldBe(LotteryRunDisposition.Blocked);
		_numbers.Draws.ShouldBe(0);
	}

	[Fact]
	public async Task TickAsync_WhileARoundIsPending_BlocksTheNextScheduledRound()
	{
		await CreateRoundAsync("2026-11-03", LotteryRoundStatus.Failed, LotteryFailedStep.Announcement, attempts: 7);
		_time.Set(TestInstants.Utc("2026-11-10T19:05:00"));

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryTickAction.Blocked);
		(await _rounds.GetByRoundKeyAsync("2026-11-10")).ShouldBeNull();
	}

	[Fact]
	public async Task TickAsync_AStaleDrawingRound_ResumesWithoutDrawingASecondNumber()
	{
		// Simulates a crash after the number was stored: the round is still `drawing`, the entries are frozen.
		var round = await CreateRoundAsync("2026-11-10", LotteryRoundStatus.Drawing);
		round.WinningNumber = 50;
		round.RunAt = TestInstants.UtcTime("2026-11-10T19:00:00");
		round.PotBefore = BasePot;
		round.ClaimedAt = TestInstants.UtcTime("2026-11-09T19:00:00");
		await _rounds.UpsertAsync(round);

		await _entries.AddIfMissingAsync(new LotteryEntry { RoundKey = "2026-11-10", DiscordId = 7001UL, Number = 50 });

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.Ran);
		_numbers.Draws.ShouldBe(0);
		var resumed = await ReadRoundAsync("2026-11-10");
		resumed!.Status.ShouldBe(LotteryRoundStatus.Completed);
		resumed.WinningNumber.ShouldBe(50);
		(await _winners.GetByRoundKeyAsync("2026-11-10")).ShouldHaveSingleItem();
	}

	#endregion

	#region Catch-up, skip and restart

	[Fact]
	public async Task TickAsync_AfterThreeMissedWeeks_SkipsTheOlderDatesAndRunsTheMostRecentOnce()
	{
		await SeedCompletedRoundAsync("2026-10-13");
		_time.Set(TestInstants.Utc("2026-11-05T12:00:00")); // Thursday after three missed Tuesdays.
		_numbers.DefaultNumber = 50;

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.Ran);

		var skipped = (await _rounds.GetByStatusAsync(LotteryRoundStatus.Skipped))
			.Select(round => round.RoundKey).OrderBy(key => key).ToList();
		skipped.ShouldBe(["2026-10-20", "2026-10-27"]);

		var ran = await ReadRoundAsync("2026-11-03");
		ran!.Status.ShouldBe(LotteryRoundStatus.Completed);

		// Skipped dates never grow the pot: only the one completed no-win draw did.
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot + Increment);
		var increments = (await _ledger.GetByRoundKeyAsync("2026-11-03"))
			.Count(entry => entry.Type == LotteryPotLedgerType.WeeklyIncrement);
		increments.ShouldBe(1);
	}

	[Fact]
	public async Task TickAsync_WithNoRoundsMidWeek_WaitsForTheNextSlot()
	{
		_time.Set(TestInstants.Utc("2026-11-05T12:00:00"));

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryTickAction.Nothing);
		(await _rounds.GetAsync()).ShouldBeEmpty();
		(await _ledger.GetLatestAsync(10)).ShouldBeEmpty();
	}

	[Fact]
	public async Task TickAsync_WithNoRoundsLaterOnTheRunDay_RunsImmediately()
	{
		_time.Set(TestInstants.Utc("2026-11-10T20:30:00"));
		_numbers.DefaultNumber = 50;

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.Ran);
		(await ReadRoundAsync("2026-11-10"))!.Status.ShouldBe(LotteryRoundStatus.Completed);
	}

	[Fact]
	public async Task SkipAsync_DoesNotDrawDoesNotTouchThePotAndCarriesTheGuesses()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		await _extraGuesses.CreateAsync(new ExtraLotteryGuess { DiscordId = 7001UL, Reason = "event" });
		_time.Set(TestInstants.Utc("2026-11-09T12:00:00")); // Monday before the next Tuesday.

		var result = await _runner.SkipAsync(9001UL, "holiday");

		result.Disposition.ShouldBe(LotteryRunDisposition.Skipped);

		var skipped = await ReadRoundAsync("2026-11-10");
		skipped!.Status.ShouldBe(LotteryRoundStatus.Skipped);
		skipped.WinningNumber.ShouldBeNull();

		// Guesses carry; the weekly allowance is not reset because nothing is flushed.
		(await _guesses.GetAsync()).ShouldHaveSingleItem();
		(await _extraGuesses.GetAsync()).ShouldHaveSingleItem();

		// No draw, no pot movement, and not even the opening balance is written.
		(await _ledger.GetLatestAsync(10)).ShouldBeEmpty();
		(await _winners.GetAsync()).ShouldBeEmpty();
		_numbers.Draws.ShouldBe(0);
	}

	[Fact]
	public async Task TickAsync_AfterASkip_DoesNotRunTheSkippedDate()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		_time.Set(TestInstants.Utc("2026-11-09T12:00:00"));
		await _runner.SkipAsync(9001UL);

		_time.Set(TestInstants.Utc("2026-11-10T19:05:00"));
		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryTickAction.Nothing);
		_numbers.Draws.ShouldBe(0);
	}

	#endregion

	#region Failures and retries

	[Fact]
	public async Task TickAsync_DrawFailure_StaysDrawingWithTheDocumentedBackoffAndNeverRedraws()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_numbers.DefaultNumber = 50;
		_flakyEntries.FailuresRemaining = 1;

		var failed = await _runner.TickAsync();

		failed.Action.ShouldBe(LotteryRunDisposition.Retrying);
		var round = await ReadRoundAsync("2026-11-10");
		round!.Status.ShouldBe(LotteryRoundStatus.Drawing);
		round.FailedStep.ShouldBe(LotteryFailedStep.Draw);
		round.Attempts.ShouldBe(1);
		round.NextRetryAt.ShouldBe(TestInstants.UtcTime("2026-11-10T19:00:30"));
		round.LastError.ShouldNotBeNull();
		round.WinningNumber.ShouldBeNull();

		var waiting = await _runner.TickAsync();
		waiting.Action.ShouldBe(LotteryTickAction.WaitingForRetry);

		_time.Advance(TimeSpan.FromSeconds(31));
		var resumed = await _runner.TickAsync();

		resumed.Action.ShouldBe(LotteryRunDisposition.Ran);
		(await ReadRoundAsync("2026-11-10"))!.Status.ShouldBe(LotteryRoundStatus.Completed);
		_numbers.Draws.ShouldBe(1);
	}

	[Fact]
	public async Task TickAsync_PublisherFailure_StaysDrawnAndRetriesTheSameResultWithoutRedrawing()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_numbers.DefaultNumber = 50;
		_publisher.PublishFailure = new InvalidOperationException("Discord is down");

		var failed = await _runner.TickAsync();

		failed.Action.ShouldBe(LotteryRunDisposition.Drawn);
		var round = await ReadRoundAsync("2026-11-10");
		round!.Status.ShouldBe(LotteryRoundStatus.Drawn);
		round.FailedStep.ShouldBe(LotteryFailedStep.Announcement);
		round.Attempts.ShouldBe(1);
		round.WinningNumber.ShouldBe(50);
		round.PotAfter.ShouldBe(BasePot);
		round.PostingStartedAt.ShouldNotBeNull();
		round.NextRetryAt.ShouldNotBeNull();

		// A scheduled retry is not a needs-admin state: it must not ping the admins.
		_alerts.Alerts.ShouldBeEmpty();

		// The same result is retried: no second number, no second winner, no second ledger movement.
		_publisher.PublishFailure = null;
		_time.Set(round.NextRetryAt!.Value);
		var resumed = await _runner.TickAsync();

		resumed.Action.ShouldBe(LotteryRunDisposition.Ran);
		_numbers.Draws.ShouldBe(1);
		(await _winners.GetByRoundKeyAsync("2026-11-10")).ShouldHaveSingleItem();
		(await _ledger.GetByRoundKeyAsync("2026-11-10")).Count(entry =>
			entry.Type == LotteryPotLedgerType.Payout).ShouldBe(1);
		_publisher.Published.ShouldHaveSingleItem();
		(await ReadRoundAsync("2026-11-10"))!.Status.ShouldBe(LotteryRoundStatus.Completed);
	}

	[Fact]
	public async Task TickAsync_AnnouncementThatCannotBePostedWithinTheWindow_FailsWithoutRedrawing()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_numbers.DefaultNumber = 50;
		_publisher.PublishFailure = new InvalidOperationException("Discord is down");

		await _runner.TickAsync();

		// Two hours later the one-hour window has closed; the automatic retry is terminal.
		_time.Advance(TimeSpan.FromHours(2));
		var failed = await _runner.TickAsync();

		failed.Action.ShouldBe(LotteryRunDisposition.Failed);
		var round = await ReadRoundAsync("2026-11-10");
		round!.Status.ShouldBe(LotteryRoundStatus.Failed);
		round.FailedStep.ShouldBe(LotteryFailedStep.Announcement);
		round.WinningNumber.ShouldBe(50);
		_numbers.Draws.ShouldBe(1);

		// Exactly one needs-admin alert for the terminal state, carrying what an admin needs to act on.
		var alert = _alerts.Alerts.ShouldHaveSingleItem();
		alert.RoundKey.ShouldBe("2026-11-10");
		alert.FailedStep.ShouldBe(LotteryFailedStep.Announcement);
		alert.Error.ShouldNotBeNullOrWhiteSpace();
		alert.Attempts.ShouldBeGreaterThan(0);
		alert.NextRetryAt.ShouldBeNull();
		alert.NeedsAdmin.ShouldBeTrue();

		// The retry inside the window never pinged.
		_alerts.Alerts.Count.ShouldBe(1);

		// And a failed round blocks the next scheduled date.
		_time.Set(TestInstants.Utc("2026-11-17T19:05:00"));
		var blocked = await _runner.TickAsync();
		blocked.Action.ShouldBe(LotteryTickAction.Blocked);
	}

	[Fact]
	public async Task RetryAsync_AfterATerminalAnnouncementFailure_ReArmsTheWindowAndPostsTheSameResult()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_numbers.DefaultNumber = 50;
		_publisher.PublishFailure = new InvalidOperationException("Discord is down");
		await _runner.TickAsync();
		_time.Advance(TimeSpan.FromHours(2));
		await _runner.TickAsync();

		_publisher.PublishFailure = null;
		var retried = await _runner.RetryAsync(9001UL);

		retried.Disposition.ShouldBe(LotteryRunDisposition.Ran);
		_numbers.Draws.ShouldBe(1);
		(await ReadRoundAsync("2026-11-10"))!.Status.ShouldBe(LotteryRoundStatus.Completed);
	}

	[Fact]
	public async Task TickAsync_ExhaustedDrawRetries_PostsOneNeedsAdminAlert()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_numbers.DefaultNumber = 50;
		_flakyEntries.FailuresRemaining = 6;

		for (var attempt = 1; attempt <= 5; attempt++)
		{
			var failing = await _runner.TickAsync();
			failing.Action.ShouldBe(LotteryRunDisposition.Retrying);

			var pending = await ReadRoundAsync("2026-11-10");
			pending!.NextRetryAt.ShouldNotBeNull();
			_time.Set(pending.NextRetryAt!.Value);
		}

		var terminal = await _runner.TickAsync();

		terminal.Action.ShouldBe(LotteryRunDisposition.Failed);
		(await ReadRoundAsync("2026-11-10"))!.Status.ShouldBe(LotteryRoundStatus.Failed);

		// One alert, at the end of the backoff, and it pings because the round is terminal.
		var alert = _alerts.Alerts.ShouldHaveSingleItem();
		alert.RoundKey.ShouldBe("2026-11-10");
		alert.FailedStep.ShouldBe(LotteryFailedStep.Draw);
		alert.Attempts.ShouldBe(6);
		alert.NextRetryAt.ShouldBeNull();
		alert.NeedsAdmin.ShouldBeTrue();
	}

	[Fact]
	public async Task TickAsync_WithAPostingMarkerAndAnExistingPost_AdoptsItInsteadOfPostingAgain()
	{
		var round = await CreateRoundAsync("2026-11-10", LotteryRoundStatus.Drawn);
		round.WinningNumber = 50;
		round.RunAt = TestInstants.UtcTime("2026-11-10T19:00:00");
		round.PotBefore = BasePot;
		round.PotAfter = BasePot;
		round.PostingStartedAt = TestInstants.UtcTime("2026-11-10T19:00:10");
		await _rounds.UpsertAsync(round);
		_publisher.ExistingPostId = "existing-message";

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.Ran);
		_publisher.Published.ShouldBeEmpty();
		var completed = await ReadRoundAsync("2026-11-10");
		completed!.Status.ShouldBe(LotteryRoundStatus.Completed);
		completed.AnnouncementMessageId.ShouldBe("existing-message");
	}

	#endregion

	#region Flags and dry run

	[Fact]
	public async Task TickAsync_WhenAutomationIsDisabled_DoesNothing()
	{
		_settings.Settings = TestLotterySettings.Create(automationEnabled: false);

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryTickAction.Disabled);
		(await _rounds.GetAsync()).ShouldBeEmpty();
		(await _ledger.GetLatestAsync(10)).ShouldBeEmpty();
		_numbers.Draws.ShouldBe(0);
	}

	[Fact]
	public async Task TickAsync_WhenPaused_DoesNothing()
	{
		_settings.Settings = TestLotterySettings.Create(pauseAutomation: true);

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryTickAction.Paused);
		(await _rounds.GetAsync()).ShouldBeEmpty();
		_numbers.Draws.ShouldBe(0);
	}

	[Fact]
	public async Task TickAsync_DryRun_PublishesAPreviewAndWritesNothing()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_settings.Settings = TestLotterySettings.Create(dryRun: true);
		_numbers.DefaultNumber = 50;

		var result = await _runner.TickAsync();

		result.Action.ShouldBe(LotteryRunDisposition.DryRun);
		_publisher.Published.ShouldHaveSingleItem();
		_publisher.Published[0].DryRun.ShouldBeTrue();

		// criterion 20: no round, entries, winners, ledger or flush.
		(await _rounds.GetByRoundKeyAsync("2026-11-10")).ShouldBeNull();
		(await _ledger.GetLatestAsync(10)).ShouldBeEmpty();
		(await _winners.GetAsync()).ShouldBeEmpty();
		(await _guesses.GetAsync()).ShouldHaveSingleItem();

		// The minute-by-minute tick does not post the same preview again.
		var again = await _runner.TickAsync();
		again.Action.ShouldBe(LotteryRunDisposition.DryRun);
		_publisher.Published.Count.ShouldBe(1);
	}

	#endregion

	#region Void and reschedule

	[Fact]
	public async Task VoidAsync_ReversesTheLedgerAndBlocksASilentRedraw()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_numbers.DefaultNumber = 50;
		await _runner.TickAsync();

		var result = await _runner.VoidAsync("2026-11-10", 9001UL, "wrong number drawn");

		result.Disposition.ShouldBe(LotteryRunDisposition.Voided);

		var voided = await ReadRoundAsync("2026-11-10");
		voided!.Status.ShouldBe(LotteryRoundStatus.Voided);
		voided.VoidReason.ShouldBe("wrong number drawn");
		voided.VoidedByDiscordId.ShouldBe(9001UL);
		voided.VoidedAt.ShouldNotBeNull();

		// The payout is credited back and the reset is debited again, so the pot is the pre-draw pot.
		var reversal = await _ledger.GetByIdempotencyKeyAsync("2026-11-10:void:payout");
		reversal!.Amount.ShouldBe(BasePot);
		reversal.Type.ShouldBe(LotteryPotLedgerType.Correction);
		var resetReversal = await _ledger.GetByIdempotencyKeyAsync("2026-11-10:void:reset");
		resetReversal!.Amount.ShouldBe(-BasePot);
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot);

		_publisher.VoidAnnotations.ShouldHaveSingleItem();

		// Never silently re-draw the voided date, even when the next tick happens.
		var tick = await _runner.TickAsync();
		tick.Action.ShouldBe(LotteryTickAction.Nothing);
		_numbers.Draws.ShouldBe(1);
	}

	[Fact]
	public async Task VoidAsync_ANoWinRound_TakesTheIncrementBackWithoutGoingBelowTheBase()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		_numbers.DefaultNumber = 50;
		await _runner.TickAsync();
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot + Increment);

		await _runner.VoidAsync("2026-11-10", 9001UL, "test");

		var reversal = await _ledger.GetByIdempotencyKeyAsync("2026-11-10:void:weekly_increment");
		reversal!.Amount.ShouldBe(-Increment);
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot);
	}

	[Fact]
	public async Task VoidAsync_WhenThePotHasMovedOn_ClampsAtTheBasePot()
	{
		// The no-win round grew the pot to 2M; the next winning round spent it back down to the base.
		await SeedCompletedRoundAsync("2026-10-27");
		_numbers.DefaultNumber = 50;
		_time.Set(TestInstants.Utc("2026-11-03T19:05:00"));
		await _runner.TickAsync();
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot + Increment);

		await SeedGuessAsync(7001UL, 50);
		_time.Set(TestInstants.Utc("2026-11-10T19:05:00"));
		await _runner.TickAsync();
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot);

		// Reversing the +increment cannot take the pot below the base, so it is applied as zero.
		await _runner.VoidAsync("2026-11-03", 9001UL, "late correction");

		var reversal = await _ledger.GetByIdempotencyKeyAsync("2026-11-03:void:weekly_increment");
		reversal!.Amount.ShouldBe(0);
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(BasePot);
	}

	[Fact]
	public async Task RescheduleAsync_MovesTheRoundKeyAndKeepsTheGuesses()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedGuessAsync(7001UL, 50);
		_time.Set(TestInstants.Utc("2026-11-05T12:00:00"));

		var result = await _runner.RescheduleAsync(TestInstants.UtcTime("2026-11-12T18:00:00"), 9001UL);

		result.Disposition.ShouldBe(LotteryRunDisposition.Rescheduled);
		(await _rounds.GetByRoundKeyAsync("2026-11-10")).ShouldBeNull();
		var moved = await _rounds.GetByRoundKeyAsync("2026-11-12");
		moved.ShouldNotBeNull();
		moved!.ScheduledAt.ShouldBe(TestInstants.UtcTime("2026-11-12T18:00:00"));
		(await _guesses.GetAsync()).ShouldHaveSingleItem();

		// The moved round runs at its new instant.
		_time.Set(TestInstants.Utc("2026-11-12T18:05:00"));
		_numbers.DefaultNumber = 50;
		var tick = await _runner.TickAsync();
		tick.Action.ShouldBe(LotteryRunDisposition.Ran);
		(await ReadRoundAsync("2026-11-12"))!.Status.ShouldBe(LotteryRoundStatus.Completed);
	}

	#endregion

	#region Status

	[Fact]
	public async Task GetStatusAsync_ReportsThePotTheNextRunAndThePendingRound()
	{
		await SeedCompletedRoundAsync("2026-11-03");
		await SeedPotAsync();

		var status = await _runner.GetStatusAsync();

		status.Pot.ShouldBe(BasePot + Increment);
		status.LatestRound!.RoundKey.ShouldBe("2026-11-03");
		status.PendingRound.ShouldBeNull();
		status.NextRunAtUtc.ShouldBe(TestInstants.UtcTime("2026-11-17T19:00:00"));
		status.NextRoundKey.ShouldBe("2026-11-17");
		status.AutomationStatus.ShouldBe(LotteryAutomationStatus.LiveKind);
		status.IsDegraded.ShouldBeFalse();
	}

	#endregion

	#region Helpers

	private async Task SeedCompletedRoundAsync(string roundKey)
	{
		await CreateRoundAsync(roundKey, LotteryRoundStatus.Completed);
	}

	private async Task<LotteryRound> CreateRoundAsync(string roundKey, string status, string? failedStep = null,
		int attempts = 0)
	{
		var round = new LotteryRound
		{
			RoundKey = roundKey,
			ScheduledAt = TestInstants.UtcTime($"{roundKey}T19:00:00"),
			Status = status,
			FailedStep = failedStep,
			Attempts = attempts,
			RunAt = status == LotteryRoundStatus.Completed
				? TestInstants.UtcTime($"{roundKey}T19:00:00")
				: null
		};

		await _rounds.CreateAsync(round);
		return round;
	}

	private async Task SeedGuessAsync(ulong discordId, int number, string createdAtUtc = "2026-11-01T10:00:00")
	{
		// Upsert preserves DateCreated, which the draw uses to order winners for the rounding remainder.
		await _guesses.UpsertAsync(new LotteryGuess
		{
			DiscordId = discordId,
			Number = number,
			DateCreated = TestInstants.UtcTime(createdAtUtc)
		});
	}

	private async Task<LotteryRound?> ReadRoundAsync(string roundKey)
	{
		return await _rounds.GetByRoundKeyAsync(roundKey);
	}

	private async Task AssertLedgerAsync(string roundKey, long expectedFirstAmount, string[] expectedTypes)
	{
		var entries = await _ledger.GetByRoundKeyAsync(roundKey);
		entries.Select(entry => entry.Type).ShouldBe(expectedTypes);
		entries[0].Amount.ShouldBe(expectedFirstAmount);
		entries.ShouldAllBe(entry => entry.IdempotencyKey.StartsWith(roundKey));
	}

	private async Task SeedPotAsync()
	{
		await _pot.EnsureBalanceAsync(BasePot);
		await _pot.AppendAsync(null, LotteryPotLedgerType.WeeklyIncrement, Increment, BasePot + Increment,
			"seed", null, "test:seed-increment");
	}

	#endregion
}
