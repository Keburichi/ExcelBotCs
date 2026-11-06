using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     Runs <see cref="ILotteryStartupCheck" /> once, in the background, so a slow or unreachable database cannot
///     delay the host. The result is kept on this singleton for the scheduler, which must not schedule anything while
///     <see cref="LotteryStartupResult.IsDegraded" /> is true (spec §5.10).
/// </summary>
public sealed class LotteryStartupCheckService : IHostedService
{
	private readonly ILotteryStartupCheck _check;
	private readonly ILogger<LotteryStartupCheckService> _logger;

	public LotteryStartupCheckService(ILotteryStartupCheck check, ILogger<LotteryStartupCheckService> logger)
	{
		_check = check;
		_logger = logger;
	}

	/// <summary>The last check result, or <c>null</c> while the background check is still running.</summary>
	public LotteryStartupResult? Result { get; private set; }

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_ = RunAsync(cancellationToken);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	private async Task RunAsync(CancellationToken cancellationToken)
	{
		try
		{
			Result = await _check.CheckAsync(cancellationToken);
		}
		catch (Exception exception)
		{
			// ILotteryStartupCheck promises not to throw; this is the last line of defence for the fire-and-forget
			// start so nothing becomes an unobserved task exception.
			Result = LotteryStartupResult.Degraded([$"The lottery startup check failed: {exception.Message}"]);
			_logger.LogError(exception, "Lottery startup check could not run; lottery automation starts degraded");
		}
	}
}
