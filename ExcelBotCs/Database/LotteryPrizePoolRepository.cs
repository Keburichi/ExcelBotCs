using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryPrizePoolRepository : BaseRepository<LotteryPrize>, ILotteryPrizePoolRepository
{
	public LotteryPrizePoolRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_prize_pool";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryPrize>> GetIndexModels()
	{
		return
		[
			Index("ix_lottery_prize_pool_Status", Ascending(p => p.Status)),
			Index("ix_lottery_prize_pool_AwardedRoundKey", Ascending(p => p.AwardedRoundKey))
		];
	}

	public async Task<List<LotteryPrize>> GetInPoolAsync()
	{
		return await Collection.Find(p => p.Status == LotteryPrizePoolStatus.InPool)
			.SortBy(p => p.AddedAt)
			.ThenBy(p => p.Id)
			.ToListAsync();
	}

	public async Task<List<LotteryPrize>> GetByAwardedRoundKeyAsync(string roundKey)
	{
		return await Collection.Find(p => p.AwardedRoundKey == roundKey)
			.SortBy(p => p.Name)
			.ToListAsync();
	}
}
