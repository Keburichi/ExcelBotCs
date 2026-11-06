using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryWinnerRepository : BaseRepository<LotteryWinner>, ILotteryWinnerRepository
{
	public LotteryWinnerRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_winners";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryWinner>> GetIndexModels()
	{
		// No unique index: historical rows share a round with DiscordId null and a shared draw has two winners.
		return
		[
			Index("ix_lottery_winners_RoundKey", Ascending(w => w.RoundKey)),
			Index("ix_lottery_winners_DiscordId", Ascending(w => w.DiscordId)),
			Index("ix_lottery_winners_Guess", Ascending(w => w.Guess))
		];
	}

	public async Task<List<LotteryWinner>> GetByRoundKeyAsync(string roundKey)
	{
		return await Collection.Find(w => w.RoundKey == roundKey)
			.SortBy(w => w.DateCreated)
			.ToListAsync();
	}

	public async Task<List<LotteryWinner>> GetLatestAsync(int limit)
	{
		return await Collection.Find(_ => true)
			.SortByDescending(w => w.RoundKey)
			.ThenBy(w => w.DateCreated)
			.Limit(limit)
			.ToListAsync();
	}
}
