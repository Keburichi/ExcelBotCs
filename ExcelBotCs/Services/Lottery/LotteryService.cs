using ExcelBotCs.Data;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Enums;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using MongoDB.Driver;

namespace ExcelBotCs.Services.Lottery;

public class LotteryService : ILotteryService
{
    private readonly Prng _rng;
    private readonly IMemberService _memberService;
    private readonly IDiscordMessageService _discordMessageService;
    private readonly Repository<ExtraLotteryGuess> _extraLotteryGuesses;
    private readonly Repository<LotteryGuess> _lotteryGuesses;
    private readonly Repository<LotteryResult> _lotteryResults;

    public LotteryService(Prng rng, Data.Database database,
        IMemberService memberService, IDiscordMessageService discordMessageService)
    {
        _rng = rng;
        _memberService = memberService;
        _discordMessageService = discordMessageService;
        _extraLotteryGuesses = database.GetCollection<ExtraLotteryGuess>("extra_lottery_guesses");
        _lotteryGuesses = database.GetCollection<LotteryGuess>("lottery_guesses");
        _lotteryResults = database.GetCollection<LotteryResult>("lottery_results");
    }

    public async Task<string> GuessAsync(ulong discordUserId, int number)
    {
        var result = await TryGuess(discordUserId, number);

        if (result is SuccessGuessResponse success)
        {
            await InsertGuessAsync(discordUserId, number);
            await _discordMessageService.PostInLotteryChannelAsync(
                $"<@{discordUserId}> guessed {number}. Current guesses: {success.PrettyCurrentGuesses}");
        }

        return result switch
        {
            NotFcMemberGuessResponse _ => "Only FC members can participate in the lottery",
            OutOfRangeGuessResponse _ => "You can only pick a number between 1 and 99.",
            AlreadyGuessedNumberGuessResponse _ => $"You have already guessed {number}!",
            NoMoreGuessesGuessResponse r =>
                $"You don't have any guesses left! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
            SuccessGuessResponse r =>
                $"Your guess for {r.Number} was recorded! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess. {(await GetRemainingGuessesAsync(discordUserId)).Output}",
            _ => throw new NotImplementedException()
        };
    }

    public async Task<string> GetUnusedNumbersAsync()
    {
        var guessedNumbers = await GetGuessedNumbersAsync();
        var formattedNumbers = (List<string>)
            ["  ", .. Enumerable.Range(1, 99).Select(num => guessedNumbers.Contains(num) ? "__" : num.ToString("D2"))];
        return string.Join('\n', formattedNumbers.Chunk(10).Select(subList => string.Join(' ', subList)));
    }

