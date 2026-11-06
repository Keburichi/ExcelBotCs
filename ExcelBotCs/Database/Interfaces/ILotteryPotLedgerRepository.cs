using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

/// <summary>
///     A position in the admin ledger table: the previous page's last entry. Ordering is creation instant then id,
///     newest first, so the id is the tie-breaker for two entries written in the same tick.
/// </summary>
public sealed record LotteryLedgerPageAnchor(DateTime DateCreatedUtc, string Id);

public interface ILotteryPotLedgerRepository : IBaseRepository<LotteryPotLedgerEntry>
{
	Task<List<LotteryPotLedgerEntry>> GetByRoundKeyAsync(string roundKey);

	/// <summary>The newest ledger entries, newest first.</summary>
	Task<List<LotteryPotLedgerEntry>> GetLatestAsync(int limit);

	Task<LotteryPotLedgerEntry?> GetByIdempotencyKeyAsync(string idempotencyKey);

	/// <summary>
	///     One page of the admin ledger, newest first. <paramref name="after" /> is the previous page's last entry, or
	///     <c>null</c> for the first page; the filter and the sort agree exactly, so no entry is repeated or skipped.
	/// </summary>
	Task<List<LotteryPotLedgerEntry>> GetPageAsync(LotteryLedgerPageAnchor? after, int limit);
}
