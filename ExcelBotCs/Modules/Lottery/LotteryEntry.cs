using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     One guess submitted for one round. The full guess set of a round lives here; the unique
///     <c>(RoundKey, DiscordId, Number)</c> index makes re-importing an archive result idempotent.
/// </summary>
public class LotteryEntry : BaseEntity
{
	public string RoundKey { get; set; } = string.Empty;
	public ulong DiscordId { get; set; }
	public int Number { get; set; }
}
