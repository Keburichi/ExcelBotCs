using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface IExtraLotteryGuessRepository : IBaseRepository<ExtraLotteryGuess>
{
    Task<List<ExtraLotteryGuess>> GetByDiscordIdAsync(ulong discordId);
    Task DeleteAllAsync();
}