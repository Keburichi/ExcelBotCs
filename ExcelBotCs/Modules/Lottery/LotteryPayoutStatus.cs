namespace ExcelBotCs.Modules.Lottery;

/// <summary>The possible values of <see cref="LotteryWinner.PayoutStatus" />.</summary>
public static class LotteryPayoutStatus
{
	public const string Pending = "pending";
	public const string Paid = "paid";
	public const string Forfeited = "forfeited";

	public static readonly IReadOnlyList<string> All = [Pending, Paid, Forfeited];
}
