using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryRoundRepository : BaseRepository<LotteryRound>, ILotteryRoundRepository
{
	public LotteryRoundRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_rounds";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryRound>> GetIndexModels()
	{
		return
		[
			Index("ux_lottery_rounds_RoundKey", Ascending(r => r.RoundKey), unique: true),
			Index("ix_lottery_rounds_ScheduledAt", Ascending(r => r.ScheduledAt)),
			Index("ix_lottery_rounds_Status", Ascending(r => r.Status))
		];
	}

	public async Task<LotteryRound?> GetByRoundKeyAsync(string roundKey)
	{
		return await Collection.Find(r => r.RoundKey == roundKey).FirstOrDefaultAsync();
	}

	public async Task<List<LotteryRound>> GetByStatusAsync(string status)
	{
		return await Collection.Find(r => r.Status == status)
			.SortByDescending(r => r.ScheduledAt)
			.ToListAsync();
	}

	public async Task<List<LotteryRound>> GetLatestAsync(int limit)
	{
		return await Collection.Find(_ => true)
			.SortByDescending(r => r.ScheduledAt)
			.Limit(limit)
			.ToListAsync();
	}

	public async Task<bool> ImportCompletedRoundAsync(string roundKey, DateTime runAtUtc, int winningNumber)
	{
		var update = Builders<LotteryRound>.Update
			.Set(r => r.RunAt, runAtUtc)
			.Set(r => r.Status, LotteryRoundStatus.Completed)
			.Set(r => r.WinningNumber, winningNumber)
			.SetOnInsert(r => r.RoundKey, roundKey)
			.SetOnInsert(r => r.ScheduledAt, runAtUtc)
			.SetOnInsert(r => r.DateCreated, DateTime.UtcNow)
			.SetOnInsert(r => r.DateModified, DateTime.UtcNow);

		var result = await Collection.UpdateOneAsync(
			r => r.RoundKey == roundKey,
			update,
			new UpdateOptions { IsUpsert = true });

		return result.UpsertedId != null;
	}
}
