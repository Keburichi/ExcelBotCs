using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     An extra guess granted to a member for a round. The award is kept per round instead of being flushed after
///     the draw, so the grant history survives the round.
/// </summary>
public class LotteryAward : BaseEntity
{
	public string RoundKey { get; set; } = string.Empty;
	public ulong DiscordId { get; set; }
	public string Reason { get; set; } = string.Empty;
	public ulong? AwardedByDiscordId { get; set; }
}
