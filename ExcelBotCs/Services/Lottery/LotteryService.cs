using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Extensions;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Enums;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

public class LotteryService : ILotteryService
{
    private readonly IDiscordMessageService _discordMessageService;
    private readonly IExtraLotteryGuessRepository _extraLotteryGuesses;
    private readonly ILotteryGuessRepository _lotteryGuesses;
    private readonly IMemberService _memberService;
    private readonly Prng _rng;
    private readonly ILotteryAwardRepository _awards;
    private readonly ILotteryRoundRepository _rounds;
    private readonly ILotteryRoundRunner _runner;
    private readonly ILotteryScheduleService _schedule;

    public LotteryService(Prng rng,
        ILotteryGuessRepository lotteryGuessRepository,
        IExtraLotteryGuessRepository extraLotteryGuessRepository,
        IMemberService memberService, IDiscordMessageService discordMessageService,
        ILotteryRoundRunner runner,
        ILotteryRoundRepository roundRepository,
        ILotteryAwardRepository awardRepository,
        ILotteryScheduleService scheduleService)
    {
        _rng = rng;
        _memberService = memberService;
        _discordMessageService = discordMessageService;
        _lotteryGuesses = lotteryGuessRepository;
        _extraLotteryGuesses = extraLotteryGuessRepository;
        _runner = runner;
        _rounds = roundRepository;
        _awards = awardRepository;
        _schedule = scheduleService;
    }

    public async Task<IGuessResponse> GuessAsync(ulong discordUserId, int number)
    {
        var result = await TryGuess(discordUserId, number);

        if (result is SuccessGuessResponse success)
        {
            await InsertGuessAsync(discordUserId, number);
            await _discordMessageService.PostInLotteryChannelAsync(
                $"<@{discordUserId}> guessed {number}. Current guesses: {success.PrettyCurrentGuesses}");
        }

        return result;
    }

    public async Task<UnusedNumbersResponse> GetUnusedNumbersAsync()
    {
        var guessedNumbers = await GetGuessedNumbersAsync();
        var unusedNumbers = Enumerable.Range(1, 99).Where(num => !guessedNumbers.Contains(num)).ToList();
        return new UnusedNumbersResponse(guessedNumbers, unusedNumbers);
    }

