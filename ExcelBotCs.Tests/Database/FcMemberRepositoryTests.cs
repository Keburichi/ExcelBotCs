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

public class FcMemberRepositoryTests : MongoDbTest
{
    private IFcMemberRepository _repository;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new FcMemberRepository(mongoClient, databaseOptions);
    }

    [TestIsNullOrEmptyString]
    public async Task GetByCharacterId_ReturnsNull_WhenCharacterIdIsNull(string characterId)
    {
        Assert.That(await _repository.GetByCharacterId(characterId), Is.Null);
    }

    [Test]
    public async Task GetByCharacterId_ReturnsMember()
    {
        var fcMember = new FcMember().PopulateWithRandomData();
        await _repository.CreateAsync(fcMember);

        var result = await _repository.GetByCharacterId(fcMember.CharacterId);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CharacterId, Is.EqualTo(fcMember.CharacterId));
    }
}