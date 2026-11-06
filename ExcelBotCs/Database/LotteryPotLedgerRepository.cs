using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryPotLedgerRepository : BaseRepository<LotteryPotLedgerEntry>, ILotteryPotLedgerRepository
{
	public LotteryPotLedgerRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_pot_ledger";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryPotLedgerEntry>> GetIndexModels()
	{
		return
		[
			Index("ix_lottery_pot_ledger_RoundKey_DateCreated",
				Combine(Ascending(e => e.RoundKey), Descending(e => e.DateCreated))),
			Index("ix_lottery_pot_ledger_Type", Ascending(e => e.Type)),
			Index("ux_lottery_pot_ledger_IdempotencyKey", Ascending(e => e.IdempotencyKey), unique: true)
		];
	}

	public async Task<List<LotteryPotLedgerEntry>> GetByRoundKeyAsync(string roundKey)
	{
		return await Collection.Find(e => e.RoundKey == roundKey)
			.SortBy(e => e.DateCreated)
			.ThenBy(e => e.Id)
			.ToListAsync();
	}

	public async Task<List<LotteryPotLedgerEntry>> GetLatestAsync(int limit)
	{
		return await Collection.Find(_ => true)
			.SortByDescending(e => e.DateCreated)
			.ThenByDescending(e => e.Id)
			.Limit(limit)
			.ToListAsync();
	}

	public async Task<LotteryPotLedgerEntry?> GetByIdempotencyKeyAsync(string idempotencyKey)
	{
		return await Collection.Find(e => e.IdempotencyKey == idempotencyKey).FirstOrDefaultAsync();
	}

	public async Task<List<LotteryPotLedgerEntry>> GetPageAsync(LotteryLedgerPageAnchor? after, int limit)
	{
		var sort = Builders<LotteryPotLedgerEntry>.Sort
			.Descending(e => e.DateCreated)
			.Descending(e => e.Id);

		var filter = Builders<LotteryPotLedgerEntry>.Filter.Empty;
		if (after is not null)
		{
			filter = Builders<LotteryPotLedgerEntry>.Filter.Or(
				Builders<LotteryPotLedgerEntry>.Filter.Lt(e => e.DateCreated, after.DateCreatedUtc),
				Builders<LotteryPotLedgerEntry>.Filter.And(
					Builders<LotteryPotLedgerEntry>.Filter.Eq(e => e.DateCreated, after.DateCreatedUtc),
					Builders<LotteryPotLedgerEntry>.Filter.Lt(e => e.Id, after.Id)));
		}

		return await Collection.Find(filter).Sort(sort).Limit(limit).ToListAsync();
	}
}
