using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotterySettingsRepository : IBaseRepository<LotterySettings>
{
	/// <summary>The single settings document, or <c>null</c> before the first save.</summary>
	Task<LotterySettings?> GetSingletonAsync();

	/// <summary>Creates the settings document or replaces it, keyed by <see cref="LotterySettings.SingletonKey" />.</summary>
	Task SaveAsync(LotterySettings settings);

	/// <summary>
	///     Records the round key of the last scheduled dry-run tick. It upserts the singleton document (with the §7
	///     defaults for a fresh deployment) so a dry-run week before the first admin save still leaves the marker.
	/// </summary>
	Task SetLastDryRunRoundKeyAsync(string roundKey);
}
