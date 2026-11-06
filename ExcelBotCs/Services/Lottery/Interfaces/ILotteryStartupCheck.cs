using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     Checks the lottery deployment configuration and the stored settings once, at startup. A bad or missing value
///     must degrade (the scheduler stays off) and raise exactly one alert, never throw (spec §5.10).
/// </summary>
public interface ILotteryStartupCheck
{
	/// <summary>Never throws; returns <see cref="LotteryStartupResult.IsDegraded" /> for any problem found.</summary>
	Task<LotteryStartupResult> CheckAsync(CancellationToken cancellationToken = default);
}
