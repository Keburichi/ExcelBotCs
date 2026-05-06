using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

public static class LotteryResponseFormatter
{
    public static string FormatGuessResponse(IGuessResponse response, int remainingGuesses = 0)
    {
        return response switch
        {
            NotFcMemberGuessResponse => "Only FC members can participate in the lottery",
            OutOfRangeGuessResponse => "You can only pick a number between 1 and 99.",
            AlreadyGuessedNumberGuessResponse r => $"You have already guessed {r.Number}!",
            NotCurrentGuessedNumberGuessResponse r =>
                $"You have not guessed {r.Number}. You need to use a number you have already guessed in order to change it.",
            NoMoreGuessesGuessResponse r =>
                $"You don't have any guesses left! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
            SuccessGuessResponse r =>
                $"Your guess for {r.Number} was recorded! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
            RandomGuessTimeoutResponse => "Picking a number took too long, try again later. If this keeps happening, contact one of the officers",
            RandomGuessErrorResponse => "Something went wrong, try again later. If this keeps happening, let Zahrymm know.",
            _ => throw new NotImplementedException()
        };
    }

    public static string FormatChangeGuessResponse(IGuessResponse response, int oldNumber, int newNumber)
    {
        return response switch
        {
            NotFcMemberGuessResponse => "Only FC members can participate in the lottery",
            OutOfRangeGuessResponse => "You can only pick a number between 1 and 99.",
            AlreadyGuessedNumberGuessResponse => $"You have already guessed {newNumber}.",
            NotCurrentGuessedNumberGuessResponse =>
                $"You have not guessed {oldNumber}. You need to use a number you have already guessed in order to change it.",
            SuccessGuessResponse r =>
                $"Your guess for {oldNumber} was changed to {newNumber}! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
            _ => throw new NotImplementedException()
        };
    }

    public static string FormatViewResponse(IViewResponse response)
    {
        return response switch
        {
            NotFcMemberViewResponse => "Only FC members can participate in the lottery",
            ViewResponse r => r.CurrentGuesses.Count == 0
                ? r.RemainingMessage
                : $"Current guesses: {string.Join(", ", r.CurrentGuesses)}. {r.RemainingMessage}",
            _ => throw new NotImplementedException()
        };
    }

    public static string FormatWhoGuessedResponse(WhoGuessedResponse response)
    {
        return response.Users.Count switch
        {
            0 => $"Nobody has guessed {response.Number}.",
            1 => $"<@{response.Users[0].DiscordId}> has guessed {response.Number}.",
            _ => $"{string.Join(", ", response.Users.Select(u => $"<@{u.DiscordId}>"))} have all guessed {response.Number}."
        };
    }

    public static string FormatUnusedNumbersResponse(UnusedNumbersResponse response)
    {
        var formattedNumbers = (List<string>)
            ["  ", .. Enumerable.Range(1, 99).Select(num => response.UsedNumbers.Contains(num) ? "__" : num.ToString("D2"))];
        return string.Join(Environment.NewLine,
            formattedNumbers.Chunk(10).Select(subList => string.Join(' ', subList)));
    }
}
