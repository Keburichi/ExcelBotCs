namespace ExcelBotCs.Modules.Lottery;

/// <summary>The possible values of <see cref="LotteryPotLedgerEntry.Type" />.</summary>
public static class LotteryPotLedgerType
{
	public const string Reset = "reset";
	public const string WeeklyIncrement = "weekly_increment";
	public const string Riddle = "riddle";
	public const string Donation = "donation";
	public const string Correction = "correction";
	public const string Payout = "payout";

	public static readonly IReadOnlyList<string> All = [Reset, WeeklyIncrement, Riddle, Donation, Correction, Payout];
}
