using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.TestFramework.TestData;
using ExcelBotCs.TestFramework.Utils;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class MemberRoleRepositoryTests : MongoDbTest
{
    private IMemberRoleRepository _repository;

    public MemberRoleRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new MemberRoleRepository(mongoClient, databaseOptions);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public async Task GetByDiscordId_ReturnsNull_WhenDiscordIdIsNull(string? discordId)
    {
        var result = await _repository.GetByDiscordId(discordId);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByDiscordId_ReturnsMemberRole_WhenDiscordIdExists()
    {
        var dummyMemberRole = new MemberRole().PopulateWithRandomData();
        await _repository.CreateAsync(dummyMemberRole);

        var result = await _repository.GetByDiscordId(dummyMemberRole.DiscordId);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(dummyMemberRole.Name);
        result.DiscordId.ShouldBe(dummyMemberRole.DiscordId);
    }
}
