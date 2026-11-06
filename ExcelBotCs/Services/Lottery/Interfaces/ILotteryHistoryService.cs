using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The member history timeline (spec §5.8, §8): the winners of every recorded draw, newest first, keyset
///     paginated, plus the summary numbers the page shows. Member-only; payout status never appears here.
/// </summary>
public interface ILotteryHistoryService
{
	/// <summary>
	///     One page of the timeline. <paramref name="limit" /> is clamped to the supported range (1 to 50) and
	///     defaults to 10 winners. An absent cursor means the first page; a cursor this endpoint did not issue yields
	///     <see cref="LotteryHistoryInvalidCursorResult" /> for a typed 400.
	/// </summary>
	Task<ILotteryHistoryResult> GetPageAsync(string? cursor, int? limit,
		CancellationToken cancellationToken = default);
}
