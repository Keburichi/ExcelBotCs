using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

public interface ILotteryService
{
    Task<IGuessResponse> Guess(ulong discordUserId, int number);
    Task<bool> CanParticipate(ulong discordUserId);
    Task<IGuessResponse> TryGuess(ulong discordUserId, int number);
    Task<IGuessResponse> TryChangeGuess(ulong discordUserId, int oldNumber, int newNumber);
    public Task AwardUsers(SuccessAwardResponse successAwardResponse);
}