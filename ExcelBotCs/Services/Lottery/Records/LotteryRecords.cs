using ExcelBotCs.Services.Lottery.Interfaces;

namespace ExcelBotCs.Services.Lottery.Records;

#region Guess Responses

public record SuccessGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses, int Number)
    : IGuessResponse;

public record OutOfRangeGuessResponse : IGuessResponse;

public record NotFcMemberGuessResponse : IGuessResponse;

public record AlreadyGuessedNumberGuessResponse(int Number) : IGuessResponse;

public record NotCurrentGuessedNumberGuessResponse(int Number) : IGuessResponse;

public record NoMoreGuessesGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses)
    : IGuessResponse;

public record RandomGuessTimeoutResponse : IGuessResponse;

public record RandomGuessErrorResponse : IGuessResponse;

#endregion

#region Award Responses

public record NoUsersAwardResponse() : IAwardResponse;

public record SuccessAwardResponse(IEnumerable<ulong> DiscordUserIds, string PrettyUsersAwarded, string Reason)
    : IAwardResponse;

#endregion

#region View Responses

public interface IViewResponse { }

public record NotFcMemberViewResponse() : IViewResponse;

public record ViewResponse(List<int> CurrentGuesses, int UsedGuesses, int TotalGuesses, string RemainingMessage)
    : IViewResponse;

#endregion

#region Who Guessed Responses

public record LotteryUser(ulong DiscordId, string DiscordName);

public record WhoGuessedResponse(int Number, List<LotteryUser> Users);

#endregion

#region Unused Numbers Response

public record UnusedNumbersResponse(List<int> UsedNumbers, List<int> UnusedNumbers);

#endregion