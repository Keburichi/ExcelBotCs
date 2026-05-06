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

public class FightRepositoryTests : MongoDbTest
{
    private IFightRepository _repository;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new FightRepository(mongoClient, databaseOptions);
    }

    [TestIsNullOrEmptyString]
    public async Task GetByNameAndTypeAsync_ReturnsNull_WhenNameIsNull(string name)
    {
        Assert.That(await _repository.GetByNameAndTypeAsync(name, FightType.Extreme), Is.Null);
    }

    [Test]
    public async Task GetByNameAndTypeAsync_ReturnsFight_WhenNameExists()
    {
        var dummyFight = new Fight().PopulateWithRandomData();
        await _repository.CreateAsync(dummyFight);

        var result = await _repository.GetByNameAndTypeAsync(dummyFight.Name, dummyFight.Type);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(dummyFight.Name));
        Assert.That(result.Type, Is.EqualTo(dummyFight.Type));
    }
}