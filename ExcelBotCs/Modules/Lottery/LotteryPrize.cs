using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     A non-Gil prize item. Items are indivisible: an awarded prize is assigned to exactly one winner or stays
///     in the pool. Gil prizes are tracked on <see cref="LotteryWinner" />, not here.
/// </summary>
public class LotteryPrize : BaseEntity
{
	public string Name { get; set; } = string.Empty;
	public string Status { get; set; } = LotteryPrizePoolStatus.InPool;
	public ulong? AwardedToDiscordId { get; set; }
	public string? AwardedToName { get; set; }
	public string? AwardedRoundKey { get; set; }
	public ulong? AddedByDiscordId { get; set; }
	public DateTime AddedAt { get; set; }
}
