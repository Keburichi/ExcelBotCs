using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryPotLedgerRepository : IBaseRepository<LotteryPotLedgerEntry>
{
	Task<List<LotteryPotLedgerEntry>> GetByRoundKeyAsync(string roundKey);

	/// <summary>The newest ledger entries, newest first.</summary>
	Task<List<LotteryPotLedgerEntry>> GetLatestAsync(int limit);

	Task<LotteryPotLedgerEntry?> GetByIdempotencyKeyAsync(string idempotencyKey);
}
