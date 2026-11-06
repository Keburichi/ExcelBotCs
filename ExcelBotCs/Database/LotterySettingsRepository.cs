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
}
