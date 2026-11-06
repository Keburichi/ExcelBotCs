namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     The lottery automation status the admin page shows next to the Pause toggle (spec §7 "Status"). It is derived
///     from the deployment kill switch and the two stored flags, never stored itself, so it cannot drift from them.
///     <see cref="Disabled" /> is the final override: with <c>Lottery:Automation:Enabled=false</c> nothing runs,
///     whatever the stored settings say.
/// </summary>
public sealed record LotteryAutomationStatus(string Kind, string Label, bool SchedulerEnabled)
{
	/// <summary>Enabled, live posting, automation not paused.</summary>
	public const string LiveKind = "live";

	/// <summary>Enabled but posting is a dry run; the scheduler still runs.</summary>
	public const string DryRunKind = "dry-run";

	/// <summary>Admin pause: the scheduler is stopped, the manual "Run now" stays live.</summary>
	public const string PausedKind = "paused";

	/// <summary>Deployment kill switch on: the scheduler is stopped and nothing is posted.</summary>
	public const string DisabledKind = "disabled";

	public static LotteryAutomationStatus Live { get; } = new(LiveKind, "Live", true);

	public static LotteryAutomationStatus DryRun { get; } = new(DryRunKind, "Dry run", true);

	public static LotteryAutomationStatus Paused { get; } = new(PausedKind, "Paused", false);

	public static LotteryAutomationStatus Disabled { get; } = new(DisabledKind, "Disabled", false);

	public static LotteryAutomationStatus Derive(bool enabled, bool dryRun, bool pauseAutomation)
	{
		if (!enabled) return Disabled;
		if (pauseAutomation) return Paused;
		return dryRun ? DryRun : Live;
	}
}
