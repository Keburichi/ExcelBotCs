using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The one-time, idempotent import of the legacy <c>lottery_results</c> archive into
///     <c>lottery_rounds</c> and <c>lottery_entries</c> (spec §6 "Backfill"). It never creates winners and never
///     writes to the archive.
/// </summary>
public interface ILotteryArchiveBackfillService
{
	Task<LotteryBackfillResult> BackfillAsync();
}
