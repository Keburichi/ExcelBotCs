using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryWinnerRepositoryTests : MongoDbTest
{
	private ILotteryWinnerRepository _repository = null!;

	public LotteryWinnerRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_repository = new LotteryWinnerRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task GetByRoundKeyAsync_ReturnsWinnersOfThatRoundNewestFirstWithinTheRound()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-07-21",
			DiscordId = discordId,
			DisplayName = "Siegward",
			Guess = 56,
			GilPrize = 80000000,
			BonusItems = ["Chaotic Mount"],
			PayoutStatus = LotteryPayoutStatus.Paid
		});

		var winners = await _repository.GetByRoundKeyAsync("2026-07-21");

		winners.Count.ShouldBe(1);
		winners[0].RoundKey.ShouldBe("2026-07-21");
		winners[0].DiscordId.ShouldBe(discordId);
		winners[0].DisplayName.ShouldBe("Siegward");
		winners[0].Guess.ShouldBe(56);
		winners[0].GilPrize.ShouldBe(80000000);
		winners[0].BonusItems.ShouldBe(new[] { "Chaotic Mount" });
		winners[0].PayoutStatus.ShouldBe("paid");
	}

	[Fact]
	public async Task CreateAsync_PersistsHistoricalWinnerWithoutDiscordId()
	{
		await _repository.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-01-01",
			DiscordId = null,
			DisplayName = "Karya",
			Guess = 66,
			GilPrize = 2500000,
			BonusItems = [],
			PayoutStatus = LotteryPayoutStatus.Paid,
			PayoutNote = "historical record"
		});

		var winners = await _repository.GetByRoundKeyAsync("2026-01-01");

		winners.Count.ShouldBe(1);
		winners[0].DiscordId.ShouldBeNull();
		winners[0].BonusItems.ShouldBeEmpty();
		winners[0].PayoutActorDiscordId.ShouldBeNull();
		winners[0].PayoutAt.ShouldBeNull();
		winners[0].PayoutNote.ShouldBe("historical record");
	}

	[Fact]
	public async Task CreateAsync_AllowsTwoHistoricalWinnersInTheSameRound()
	{
		await _repository.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-01-01", DiscordId = null, DisplayName = "Karya", Guess = 66, GilPrize = 2500000,
			BonusItems = [], PayoutStatus = LotteryPayoutStatus.Paid
		});
		await _repository.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-01-01", DiscordId = null, DisplayName = "Uma", Guess = 66, GilPrize = 2500000,
			BonusItems = [], PayoutStatus = LotteryPayoutStatus.Paid
		});

		var winners = await _repository.GetByRoundKeyAsync("2026-01-01");

		winners.Count.ShouldBe(2);
		winners.Select(w => w.DisplayName).ShouldBe(new[] { "Karya", "Uma" }, ignoreOrder: true);
	}

	[Fact]
	public async Task UpdateAsync_RecordsPayout()
	{
		var actorDiscordId = ulong.Parse(GenerateRandomDiscordId());
		var winner = new LotteryWinner
		{
			RoundKey = "2026-07-28",
			DiscordId = ulong.Parse(GenerateRandomDiscordId()),
			DisplayName = "Elara",
			Guess = 94,
			GilPrize = 1100000,
			BonusItems = [],
			PayoutStatus = LotteryPayoutStatus.Pending
		};
		await _repository.CreateAsync(winner);

		winner.PayoutStatus = LotteryPayoutStatus.Paid;
		winner.PayoutActorDiscordId = actorDiscordId;
		winner.PayoutAt = new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc);
		winner.PayoutNote = "paid in game";
		await _repository.UpdateAsync(winner.Id, winner);

		var stored = (await _repository.GetByRoundKeyAsync("2026-07-28")).Single();
		stored.PayoutStatus.ShouldBe("paid");
		stored.PayoutActorDiscordId.ShouldBe(actorDiscordId);
		stored.PayoutAt.ShouldBe(new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc));
		stored.PayoutNote.ShouldBe("paid in game");
	}

	[Fact]
	public async Task GetLatestAsync_ReturnsWinnersFromTheNewestRoundsFirst()
	{
		await _repository.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-07-21", DiscordId = null, DisplayName = "Older", Guess = 10, GilPrize = 1000000,
			BonusItems = [], PayoutStatus = LotteryPayoutStatus.Paid
		});
		await _repository.CreateAsync(new LotteryWinner
		{
			RoundKey = "2026-07-28", DiscordId = null, DisplayName = "Newer", Guess = 20, GilPrize = 1000000,
			BonusItems = [], PayoutStatus = LotteryPayoutStatus.Paid
		});

		var latest = await _repository.GetLatestAsync(1);

		latest.Count.ShouldBe(1);
		latest[0].DisplayName.ShouldBe("Newer");
	}
}
