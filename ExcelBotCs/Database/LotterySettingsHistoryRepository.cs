using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotterySettingsHistoryRepository
	: BaseRepository<LotterySettingsHistory>, ILotterySettingsHistoryRepository
{
	public LotterySettingsHistoryRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_settings_history";
	}

	public override IReadOnlyList<CreateIndexModel<LotterySettingsHistory>> GetIndexModels()
	{
		return
		[
			Index("ix_lottery_settings_history_SettingsKey_Version",
				Combine(Ascending(h => h.SettingsKey), Ascending(h => h.Version)))
		];
	}

	public async Task<List<LotterySettingsHistory>> GetBySettingsKeyAsync(string settingsKey)
	{
		return await Collection.Find(h => h.SettingsKey == settingsKey)
			.SortBy(h => h.Version)
			.ToListAsync();
	}
}
