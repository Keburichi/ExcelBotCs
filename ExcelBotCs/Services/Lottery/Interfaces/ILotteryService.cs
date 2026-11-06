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

    Task<List<WhoGuessedResponse>> GetAllGuessesAsync();
    Task<WhoGuessedResponse> WhoGuessedAsync(int number);

    Task<IViewResponse> ViewAsync(ulong discordUserId);

    /// <summary>
    ///     The manual admin run: the same round engine the scheduler uses, with the admin as the actor. It is the live
    ///     escape hatch and is not affected by the automation kill switch or the dry-run flag.
    /// </summary>
    Task<LotteryRunResult> RunLotteryAsync(ulong discordUserId);
    Task<IAwardResponse> TryAwardUsersAsync(string reason, List<ulong> userIds);

    /// <summary>
    ///     Grants the extra guesses: writes the legacy <c>extra_lottery_guesses</c> rows that drive the weekly allowance
    ///     and the <c>lottery_awards</c> audit rows keyed by the current round, then announces the award.
    ///     <paramref name="actorDiscordId" /> is the officer or admin granting it.
    /// </summary>
    Task AwardUsersAsync(SuccessAwardResponse success, ulong? actorDiscordId = null);
}