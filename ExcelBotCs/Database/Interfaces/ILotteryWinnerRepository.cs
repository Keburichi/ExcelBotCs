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
}
