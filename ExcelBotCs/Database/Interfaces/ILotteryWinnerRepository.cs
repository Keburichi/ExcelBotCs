using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryWinnerRepository : IBaseRepository<LotteryWinner>
{
	Task<List<LotteryWinner>> GetByRoundKeyAsync(string roundKey);

	/// <summary>
	///     The newest winners across rounds, ordered by <c>RoundKey</c> descending then creation order. Two winners
	///     of a shared draw both appear. This is the last-five read path.
	/// </summary>
	Task<List<LotteryWinner>> GetLatestAsync(int limit);

	/// <summary>
	///     Inserts a winner unless the same <c>(RoundKey, DiscordId, Guess)</c> row already exists, so a retried draw
	///     resumes its winner writes instead of paying a winner twice.
	/// </summary>
	/// <returns><c>true</c> when the row was created, <c>false</c> when it was already present.</returns>
	Task<bool> AddIfMissingAsync(LotteryWinner winner);
}
