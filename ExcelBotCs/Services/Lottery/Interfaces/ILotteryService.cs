using ExcelBotCs.Services.Lottery.Enums;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

public interface ILotteryService
{
    Task<IGuessResponse> GuessAsync(ulong discordUserId, int number);
    Task<UnusedNumbersResponse> GetUnusedNumbersAsync();

    Task<IGuessResponse> RandomGuessAsync(ulong discordUserId, CancellationTokenSource cts,
        RandomGuessType guessType = RandomGuessType.UnusedOnly);

    Task<IGuessResponse> ChangeGuessAsync(ulong discordUserId, int old, int @new);

    Task<WhoGuessedResponse> WhoGuessedAsync(int number);

    Task<IViewResponse> ViewAsync(ulong discordUserId);

    Task RunLotteryAsync(ulong discordUserId);
    Task RemindAsync(ulong discordUserId);
    Task<IAwardResponse> TryAwardUsersAsync(string reason, List<ulong> userIds);
    Task AwardUsersAsync(SuccessAwardResponse success);
}