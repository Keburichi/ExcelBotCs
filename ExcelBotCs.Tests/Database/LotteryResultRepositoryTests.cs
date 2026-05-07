using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryResultRepositoryTests : MongoDbTest
{
    private ILotteryResultRepository _repository = null!;

    public LotteryResultRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new LotteryResultRepository(mongoClient, databaseOptions);
    }

    [Fact]
    public async Task GetAsync_ReturnsEmpty_WhenNoResultsExist()
    {
        var result = await _repository.GetAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
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
        all.Count.ShouldBe(1);
        all[0].WinningNumber.ShouldBe(42);
        all[0].Guesses.Count.ShouldBe(2);
        all[0].Guesses.Any(g => g.DiscordId == discordId).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_ReturnsAllResults()
    {
        await _repository.CreateAsync(new LotteryResult { WinningNumber = 1, Guesses = [] });
        await _repository.CreateAsync(new LotteryResult { WinningNumber = 2, Guesses = [] });
        await _repository.CreateAsync(new LotteryResult { WinningNumber = 3, Guesses = [] });

        var result = await _repository.GetAsync();

        result.Count.ShouldBe(3);
        result.Select(r => r.WinningNumber).ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: true);
    }

    [Fact]
    public async Task DeleteAsync_RemovesResult()
    {
        var lotteryResult = new LotteryResult { WinningNumber = 99, Guesses = [] };
        await _repository.CreateAsync(lotteryResult);

        var before = await _repository.GetAsync();
        before.Count.ShouldBe(1);

        await _repository.DeleteAsync(lotteryResult.Id);

        var after = await _repository.GetAsync();
        after.ShouldBeEmpty();
    }
}
