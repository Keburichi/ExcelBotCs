using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryAwardRepository : BaseRepository<LotteryAward>, ILotteryAwardRepository
{
	public LotteryAwardRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_awards";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryAward>> GetIndexModels()
	{
		return
		[
			Index("ix_lottery_awards_RoundKey_DiscordId",
				Combine(Ascending(a => a.RoundKey), Ascending(a => a.DiscordId)))
		];
	}

	public async Task<List<LotteryAward>> GetByRoundKeyAsync(string roundKey)
	{
		return await Collection.Find(a => a.RoundKey == roundKey)
			.SortBy(a => a.DateCreated)
			.ThenBy(a => a.Id)
			.ToListAsync();
	}

	public async Task<List<LotteryAward>> GetByRoundKeyAndDiscordIdAsync(string roundKey, ulong discordId)
	{
		return await Collection.Find(a => a.RoundKey == roundKey && a.DiscordId == discordId)
			.SortBy(a => a.DateCreated)
			.ThenBy(a => a.Id)
			.ToListAsync();
	}

	public async Task<List<LotteryAward>> GetByDiscordIdAsync(ulong discordId)
	{
		return await Collection.Find(a => a.DiscordId == discordId)
			.SortBy(a => a.RoundKey)
			.ToListAsync();
	}
}
