namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     The outcome of the lottery startup check (spec §5.10). <see cref="IsDegraded" /> means the scheduler must not
///     schedule anything until the configuration is fixed and the process restarted; the alert has already been
///     raised once, through the normal error log that Discord mirrors into the log channel.
/// </summary>
public sealed record LotteryStartupResult(bool IsDegraded, IReadOnlyList<string> Problems)
{
	public bool SchedulerEnabled => !IsDegraded;

	public static LotteryStartupResult Healthy { get; } = new(false, []);

	public static LotteryStartupResult Degraded(IReadOnlyList<string> problems)
	{
		return new LotteryStartupResult(true, problems);
	}
}
