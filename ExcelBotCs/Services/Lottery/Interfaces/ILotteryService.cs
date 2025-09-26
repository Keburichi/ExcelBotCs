using ExcelBotCs.Services.Lottery.Enums;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

public interface ILotteryService
{
    Task<string> GuessAsync(ulong discordUserId, int number);
    Task<string> GetUnusedNumbersAsync();

    Task<string> RandomGuessAsync(ulong discordUserId, CancellationTokenSource cts,
        RandomGuessType guessType = RandomGuessType.UnusedOnly);

    Task<string> ChangeGuessAsync(ulong discordUserId, int old, int @new);

    Task<string> WhoGuessedAsync(int number);
    
    Task<string> ViewAsync(ulong discordUserId);
    
    Task RunLotteryAsync(ulong discordUserId);
    Task RemindAsync(ulong discordUserId);
    Task<IAwardResponse> TryAwardUsersAsync(string reason, List<ulong> userIds);
    Task AwardUsersAsync(SuccessAwardResponse success);
}