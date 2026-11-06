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

	public async Task<bool> AddIfMissingAsync(LotteryWinner winner)
	{
		// Winners have no unique index (historical rows share a round with a null DiscordId and a shared draw has
		// several rows), so the natural key is matched explicitly. A retried draw resumes instead of paying twice.
		var filter = Builders<LotteryWinner>.Filter.Eq(w => w.RoundKey, winner.RoundKey) &
		             Builders<LotteryWinner>.Filter.Eq(w => w.DiscordId, winner.DiscordId) &
		             Builders<LotteryWinner>.Filter.Eq(w => w.Guess, winner.Guess);

		var createdAt = winner.DateCreated == default ? DateTime.UtcNow : winner.DateCreated;

		var update = Builders<LotteryWinner>.Update
			.SetOnInsert(w => w.RoundKey, winner.RoundKey)
			.SetOnInsert(w => w.DiscordId, winner.DiscordId)
			.SetOnInsert(w => w.DisplayName, winner.DisplayName)
			.SetOnInsert(w => w.Guess, winner.Guess)
			.SetOnInsert(w => w.GilPrize, winner.GilPrize)
			.SetOnInsert(w => w.BonusItems, winner.BonusItems)
			.SetOnInsert(w => w.PayoutStatus, winner.PayoutStatus)
			.SetOnInsert(w => w.DateCreated, createdAt)
			.SetOnInsert(w => w.DateModified, createdAt);

		var result = await Collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
		return result.UpsertedId != null;
	}
}
