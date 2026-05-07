using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryGuessRepositoryTests : MongoDbTest
{
    private ILotteryGuessRepository _repository = null!;

    public LotteryGuessRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new LotteryGuessRepository(mongoClient, databaseOptions);
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

        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = 10 });
        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = 20 });
        await _repository.CreateAsync(new LotteryGuess { DiscordId = otherDiscordId, Number = 30 });

        var result = await _repository.GetByDiscordIdAsync(discordId);

        result.Count.ShouldBe(2);
        result.All(g => g.DiscordId == discordId).ShouldBeTrue();
        result.Select(g => g.Number).ShouldBe(new[] { 10, 20 }, ignoreOrder: true);
    }

    [Fact]
    public async Task GetByDiscordIdAsync_DoesNotReturnOtherUsersGuesses()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new LotteryGuess { DiscordId = otherDiscordId, Number = 42 });

        var result = await _repository.GetByDiscordIdAsync(discordId);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteByDiscordIdAndNumberAsync_DeletesMatchingGuess()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = 7 });
        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = 13 });

        await _repository.DeleteByDiscordIdAndNumberAsync(discordId, 7);

        var remaining = await _repository.GetByDiscordIdAsync(discordId);
        remaining.Count.ShouldBe(1);
        remaining[0].Number.ShouldBe(13);
    }

    [Fact]
    public async Task DeleteByDiscordIdAndNumberAsync_DoesNotDeleteOtherUsersGuess()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = 5 });
        await _repository.CreateAsync(new LotteryGuess { DiscordId = otherDiscordId, Number = 5 });

        await _repository.DeleteByDiscordIdAndNumberAsync(discordId, 5);

        var otherRemaining = await _repository.GetByDiscordIdAsync(otherDiscordId);
        otherRemaining.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteByDiscordIdAndNumberAsync_DoesNotDeleteDifferentNumberForSameUser()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = 1 });
        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId, Number = 2 });

        await _repository.DeleteByDiscordIdAndNumberAsync(discordId, 1);

        var remaining = await _repository.GetByDiscordIdAsync(discordId);
        remaining.Count.ShouldBe(1);
        remaining[0].Number.ShouldBe(2);
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesAllGuesses()
    {
        var discordId1 = ulong.Parse(GenerateRandomDiscordId());
        var discordId2 = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId1, Number = 10 });
        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId1, Number = 20 });
        await _repository.CreateAsync(new LotteryGuess { DiscordId = discordId2, Number = 30 });

        var before = await _repository.GetAsync();
        before.Count.ShouldBe(3);

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
