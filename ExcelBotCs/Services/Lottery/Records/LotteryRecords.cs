using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;

namespace ExcelBotCs.Services.Lottery.Records;

#region Guess Responses

record SuccessGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses, int Number)
    : IGuessResponse;

record OutOfRangeGuessResponse : IGuessResponse;

record NotFcMemberGuessResponse : IGuessResponse;

record AlreadyGuessedNumberGuessResponse(int Number) : IGuessResponse;

record NotCurrentGuessedNumberGuessResponse(int Number) : IGuessResponse;

record NoMoreGuessesGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses)
    : IGuessResponse;

#endregion

#region Award Responses

public record SuccessAwardResponse(IEnumerable<ulong> DiscordUserIds, string PrettyUsersAwarded, string Reason)
    : IAwardResponse;

public record NoUsersAwardResponse() : IAwardResponse;

#endregion