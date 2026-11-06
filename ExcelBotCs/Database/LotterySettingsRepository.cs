using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotterySettingsRepository : BaseRepository<LotterySettings>, ILotterySettingsRepository
{
	public LotterySettingsRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_settings";
	}

	public override IReadOnlyList<CreateIndexModel<LotterySettings>> GetIndexModels()
	{
		return
		[
			Index("ux_lottery_settings_Key", Ascending(s => s.Key), unique: true)
		];
	}

	public async Task<LotterySettings?> GetSingletonAsync()
	{
		return await Collection.Find(s => s.Key == LotterySettings.SingletonKey).FirstOrDefaultAsync();
	}

	public async Task SaveAsync(LotterySettings settings)
	{
		settings.Key = LotterySettings.SingletonKey;

		var existing = await GetSingletonAsync();
		if (existing is null)
		{
			await CreateAsync(settings);
			return;
		}

		// Keep the stored document identity: a replaced document must not try to change _id or DateCreated.
		settings.Id = existing.Id;
		settings.DateCreated = existing.DateCreated;
		await UpdateAsync(settings.Id, settings);
	}

	public async Task SetLastDryRunRoundKeyAsync(string roundKey)
	{
		var now = DateTime.UtcNow;

		// The document may not exist yet (a dry-run week before the first admin save), so the insert carries the §7
		// defaults: a bare document with empty fields would make the settings look corrupt and degrade the startup check.
		var update = Builders<LotterySettings>.Update
			.Set(s => s.LastDryRunRoundKey, roundKey)
			.Set(s => s.DateModified, now)
			.SetOnInsert(s => s.Key, LotterySettings.SingletonKey)
			.SetOnInsert(s => s.RunDay, LotterySettingsDefaults.RunDay)
			.SetOnInsert(s => s.RunTime, LotterySettingsDefaults.RunTime)
			.SetOnInsert(s => s.RunTimezone, LotterySettingsDefaults.RunTimezone)
			.SetOnInsert(s => s.ReminderDay, LotterySettingsDefaults.ReminderDay)
			.SetOnInsert(s => s.ReminderTime, LotterySettingsDefaults.ReminderTime)
			.SetOnInsert(s => s.ReminderTimezone, LotterySettingsDefaults.ReminderTimezone)
			.SetOnInsert(s => s.WinTemplate, LotterySettingsDefaults.WinTemplate)
			.SetOnInsert(s => s.NoWinTemplate, LotterySettingsDefaults.NoWinTemplate)
			.SetOnInsert(s => s.DateCreated, now);

		try
		{
			await Collection.UpdateOneAsync(s => s.Key == LotterySettings.SingletonKey, update,
				new UpdateOptions { IsUpsert = true });
		}
		catch (MongoWriteException exception) when (MongoDuplicateKey.IsDuplicate(exception))
		{
			// Two ticks raced to create the singleton; the row that won carries the same marker value.
		}
	}
}
