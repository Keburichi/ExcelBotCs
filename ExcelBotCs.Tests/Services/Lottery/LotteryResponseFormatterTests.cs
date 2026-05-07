using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Tests.Services.Lottery;

public class LotteryResponseFormatterTests
{
    #region FormatGuessResponse Tests

    [Fact]
    public void FormatGuessResponse_NotFcMember()
    {
        LotteryResponseFormatter.FormatGuessResponse(new NotFcMemberGuessResponse()).ShouldBe("Only FC members can participate in the lottery");
    }

    [Fact]
    public void FormatGuessResponse_OutOfRangeResponse()
    {
        LotteryResponseFormatter.FormatGuessResponse(new OutOfRangeGuessResponse()).ShouldBe("You can only pick a number between 1 and 99.");
    }

    [Fact]
    public void FormatGuessResponse_AlreadyGuessedNumberResponse()
    {
        LotteryResponseFormatter.FormatGuessResponse(new AlreadyGuessedNumberGuessResponse(3)).ShouldBe("You have already guessed 3!");
    }

    [Fact]
    public void FormatGuessResponse_NotCurrentlyGuessedResponse()
    {
        LotteryResponseFormatter.FormatGuessResponse(new NotCurrentGuessedNumberGuessResponse(3)).ShouldBe(
                "You have not guessed 3. You need to use a number you have already guessed in order to change it.");
    }

    [Fact]
    public void FormatGuessResponse_NoMoreGuessesResponse()
    {
        LotteryResponseFormatter.FormatGuessResponse(new NoMoreGuessesGuessResponse([3], "3")).ShouldBe(
                "You don't have any guesses left! Current guesses: 3. You can use `/lottery change` to change an existing guess.");
    }

    [Fact]
    public void FormatGuessResponse_SuccessGuessResponse()
    {
        LotteryResponseFormatter.FormatGuessResponse(new SuccessGuessResponse([3], "3", 3)).ShouldBe(
                "Your guess for 3 was recorded! Current guesses: 3. You can use `/lottery change` to change an existing guess.");
    }

    [Fact]
    public void FormatGuessResponse_RandomGuessTimeoutResponse()
    {
        LotteryResponseFormatter.FormatGuessResponse(new RandomGuessTimeoutResponse()).ShouldBe(
                "Picking a number took too long, try again later. If this keeps happening, contact one of the officers");
    }

    [Fact]
    public void FormatGuessResponse_RandomGuessErrorResponse()
    {
        LotteryResponseFormatter.FormatGuessResponse(new RandomGuessErrorResponse()).ShouldBe("Something went wrong, try again later. If this keeps happening, let Zahrymm know.");
    }

    [Fact]
    public void FormatGuessResponse_ThrowsNotImplementedException()
    {
        Should.Throw<NotImplementedException>(() =>
            LotteryResponseFormatter.FormatGuessResponse(new TestGuessResponse()));
    }

    private record TestGuessResponse : IGuessResponse;

    #endregion

    #region FormatChangeGuessResponse Tests

    [Fact]
    public void FormatChangeGuessResponse_NotFcMember()
    {
        LotteryResponseFormatter.FormatChangeGuessResponse(new NotFcMemberGuessResponse(), 3, 3).ShouldBe("Only FC members can participate in the lottery");
    }

    [Fact]
    public void FormatChangeGuessResponse_OutOfRangeResponse()
    {
        LotteryResponseFormatter.FormatChangeGuessResponse(new OutOfRangeGuessResponse(), 3, 3).ShouldBe("You can only pick a number between 1 and 99.");
    }

    [Fact]
    public void FormatChangeGuessResponse_AlreadyGuessedNumberResponse()
    {
        LotteryResponseFormatter.FormatChangeGuessResponse(new AlreadyGuessedNumberGuessResponse(3), 3, 3).ShouldBe("You have already guessed 3.");
    }

    [Fact]
    public void FormatChangeGuessResponse_NotCurrentlyGuessedResponse()
    {
        LotteryResponseFormatter.FormatChangeGuessResponse(new NotCurrentGuessedNumberGuessResponse(3), 3, 3).ShouldBe(
                "You have not guessed 3. You need to use a number you have already guessed in order to change it.");
    }

    [Fact]
    public void FormatChangeGuessResponse_SuccessGuessResponse()
    {
        LotteryResponseFormatter.FormatChangeGuessResponse(new SuccessGuessResponse([3], "3", 3), 3, 3).ShouldBe(
                "Your guess for 3 was changed to 3! Current guesses: 3. You can use `/lottery change` to change an existing guess.");
    }