    public async Task<string> RandomGuessAsync(ulong discordUserId, CancellationTokenSource cts,
        RandomGuessType numberPool = RandomGuessType.UnusedOnly)
    {
        var task = TryRandomGuessAsync(discordUserId, cts.Token, numberPool);

        if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token)) == task)
        {
            await cts.CancelAsync();
            var result = await task;

            if (result is SuccessGuessResponse success)
            {
                await InsertGuessAsync(discordUserId, success.Number);
                await _discordMessageService.PostInLotteryChannelAsync(
                    $"<@{discordUserId}> used a random draw and got {success.Number}. Current guesses: {success.PrettyCurrentGuesses}");
            }

            return result switch
            {
                SuccessGuessResponse guessResponse =>
                    $"Your guess for {guessResponse.Number} was recorded! Current guesses: {guessResponse.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
                NotFcMemberGuessResponse => "Only FC members can participate in the lottery",
                NoMoreGuessesGuessResponse r =>
                    $"You don't have any guesses left! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess. {(await GetRemainingGuessesAsync(discordUserId)).Output}",
                OutOfRangeGuessResponse => "This number pool has no valid numbers to use, try another number pool.",
                _ => "Something went wrong, try again later. If this keeps happening, let Zahrymm know."
            };
        }
        else
        {
            await cts.CancelAsync();
            return
                "Picking a number took too long, try again later. If this keeps happening, contact one of the officers";
        }
    }

    public async Task<string> ChangeGuessAsync(ulong discordUserId, int old, int @new)
    {
        var result = await TryChangeGuessAsync(discordUserId, old, @new);

        if (result is SuccessGuessResponse success)
        {
            await ChangeGuess(discordUserId, old, @new);
            await _discordMessageService.PostInLotteryChannelAsync(
                $"<@{discordUserId}> changed a guess from {old} to {@new}. Current guesses: {success.PrettyCurrentGuesses}");
        }

        return result switch
        {
            NotFcMemberGuessResponse _ => "Only FC members can participate in the lottery",
            OutOfRangeGuessResponse _ => "You can only pick a number between 1 and 99.",
            AlreadyGuessedNumberGuessResponse _ => $"You have already guessed {@new}.",
            NotCurrentGuessedNumberGuessResponse _ =>
                $"You have not guessed {old}. You need to use a number you have already guessed in order to change it.",
            SuccessGuessResponse r =>
                $"Your guess for {old} was changed to {@new}! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
            _ => throw new NotImplementedException()
        };
    }

    public async Task<string> WhoGuessedAsync(int number)
    {
        var currentGuesses = await _lotteryGuesses
            .Where(guess => guess.Number == number)
            .ToListAsync();

        return currentGuesses.Count switch
        {
            0 => $"Nobody has guessed {number}.",
            1 =>
                $"{currentGuesses.Select(user => $"<@{user.DiscordId}>").ToList().PrettyJoin()} has guessed {number}.",
            _ =>
                $"{currentGuesses.Select(user => $"<@{user.DiscordId}>").ToList().PrettyJoin()} have all guessed {number}."
        };
    }

    public async Task<string> ViewAsync(ulong discordUserId)
    {
        if(! await CanParticipateAsync(discordUserId))
            return "Only FC members can participate in the lottery";
        
        var (currentGuesses, displayAmount) = await GetRemainingGuessesAsync(discordUserId);

        if (currentGuesses.Count == 0)
            return displayAmount;
        
        var guesses = currentGuesses.Select(guess => guess.Number).ToList();
        guesses.Sort();
        var displayedGuesses = guesses.Select(guess => guess.ToString()).ToList().PrettyJoin();

        return $"Current guesses: {displayedGuesses}. {displayAmount}";
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

        var allResults = await _lotteryGuesses.Where(_ => true).ToListAsync();
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
        var fcMembers = await _memberService.GetFcMembers();
        var currentGuesses = (await _lotteryGuesses.Where(_ => true).ToListAsync()).Select(guess => guess.DiscordId)
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());
        var awardedGuesses = (await _extraLotteryGuesses.Where(_ => true).ToListAsync())
            .Select(award => award.DiscordId)
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());

        foreach (var fcMember in fcMembers)
            currentGuesses.TryAdd(ulong.Parse(fcMember.DiscordId), 0);

        var remainingGuesses = new List<(ulong Id, int Remaining)>();

        foreach (var (id, current) in currentGuesses)
        {
            if (awardedGuesses.TryGetValue(id, out var remaining))
            {
                var count = (remaining + 1) - current;
                if (count > 0)
                    remainingGuesses.Add((id, count));
            }
            else
            {
                if (current == 0)
                    remainingGuesses.Add((id, 1));
            }
        }

        var output = remainingGuesses
            .GroupBy(x => x.Remaining)
            .OrderBy(x => x.Key)
            .Aggregate(string.Empty,
                (current, guesses)
                    => current + $"{guesses.Key} guess{(guesses.Key == 1 ? "" : "es")} remaining: {guesses.Select(user => $"<@{user.Id}>").ToList().PrettyJoin()}\n");

        // TODO: Figure out what exactly this is being used for
        // await FollowupAsync(output, ephemeral: true);

        var previousParticipants = (await _lotteryResults.Where(_ => true).ToListAsync())
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
                    => current + $"{guesses.Key} guess{(guesses.Key == 1 ? "" : "es")} remaining: {guesses.Select(user => $"<@{user.Id}>").ToList().PrettyJoin()}\n");

        await _discordMessageService.PostInLotteryChannelAsync(intersectionOutput);
    }

    private async Task SaveGuesses(int winningNumber, List<LotteryGuess> allResults)
    {
        var result = new LotteryResult()
        {
            WinningNumber = winningNumber,
            Guesses = allResults
        };

        await _lotteryResults.Insert(result);
    }

    private async Task Flush()
    {
        await _lotteryGuesses.DeleteAll();
        await _extraLotteryGuesses.DeleteAll();
    }

    private async Task<List<int>> GetGuessedNumbersAsync()
    {
        return (await _lotteryGuesses.Where(_ => true)
                .ToListAsync())
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
        var currentGuesses = await _lotteryGuesses
            .Where(guess => guess.DiscordId == discordUserId)
            .ToListAsync();

        var extraAwardedGuesses = await _extraLotteryGuesses
            .Where(guess => guess.DiscordId == discordUserId)
            .ToListAsync();

        var currentGuessAmount = currentGuesses.Count;
        var allowedGuessAmount = 1 + extraAwardedGuesses.Count;
        var displayAmount = allowedGuessAmount == 1
            ? (currentGuessAmount == 1
                ? $"You have used your guess."
                : $"You have not used your guess.")
            : $"You have used {currentGuessAmount} of your {allowedGuessAmount} guesses.";

        return (currentGuesses, displayAmount);
    }

    private async Task InsertGuessAsync(ulong discordUserId, int number)
        => await _lotteryGuesses.Insert(new LotteryGuess()
        {
            DiscordId = discordUserId, Number = number
        });

    private async Task<bool> CanParticipateAsync(ulong discordUserId)
    {
        var user = await _memberService.GetByDiscordId(discordUserId);
        return user.IsMember != null && user.IsMember.Value;
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
            if (ctx.IsCancellationRequested)
                return new AlreadyGuessedNumberGuessResponse(0);

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

        var currentGuesses = await _lotteryGuesses
            .Where(guess => guess.DiscordId == discordUserId)
            .ToListAsync();

        if (currentGuesses.Any(guess => guess.Number == number))
            return new AlreadyGuessedNumberGuessResponse(number);

        var extraAwardedGuesses = await _extraLotteryGuesses
            .Where(guess => guess.DiscordId == discordUserId)
            .ToListAsync();

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

        var currentGuesses = await _lotteryGuesses
            .Where(guess => guess.DiscordId == discordUserId)
            .ToListAsync();

        if (currentGuesses.All(guess => guess.Number != oldNumber))
            return new NotCurrentGuessedNumberGuessResponse(oldNumber);

        if (currentGuesses.Any(guess => guess.Number == newNumber))
            return new AlreadyGuessedNumberGuessResponse(newNumber);

        currentGuesses.RemoveAll(guess => guess.Number == oldNumber);

        var numbers = ((List<int>)[newNumber, .. currentGuesses.Select(guess => guess.Number)]).ToList();
        numbers.Sort();
        var prettyNumbers = numbers.Select(guess => guess.ToString()).ToList().PrettyJoin();

        return new SuccessGuessResponse(numbers, prettyNumbers, newNumber);
    }

    private async Task ChangeGuess(ulong discordUserId, int oldNumber, int newNumber)
    {
        await _lotteryGuesses.Delete(guess => guess.DiscordId == discordUserId && guess.Number == oldNumber);
        await _lotteryGuesses.Insert(new LotteryGuess() { DiscordId = discordUserId, Number = newNumber });
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
        {
            await _extraLotteryGuesses.Insert(new ExtraLotteryGuess()
                { DiscordId = userId, Reason = success.Reason });
        }

        if (success.DiscordUserIds.Count() == 1)
        {
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{success.PrettyUsersAwarded} has been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
        }
        else
        {
            await _discordMessageService.PostInLotteryChannelAsync(
                $"{success.PrettyUsersAwarded} have all been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
        }
    }

    #endregion
}