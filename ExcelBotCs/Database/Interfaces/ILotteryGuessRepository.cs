using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryGuessRepository : IBaseRepository<LotteryGuess>
{
    Task<List<LotteryGuess>> GetByDiscordIdAsync(ulong discordId);
    Task DeleteByDiscordIdAndNumberAsync(ulong discordId, int number);
    Task DeleteAllAsync();
}