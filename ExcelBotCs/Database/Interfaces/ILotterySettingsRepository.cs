using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotterySettingsRepository : IBaseRepository<LotterySettings>
{
	/// <summary>The single settings document, or <c>null</c> before the first save.</summary>
	Task<LotterySettings?> GetSingletonAsync();

	/// <summary>Creates the settings document or replaces it, keyed by <see cref="LotterySettings.SingletonKey" />.</summary>
	Task SaveAsync(LotterySettings settings);
}
