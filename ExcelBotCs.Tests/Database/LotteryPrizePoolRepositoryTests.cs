using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryPrizePoolRepositoryTests : MongoDbTest
{
	private ILotteryPrizePoolRepository _repository = null!;

	public LotteryPrizePoolRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_repository = new LotteryPrizePoolRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task CreateAsync_PersistsInPoolPrize()
	{
		await _repository.CreateAsync(new LotteryPrize
		{
			Name = "Fuax Mount",
			Status = LotteryPrizePoolStatus.InPool,
			AddedAt = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc)
		});

		var inPool = await _repository.GetInPoolAsync();

		inPool.Count.ShouldBe(1);
		inPool[0].Name.ShouldBe("Fuax Mount");
		inPool[0].Status.ShouldBe("in-pool");
		inPool[0].AwardedToDiscordId.ShouldBeNull();
		inPool[0].AwardedToName.ShouldBeNull();
		inPool[0].AwardedRoundKey.ShouldBeNull();
		inPool[0].AddedByDiscordId.ShouldBeNull();
	}

	[Fact]
	public async Task GetInPoolAsync_ExcludesAwardedPrizes()
	{
		var prize = new LotteryPrize
		{
			Name = "Yan",
			Status = LotteryPrizePoolStatus.InPool,
			AddedAt = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc)
		};
		await _repository.CreateAsync(prize);

		prize.Status = LotteryPrizePoolStatus.Awarded;
		prize.AwardedToDiscordId = ulong.Parse(GenerateRandomDiscordId());
		prize.AwardedToName = "Siegward";
		prize.AwardedRoundKey = "2026-07-21";
		await _repository.UpdateAsync(prize.Id, prize);

		var inPool = await _repository.GetInPoolAsync();
		var awarded = await _repository.GetByAwardedRoundKeyAsync("2026-07-21");

		inPool.ShouldBeEmpty();
		awarded.Count.ShouldBe(1);
		awarded[0].Name.ShouldBe("Yan");
		awarded[0].AwardedToName.ShouldBe("Siegward");
		awarded[0].AwardedToDiscordId.ShouldBe(prize.AwardedToDiscordId);
	}

	[Fact]
	public async Task GetInPoolAsync_ReturnsOnlyInPoolPrizes()
	{
		await _repository.CreateAsync(new LotteryPrize
		{
			Name = "Cosmic Mount", Status = LotteryPrizePoolStatus.InPool,
			AddedAt = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc)
		});
		await _repository.CreateAsync(new LotteryPrize
		{
			Name = "Chaotic Mount", Status = LotteryPrizePoolStatus.Awarded,
			AddedAt = new DateTime(2026, 6, 21, 18, 0, 0, DateTimeKind.Utc), AwardedRoundKey = "2026-06-21"
		});

		var inPool = await _repository.GetInPoolAsync();

		inPool.Count.ShouldBe(1);
		inPool[0].Name.ShouldBe("Cosmic Mount");
	}
}
