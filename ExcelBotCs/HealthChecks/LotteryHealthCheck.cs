using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExcelBotCs.HealthChecks;

/// <summary>
///     The <c>lottery</c> detail of <c>/health</c> (spec §5.10 "Health", §14; criterion 17). It reports the automation
///     state, the blocking round and the startup verdict, and it never returns <see cref="HealthStatus.Unhealthy" />:
///     the lottery is not a liveness dependency, so a stuck round, a paused scheduler or a slow database must never
///     make the container fail its probe. The endpoint maps <see cref="HealthStatus.Degraded" /> to 200, so this detail
///     stays informational.
/// </summary>
public sealed class LotteryHealthCheck : IHealthCheck
{
	/// <summary>The registration name the report and the runbook use.</summary>
	public const string Name = "lottery";

	/// <summary>
	///     The read budget for the status snapshot. A health probe may not hang on a slow database; the Mongo check
	///     already owns connectivity, so this one gives up and reports degraded instead.
	/// </summary>
	public static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(3);

	private readonly ILotteryRoundRunner _runner;
	private readonly LotteryStartupCheckService? _startupCheck;
	private readonly TimeSpan _readTimeout;

	public LotteryHealthCheck(ILotteryRoundRunner runner, LotteryStartupCheckService? startupCheck = null,
		TimeSpan? readTimeout = null)
	{
		_runner = runner;
		_startupCheck = startupCheck;
		_readTimeout = readTimeout ?? DefaultReadTimeout;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		budget.CancelAfter(_readTimeout);

		try
		{
			var snapshot = await _runner.GetStatusAsync(budget.Token);
			return Describe(snapshot, _startupCheck?.Result);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			return Unavailable($"The lottery status could not be read within {_readTimeout.TotalSeconds:0.#}s.");
		}
		catch (Exception exception)
		{
			// The mongodb check reports the database; this detail only says it could not read the lottery state.
			return Unavailable($"The lottery status could not be read: {exception.Message}");
		}
	}

	/// <summary>
	///     Maps a status snapshot and the startup verdict to the detail (spec §5.10, criterion 17). The mapping is pure
	///     and lives here so it is testable and so the runbook has exactly one place to read it from.
	///     <para>
	///         The accepted rule (#105): <b>healthy</b> when no round is pending or failed — the last round completed or
	///         was skipped and the next is scheduled — and the startup check passed; <b>degraded</b> when a round is
	///         pending/failed or the startup check is degraded or still running. The rollout flags (dry run, paused,
	///         disabled) are an operator decision rather than a fault, so they are reported in <c>automationNote</c> and
	///         in the description without flipping the status. Never unhealthy.
	///     </para>
	/// </summary>
	public static HealthCheckResult Describe(LotteryStatusSnapshot snapshot, LotteryStartupResult? startup)
	{
		var reasons = new List<string>();
		var notes = new List<string>();

		switch (snapshot.AutomationStatus)
		{
			case LotteryAutomationStatus.LiveKind:
				break;

			case LotteryAutomationStatus.DryRunKind:
				notes.Add("Lottery automation is a dry run: draws are simulated and posted to the log channel as " +
				          "[DRY RUN].");
				break;

			case LotteryAutomationStatus.PausedKind:
				notes.Add("Lottery automation is paused by the stored settings; the manual Run now still works.");
				break;

			default:
				notes.Add("Lottery automation is disabled (Lottery:Automation:Enabled=false); nothing is scheduled " +
				          "automatically and the manual Run now stays live.");
				break;
		}

		var pending = snapshot.PendingRound;
		if (pending is not null)
			reasons.Add(DescribePendingRound(pending, snapshot.LastError));

		var startupState = StartupState(startup, reasons);

		var data = new Dictionary<string, object>
		{
			["state"] = snapshot.AutomationStatus,
			["schedulerEnabled"] = snapshot.SchedulerEnabled,
			["degraded"] = reasons.Count > 0,
			["startupCheck"] = startupState
		};

		if (notes.Count > 0) data["automationNote"] = string.Join(" ", notes);

		if (pending is not null)
		{
			data["pendingRoundKey"] = pending.RoundKey;
			data["pendingStatus"] = pending.Status;
			if (pending.FailedStep is not null) data["failedStep"] = pending.FailedStep;
			if (pending.Attempts > 0) data["attempts"] = pending.Attempts;
			if (pending.NextRetryAt is { } retryAt) data["nextRetryAtUtc"] = retryAt.ToString("O");
		}

		if (snapshot.LastError is not null) data["lastError"] = snapshot.LastError;
		if (snapshot.NextRoundKey is not null) data["nextRoundKey"] = snapshot.NextRoundKey;
		if (snapshot.NextRunAtUtc is { } nextRun) data["nextRunAtUtc"] = nextRun.ToString("O");
		if (startup is { IsDegraded: true }) data["problems"] = startup.Problems.ToArray();

		if (reasons.Count > 0)
			return HealthCheckResult.Degraded(string.Join(" ", reasons), data: data);

		var next = snapshot.NextRoundKey is null
			? "No next round is scheduled."
			: $"The next round is {snapshot.NextRoundKey}.";

		// With nothing to flag, say plainly that the automation is live; otherwise the informational note leads.
		var summary = notes.Count > 0 ? notes : ["Lottery automation is live."];

		return HealthCheckResult.Healthy(string.Join(" ", summary.Append(next)), data);
	}

	private static string StartupState(LotteryStartupResult? startup, List<string> reasons)
	{
		switch (startup)
		{
			case null:
				reasons.Add("The lottery startup check has not finished; nothing is scheduled until it does.");
				return "pending";

			case { IsDegraded: true }:
				reasons.Add($"The lottery startup check is degraded: {string.Join("; ", startup.Problems)}");
				return "degraded";

			default:
				return "healthy";
		}
	}

	private static string DescribePendingRound(LotteryRound pending, string? lastError)
	{
		var where = pending.FailedStep is null
			? $"Round {pending.RoundKey} is {pending.Status} and unfinished; the next round is blocked until it is " +
			  "resumed or voided."
			: $"Round {pending.RoundKey} failed at the {pending.FailedStep} step after {pending.Attempts} attempt(s); " +
			  "retry or void it on /lottery/admin.";

		return lastError is null ? where : $"{where} Last error: {lastError}";
	}

	/// <summary>Degraded, never unhealthy: an unreadable status must not fail the container's probe.</summary>
	private static HealthCheckResult Unavailable(string description)
	{
		return HealthCheckResult.Degraded(description, data: new Dictionary<string, object>
		{
			["state"] = "unknown",
			["degraded"] = true
		});
	}
}
