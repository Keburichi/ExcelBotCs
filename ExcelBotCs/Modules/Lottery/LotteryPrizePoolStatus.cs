namespace ExcelBotCs.Modules.Lottery;

/// <summary>The possible values of <see cref="LotteryPrize.Status" />.</summary>
public static class LotteryPrizePoolStatus
{
	public const string InPool = "in-pool";
	public const string Awarded = "awarded";

	public static readonly IReadOnlyList<string> All = [InPool, Awarded];
}
