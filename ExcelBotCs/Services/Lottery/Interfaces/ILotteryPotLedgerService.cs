using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     Reads and appends the append-only pot ledger (spec §5.4, #98). The pot is always derived from the newest
///     <c>BalanceAfter</c>; there is no stored running total. Every append carries a deterministic idempotency key, so
///     a retried draw resumes rather than double-charging the pot.
/// </summary>
public interface ILotteryPotLedgerService
{
	/// <summary>
	///     The current pot: the newest entry's balance, or the base pot when the ledger is empty. Never writes — spec §6
	///     "Opening pot (exactly one document at cutover; no auto set)": the opening balance is a manual seed, so an
	///     unseeded deployment starts at the configured base pot until the first ledger write.
	/// </summary>
	Task<long> GetBalanceAsync(long basePot, CancellationToken cancellationToken = default);

	/// <summary>The entry stored under an idempotency key, or <c>null</c>.</summary>
	Task<LotteryPotLedgerEntry?> FindAsync(string idempotencyKey, CancellationToken cancellationToken = default);

	/// <summary>The round's ledger entries in creation order; the void reversal reads them.</summary>
	Task<List<LotteryPotLedgerEntry>> GetByRoundKeyAsync(string roundKey,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Appends an entry unless its idempotency key is already present, in which case the stored entry is returned
	///     and nothing is written. Both the caller's computed <paramref name="balanceAfter" /> and the reason are
	///     stored as given; the caller has already applied the pot rules.
	/// </summary>
	Task<LotteryPotLedgerEntry> AppendAsync(string? roundKey, string type, long amount, long balanceAfter,
		string reason, ulong? actorDiscordId, string idempotencyKey, CancellationToken cancellationToken = default);
}