    public async Task<IGuessResponse> RandomGuessAsync(ulong discordUserId, CancellationTokenSource cts,
        RandomGuessType numberPool = RandomGuessType.UnusedOnly)
    {
        var task = TryRandomGuessAsync(discordUserId, cts.Token, numberPool);

        if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token)) == task)
        {
            await cts.CancelAsync();

            if (task.IsCanceled)
                return new RandomGuessTimeoutResponse();

            var result = await task;

            if (result is SuccessGuessResponse success)
            {
                await InsertGuessAsync(discordUserId, success.Number);
                await _discordMessageService.PostInLotteryChannelAsync(
                    $"<@{discordUserId}> used a random draw and got {success.Number}. Current guesses: {success.PrettyCurrentGuesses}");
            }

            return result ?? new RandomGuessErrorResponse();
        }

        await cts.CancelAsync();
        return new RandomGuessTimeoutResponse();
    }

    public async Task<IGuessResponse> ChangeGuessAsync(ulong discordUserId, int old, int @new)
    {
        var result = await TryChangeGuessAsync(discordUserId, old, @new);

        if (result is SuccessGuessResponse success)
        {
            await ChangeGuess(discordUserId, old, @new);
            await _discordMessageService.PostInLotteryChannelAsync(
                $"<@{discordUserId}> changed a guess from {old} to {@new}. Current guesses: {success.PrettyCurrentGuesses}");
        }

        return result;
    }

    public async Task<List<WhoGuessedResponse>> GetAllGuessesAsync()
    {
        var currentGuesses = await _lotteryGuesses.GetAsync();

        var discordUserIds = currentGuesses.Select(guess => guess.DiscordId).Distinct().ToList();
        var members = await _memberService.GetByDiscordIds(discordUserIds);

        return currentGuesses
            .GroupBy(guess => guess.Number)
            .Select(group =>
            {
                var users = group.Select(guess =>
                {
                    var member = members.FirstOrDefault(m => m.DiscordId == guess.DiscordId.ToString());
                    return new LotteryUser(guess.DiscordId, member?.DiscordName ?? $"Unknown User ({guess.DiscordId})");
                }).ToList();
                return new WhoGuessedResponse(group.Key, users);
            })
            .OrderBy(r => r.Number)
            .ToList();
    }

    public async Task<WhoGuessedResponse> WhoGuessedAsync(int number)
    {
        var allGuesses = await _lotteryGuesses.GetAsync();
        var currentGuesses = allGuesses.Where(g => g.Number == number).ToList();

        var discordUserIds = currentGuesses.Select(guess => guess.DiscordId).ToList();
        var members = await _memberService.GetByDiscordIds(discordUserIds);

        var users = discordUserIds.Select(id =>
        {
            var member = members.FirstOrDefault(m => m.DiscordId == id.ToString());
            return new LotteryUser(id, member?.DiscordName ?? $"Unknown User ({id})");
        }).ToList();

        return new WhoGuessedResponse(number, users);
    }

    public async Task<IViewResponse> ViewAsync(ulong discordUserId)
    {
        if (!await CanParticipateAsync(discordUserId))
            return new NotFcMemberViewResponse();

        var (currentGuesses, displayAmount) = await GetRemainingGuessesAsync(discordUserId);

        var extraAwardedGuesses = await _extraLotteryGuesses.GetByDiscordIdAsync(discordUserId);

        var guesses = currentGuesses.Select(guess => guess.Number).ToList();
        guesses.Sort();

        return new ViewResponse(guesses, currentGuesses.Count,
            LotteryGuessAllowance.Allowed(extraAwardedGuesses.Count), displayAmount);
    }

    public async Task<LotteryRunResult> RunLotteryAsync(ulong discordUserId)
    {
        var executingUser = await _memberService.GetByDiscordId(discordUserId);

        if (executingUser == null)
            throw new ArgumentException($"The user with the id {discordUserId} was not found.");

        if (!executingUser.IsAdmin.GetValueOrDefault())
            return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
                "Only admins can run the lottery.");

        // The scheduler and the manual run share one code path; the manual run simply has a real actor.
        return await _runner.RunNowAsync(discordUserId);
    }

    private async Task<List<int>> GetGuessedNumbersAsync()
    {
        return (await _lotteryGuesses.GetAsync())
            .Select(guess => guess.Number)
            .Distinct()
            .ToList();
    }

    private async Task<List<int>> GetNotGuessedNumbersAsync()
    {
        return Enumerable.Range(1, 99).Except(await GetGuessedNumbersAsync()).ToList();
    }

    private async Task<(List<LotteryGuess>? CurrentGuesses, string Output)> GetRemainingGuessesAsync(
        ulong discordUserId)
    {
        var currentGuesses = await _lotteryGuesses.GetByDiscordIdAsync(discordUserId);
        var extraAwardedGuesses = await _extraLotteryGuesses.GetByDiscordIdAsync(discordUserId);

        var currentGuessAmount = currentGuesses.Count;
        var allowedGuessAmount = LotteryGuessAllowance.Allowed(extraAwardedGuesses.Count);
        var displayAmount = allowedGuessAmount == 1
            ? currentGuessAmount == 1
                ? "You have used your guess."
                : "You have not used your guess."
            : $"You have used {currentGuessAmount} of your {allowedGuessAmount} guesses.";

        return (currentGuesses, displayAmount);
    }

    private async Task InsertGuessAsync(ulong discordUserId, int number)
    {
        await _lotteryGuesses.CreateAsync(new LotteryGuess
        {
            DiscordId = discordUserId, Number = number
        });
    }

    private async Task<bool> CanParticipateAsync(ulong discordUserId)
    {
        var user = await _memberService.GetByDiscordId(discordUserId);
        return user?.IsMember != null && user.IsMember.Value;
    }

    /// <summary>
    ///     Once a round owns the guess set — drawing, announcing or blocked on a failure — guessing is refused instead
    ///     of racing the draw or being flushed away by the cleanup. The caller gets a typed response, not an exception.
    /// </summary>
    private async Task<bool> IsRunInFlightAsync()
    {
        var blocking = await _rounds.GetNewestBlockingAsync();
        return blocking is not null && LotteryRoundStatus.BlocksGuessing(blocking.Status);
    }

    #region Guess

    private async Task<IGuessResponse> TryRandomGuessAsync(ulong discordUserId, CancellationToken ctx,
        RandomGuessType type)
    {
        var numberPool = (type switch
        {
            RandomGuessType.Any => Enumerable.Range(0, 99),
            RandomGuessType.UsedOnly => await GetGuessedNumbersAsync(),
            RandomGuessType.UnusedOnly => await GetNotGuessedNumbersAsync(),
            _ => throw new ArgumentException()
        }).ToList();

        if (numberPool.Count == 0)
            return new OutOfRangeGuessResponse();

        while (true)
        {
            ctx.ThrowIfCancellationRequested();

            var randomNumber = _rng.Pick(numberPool).First();
            var result = await TryGuess(discordUserId, randomNumber);

            if (result is SuccessGuessResponse or NotFcMemberGuessResponse or NoMoreGuessesGuessResponse)
                return result;
        }
    }

    private async Task<IGuessResponse> TryGuess(ulong discordUserId, int number)
    {
        if (!await CanParticipateAsync(discordUserId))
            return new NotFcMemberGuessResponse();

        if (await IsRunInFlightAsync())
            return new LotteryRunningGuessResponse();

        if (number is <= 0 or >= 100)
            return new OutOfRangeGuessResponse();

        var currentGuesses = await _lotteryGuesses.GetByDiscordIdAsync(discordUserId);

        if (currentGuesses.Any(guess => guess.Number == number))
            return new AlreadyGuessedNumberGuessResponse(number);

        var extraAwardedGuesses = await _extraLotteryGuesses.GetByDiscordIdAsync(discordUserId);

        var currentGuessAmount = currentGuesses.Count;

        var numbers = currentGuesses.Select(guess => guess.Number).ToList();
        var guessesBlocked = LotteryGuessAllowance.Remaining(currentGuessAmount, extraAwardedGuesses.Count) <= 0;

        if (!guessesBlocked)
            numbers.Add(number);

        numbers.Sort();
        var prettyNumbers = numbers.Select(guess => guess.ToString()).ToList().PrettyJoin();

        return guessesBlocked
            ? new NoMoreGuessesGuessResponse(numbers, prettyNumbers)
            : new SuccessGuessResponse(numbers, prettyNumbers, number);
    }

    #endregion

    #region Change Guess

    public async Task<IGuessResponse> TryChangeGuessAsync(ulong discordUserId, int oldNumber, int newNumber)
    {
        if (!await CanParticipateAsync(discordUserId))
            return new NotFcMemberGuessResponse();

        if (await IsRunInFlightAsync())
            return new LotteryRunningGuessResponse();

        if (newNumber is <= 0 or >= 100)
            return new OutOfRangeGuessResponse();

        var currentGuesses = await _lotteryGuesses.GetByDiscordIdAsync(discordUserId);

        if (currentGuesses.All(guess => guess.Number != oldNumber))
            return new NotCurrentGuessedNumberGuessResponse(oldNumber);

        if (currentGuesses.Any(guess => guess.Number == newNumber))
            return new AlreadyGuessedNumberGuessResponse(newNumber);

        currentGuesses.RemoveAll(guess => guess.Number == oldNumber);

        var numbers = (List<int>)[newNumber, .. currentGuesses.Select(guess => guess.Number)];
        numbers.Sort();
        var prettyNumbers = numbers.Select(guess => guess.ToString()).ToList().PrettyJoin();

        return new SuccessGuessResponse(numbers, prettyNumbers, newNumber);
    }

    private async Task ChangeGuess(ulong discordUserId, int oldNumber, int newNumber)
    {
        await _lotteryGuesses.DeleteByDiscordIdAndNumberAsync(discordUserId, oldNumber);
        await _lotteryGuesses.CreateAsync(new LotteryGuess { DiscordId = discordUserId, Number = newNumber });
    }

    #endregion

    #region Award

    public async Task<IAwardResponse> TryAwardUsersAsync(string reason, List<ulong> userIds)
    {
        if (!userIds.Any())
            return new NoUsersAwardResponse();

        var text = userIds.Select(userId => $"<@{userId}>").ToList().PrettyJoin();
        return new SuccessAwardResponse(userIds, text, reason);
    }

    public async Task AwardUsersAsync(SuccessAwardResponse success, ulong? actorDiscordId = null)
    {
        // The award belongs to the round the next draw will use. The legacy row drives the weekly allowance (and is
        // flushed only when the round completes); the lottery_awards row is the permanent audit.
        var roundKey = await _schedule.GetCurrentRoundKeyAsync();

        foreach (var userId in success.DiscordUserIds)
        {
            await _extraLotteryGuesses.CreateAsync(new ExtraLotteryGuess
                { DiscordId = userId, Reason = success.Reason });

            await _awards.CreateAsync(new LotteryAward
            {
                RoundKey = roundKey,
                DiscordId = userId,
                Reason = success.Reason,
                AwardedByDiscordId = actorDiscordId
            });
        }

        if (success.DiscordUserIds.Count() == 1)
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{success.PrettyUsersAwarded} has been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
        else
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{success.PrettyUsersAwarded} have all been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
    }

    #endregion
}