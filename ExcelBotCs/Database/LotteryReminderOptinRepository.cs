using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryReminderOptinRepository
	: BaseRepository<LotteryReminderOptin>, ILotteryReminderOptinRepository
{
	public LotteryReminderOptinRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_reminder_optins";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryReminderOptin>> GetIndexModels()
	{
		return
		[
			Index("ux_lottery_reminder_optins_DiscordId", Ascending(o => o.DiscordId), unique: true)
		];
	}

	public async Task<LotteryReminderOptin?> GetByDiscordIdAsync(ulong discordId)
	{
		return await Collection.Find(o => o.DiscordId == discordId).FirstOrDefaultAsync();
	}

	public async Task SetEnabledAsync(ulong discordId, bool enabled)
	{
		var now = DateTime.UtcNow;
		var update = Builders<LotteryReminderOptin>.Update
			.Set(o => o.Enabled, enabled)
			.Set(o => o.DateModified, now)
			.SetOnInsert(o => o.DiscordId, discordId)
			.SetOnInsert(o => o.DateCreated, now);

		await Collection.UpdateOneAsync(
			o => o.DiscordId == discordId,
			update,
			new UpdateOptions { IsUpsert = true });
	}

	public async Task<List<LotteryReminderOptin>> GetEnabledAsync()
	{
		return await Collection.Find(o => o.Enabled)
			.SortBy(o => o.DiscordId)
			.ToListAsync();
	}
}
