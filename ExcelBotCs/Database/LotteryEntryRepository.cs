using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryEntryRepository : BaseRepository<LotteryEntry>, ILotteryEntryRepository
{
	public LotteryEntryRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_entries";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryEntry>> GetIndexModels()
	{
		return
		[
			Index("ux_lottery_entries_RoundKey_DiscordId_Number",
				Combine(Ascending(e => e.RoundKey), Ascending(e => e.DiscordId), Ascending(e => e.Number)),
				unique: true),
			Index("ix_lottery_entries_Number", Ascending(e => e.Number))
		];
	}

	public async Task<List<LotteryEntry>> GetByRoundKeyAsync(string roundKey)
	{
		return await Collection.Find(e => e.RoundKey == roundKey)
			.SortBy(e => e.Number)
			.ToListAsync();
	}

	public async Task<List<LotteryEntry>> GetByRoundKeyAndDiscordIdAsync(string roundKey, ulong discordId)
	{
		return await Collection.Find(e => e.RoundKey == roundKey && e.DiscordId == discordId)
			.SortBy(e => e.Number)
			.ToListAsync();
	}

	public async Task<List<LotteryEntry>> GetByDiscordIdAsync(ulong discordId)
	{
		return await Collection.Find(e => e.DiscordId == discordId)
			.SortBy(e => e.RoundKey)
			.ToListAsync();
	}

	public async Task<bool> AddIfMissingAsync(LotteryEntry entry)
	{
		var update = Builders<LotteryEntry>.Update
			.SetOnInsert(e => e.RoundKey, entry.RoundKey)
			.SetOnInsert(e => e.DiscordId, entry.DiscordId)
			.SetOnInsert(e => e.Number, entry.Number)
			.SetOnInsert(e => e.DateCreated, DateTime.UtcNow)
			.SetOnInsert(e => e.DateModified, DateTime.UtcNow);

		var result = await Collection.UpdateOneAsync(
			e => e.RoundKey == entry.RoundKey && e.DiscordId == entry.DiscordId && e.Number == entry.Number,
			update,
			new UpdateOptions { IsUpsert = true });

		return result.UpsertedId != null;
	}
}
