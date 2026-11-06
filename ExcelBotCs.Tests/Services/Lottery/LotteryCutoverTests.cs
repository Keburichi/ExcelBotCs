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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The §12 cutover sequence on a scratch database, end to end (criteria 21 and 23): create the collections and
///     indexes, insert the manual history and the opening pot, run the one-time <c>lottery_results</c> backfill twice,
///     and only then let the first automated round run. Nothing is duplicated, the archive is untouched and the
///     history that was inserted by hand survives.
/// </summary>
[Collection("MongoDB")]
public class LotteryCutoverTests : MongoDbTest
{
	private const string DatabaseName = "TestDatabase";
	private const long BasePot = 1_000_000;
	private const long OpeningPot = 21_000_000;

	private static readonly DateTimeOffset FirstAutomatedRun = TestInstants.Utc("2026-11-10T19:00:00");

	private IMongoClient _mongoClient = null!;

	private ILotteryResultRepository _results = null!;
	private LotteryRoundRepository _rounds = null!;
	private LotteryEntryRepository _entries = null!;
	private LotteryWinnerRepository _winners = null!;
	private LotteryPotLedgerRepository _ledger = null!;
	private ILotteryGuessRepository _guesses = null!;
	private IExtraLotteryGuessRepository _extraGuesses = null!;

	private LotteryIndexInitializer _indexes = null!;
	private LotteryArchiveBackfillService _backfill = null!;
	private LotteryPotLedgerService _pot = null!;
	private LotteryRoundRunner _runner = null!;

	public LotteryCutoverTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_mongoClient = mongoClient;
		_results = new LotteryResultRepository(mongoClient, databaseOptions);
		_rounds = new LotteryRoundRepository(mongoClient, databaseOptions);
		_entries = new LotteryEntryRepository(mongoClient, databaseOptions);
		_winners = new LotteryWinnerRepository(mongoClient, databaseOptions);
		_ledger = new LotteryPotLedgerRepository(mongoClient, databaseOptions);
		_guesses = new LotteryGuessRepository(mongoClient, databaseOptions);
		_extraGuesses = new ExtraLotteryGuessRepository(mongoClient, databaseOptions);

		_indexes = new LotteryIndexInitializer(_rounds, _entries, _winners, _ledger,
			new LotteryReminderOptinRepository(mongoClient, databaseOptions),
			new LotteryAwardRepository(mongoClient, databaseOptions),
			new LotteryPrizePoolRepository(mongoClient, databaseOptions),
			new LotterySettingsRepository(mongoClient, databaseOptions),
			new LotterySettingsHistoryRepository(mongoClient, databaseOptions),
			NullLogger<LotteryIndexInitializer>.Instance);

		_backfill = new LotteryArchiveBackfillService(_results, _rounds, _entries,
			NullLogger<LotteryArchiveBackfillService>.Instance);

