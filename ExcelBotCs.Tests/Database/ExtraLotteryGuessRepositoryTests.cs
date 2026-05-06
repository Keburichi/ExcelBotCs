using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[TestFixture]
public class ExtraLotteryGuessRepositoryTests : MongoDbTest
{
    private IExtraLotteryGuessRepository _repository = null!;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new ExtraLotteryGuessRepository(mongoClient, databaseOptions);
    }

    [Test]
    public async Task GetByDiscordIdAsync_ReturnsEmpty_WhenNoGuessesExist()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());

        var result = await _repository.GetByDiscordIdAsync(discordId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetByDiscordIdAsync_ReturnsGuesses_WhenGuessesExist()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId, Reason = "Reason A" });
        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId, Reason = "Reason B" });
        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = otherDiscordId, Reason = "Reason C" });

        var result = await _repository.GetByDiscordIdAsync(discordId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(g => g.DiscordId == discordId), Is.True);
        Assert.That(result.Select(g => g.Reason), Is.EquivalentTo(new[] { "Reason A", "Reason B" }));
    }

    [Test]
    public async Task GetByDiscordIdAsync_DoesNotReturnOtherUsersGuesses()
    {
        var discordId = ulong.Parse(GenerateRandomDiscordId());
        var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = otherDiscordId, Reason = "Other reason" });

        var result = await _repository.GetByDiscordIdAsync(discordId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task DeleteAllAsync_RemovesAllGuesses()
    {
        var discordId1 = ulong.Parse(GenerateRandomDiscordId());
        var discordId2 = ulong.Parse(GenerateRandomDiscordId());

        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId1, Reason = "First" });
        await _repository.CreateAsync(new ExtraLotteryGuess { DiscordId = discordId2, Reason = "Second" });

        var before = await _repository.GetAsync();
        Assert.That(before, Has.Count.EqualTo(2));

        await _repository.DeleteAllAsync();

        var after = await _repository.GetAsync();
        Assert.That(after, Is.Empty);
    }

    [Test]
    public async Task DeleteAllAsync_IsIdempotent_WhenCollectionIsAlreadyEmpty()
    {
        await _repository.DeleteAllAsync();

        var result = await _repository.GetAsync();
        Assert.That(result, Is.Empty);
    }
}