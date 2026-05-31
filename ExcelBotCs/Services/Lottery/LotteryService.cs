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
    private readonly IBonusLotteryResultRepository _bonusLotteryResults;
    private readonly IDiscordMessageService _discordMessageService;
    private readonly IExtraLotteryGuessRepository _extraLotteryGuesses;
    private readonly ILotteryGuessRepository _lotteryGuesses;
    private readonly ILotteryResultRepository _lotteryResults;
    private readonly IMemberService _memberService;
    private readonly Prng _rng;

    public LotteryService(Prng rng,
        ILotteryGuessRepository lotteryGuessRepository,
        IExtraLotteryGuessRepository extraLotteryGuessRepository,
        ILotteryResultRepository lotteryResultRepository,
        IBonusLotteryResultRepository bonusLotteryResultRepository,
        IMemberService memberService, IDiscordMessageService discordMessageService)
    {
        _rng = rng;
        _memberService = memberService;
        _discordMessageService = discordMessageService;
        _lotteryGuesses = lotteryGuessRepository;
        _extraLotteryGuesses = extraLotteryGuessRepository;
        _lotteryResults = lotteryResultRepository;
        _bonusLotteryResults = bonusLotteryResultRepository;
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

        return new ViewResponse(guesses, currentGuesses.Count, 1 + extraAwardedGuesses.Count, displayAmount);
    }

    public async Task RunLotteryAsync(ulong discordUserId)
    {
        var executingUser = await _memberService.GetByDiscordId(discordUserId);

        if (executingUser == null)
            throw new ArgumentException($"The user with the id {discordUserId} was not found.");

        if (!executingUser.IsAdmin.GetValueOrDefault())
            return;

        var randomNumber = _rng.NextInt(0, 99);
        var randomNumberDisplay = $"# The winning number is {randomNumber}";

        var allResults = await _lotteryGuesses.GetAsync();
        var winners = allResults.Where(guess => guess.Number == randomNumber).ToList();

        await SaveGuesses(randomNumber, allResults);

        if (winners.Count == 0)
        {
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{randomNumberDisplay}.\nNobody won, better luck next time!");
        }
        else
        {
            var winnersDisplay = winners.Select(user => $"<@{user.DiscordId}>").ToList().PrettyJoin();
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{randomNumberDisplay}. Congratulations to {winnersDisplay}!");
        }

        var grouped = allResults.GroupBy(guess => guess.DiscordId).OrderBy(group => group.Count());
        await _discordMessageService.PostInLotteryChannelAsync(
            $"## Participants\n{string.Join('\n', grouped.Select(group => $"<@{group.Key}>: {group.Select(guess => guess.Number.ToString()).ToList().PrettyJoin()}"))}");

        await Flush();
    }

    public async Task RemindAsync(ulong discordUserId)
    {
        var executingUser = await _memberService.GetByDiscordId(discordUserId);

        if (executingUser == null)
            throw new ArgumentException($"The user with the id {discordUserId} was not found.");

        if (!executingUser.IsAdmin.GetValueOrDefault())
            return;

        var fcMembers = await _memberService.GetFcMembers();
        var currentGuesses = (await _lotteryGuesses.GetAsync()).Select(guess => guess.DiscordId)
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());
        var awardedGuesses = (await _extraLotteryGuesses.GetAsync())
            .Select(award => award.DiscordId)
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());

        foreach (var fcMember in fcMembers)
            currentGuesses.TryAdd(ulong.Parse(fcMember.DiscordId), 0);

        var remainingGuesses = new List<(ulong Id, int Remaining)>();

        foreach (var (id, current) in currentGuesses)
            if (awardedGuesses.TryGetValue(id, out var remaining))
            {
                var count = remaining + 1 - current;
                if (count > 0)
                    remainingGuesses.Add((id, count));
            }
            else
            {
                if (current == 0)
                    remainingGuesses.Add((id, 1));
            }

        var previousParticipants = (await _lotteryResults.GetAsync())
            .OrderBy(result => result.DateCreated)
            .Take(3)
            .SelectMany(result => result.Guesses)
            .Select(guess => guess.DiscordId)
            .Distinct();

        var intersectionOutput = remainingGuesses
            .Where(guess => previousParticipants.Contains(guess.Id))
            .GroupBy(x => x.Remaining)
            .OrderBy(x => x.Key)
            .Aggregate("## Use your guesses before it's too late!\n",
                (current, guesses)
                    => current +
                       $"{guesses.Key} guess{(guesses.Key == 1 ? "" : "es")} remaining: {guesses.Select(user => $"<@{user.Id}>").ToList().PrettyJoin()}\n");

        await _discordMessageService.PostInLotteryChannelAsync(intersectionOutput);
    }

    private async Task SaveGuesses(int winningNumber, List<LotteryGuess> allResults)
    {
        var result = new LotteryResult
        {
            WinningNumber = winningNumber,
            Guesses = allResults
        };

        await _lotteryResults.CreateAsync(result);
    }

    private async Task Flush()
    {
        await _lotteryGuesses.DeleteAllAsync();
        await _extraLotteryGuesses.DeleteAllAsync();
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
        var allowedGuessAmount = 1 + extraAwardedGuesses.Count;
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

        if (number is <= 0 or >= 100)
            return new OutOfRangeGuessResponse();

        var currentGuesses = await _lotteryGuesses.GetByDiscordIdAsync(discordUserId);

        if (currentGuesses.Any(guess => guess.Number == number))
            return new AlreadyGuessedNumberGuessResponse(number);

        var extraAwardedGuesses = await _extraLotteryGuesses.GetByDiscordIdAsync(discordUserId);

        var currentGuessAmount = currentGuesses.Count;
        var allowedGuessAmount = 1 + extraAwardedGuesses.Count;

        var numbers = currentGuesses.Select(guess => guess.Number).ToList();
        var guessesBlocked = allowedGuessAmount - currentGuessAmount <= 0;

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

    public async Task AwardUsersAsync(SuccessAwardResponse success)
    {
        foreach (var userId in success.DiscordUserIds)
            await _extraLotteryGuesses.CreateAsync(new ExtraLotteryGuess
                { DiscordId = userId, Reason = success.Reason });

        if (success.DiscordUserIds.Count() == 1)
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{success.PrettyUsersAwarded} has been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
        else
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{success.PrettyUsersAwarded} have all been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
    }

    #endregion

    #region Bonus Lottery

    public async Task<BonusLotteryDrawResponse> RunBonusLotteryAsync(ulong discordUserId, string prize)
    {
        var executingUser = await _memberService.GetByDiscordId(discordUserId);

        if (executingUser == null)
            throw new ArgumentException($"The user with the id {discordUserId} was not found.");

        if (!executingUser.IsAdmin.GetValueOrDefault())
            throw new UnauthorizedAccessException("Only admins can run the bonus lottery.");

        var extraGuesses = await _extraLotteryGuesses.GetAsync();

        if (extraGuesses.Count == 0)
            throw new InvalidOperationException("No entries in the bonus lottery pool.");

        var discordIds = extraGuesses.Select(g => g.DiscordId).Distinct().ToList();
        var members = await _memberService.GetByDiscordIds(discordIds);

        var entries = extraGuesses.Select(g =>
        {
            var member = members.FirstOrDefault(m => m.DiscordId == g.DiscordId.ToString());
            return new BonusLotteryEntry
            {
                DiscordId = g.DiscordId,
                DiscordName = member?.DiscordName ?? $"Unknown ({g.DiscordId})",
                Reason = g.Reason
            };
        }).ToList();

        var hasWinner = _rng.NextFloat() < 0.2f;

        BonusLotteryEntry? winner = null;
        var winnerIndex = -1;

        if (hasWinner)
        {
            winnerIndex = (int)(_rng.NextFloat() * entries.Count);
            if (winnerIndex >= entries.Count) winnerIndex = entries.Count - 1;
            winner = entries[winnerIndex];
        }

        var result = new BonusLotteryResult
        {
            HasWinner = hasWinner,
            WinnerDiscordId = winner?.DiscordId,
            WinnerName = winner?.DiscordName,
            Prize = prize,
            Entries = entries
        };
        await _bonusLotteryResults.CreateAsync(result);

        if (hasWinner)
        {
            await _discordMessageService.PostInLotteryChannelAsync(
                $"## Bonus Lottery!\nA bonus draw was held for: **{prize}**\nCongratulations to <@{winner!.DiscordId}>!");
        }
        else
        {
            await _discordMessageService.PostInLotteryChannelAsync(
                $"## Bonus Lottery!\nA bonus draw was held for: **{prize}**\nThe wheel of fortune says... no winner this time! Better luck next draw.");
        }

        return new BonusLotteryDrawResponse(hasWinner, winner, entries, prize, winnerIndex);
    }

    public async Task<List<BonusLotteryEntry>> GetBonusLotteryEntriesAsync()
    {
        var extraGuesses = await _extraLotteryGuesses.GetAsync();

        if (extraGuesses.Count == 0)
            return [];

        var discordIds = extraGuesses.Select(g => g.DiscordId).Distinct().ToList();
        var members = await _memberService.GetByDiscordIds(discordIds);

        return extraGuesses.Select(g =>
        {
            var member = members.FirstOrDefault(m => m.DiscordId == g.DiscordId.ToString());
            return new BonusLotteryEntry
            {
                DiscordId = g.DiscordId,
                DiscordName = member?.DiscordName ?? $"Unknown ({g.DiscordId})",
                Reason = g.Reason
            };
        }).ToList();
    }

    #endregion
}