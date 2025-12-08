using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Tests.Services.Lottery;

[TestFixture]
public class LotteryResponseFormatterTests
{
    #region FormatGuessResponse Tests

    [Test]
    public void FormatGuessResponse_NotFcMember()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new NotFcMemberGuessResponse()),
            Is.EqualTo("Only FC members can participate in the lottery"));
    }

    [Test]
    public void FormatGuessResponse_OutOfRangeResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new OutOfRangeGuessResponse()),
            Is.EqualTo("You can only pick a number between 1 and 99."));
    }

    [Test]
    public void FormatGuessResponse_AlreadyGuessedNumberResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new AlreadyGuessedNumberGuessResponse(3)),
            Is.EqualTo("You have already guessed 3!"));
    }

    [Test]
    public void FormatGuessResponse_NotCurrentlyGuessedResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new NotCurrentGuessedNumberGuessResponse(3)),
            Is.EqualTo(
                "You have not guessed 3. You need to use a number you have already guessed in order to change it."));
    }

    [Test]
    public void FormatGuessResponse_NoMoreGuessesResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new NoMoreGuessesGuessResponse([3], "3")),
            Is.EqualTo(
                "You don't have any guesses left! Current guesses: 3. You can use `/lottery change` to change an existing guess."));
    }

    [Test]
    public void FormatGuessResponse_SuccessGuessResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new SuccessGuessResponse([3], "3", 3)),
            Is.EqualTo(
                "Your guess for 3 was recorded! Current guesses: 3. You can use `/lottery change` to change an existing guess."));
    }

    [Test]
    public void FormatGuessResponse_RandomGuessTimeoutResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new RandomGuessTimeoutResponse()),
            Is.EqualTo(
                "Picking a number took too long, try again later. If this keeps happening, contact one of the officers"));
    }

    [Test]
    public void FormatGuessResponse_RandomGuessErrorResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new RandomGuessErrorResponse()),
            Is.EqualTo("Something went wrong, try again later. If this keeps happening, let Zahrymm know."));
    }

    [Test]
    public void FormatGuessResponse_ThrowsNotImplementedException()
    {
        Assert.That(() => LotteryResponseFormatter.FormatGuessResponse(new TestGuessResponse()),
            Throws.TypeOf<NotImplementedException>());
    }

    private record TestGuessResponse : IGuessResponse;

    #endregion

    #region FormatChangeGuessResponse Tests

    [Test]
    public void FormatChangeGuessResponse_NotFcMember()
    {
        Assert.That(() => LotteryResponseFormatter.FormatChangeGuessResponse(new NotFcMemberGuessResponse(), 3, 3),
            Is.EqualTo("Only FC members can participate in the lottery"));
    }

    [Test]
    public void FormatChangeGuessResponse_OutOfRangeResponse()
    {
        Assert.That(() => LotteryResponseFormatter.FormatChangeGuessResponse(new OutOfRangeGuessResponse(), 3, 3),
            Is.EqualTo("You can only pick a number between 1 and 99."));
    }

    [Test]
    public void FormatChangeGuessResponse_AlreadyGuessedNumberResponse()
    {
        Assert.That(
            () => LotteryResponseFormatter.FormatChangeGuessResponse(new AlreadyGuessedNumberGuessResponse(3), 3, 3),
            Is.EqualTo("You have already guessed 3."));
    }

    [Test]
    public void FormatChangeGuessResponse_NotCurrentlyGuessedResponse()
    {
        Assert.That(
            () => LotteryResponseFormatter.FormatChangeGuessResponse(new NotCurrentGuessedNumberGuessResponse(3), 3, 3),
            Is.EqualTo(
                "You have not guessed 3. You need to use a number you have already guessed in order to change it."));
    }

    [Test]
    public void FormatChangeGuessResponse_SuccessGuessResponse()
    {
        Assert.That(
            () => LotteryResponseFormatter.FormatChangeGuessResponse(new SuccessGuessResponse([3], "3", 3), 3, 3),
            Is.EqualTo(
                "Your guess for 3 was changed to 3! Current guesses: 3. You can use `/lottery change` to change an existing guess."));
    }

    [Test]
    public void FormatChangeGuessResponse_ThrowsNotImplementedException()
    {
        Assert.That(() => LotteryResponseFormatter.FormatChangeGuessResponse(new TestGuessResponse(), 3, 3),
            Throws.TypeOf<NotImplementedException>());
    }

    #endregion

    #region FormatViewResponse Tests

    [Test]
    public void FormatViewResponse_NotFcMember()
    {
        var response = new NotFcMemberViewResponse();
        var result = LotteryResponseFormatter.FormatViewResponse(response);

        Assert.That(result, Is.EqualTo("Only FC members can participate in the lottery"));
    }

    [Test]
    public void FormatViewResponse_NoGuesses()
    {
        var remainingMessage = "You have 1 guess remaining";
        var response = new ViewResponse(new List<int>(), 0, 1, remainingMessage);
        var result = LotteryResponseFormatter.FormatViewResponse(response);

        Assert.That(result, Is.EqualTo(remainingMessage));
    }

    [Test]
    public void FormatViewResponse_WithGuesses()
    {
        var remainingMessage = "You have 1 guess remaining";
        var response = new ViewResponse(new List<int> { 1 }, 1, 2, remainingMessage);
        var result = LotteryResponseFormatter.FormatViewResponse(response);

        Assert.That(result, Is.EqualTo($"Current guesses: 1. {remainingMessage}"));
    }

    [Test]
    public void FormatViewResponse_ThrowsNotImplementedException()
    {
        Assert.That(() => LotteryResponseFormatter.FormatViewResponse(new TestViewResponse()),
            Throws.TypeOf<NotImplementedException>());
    }

    private record TestViewResponse : IViewResponse;

    #endregion

    #region FormatWhoGuessResponse Tests

    [Test]
    public void FormatWhoGuessedResponse_NobodyGuessed()
    {
        var randomNumber = Random.Shared.Next(1, 99);
        var response = new WhoGuessedResponse(randomNumber, new List<LotteryUser>());
        var result = LotteryResponseFormatter.FormatWhoGuessedResponse(response);

        Assert.That(result, Is.EqualTo($"Nobody has guessed {randomNumber}."));
    }

    [Test]
    public void FormatWhoGuessedResponse_OnUserGuessed()
    {
        var randomNumber = Random.Shared.Next(1, 99);
        var response = new WhoGuessedResponse(randomNumber, new List<LotteryUser>
        {
            new(1234, "Test")
        });
        var result = LotteryResponseFormatter.FormatWhoGuessedResponse(response);

        Assert.That(result, Is.EqualTo($"<@1234> has guessed {randomNumber}."));
    }

    [Test]
    public void FormatWhoGuessedResponse_MultipleUsersGuessed()
    {
        var randomNumber = Random.Shared.Next(1, 99);
        var response = new WhoGuessedResponse(randomNumber, new List<LotteryUser>
        {
            new(1234, "Test 1"),
            new(5678, "Test 2")
        });
        var result = LotteryResponseFormatter.FormatWhoGuessedResponse(response);

        Assert.That(result, Is.EqualTo($"<@1234>, <@5678> have all guessed {randomNumber}."));
    }

    #endregion

    #region FormatUnusedNumbersResponse Tests

    [Test]
    public void FormatUnusedNumbersResponse_NoUsedNumbers()
    {
        var result =
            LotteryResponseFormatter.FormatUnusedNumbersResponse(new UnusedNumbersResponse(new List<int>(),
                Enumerable.Range(1, 99).ToList()));

        var chunks = result.Split(Environment.NewLine);

        Assert.That(chunks.Length, Is.EqualTo(10));

        foreach (var chunk in chunks)
        {
            var numbers = chunk.Trim().Split(" ");

            Assert.That(numbers.Length, chunk.Contains("01") ? Is.EqualTo(9) : Is.EqualTo(10));
        }
    }

    [Test]
    public void FormatUnusedNumbersResponse_WithUsedNumbers()
    {
        var result =
            LotteryResponseFormatter.FormatUnusedNumbersResponse(
                new UnusedNumbersResponse(new List<int>
                {
                    33
                }, Enumerable.Range(1, 99).ToList()));

        Assert.That(result, Does.Not.Contain("33"));
    }

    [Test]
    public void FormatUnusedNumbersResponse_NoUnusedNumbers()
    {
        var result =
            LotteryResponseFormatter.FormatUnusedNumbersResponse(
                new UnusedNumbersResponse(Enumerable.Range(1, 99).ToList(), Enumerable.Range(1, 99).ToList()));
        Assert.That(result.Select(x => x == '_').Where(x => x).ToList().Count, Is.EqualTo(198));
    }

    #endregion
}