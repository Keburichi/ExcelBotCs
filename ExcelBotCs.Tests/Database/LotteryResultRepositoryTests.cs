using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[TestFixture]
public class LotteryResultRepositoryTests : MongoDbTest
{
    private ILotteryResultRepository _repository = null!;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new LotteryResultRepository(mongoClient, databaseOptions);
    }

    [Test]
    public async Task GetAsync_ReturnsEmpty_WhenNoResultsExist()
    {
        var result = await _repository.GetAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CreateAsync_PersistsResult_WithEmbeddedGuesses()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var lotteryResult = new LotteryResult
        {
            WinningNumber = 42,
            Guesses =
            [
                new LotteryGuess { DiscordId = discordId, Number = 42 },
                new LotteryGuess { DiscordId = ulong.Parse(GenerateRandomDiscordId()), Number = 15 }
            ]
        };

        await _repository.CreateAsync(lotteryResult);

        var all = await _repository.GetAsync();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].WinningNumber, Is.EqualTo(42));
        Assert.That(all[0].Guesses, Has.Count.EqualTo(2));
        Assert.That(all[0].Guesses.Any(g => g.DiscordId == discordId), Is.True);
    }

    [Test]
    public async Task GetAsync_ReturnsAllResults()
    {
        await _repository.CreateAsync(new LotteryResult { WinningNumber = 1, Guesses = [] });
        await _repository.CreateAsync(new LotteryResult { WinningNumber = 2, Guesses = [] });
        await _repository.CreateAsync(new LotteryResult { WinningNumber = 3, Guesses = [] });

        var result = await _repository.GetAsync();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(r => r.WinningNumber), Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task DeleteAsync_RemovesResult()
    {
        var lotteryResult = new LotteryResult { WinningNumber = 99, Guesses = [] };
        await _repository.CreateAsync(lotteryResult);

        var before = await _repository.GetAsync();
        Assert.That(before, Has.Count.EqualTo(1));

        await _repository.DeleteAsync(lotteryResult.Id);

        var after = await _repository.GetAsync();
        Assert.That(after, Is.Empty);
    }
}