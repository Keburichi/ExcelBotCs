using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     The admin-editable half of the lottery settings: exactly the fields the admin page saves (spec §7
///     "Operational settings"). Deployment config is deliberately absent — base pot, weekly increment, channel ids
///     and the automation kill switch are not admin-editable.
/// </summary>
public sealed record LotterySettingsUpdate(
	string RunDay,
	string RunTime,
	string RunTimezone,
	string ReminderDay,
	string ReminderTime,
	string ReminderTimezone,
	bool RemindersEnabled,
	bool PauseAutomation,
	string WinTemplate,
	string NoWinTemplate)
{
	/// <summary>The defaults a fresh install (or a "reset to defaults" request) starts from.</summary>
	public static LotterySettingsUpdate FromDefaults()
	{
		return new LotterySettingsUpdate(
			LotterySettingsDefaults.RunDay,
			LotterySettingsDefaults.RunTime,
			LotterySettingsDefaults.RunTimezone,
			LotterySettingsDefaults.ReminderDay,
			LotterySettingsDefaults.ReminderTime,
			LotterySettingsDefaults.ReminderTimezone,
			LotterySettingsDefaults.RemindersEnabled,
			LotterySettingsDefaults.PauseAutomation,
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate);
	}
}

/// <summary>One rejected field on save, in the shape the later admin API returns to the form.</summary>
public sealed record LotterySettingsValidationError(string Field, string Message);

/// <summary>
///     The effective settings every later ticket reads: the stored document (validated, invalid fields replaced by
///     their defaults) merged with the deployment config that the scheduler and the draw need.
/// </summary>
public sealed record LotteryEffectiveSettings(
	string RunDay,
	string RunTime,
	string RunTimezone,
	string ReminderDay,
	string ReminderTime,
	string ReminderTimezone,
	bool RemindersEnabled,
	bool PauseAutomation,
	string WinTemplate,
	string NoWinTemplate,
	int Version,
	ulong? UpdatedByDiscordId,
	bool AutomationEnabled,
	bool DryRun,
	long BasePot,
	long WeeklyIncrement,
	IReadOnlyList<string> InvalidFields)
{
	/// <summary>Live / Dry run / Paused / Disabled, derived from the flags above.</summary>
	public LotteryAutomationStatus Status => LotteryAutomationStatus.Derive(AutomationEnabled, DryRun, PauseAutomation);

	/// <summary>The admin-editable half, for prefilling the admin form and for resetting it to the current values.</summary>
	public LotterySettingsUpdate ToUpdate()
	{
		return new LotterySettingsUpdate(RunDay, RunTime, RunTimezone, ReminderDay, ReminderTime, ReminderTimezone,
			RemindersEnabled, PauseAutomation, WinTemplate, NoWinTemplate);
	}
}

/// <summary>The outcome of a settings save: rejected (nothing written) or accepted (version bumped and audited).</summary>
public sealed record LotterySettingsSaveResult(
	bool Success,
	bool Changed,
	LotteryEffectiveSettings? Settings,
	IReadOnlyList<LotterySettingsValidationError> Errors)
{
	public static LotterySettingsSaveResult Rejected(IReadOnlyList<LotterySettingsValidationError> errors)
	{
		return new LotterySettingsSaveResult(false, false, null, errors);
	}

	public static LotterySettingsSaveResult Unchanged(LotteryEffectiveSettings settings)
	{
		return new LotterySettingsSaveResult(true, false, settings, []);
	}

	public static LotterySettingsSaveResult Saved(LotteryEffectiveSettings settings)
	{
		return new LotterySettingsSaveResult(true, true, settings, []);
	}
}
