using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotterySettingsHistoryRepository : IBaseRepository<LotterySettingsHistory>
{
	/// <summary>The audit rows for one settings key, oldest version first.</summary>
	Task<List<LotterySettingsHistory>> GetBySettingsKeyAsync(string settingsKey);
}
