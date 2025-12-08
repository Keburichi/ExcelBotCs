using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery;
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
    }

    [Test]
    public async Task RandomGuessAsync_ErrorResponse()
    {
    }

    [Test]
    public async Task RandomGuessAsync_Any_Success()
    {
    }

    [Test]
    public async Task RandomGuessAsync_UnusedOnly_Success()
    {
    }

    [Test]
    public async Task RandomGuessAsync_UsedOnly_Success()
    {
    }

    #endregion
}