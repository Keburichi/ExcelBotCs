using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     The singleton, admin-editable operational settings document. Deployment config (tokens, channel ids) lives in
///     appsettings/env and is not stored here.
/// </summary>
public class LotterySettings : BaseEntity
{
	/// <summary>The only key the singleton document ever has.</summary>
	public const string SingletonKey = "lottery";

	public string Key { get; set; } = SingletonKey;

	public string RunDay { get; set; } = string.Empty;

	/// <summary>Local run time, <c>HH:mm</c>.</summary>
	public string RunTime { get; set; } = string.Empty;

	public string RunTimezone { get; set; } = "Europe/Berlin";
	public string ReminderDay { get; set; } = string.Empty;
	public string ReminderTime { get; set; } = string.Empty;
	public string ReminderTimezone { get; set; } = "Europe/Berlin";
	public bool RemindersEnabled { get; set; }
	public bool PauseAutomation { get; set; }
	public string WinTemplate { get; set; } = string.Empty;
	public string NoWinTemplate { get; set; } = string.Empty;

	/// <summary>Incremented on every save; mirrors the <c>lottery_settings_history</c> version.</summary>
	public int Version { get; set; }

	public ulong? UpdatedByDiscordId { get; set; }
}
