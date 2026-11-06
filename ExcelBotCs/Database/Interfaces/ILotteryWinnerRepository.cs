using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

/// <summary>
///     A position in the winner timeline. It is the last row of the previous page — round key, creation instant and
///     id — so the next page starts strictly after it. The id is the final tie-breaker, which is what keeps the two
///     winners of a shared draw (same round, same instant) together instead of skipping or repeating one.
/// </summary>
public sealed record LotteryWinnerPageAnchor(string RoundKey, DateTime DateCreatedUtc, string Id);

/// <summary>The summary numbers of the member history page: rows, distinct draws and the newest round.</summary>
public sealed record LotteryWinnerHistoryCounts(int Winners, int Draws, string? NewestRoundKey);

public interface ILotteryWinnerRepository : IBaseRepository<LotteryWinner>
{
	Task<List<LotteryWinner>> GetByRoundKeyAsync(string roundKey);

	/// <summary>
	///     The newest winners across rounds, ordered by <c>RoundKey</c> descending then creation order. Two winners
	///     of a shared draw both appear. This is the last-five read path.
	/// </summary>
	Task<List<LotteryWinner>> GetLatestAsync(int limit);

	/// <summary>
	///     One page of the member history timeline (spec §5.8): newest <c>RoundKey</c> first, then creation order and
	///     id within the round. <paramref name="after" /> is the previous page's last row, or <c>null</c> for the
	///     first page. Ordering and filter agree exactly, so a shared draw is neither split nor duplicated.
	/// </summary>
	Task<List<LotteryWinner>> GetHistoryPageAsync(LotteryWinnerPageAnchor? after, int limit);

	/// <summary>The collection totals: how many winners are recorded and across how many distinct draws.</summary>
	Task<LotteryWinnerHistoryCounts> GetHistoryCountsAsync();

	/// <summary>
	///     Inserts a winner unless the same <c>(RoundKey, DiscordId, Guess)</c> row already exists, so a retried draw
	///     resumes its winner writes instead of paying a winner twice.
	/// </summary>
	/// <returns><c>true</c> when the row was created, <c>false</c> when it was already present.</returns>
	Task<bool> AddIfMissingAsync(LotteryWinner winner);
}
