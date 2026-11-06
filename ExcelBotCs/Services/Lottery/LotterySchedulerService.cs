using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The in-process scheduler (spec §5.1, §10). It ticks about once a minute through the injected
///     <see cref="TimeProvider" /> and delegates every decision to <see cref="ILotteryRoundRunner" />, which reads the
///     stored settings and honours the deployment kill switch, the pause and the dry run. The tick interval only
///     bounds how late a run can be: the run slot itself comes from the schedule.
/// </summary>
public sealed class LotterySchedulerService : BackgroundService
{
	private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

	private readonly ILogger<LotterySchedulerService> _logger;
	private readonly ILotteryRoundRunner _runner;
	private readonly TimeProvider _time;

	public LotterySchedulerService(IServiceScopeFactory scopeFactory, ILotteryRoundRunner runner, TimeProvider time,
		ILogger<LotterySchedulerService> logger) : base(scopeFactory)
	{
		_runner = runner;
		_time = time;
		_logger = logger;
	}

	/// <summary>The interval between ticks; one minute keeps a run within a minute of its slot.</summary>
	public static TimeSpan Interval => TickInterval;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Lottery scheduler started; checking for due rounds every {Interval}", TickInterval);

		using var timer = new PeriodicTimer(TickInterval, _time);

		while (!stoppingToken.IsCancellationRequested)
		{
			await TickOnceAsync(stoppingToken);

			try
			{
				if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	/// <summary>
	///     One tick, exposed so integration tests can drive the scheduler with a fake clock without starting the host.
	///     <see cref="ILotteryRoundRunner.TickAsync" /> already promises not to throw; this is the last line of
	///     defence for the loop.
	/// </summary>
	public async Task<LotteryTickResult> TickOnceAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			return await _runner.TickAsync(cancellationToken);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "The lottery scheduler tick threw; the next tick will retry");
			return new LotteryTickResult(LotteryTickAction.Nothing,
				$"The lottery scheduler tick threw: {exception.Message}");
		}
	}
}
