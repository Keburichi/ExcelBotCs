using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryAwardRepository : IBaseRepository<LotteryAward>
{
	Task<List<LotteryAward>> GetByRoundKeyAsync(string roundKey);
	Task<List<LotteryAward>> GetByRoundKeyAndDiscordIdAsync(string roundKey, ulong discordId);
	Task<List<LotteryAward>> GetByDiscordIdAsync(ulong discordId);
}
