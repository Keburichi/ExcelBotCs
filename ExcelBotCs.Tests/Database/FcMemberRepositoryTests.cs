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
public class FcMemberRepositoryTests : MongoDbTest
{
    private IFcMemberRepository _repository;

    public FcMemberRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new FcMemberRepository(mongoClient, databaseOptions);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public async Task GetByCharacterId_ReturnsNull_WhenCharacterIdIsNull(string? characterId)
    {
        var result = await _repository.GetByCharacterId(characterId);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCharacterId_ReturnsMember()
    {
        var fcMember = new FcMember().PopulateWithRandomData();
        await _repository.CreateAsync(fcMember);

        var result = await _repository.GetByCharacterId(fcMember.CharacterId);
        result.ShouldNotBeNull();
        result.CharacterId.ShouldBe(fcMember.CharacterId);
    }
}
