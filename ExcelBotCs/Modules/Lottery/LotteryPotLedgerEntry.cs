using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     One append-only change to the pot. The current pot is the <see cref="BalanceAfter" /> of the newest entry;
///     there is no separate balance document. <see cref="IdempotencyKey" /> is unique so a retried write cannot
///     double-count.
/// </summary>
public class LotteryPotLedgerEntry : BaseEntity
{
	/// <summary><c>null</c> for the opening balance, which belongs to no round.</summary>
	public string? RoundKey { get; set; }

	public string Type { get; set; } = string.Empty;

	/// <summary>Signed amount: <c>payout</c> is negative, other types are positive unless a correction is negative.</summary>
	public long Amount { get; set; }

	public string Reason { get; set; } = string.Empty;
	public ulong? ActorDiscordId { get; set; }

	/// <summary>The pot after applying <see cref="Amount" />.</summary>
	public long BalanceAfter { get; set; }

	/// <summary>Unique per entry; a retried operation reuses its key instead of appending a second entry.</summary>
	public string IdempotencyKey { get; set; } = string.Empty;
}
