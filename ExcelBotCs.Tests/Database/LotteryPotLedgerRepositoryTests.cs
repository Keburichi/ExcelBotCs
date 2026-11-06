using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryPotLedgerRepositoryTests : MongoDbTest
{
	private ILotteryPotLedgerRepository _repository = null!;

	public LotteryPotLedgerRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_repository = new LotteryPotLedgerRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task CreateAsync_PersistsOpeningBalanceWithoutRoundKey()
	{
		await _repository.CreateAsync(new LotteryPotLedgerEntry
		{
			RoundKey = null,
			Type = LotteryPotLedgerType.Correction,
			Amount = 21000000,
			Reason = "opening balance from manual history",
			BalanceAfter = 21000000,
			IdempotencyKey = "opening-balance"
		});

		var stored = await _repository.GetByIdempotencyKeyAsync("opening-balance");

		stored.ShouldNotBeNull();
		stored!.RoundKey.ShouldBeNull();
		stored.Type.ShouldBe("correction");
		stored.Amount.ShouldBe(21000000);
		stored.BalanceAfter.ShouldBe(21000000);
		stored.Reason.ShouldBe("opening balance from manual history");
		stored.ActorDiscordId.ShouldBeNull();
	}

	[Fact]
	public async Task GetByIdempotencyKeyAsync_ReturnsNull_WhenNoEntryHasThatKey()
	{
		var stored = await _repository.GetByIdempotencyKeyAsync("round:2026-07-21:reset");

		stored.ShouldBeNull();
	}

	[Fact]
	public async Task CreateAsync_Fails_WhenIdempotencyKeyIsDuplicated()
	{
		await _repository.EnsureIndexesAsync();
		var entry = new LotteryPotLedgerEntry
		{
			RoundKey = "2026-07-21",
			Type = LotteryPotLedgerType.Reset,
			Amount = -21000000,
			Reason = "pot paid out",
			BalanceAfter = 1000000,
			IdempotencyKey = "round:2026-07-21:reset"
		};
		await _repository.CreateAsync(entry);

		var duplicate = new LotteryPotLedgerEntry
		{
			RoundKey = "2026-07-21",
			Type = LotteryPotLedgerType.Reset,
			Amount = -21000000,
			Reason = "retry of the same operation",
			BalanceAfter = 1000000,
			IdempotencyKey = "round:2026-07-21:reset"
		};

		await Should.ThrowAsync<MongoWriteException>(async () => await _repository.CreateAsync(duplicate));
	}

	[Fact]
	public async Task GetByRoundKeyAsync_ReturnsEntriesOfThatRoundOldestFirst()
	{
		await _repository.CreateAsync(new LotteryPotLedgerEntry
		{
			RoundKey = "2026-07-21", Type = LotteryPotLedgerType.Payout, Amount = -21000000, Reason = "payout",
			BalanceAfter = 0, IdempotencyKey = "round:2026-07-21:payout"
		});
		await _repository.CreateAsync(new LotteryPotLedgerEntry
		{
			RoundKey = "2026-07-21", Type = LotteryPotLedgerType.Reset, Amount = 1000000, Reason = "reset",
			BalanceAfter = 1000000, IdempotencyKey = "round:2026-07-21:reset"
		});
		await _repository.CreateAsync(new LotteryPotLedgerEntry
		{
			RoundKey = "2026-07-28", Type = LotteryPotLedgerType.WeeklyIncrement, Amount = 1000000,
			Reason = "no winner", BalanceAfter = 2000000, IdempotencyKey = "round:2026-07-28:increment"
		});

		var entries = await _repository.GetByRoundKeyAsync("2026-07-21");

		entries.Count.ShouldBe(2);
		entries[0].Type.ShouldBe("payout");
		entries[1].Type.ShouldBe("reset");
	}

	[Fact]
	public async Task GetLatestAsync_ReturnsNewestEntryAndSkipsNothing()
	{
		await _repository.CreateAsync(new LotteryPotLedgerEntry
		{
			RoundKey = null, Type = LotteryPotLedgerType.Correction, Amount = 21000000, Reason = "opening",
			BalanceAfter = 21000000, IdempotencyKey = "opening-balance"
		});
		await _repository.CreateAsync(new LotteryPotLedgerEntry
		{
			RoundKey = "2026-07-28", Type = LotteryPotLedgerType.Riddle, Amount = 5000000, Reason = "riddle",
			BalanceAfter = 26000000, IdempotencyKey = "riddle:1"
		});

		var latest = await _repository.GetLatestAsync(1);

		latest.Count.ShouldBe(1);
		latest[0].IdempotencyKey.ShouldBe("riddle:1");
	}
}
