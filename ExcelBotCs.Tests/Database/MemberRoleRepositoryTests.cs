using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Attributes;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.TestFramework.Utils;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

public class MemberRoleRepositoryTests : MongoDbTest
{
    private IMemberRoleRepository _repository;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new MemberRoleRepository(mongoClient, databaseOptions);
    }

    [TestIsNullOrEmptyString]
    public async Task GetByDiscordId_ReturnsNull_WhenDiscordIdIsNull(string discordId)
    {
        Assert.That(await _repository.GetByDiscordId(discordId), Is.Null);
    }

    [Test]
    public async Task GetByDiscordId_ReturnsMemberRole_WhenDiscordIdExists()
    {
        var dummyMemberRole = new MemberRole().PopulateWithRandomData();
        await _repository.CreateAsync(dummyMemberRole);

        var result = await _repository.GetByDiscordId(dummyMemberRole.DiscordId);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(dummyMemberRole.Name));
        Assert.That(result.DiscordId, Is.EqualTo(dummyMemberRole.DiscordId));
    }
}