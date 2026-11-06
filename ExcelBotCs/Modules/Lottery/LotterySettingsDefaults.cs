using ExcelBotCs.Utilities;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     The canonical defaults for every admin-editable lottery setting (spec §5.1, §5.6, §5.7 and §7). They are the
///     bottom of the precedence chain (env &gt; stored &gt; defaults) and the fallback for a stored value that fails
///     validation, so they must stay stable: the scheduler, reminder and announcement tickets all rely on them.
/// </summary>
public static class LotterySettingsDefaults
{
	public const string RunDay = "Tuesday";
	public const string RunTime = "20:00";
	public const string RunTimezone = LotteryRoundKey.TimezoneId;

	public const string ReminderDay = "Monday";
	public const string ReminderTime = "18:00";
	public const string ReminderTimezone = LotteryRoundKey.TimezoneId;

	public const bool RemindersEnabled = false;
	public const bool PauseAutomation = false;

	/// <summary>§5.6 "Winner" result block, placeholders included; the surrounding post is generated.</summary>
	public const string WinTemplate =
		"the winning number was {number}! Congratulations to {winners}! 🎉\n" +
		"They take the {prize} pot{bonusItems}.\n" +
		"The jackpot resets to {nextPot} Gil for next week.";

	/// <summary>§5.6 "No winner" result block, placeholders included.</summary>
	public const string NoWinTemplate =
		"the winning number was {number}! So no one won this week again {emoji}\n" +
		"The jackpot goes up to {pot}!!";

	/// <summary>Deployment default for <c>Lottery:Automation:BasePot</c>; never admin-editable.</summary>
	public const long BasePot = 1_000_000;

	/// <summary>Deployment default for <c>Lottery:Automation:WeeklyIncrement</c>; never admin-editable.</summary>
	public const long WeeklyIncrement = 1_000_000;
}