		_pot = new LotteryPotLedgerService(_ledger, NullLogger<LotteryPotLedgerService>.Instance);
	}

	[Fact]
	public async Task Cutover_CreatesIndexesSeedsHistoryBackfillsTwiceAndRunsTheFirstAutomatedRoundOnce()
	{
		// 1–2. Deploy disabled, create the collections and indexes.
		await _indexes.EnsureIndexesAsync();
		await AssertIndexesAsync();

		// 3. The manual seed: history and exactly one opening-pot entry.
		await SeedHistoricalHistoryAsync();
		await _pot.AppendAsync(null, LotteryPotLedgerType.Correction, OpeningPot, OpeningPot,
			"opening balance from manual history", null, "opening-balance");
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(OpeningPot);

		// 4. The legacy archive, then the one-time backfill, twice.
		await InsertArchiveResultAsync(new DateTime(2026, 10, 20, 18, 0, 0, DateTimeKind.Utc), 12, 9001UL, 33UL);
		await InsertArchiveResultAsync(new DateTime(2026, 10, 27, 18, 0, 0, DateTimeKind.Utc), 7, 9002UL);
		await InsertArchiveResultAsync(new DateTime(2026, 11, 3, 18, 0, 0, DateTimeKind.Utc), 56, 9003UL, 12UL);
		var archiveBefore = await ReadSnapshotAsync("lottery_results");

		var first = await _backfill.BackfillAsync();
		var stateAfterFirst = await ReadSnapshotAsync("lottery_rounds");

		var second = await _backfill.BackfillAsync();

		first.ResultsProcessed.ShouldBe(3);
		first.RoundsCreated.ShouldBe(3);
		first.WinnersCreated.ShouldBe(0);
		second.RoundsCreated.ShouldBe(0);
		second.WinnersCreated.ShouldBe(0);
		(await ReadSnapshotAsync("lottery_rounds")).ShouldBe(stateAfterFirst);
		(await ReadSnapshotAsync("lottery_results")).ShouldBe(archiveBefore);

		// 23. The archive and the new collections coexist; the archive dates became rounds and nothing was duplicated.
		var backfilled = await _rounds.GetAsync();
		backfilled.Select(round => round.RoundKey).OrderBy(key => key, StringComparer.Ordinal)
			.ShouldBe(["2026-10-20", "2026-10-27", "2026-11-03"]);
		backfilled.ShouldAllBe(round => round.Status == LotteryRoundStatus.Completed);
		backfilled.Count.ShouldBe(3);
		(await _entries.GetByRoundKeyAsync("2026-10-20")).Count.ShouldBe(2);

		// 5–7. The schedule is live now; the first automated round is the next scheduled date.
		_runner = BuildRunner();

		var tick = await _runner.TickAsync();

		tick.Action.ShouldBe(LotteryRunDisposition.Ran);

		var rounds = await _rounds.GetAsync();
		rounds.Count.ShouldBe(4);
		rounds.Select(round => round.RoundKey).Distinct(StringComparer.Ordinal).Count().ShouldBe(4);
		(await _rounds.GetByRoundKeyAsync("2026-11-10"))!.Status.ShouldBe(LotteryRoundStatus.Completed);

		// The manual seed is untouched and the first round moved the pot from the seeded balance.
		var winners = await _winners.GetAsync();
		winners.Count(winner => winner.DisplayName is "Keburichi" or "Siegward").ShouldBe(2);
		winners.ShouldAllBe(winner => winner.PayoutNote == "historical record");
		(await _ledger.GetByIdempotencyKeyAsync("opening-balance")).ShouldNotBeNull();

		// No winner guessed correctly in the first round, so it grew the pot by one increment.
		(await _pot.GetBalanceAsync(BasePot)).ShouldBe(OpeningPot + 1_000_000);
		(await _ledger.GetByIdempotencyKeyAsync("2026-11-10:weekly_increment")).ShouldNotBeNull();

		// A second tick does not draw a second round for the same date.
		await _runner.TickAsync();
		(await _rounds.GetAsync()).Count.ShouldBe(4);
	}

	private LotteryRoundRunner BuildRunner()
	{
		var time = new FakeTimeProvider(FirstAutomatedRun);
		var settings = new StubLotterySettingsService(
			TestLotterySettings.Create(basePot: BasePot, weeklyIncrement: 1_000_000));

		var numbers = new FakeLotteryNumberGenerator { DefaultNumber = 99 };
		var members = new Mock<IMemberService>();
		members.Setup(service => service.GetByDiscordIds(It.IsAny<IEnumerable<ulong>>()))
			.ReturnsAsync((IEnumerable<ulong> ids) => ids
				.Select(id => new Member { DiscordId = id.ToString(), DiscordName = $"Member {id}" })
				.ToList());

		return new LotteryRoundRunner(settings, _rounds, _entries, _winners, _pot, _guesses, _extraGuesses,
			members.Object, numbers, new FakeLotteryAnnouncementPublisher(), time,
			Mock.Of<ILogger<LotteryRoundRunner>>(), TestStartupCheck.Healthy(),
			new FakeLotteryAlertService());
	}

	/// <summary>The 35-row seed reduced to the shapes the cutover can break: no Discord id, paid, one with an item.</summary>
	private async Task SeedHistoricalHistoryAsync()
	{
		await _winners.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-07-21",
			DiscordId = null,
			DisplayName = "Siegward",
			Guess = 56,
			GilPrize = 80_000_000,
			BonusItems = ["Chaotic Mount"],
			PayoutStatus = LotteryPayoutStatus.Paid,
			PayoutNote = "historical record",
			DateCreated = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc)
		});

		await _winners.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-03-17",
			DiscordId = null,
			DisplayName = "Keburichi",
			Guess = 92,
			GilPrize = 33_000_000,
			BonusItems = [],
			PayoutStatus = LotteryPayoutStatus.Paid,
			PayoutNote = "historical record",
			DateCreated = new DateTime(2026, 3, 17, 18, 0, 0, DateTimeKind.Utc)
		});
	}

	private async Task AssertIndexesAsync()
	{
		var database = _mongoClient.GetDatabase(DatabaseName);

		// The unique keys are load-bearing for idempotency; the cutover must have created them.
		await AssertUniqueIndexAsync(database, "lottery_rounds", "RoundKey");
		await AssertUniqueIndexAsync(database, "lottery_entries", "RoundKey", "DiscordId", "Number");
		await AssertUniqueIndexAsync(database, "lottery_pot_ledger", "IdempotencyKey");

		var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
		collections.ShouldContain("lottery_settings");
		collections.ShouldContain("lottery_settings_history");
		collections.ShouldContain("lottery_reminder_optins");
		collections.ShouldContain("lottery_prize_pool");
	}

	private static async Task AssertUniqueIndexAsync(IMongoDatabase database, string collectionName,
		params string[] keys)
	{
		var indexes = await database.GetCollection<BsonDocument>(collectionName).Indexes.List().ToListAsync();
		var match = indexes.FirstOrDefault(index =>
		{
			var unique = index.TryGetValue("unique", out var value) && value.ToBoolean();
			if (!unique) return false;

			var indexKeys = index["key"].AsBsonDocument.Names.OrderBy(name => name, StringComparer.Ordinal);
			return indexKeys.SequenceEqual(keys.OrderBy(name => name, StringComparer.Ordinal));
		});

		match.ShouldNotBeNull(
			$"{collectionName} must have a unique index over {string.Join(", ", keys)}");
	}

	private async Task InsertArchiveResultAsync(DateTime createdAtUtc, int winningNumber,
		params ulong[] guessers)
	{
		var archive = _mongoClient.GetDatabase(DatabaseName).GetCollection<LotteryResult>("lottery_results");
		await archive.InsertOneAsync(new LotteryResult
		{
			DateCreated = createdAtUtc,
			DateModified = createdAtUtc,
			WinningNumber = winningNumber,
			Guesses = guessers
				.Select(discordId => new LotteryGuess { DiscordId = discordId, Number = 1 })
				.ToList()
		});
	}

	private async Task<string> ReadSnapshotAsync(string collectionName)
	{
		var collection = _mongoClient.GetDatabase(DatabaseName).GetCollection<BsonDocument>(collectionName);
		var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty)
			.Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
			.ToListAsync();

		return string.Join("\n", documents.Select(document => document.ToJson()));
	}
}
