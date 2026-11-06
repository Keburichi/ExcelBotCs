using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryRoundRepository : IBaseRepository<LotteryRound>
{
	Task<LotteryRound?> GetByRoundKeyAsync(string roundKey);

	Task<List<LotteryRound>> GetByStatusAsync(string status);

	/// <summary>The most recently scheduled rounds, newest first.</summary>
	Task<List<LotteryRound>> GetLatestAsync(int limit);

	/// <summary>
	///     Imports an already drawn round from the <c>lottery_results</c> archive: creates it as
	///     <see cref="LotteryRoundStatus.Completed" /> when missing, and on re-run only refreshes the run
	///     instant, the winning number and the status. Pot fields, the announcement message id and the claim
	///     metadata are never touched, so the backfill cannot clobber a live round.
	/// </summary>
	/// <returns><c>true</c> when a round was created, <c>false</c> when an existing round was refreshed.</returns>
	Task<bool> ImportCompletedRoundAsync(string roundKey, DateTime runAtUtc, int winningNumber);
}
