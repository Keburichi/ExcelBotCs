using ExcelBotCs.Attributes;
using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Models.Config;

/// <summary>
///     Deployment configuration for the automated lottery (spec §7 "Deployment config"). It is read-only for the
///     admin page and wins over the stored <c>lottery_settings</c> document. <c>Lottery:Automation:Enabled</c> is the
///     deploy kill switch: when it is false nothing is scheduled or posted, whatever the stored settings say.
/// </summary>
[OptionsSection("Lottery")]
public class LotteryOptions
{
	public LotteryAutomationOptions Automation { get; set; } = new();
}

public class LotteryAutomationOptions
{
	/// <summary><c>Lottery:Automation:Enabled</c> — the deploy kill switch for the scheduler. Defaults to off.</summary>
	public bool Enabled { get; set; }

	/// <summary>
	///     <c>Lottery:Automation:DryRun</c> — post the announcement as a <c>[DRY RUN]</c> message to the log channel
	///     with mentions suppressed. On for the first rollout.
	/// </summary>
	public bool DryRun { get; set; } = true;

	/// <summary><c>Lottery:Automation:BasePot</c> — the pot after a win and the opening balance. Not admin-editable.</summary>
	public long BasePot { get; set; } = LotterySettingsDefaults.BasePot;

	/// <summary>
	///     <c>Lottery:Automation:WeeklyIncrement</c> — what the pot grows by after a round without a winner. Not
	///     admin-editable.
	/// </summary>
	public long WeeklyIncrement { get; set; } = LotterySettingsDefaults.WeeklyIncrement;
}
