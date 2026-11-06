using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The one-time, idempotent <c>lottery_results</c> backfill (spec §6 "Backfill"): rounds and entries only, never
///     winners, and the archive stays read-only.
/// </summary>
[Collection("MongoDB")]
public class LotteryArchiveBackfillServiceTests : MongoDbTest
{
	private const string DatabaseName = "TestDatabase";

	private IMongoClient _mongoClient = null!;
	private ILotteryResultRepository _results = null!;
	private ILotteryRoundRepository _rounds = null!;
	private ILotteryEntryRepository _entries = null!;
	private ILotteryWinnerRepository _winners = null!;
	private LotteryArchiveBackfillService _service = null!;

	public LotteryArchiveBackfillServiceTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_mongoClient = mongoClient;
		_results = new LotteryResultRepository(mongoClient, databaseOptions);
		_rounds = new LotteryRoundRepository(mongoClient, databaseOptions);
		_entries = new LotteryEntryRepository(mongoClient, databaseOptions);
		_winners = new LotteryWinnerRepository(mongoClient, databaseOptions);

		_service = new LotteryArchiveBackfillService(_results, _rounds, _entries,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<LotteryArchiveBackfillService>.Instance);
	}

	[Fact]
	public async Task BackfillAsync_ImportsCompletedRoundAndItsEntries()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());
		await InsertArchiveResultAsync(
			new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc), 56,
			(discordId, 56), (otherDiscordId, 12));

		var result = await _service.BackfillAsync();

		result.ResultsProcessed.ShouldBe(1);
		result.RoundsCreated.ShouldBe(1);
		result.EntriesCreated.ShouldBe(2);
		result.WinnersCreated.ShouldBe(0);

		var round = await _rounds.GetByRoundKeyAsync("2026-07-21");
		round.ShouldNotBeNull();
		round!.Status.ShouldBe(LotteryRoundStatus.Completed);
		round.WinningNumber.ShouldBe(56);
		round.RunAt.ShouldBe(new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc));
		round.PotBefore.ShouldBeNull();
		round.PotAfter.ShouldBeNull();
		round.AnnouncementMessageId.ShouldBeNull();

		var entries = await _entries.GetByRoundKeyAsync("2026-07-21");
		entries.Count.ShouldBe(2);
		entries.ShouldContain(e => e.DiscordId == discordId && e.Number == 56);
		entries.ShouldContain(e => e.DiscordId == otherDiscordId && e.Number == 12);
	}

	[Fact]
	public async Task BackfillAsync_UsesTheEuropeBerlinLocalDateAsRoundKey()
	{
		// 23:30 UTC on 21 July is 01:30 on 22 July in Europe/Berlin (CEST, UTC+2).
		await InsertArchiveResultAsync(new DateTime(2026, 7, 21, 23, 30, 0, DateTimeKind.Utc), 7,
			(ulong.Parse(GenerateRandomDiscordId()), 7));

		await _service.BackfillAsync();

		(await _rounds.GetByRoundKeyAsync("2026-07-22")).ShouldNotBeNull();
		(await _rounds.GetByRoundKeyAsync("2026-07-21")).ShouldBeNull();
		(await _entries.GetByRoundKeyAsync("2026-07-22")).Count.ShouldBe(1);
	}

	[Fact]
	public async Task BackfillAsync_CreatesOneRoundPerArchiveResult_AndKeepsResultsWithTheSameLocalDateTogether()
	{
		var first = ulong.Parse(GenerateRandomDiscordId());
		var second = ulong.Parse(GenerateRandomDiscordId());
		await InsertArchiveResultAsync(new DateTime(2026, 7, 14, 18, 0, 0, DateTimeKind.Utc), 31, (first, 31));
		await InsertArchiveResultAsync(new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc), 56, (second, 56));

		var result = await _service.BackfillAsync();

		result.ResultsProcessed.ShouldBe(2);
		result.RoundsCreated.ShouldBe(2);
		(await _rounds.GetByStatusAsync(LotteryRoundStatus.Completed)).Count.ShouldBe(2);
	}

	[Fact]
	public async Task BackfillAsync_NeverCreatesWinners()
	{
		await InsertArchiveResultAsync(new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc), 56,
			(ulong.Parse(GenerateRandomDiscordId()), 56));

		var result = await _service.BackfillAsync();

		result.WinnersCreated.ShouldBe(0);
		(await _winners.GetAsync()).ShouldBeEmpty();
	}

	[Fact]
	public async Task BackfillAsync_LeavesTheLotteryResultsArchiveUntouched()
	{
		await InsertArchiveResultAsync(new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc), 56,
			(ulong.Parse(GenerateRandomDiscordId()), 56));
		var before = await ReadArchiveSnapshotAsync();

		await _service.BackfillAsync();

		(await ReadArchiveSnapshotAsync()).ShouldBe(before);
	}

	[Fact]
	public async Task BackfillAsync_RunTwice_YieldsIdenticalData()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await InsertArchiveResultAsync(new DateTime(2026, 7, 14, 18, 0, 0, DateTimeKind.Utc), 31, (discordId, 31));
		await InsertArchiveResultAsync(new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc), 56, (discordId, 56));
		await _service.BackfillAsync();
		var roundsAfterFirstRun = await ReadSnapshotAsync("lottery_rounds");
		var entriesAfterFirstRun = await ReadSnapshotAsync("lottery_entries");

		var second = await _service.BackfillAsync();

		(await ReadSnapshotAsync("lottery_rounds")).ShouldBe(roundsAfterFirstRun);
		(await ReadSnapshotAsync("lottery_entries")).ShouldBe(entriesAfterFirstRun);
		second.ResultsProcessed.ShouldBe(2);
		second.RoundsCreated.ShouldBe(0);
		second.RoundsRefreshed.ShouldBe(2);
		second.EntriesCreated.ShouldBe(0);
		second.EntriesAlreadyPresent.ShouldBe(2);
		second.WinnersCreated.ShouldBe(0);
		(await _winners.GetAsync()).ShouldBeEmpty();
	}

	[Fact]
	public async Task BackfillAsync_DoesNotClobberAFinishedRoundsPotFields()
	{
		await InsertArchiveResultAsync(new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc), 56,
			(ulong.Parse(GenerateRandomDiscordId()), 56));
		await _rounds.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-07-21",
			ScheduledAt = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc),
			Status = LotteryRoundStatus.Announced,
			PotBefore = 42000000,
			PotAfter = 1000000,
			AnnouncementMessageId = "123456789",
			ClaimOwner = "worker-1"
		});

		await _service.BackfillAsync();

		var round = await _rounds.GetByRoundKeyAsync("2026-07-21");
		round!.Status.ShouldBe(LotteryRoundStatus.Completed);
		round.PotBefore.ShouldBe(42000000);
		round.PotAfter.ShouldBe(1000000);
		round.AnnouncementMessageId.ShouldBe("123456789");
		round.ClaimOwner.ShouldBe("worker-1");
	}

	[Fact]
	public async Task BackfillAsync_OnAnEmptyArchive_DoesNothing()
	{
		var result = await _service.BackfillAsync();

		result.ResultsProcessed.ShouldBe(0);
		result.RoundsCreated.ShouldBe(0);
		result.EntriesCreated.ShouldBe(0);
		(await _rounds.GetAsync()).ShouldBeEmpty();
		(await _entries.GetAsync()).ShouldBeEmpty();
	}

	private async Task InsertArchiveResultAsync(DateTime createdAtUtc, int winningNumber,
		params (ulong DiscordId, int Number)[] guesses)
	{
		// Insert straight into the archive so the fixture owns DateCreated: the repository stamps "now", and the
		// round key is derived from the archive timestamp.
		var archive = _mongoClient.GetDatabase(DatabaseName).GetCollection<LotteryResult>("lottery_results");
		await archive.InsertOneAsync(new LotteryResult
		{
			DateCreated = createdAtUtc,
			DateModified = createdAtUtc,
			WinningNumber = winningNumber,
			Guesses = guesses
				.Select(g => new LotteryGuess { DiscordId = g.DiscordId, Number = g.Number })
				.ToList()
		});
	}

	private async Task<string> ReadArchiveSnapshotAsync()
	{
		return await ReadSnapshotAsync("lottery_results");
	}

	/// <summary>A stable JSON snapshot of a whole collection, ordered by id, for identity comparisons.</summary>
	private async Task<string> ReadSnapshotAsync(string collectionName)
	{
		var collection = _mongoClient.GetDatabase(DatabaseName).GetCollection<BsonDocument>(collectionName);
		var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty)
			.Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
			.ToListAsync();

		return string.Join("\n", documents.Select(d => d.ToJson()));
	}
}
