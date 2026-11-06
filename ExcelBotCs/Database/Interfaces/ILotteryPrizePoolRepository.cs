using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryPrizePoolRepository : IBaseRepository<LotteryPrize>
{
	/// <summary>Prizes still waiting for a winner.</summary>
	Task<List<LotteryPrize>> GetInPoolAsync();

	Task<List<LotteryPrize>> GetByAwardedRoundKeyAsync(string roundKey);
}
