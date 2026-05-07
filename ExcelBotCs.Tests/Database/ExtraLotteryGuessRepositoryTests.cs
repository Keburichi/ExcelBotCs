using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class ExtraLotteryGuessRepositoryTests : MongoDbTest
{
    private IExtraLotteryGuessRepository _repository = null!;

    public ExtraLotteryGuessRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new ExtraLotteryGuessRepository(mongoClient, databaseOptions);
    }

    [Fact]
    public async Task GetByDiscordIdAsync_ReturnsEmpty_WhenNoGuessesExist()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());

        var result = await _repository.GetByDiscordIdAsync(discordId);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByDiscordIdAsync_ReturnsGuesses_WhenGuessesExist()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId, Reason = "Reason A" });
        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId, Reason = "Reason B" });
        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = otherDiscordId, Reason = "Reason C" });

        var result = await _repository.GetByDiscordIdAsync(discordId);

        result.Count.ShouldBe(2);
        result.All(g => g.DiscordId == discordId).ShouldBeTrue();
        result.Select(g => g.Reason).ShouldBe(new[] { "Reason A", "Reason B" }, ignoreOrder: true);
    }

    [Fact]
    public async Task GetByDiscordIdAsync_DoesNotReturnOtherUsersGuesses()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = otherDiscordId, Reason = "Other reason" });

        var result = await _repository.GetByDiscordIdAsync(discordId);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesAllGuesses()
    {
        var discordId1 = ulong.Parse(GenerateRandomDiscordId());
        var discordId2 = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId1, Reason = "First" });
        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId2, Reason = "Second" });

        var before = await _repository.GetAsync();
        before.Count.ShouldBe(2);

        await _repository.DeleteAllAsync();

        var after = await _repository.GetAsync();
        after.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAllAsync_IsIdempotent_WhenCollectionIsAlreadyEmpty()
    {
        await _repository.DeleteAllAsync();

        var result = await _repository.GetAsync();
        result.ShouldBeEmpty();
    }
}
