namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>The outcome of one <c>lottery_results</c> backfill run.</summary>
/// <param name="ResultsProcessed">Archive documents read.</param>
/// <param name="RoundsCreated">Rounds the run created.</param>
/// <param name="RoundsRefreshed">Rounds that already existed and were refreshed.</param>
/// <param name="EntriesCreated">Entries the run created.</param>
/// <param name="EntriesAlreadyPresent">Entries that were already imported by an earlier run.</param>
/// <param name="WinnersCreated">
///     Always zero. The backfill never synthesizes winners: the manual 35-row seed is the canonical winner history.
/// </param>
public record LotteryBackfillResult(
	int ResultsProcessed,
	int RoundsCreated,
	int RoundsRefreshed,
	int EntriesCreated,
	int EntriesAlreadyPresent,
	int WinnersCreated = 0);
