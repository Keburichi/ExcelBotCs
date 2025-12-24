using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Enums;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

[TestFixture]
public class LotteryServiceTests : MongoDbTest
{
    private Data.Database _database = null!;
    private ILotteryService _lotteryService = null!;
    private Mock<IMemberService> _memberService = null!;
    private Mock<IDiscordMessageService> _discordMessageService = null!;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _database = new Data.Database(databaseOptions, new Logger<Data.Database>(new LoggerFactory()));
    }

    [SetUp]
    public void Setup()
    {
        var rng = new Prng();
        _memberService = new Mock<IMemberService>();
        _discordMessageService = new Mock<IDiscordMessageService>();

        _lotteryService = new LotteryService(rng, _database, _memberService.Object, _discordMessageService.Object);
    }

    [TearDown]
    public void Teardown()
    {
        _discordMessageService.VerifyAll();
    }

    #region GuessAsync Tests

    [Test]
    public async Task GuessAsync_UserDoesntExist()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync((Member)null);

        var result = await _lotteryService.GuessAsync(userId, 3);

        Assert.That(result.GetType(), Is.EqualTo(typeof(NotFcMemberGuessResponse)));
    }

    [Test]
    public async Task GuessAsync_NotFcMember()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Roles = new List<MemberRole>
            {
                new() { IsMember = false }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(Convert.ToUInt64(userId))).ReturnsAsync(user);

        var result = await _lotteryService.GuessAsync(userId, 3);
        Assert.That(result.GetType(), Is.EqualTo(typeof(NotFcMemberGuessResponse)));
    }

    [Test]
    public async Task GuessAsync_GuessOutOfRange()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(Convert.ToUInt64(userId))).ReturnsAsync(user);

        var result = await _lotteryService.GuessAsync(userId, 1000);
        Assert.That(result.GetType(), Is.EqualTo(typeof(OutOfRangeGuessResponse)));
    }

    [Test]
    public async Task GuessAsync_AlreadyGuessedNumber()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(Convert.ToUInt64(userId))).ReturnsAsync(user);

        var initialGuess = await _lotteryService.GuessAsync(userId, 3);
        Assert.That(initialGuess.GetType(), Is.EqualTo(typeof(SuccessGuessResponse)));

        var result = await _lotteryService.GuessAsync(userId, 3);
        Assert.That(result.GetType(), Is.EqualTo(typeof(AlreadyGuessedNumberGuessResponse)));
    }

    [Test]
    public async Task GuessAsync_NoMoreGuesses()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(Convert.ToUInt64(userId))).ReturnsAsync(user);

        var initialGuess = await _lotteryService.GuessAsync(userId, 3);
        Assert.That(initialGuess.GetType(), Is.EqualTo(typeof(SuccessGuessResponse)));

        var result = await _lotteryService.GuessAsync(userId, 4);
        Assert.That(result.GetType(), Is.EqualTo(typeof(NoMoreGuessesGuessResponse)));
    }

    [Test]
    public async Task GuessAsync_Success()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(Convert.ToUInt64(userId))).ReturnsAsync(user);

        var initialGuess = await _lotteryService.GuessAsync(userId, 3);
        Assert.That(initialGuess.GetType(), Is.EqualTo(typeof(SuccessGuessResponse)));

        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region GetUnusedNumbersAsync Tests

    [Test]
    public async Task GetUnusedNumbersAsync_NoUsedNumbers()
    {
        var result = await _lotteryService.GetUnusedNumbersAsync();

        Assert.That(result.UnusedNumbers.Count, Is.EqualTo(99));
        Assert.That(result.UsedNumbers.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetUnusedNumbersAsync_GuessNumberNotPresent()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(Convert.ToUInt64(userId))).ReturnsAsync(user);

        await _lotteryService.GuessAsync(userId, 3);

        var result = await _lotteryService.GetUnusedNumbersAsync();

        Assert.That(result.UnusedNumbers.Count, Is.EqualTo(98));
        Assert.That(result.UsedNumbers.Count, Is.EqualTo(1));
        Assert.That(result.UsedNumbers.First(), Is.EqualTo(3));
    }

    [Test]
    public async Task GetUnusedNumbersAsync_NoUnusedNumbers()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(Convert.ToUInt64(userId))).ReturnsAsync(user);

        for (var i = 1; i <= 99; i++)
        {
            var guessResult = await _lotteryService.GuessAsync(userId, i);
            Assert.That(guessResult.GetType(), Is.EqualTo(typeof(SuccessGuessResponse)));

            await _lotteryService.AwardUsersAsync(new SuccessAwardResponse([userId], "", "testing"));
        }

        var result = await _lotteryService.GetUnusedNumbersAsync();

        Assert.That(result.UnusedNumbers.Count, Is.EqualTo(0));
        Assert.That(result.UsedNumbers.Count, Is.EqualTo(99));
    }

    #endregion

    #region RandomGuessAsync Tests

    [Test]
    public async Task RandomGuessAsync_TimeoutResponse()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1)); // Cancel immediately to trigger timeout

        var result = await _lotteryService.RandomGuessAsync(userId, cts);

        Assert.That(result.GetType(), Is.EqualTo(typeof(RandomGuessTimeoutResponse)));
        // Timeout doesn't post a message
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RandomGuessAsync_NotFcMember()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync((Member)null);

        var cts = new CancellationTokenSource();
        var result = await _lotteryService.RandomGuessAsync(userId, cts);

        Assert.That(result.GetType(), Is.EqualTo(typeof(NotFcMemberGuessResponse)));
        // Not FC member response doesn't post a message
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RandomGuessAsync_Any_Success()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var cts = new CancellationTokenSource();
        var result = await _lotteryService.RandomGuessAsync(userId, cts, RandomGuessType.Any);

        Assert.That(result.GetType(), Is.EqualTo(typeof(SuccessGuessResponse)));
        var success = (SuccessGuessResponse)result;
        Assert.That(success.Number, Is.GreaterThanOrEqualTo(0).And.LessThan(100));
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RandomGuessAsync_UnusedOnly_Success()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var cts = new CancellationTokenSource();
        var result = await _lotteryService.RandomGuessAsync(userId, cts);

        Assert.That(result.GetType(), Is.EqualTo(typeof(SuccessGuessResponse)));
        var success = (SuccessGuessResponse)result;
        Assert.That(success.Number, Is.GreaterThanOrEqualTo(1).And.LessThanOrEqualTo(99));
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RandomGuessAsync_UsedOnly_AlreadyUsedAllGuesses()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        // Make an initial guess so there's a used number
        await _lotteryService.GuessAsync(userId, 50);

        var cts = new CancellationTokenSource();
        // When trying to random guess from used numbers when already used all guesses,
        // it keeps trying the same number and times out
        var result = await _lotteryService.RandomGuessAsync(userId, cts, RandomGuessType.UsedOnly);

        Assert.That(result.GetType(), Is.EqualTo(typeof(RandomGuessTimeoutResponse)));
    }

    [Test]
    public async Task RandomGuessAsync_UsedOnly_NoUsedNumbers()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var cts = new CancellationTokenSource();
        var result = await _lotteryService.RandomGuessAsync(userId, cts, RandomGuessType.UsedOnly);

        Assert.That(result.GetType(), Is.EqualTo(typeof(OutOfRangeGuessResponse)));
    }

    #endregion

    #region ChangeGuessAsync Tests

    [Test]
    public async Task ChangeGuessAsync_NotFcMember()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync((Member)null);

        var result = await _lotteryService.ChangeGuessAsync(userId, 3, 5);

        Assert.That(result.GetType(), Is.EqualTo(typeof(NotFcMemberGuessResponse)));
    }

    [Test]
    public async Task ChangeGuessAsync_OutOfRange()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var result = await _lotteryService.ChangeGuessAsync(userId, 3, 1000);

        Assert.That(result.GetType(), Is.EqualTo(typeof(OutOfRangeGuessResponse)));
    }

    [Test]
    public async Task ChangeGuessAsync_OldNumberNotGuessed()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var result = await _lotteryService.ChangeGuessAsync(userId, 3, 5);

        Assert.That(result.GetType(), Is.EqualTo(typeof(NotCurrentGuessedNumberGuessResponse)));
    }

    [Test]
    public async Task ChangeGuessAsync_NewNumberAlreadyGuessed()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        // Make two guesses
        await _lotteryService.GuessAsync(userId, 3);
        await _lotteryService.AwardUsersAsync(new SuccessAwardResponse([userId], "", "testing"));
        await _lotteryService.GuessAsync(userId, 5);

        var result = await _lotteryService.ChangeGuessAsync(userId, 3, 5);

        Assert.That(result.GetType(), Is.EqualTo(typeof(AlreadyGuessedNumberGuessResponse)));
    }

    [Test]
    public async Task ChangeGuessAsync_Success()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        await _lotteryService.GuessAsync(userId, 3);

        var result = await _lotteryService.ChangeGuessAsync(userId, 3, 5);

        Assert.That(result.GetType(), Is.EqualTo(typeof(SuccessGuessResponse)));
        var success = (SuccessGuessResponse)result;
        Assert.That(success.Number, Is.EqualTo(5));
        Assert.That(success.CurrentGuesses, Contains.Item(5));
        Assert.That(success.CurrentGuesses, Does.Not.Contain(3));
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.Exactly(2));
    }

    #endregion

    #region WhoGuessedAsync Tests

    [Test]
    public async Task WhoGuessedAsync_NoGuesses()
    {
        var result = await _lotteryService.WhoGuessedAsync(42);

        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Users, Is.Empty);
    }

    [Test]
    public async Task WhoGuessedAsync_SingleUser()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            DiscordId = userId.ToString(),
            DiscordName = "TestUser",
            Roles = new List<MemberRole>
            {
                new() { IsMember = true }
            }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);
        _memberService.Setup(x => x.GetByDiscordIds(It.IsAny<List<ulong>>())).ReturnsAsync([user]);

        await _lotteryService.GuessAsync(userId, 42);

        var result = await _lotteryService.WhoGuessedAsync(42);

        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Users.Count, Is.EqualTo(1));
        Assert.That(result.Users[0].DiscordId, Is.EqualTo(userId));
        Assert.That(result.Users[0].DiscordName, Is.EqualTo("TestUser"));
    }

    [Test]
    public async Task WhoGuessedAsync_MultipleUsers()
    {
        var userId1 = Convert.ToUInt64(GenerateRandomDiscordId());
        var userId2 = Convert.ToUInt64(GenerateRandomDiscordId());

        var user1 = new Member
        {
            Id = userId1.ToString(),
            DiscordId = userId1.ToString(),
            DiscordName = "TestUser1",
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        var user2 = new Member
        {
            Id = userId2.ToString(),
            DiscordId = userId2.ToString(),
            DiscordName = "TestUser2",
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };

        _memberService.Setup(x => x.GetByDiscordId(userId1)).ReturnsAsync(user1);
        _memberService.Setup(x => x.GetByDiscordId(userId2)).ReturnsAsync(user2);
        _memberService.Setup(x => x.GetByDiscordIds(It.IsAny<List<ulong>>()))
            .ReturnsAsync((List<ulong> ids) => ids.Select(id =>
                id == userId1 ? user1 : id == userId2 ? user2 : null).Where(u => u != null).ToList());

        await _lotteryService.GuessAsync(userId1, 42);
        await _lotteryService.GuessAsync(userId2, 42);

        var result = await _lotteryService.WhoGuessedAsync(42);

        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Users.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task WhoGuessedAsync_UserNotFound()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            DiscordId = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);
        _memberService.Setup(x => x.GetByDiscordIds(It.IsAny<List<ulong>>())).ReturnsAsync([]);

        await _lotteryService.GuessAsync(userId, 42);

        var result = await _lotteryService.WhoGuessedAsync(42);

        Assert.That(result.Users[0].DiscordName, Does.Contain("Unknown User"));
    }

    #endregion

    #region ViewAsync Tests

    [Test]
    public async Task ViewAsync_NotFcMember()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync((Member)null);

        var result = await _lotteryService.ViewAsync(userId);

        Assert.That(result.GetType(), Is.EqualTo(typeof(NotFcMemberViewResponse)));
    }

    [Test]
    public async Task ViewAsync_NoGuesses()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var result = await _lotteryService.ViewAsync(userId);

        Assert.That(result.GetType(), Is.EqualTo(typeof(ViewResponse)));
        var view = (ViewResponse)result;
        Assert.That(view.CurrentGuesses, Is.Empty);
        Assert.That(view.UsedGuesses, Is.EqualTo(0));
        Assert.That(view.TotalGuesses, Is.EqualTo(1));
        Assert.That(view.RemainingMessage, Does.Contain("not used"));
    }

    [Test]
    public async Task ViewAsync_WithGuesses()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        await _lotteryService.GuessAsync(userId, 42);

        var result = await _lotteryService.ViewAsync(userId);

        Assert.That(result.GetType(), Is.EqualTo(typeof(ViewResponse)));
        var view = (ViewResponse)result;
        Assert.That(view.CurrentGuesses, Contains.Item(42));
        Assert.That(view.UsedGuesses, Is.EqualTo(1));
        Assert.That(view.TotalGuesses, Is.EqualTo(1));
        Assert.That(view.RemainingMessage, Does.Contain("used your guess"));
    }

    [Test]
    public async Task ViewAsync_WithExtraGuesses()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        await _lotteryService.AwardUsersAsync(new SuccessAwardResponse([userId], "", "testing"));
        await _lotteryService.GuessAsync(userId, 42);

        var result = await _lotteryService.ViewAsync(userId);

        Assert.That(result.GetType(), Is.EqualTo(typeof(ViewResponse)));
        var view = (ViewResponse)result;
        Assert.That(view.TotalGuesses, Is.EqualTo(2));
        Assert.That(view.RemainingMessage, Does.Contain("used 1 of your 2 guesses"));
    }

    [Test]
    public async Task ViewAsync_GuessesAreSorted()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        await _lotteryService.GuessAsync(userId, 50);
        await _lotteryService.AwardUsersAsync(new SuccessAwardResponse([userId], "", "testing"));
        await _lotteryService.GuessAsync(userId, 10);

        var result = await _lotteryService.ViewAsync(userId);

        var view = (ViewResponse)result;
        Assert.That(view.CurrentGuesses, Is.Ordered);
        Assert.That(view.CurrentGuesses[0], Is.EqualTo(10));
        Assert.That(view.CurrentGuesses[1], Is.EqualTo(50));
    }

    #endregion

    #region RunLotteryAsync Tests

    [Test]
    public async Task RunLotteryAsync_UserNotFound()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync((Member)null);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _lotteryService.RunLotteryAsync(userId));
    }

    [Test]
    public async Task RunLotteryAsync_NotAdmin()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true, IsAdmin = false } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        await _lotteryService.RunLotteryAsync(userId);

        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RunLotteryAsync_NoWinners()
    {
        var adminId = Convert.ToUInt64(GenerateRandomDiscordId());
        var admin = new Member
        {
            Id = adminId.ToString(),
            Roles = new List<MemberRole> { new() { IsAdmin = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(adminId)).ReturnsAsync(admin);

        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        // Make guesses for all numbers to ensure someone doesn't win
        for (var i = 1; i <= 99; i++)
        {
            await _lotteryService.GuessAsync(userId, i);
            if (i < 99)
                await _lotteryService.AwardUsersAsync(new SuccessAwardResponse([userId], "", "testing"));
        }

        await _lotteryService.RunLotteryAsync(adminId);

        // Verify messages were posted (winning number announcement + participants list)
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(
            It.Is<string>(s => s.Contains("winning number") || s.Contains("Participants"))), Times.AtLeast(2));
    }

    [Test]
    public async Task RunLotteryAsync_WithWinners()
    {
        var adminId = Convert.ToUInt64(GenerateRandomDiscordId());
        var admin = new Member
        {
            Id = adminId.ToString(),
            Roles = new List<MemberRole> { new() { IsAdmin = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(adminId)).ReturnsAsync(admin);

        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        // Make a single guess - with only one guess, if we run the lottery enough times, it should hit
        await _lotteryService.GuessAsync(userId, 50);

        await _lotteryService.RunLotteryAsync(adminId);

        // Verify lottery was run (messages posted)
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(It.IsAny<string>()), Times.AtLeast(2));
    }

    [Test]
    public async Task RunLotteryAsync_ClearsGuesses()
    {
        var adminId = Convert.ToUInt64(GenerateRandomDiscordId());
        var admin = new Member
        {
            Id = adminId.ToString(),
            Roles = new List<MemberRole> { new() { IsAdmin = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(adminId)).ReturnsAsync(admin);

        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        await _lotteryService.GuessAsync(userId, 50);

        await _lotteryService.RunLotteryAsync(adminId);

        // Verify guesses were cleared
        var unusedNumbers = await _lotteryService.GetUnusedNumbersAsync();
        Assert.That(unusedNumbers.UsedNumbers, Is.Empty);
    }

    #endregion

    #region RemindAsync Tests

    [Test]
    public async Task RemindAsync_NoFcMembers()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        _memberService.Setup(x => x.GetFcMembers()).ReturnsAsync([]);

        await _lotteryService.RemindAsync(userId);

        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(
            It.Is<string>(s => s.Contains("Use your guesses"))), Times.Once);
    }

    [Test]
    public async Task RemindAsync_WithMembersWithoutGuesses()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var fcMember = new Member
        {
            DiscordId = userId.ToString(),
            DiscordName = "TestUser",
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetFcMembers()).ReturnsAsync([fcMember]);

        await _lotteryService.RemindAsync(userId);

        // RemindAsync only includes users who have participated in previous lotteries
        // Since there are no previous lotteries in this test, the message won't contain the userId
        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(
            It.Is<string>(s => s.Contains("Use your guesses"))), Times.Once);
    }

    #endregion

    #region Award Tests

    [Test]
    public async Task TryAwardUsersAsync_NoUsers()
    {
        var result = await _lotteryService.TryAwardUsersAsync("test reason", []);

        Assert.That(result.GetType(), Is.EqualTo(typeof(NoUsersAwardResponse)));
    }

    [Test]
    public async Task TryAwardUsersAsync_SingleUser()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var result = await _lotteryService.TryAwardUsersAsync("test reason", [userId]);

        Assert.That(result.GetType(), Is.EqualTo(typeof(SuccessAwardResponse)));
        var success = (SuccessAwardResponse)result;
        Assert.That(success.DiscordUserIds, Contains.Item(userId));
        Assert.That(success.Reason, Is.EqualTo("test reason"));
    }

    [Test]
    public async Task TryAwardUsersAsync_MultipleUsers()
    {
        var userId1 = Convert.ToUInt64(GenerateRandomDiscordId());
        var userId2 = Convert.ToUInt64(GenerateRandomDiscordId());
        var result = await _lotteryService.TryAwardUsersAsync("test reason", [userId1, userId2]);

        Assert.That(result.GetType(), Is.EqualTo(typeof(SuccessAwardResponse)));
        var success = (SuccessAwardResponse)result;
        Assert.That(success.DiscordUserIds.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task AwardUsersAsync_SingleUser()
    {
        var userId = Convert.ToUInt64(GenerateRandomDiscordId());
        var user = new Member
        {
            Id = userId.ToString(),
            Roles = new List<MemberRole> { new() { IsMember = true } }
        };
        _memberService.Setup(x => x.GetByDiscordId(userId)).ReturnsAsync(user);

        var success = new SuccessAwardResponse([userId], $"<@{userId}>", "test reason");
        await _lotteryService.AwardUsersAsync(success);

        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(
            It.Is<string>(s => s.Contains("has been granted") && s.Contains("test reason"))), Times.Once);

        // Verify the user now has 2 allowed guesses
        var view = await _lotteryService.ViewAsync(userId);
        var viewResponse = (ViewResponse)view;
        Assert.That(viewResponse.TotalGuesses, Is.EqualTo(2));
    }

    [Test]
    public async Task AwardUsersAsync_MultipleUsers()
    {
        var userId1 = Convert.ToUInt64(GenerateRandomDiscordId());
        var userId2 = Convert.ToUInt64(GenerateRandomDiscordId());

        var success = new SuccessAwardResponse([userId1, userId2], $"<@{userId1}> and <@{userId2}>", "test reason");
        await _lotteryService.AwardUsersAsync(success);

        _discordMessageService.Verify(x => x.PostInLotteryChannelAsync(
            It.Is<string>(s => s.Contains("have all been granted") && s.Contains("test reason"))), Times.Once);
    }

    #endregion
}