    [Fact]
    public void FormatChangeGuessResponse_ThrowsNotImplementedException()
    {
        Should.Throw<NotImplementedException>(() =>
            LotteryResponseFormatter.FormatChangeGuessResponse(new TestGuessResponse(), 3, 3));
    }

    #endregion

    #region FormatViewResponse Tests

    [Fact]
    public void FormatViewResponse_NotFcMember()
    {
        var response = new NotFcMemberViewResponse();
        var result = LotteryResponseFormatter.FormatViewResponse(response);

        result.ShouldBe("Only FC members can participate in the lottery");
    }

    [Fact]
    public void FormatViewResponse_NoGuesses()
    {
        var remainingMessage = "You have 1 guess remaining";
        var response = new ViewResponse(new List<int>(), 0, 1, remainingMessage);
        var result = LotteryResponseFormatter.FormatViewResponse(response);

        result.ShouldBe(remainingMessage);
    }

    [Fact]
    public void FormatViewResponse_WithGuesses()
    {
        var remainingMessage = "You have 1 guess remaining";
        var response = new ViewResponse(new List<int> { 1 }, 1, 2, remainingMessage);
        var result = LotteryResponseFormatter.FormatViewResponse(response);

        result.ShouldBe($"Current guesses: 1. {remainingMessage}");
    }

    [Fact]
    public void FormatViewResponse_ThrowsNotImplementedException()
    {
        Should.Throw<NotImplementedException>(() => LotteryResponseFormatter.FormatViewResponse(new TestViewResponse()));
    }

    private record TestViewResponse : IViewResponse;

    #endregion

    #region FormatWhoGuessResponse Tests

    [Fact]
    public void FormatWhoGuessedResponse_NobodyGuessed()
    {
        var randomNumber = Random.Shared.Next(1, 99);
        var response = new WhoGuessedResponse(randomNumber, new List<LotteryUser>());
        var result = LotteryResponseFormatter.FormatWhoGuessedResponse(response);

        result.ShouldBe($"Nobody has guessed {randomNumber}.");
    }

    [Fact]
    public void FormatWhoGuessedResponse_OnUserGuessed()
    {
        var randomNumber = Random.Shared.Next(1, 99);
        var response = new WhoGuessedResponse(randomNumber, new List<LotteryUser>
        {
            new(1234, "Test")
        });
        var result = LotteryResponseFormatter.FormatWhoGuessedResponse(response);

        result.ShouldBe($"<@1234> has guessed {randomNumber}.");
    }

    [Fact]
    public void FormatWhoGuessedResponse_MultipleUsersGuessed()
    {
        var randomNumber = Random.Shared.Next(1, 99);
        var response = new WhoGuessedResponse(randomNumber, new List<LotteryUser>
        {
            new(1234, "Test 1"),
            new(5678, "Test 2")
        });
        var result = LotteryResponseFormatter.FormatWhoGuessedResponse(response);

        result.ShouldBe($"<@1234>, <@5678> have all guessed {randomNumber}.");
    }

    #endregion

    #region FormatUnusedNumbersResponse Tests

    [Fact]
    public void FormatUnusedNumbersResponse_NoUsedNumbers()
    {
        var result =
            LotteryResponseFormatter.FormatUnusedNumbersResponse(new UnusedNumbersResponse(new List<int>(),
                Enumerable.Range(1, 99).ToList()));

        var chunks = result.Split(Environment.NewLine);

        chunks.Length.ShouldBe(10);

        foreach (var chunk in chunks)
        {
            var numbers = chunk.Trim().Split(" ");

            numbers.Length.ShouldBe(chunk.Contains("01") ? 9 : 10);
        }
    }

    [Fact]
    public void FormatUnusedNumbersResponse_WithUsedNumbers()
    {
        var result =
            LotteryResponseFormatter.FormatUnusedNumbersResponse(
                new UnusedNumbersResponse(new List<int>
                {
                    33
                }, Enumerable.Range(1, 99).ToList()));

        result.ShouldNotContain("33");
    }

    [Fact]
    public void FormatUnusedNumbersResponse_NoUnusedNumbers()
    {
        var result =
            LotteryResponseFormatter.FormatUnusedNumbersResponse(
                new UnusedNumbersResponse(Enumerable.Range(1, 99).ToList(), Enumerable.Range(1, 99).ToList()));
        result.Select(x => x == '_').Where(x => x).ToList().Count.ShouldBe(198);
    }

    #endregion
}