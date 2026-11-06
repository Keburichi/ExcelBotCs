using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The in-process scheduler (spec §5.1, §5.7, §10). It ticks about once a minute through the injected
///     <see cref="TimeProvider" /> and delegates every decision: the round to <see cref="ILotteryRoundRunner" />, the
///     weekly reminder to <see cref="ILotteryReminderService" />. Both read the stored settings and honour the
///     deployment kill switch, the pause and the dry run. The tick interval only bounds how late a run or a reminder
///     can be: the slot itself comes from the schedule.
/// </summary>
public sealed class LotterySchedulerService : BackgroundService
{
	private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

	private readonly ILogger<LotterySchedulerService> _logger;
	private readonly ILotteryReminderService _reminders;
	private readonly ILotteryRoundRunner _runner;
	private readonly TimeProvider _time;

	public LotterySchedulerService(IServiceScopeFactory scopeFactory, ILotteryRoundRunner runner,
		ILotteryReminderService reminders, TimeProvider time, ILogger<LotterySchedulerService> logger)
		: base(scopeFactory)
	{
		_runner = runner;
		_reminders = reminders;
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
		var roundTick = await TickRoundAsync(cancellationToken);
		await TickReminderAsync(cancellationToken);
		return roundTick;
	}

	private async Task<LotteryTickResult> TickRoundAsync(CancellationToken cancellationToken)
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

	/// <summary>
	///     The reminder gets its own guard so a broken reminder can never delay or interrupt the draw (spec §5.7,
	///     §5.10). The service already promises not to throw; this is the loop's last line of defence.
	/// </summary>
	private async Task TickReminderAsync(CancellationToken cancellationToken)
	{
		try
		{
			var outcome = await _reminders.TickAsync(cancellationToken);
			_logger.LogDebug("Lottery reminder tick: {Disposition} — {Message}", outcome.Disposition,
				outcome.Message);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "The lottery reminder tick threw; the next tick will retry");
		}
	}
}
