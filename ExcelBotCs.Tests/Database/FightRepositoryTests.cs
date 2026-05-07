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
public class FightRepositoryTests : MongoDbTest
{
    private IFightRepository _repository;

    public FightRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _repository = new FightRepository(mongoClient, databaseOptions);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public async Task GetByNameAndTypeAsync_ReturnsNull_WhenNameIsNull(string? name)
    {
        var result = await _repository.GetByNameAndTypeAsync(name, FightType.Extreme);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByNameAndTypeAsync_ReturnsFight_WhenNameExists()
    {
        var dummyFight = new Fight().PopulateWithRandomData();
        await _repository.CreateAsync(dummyFight);

        var result = await _repository.GetByNameAndTypeAsync(dummyFight.Name, dummyFight.Type);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(dummyFight.Name);
        result.Type.ShouldBe(dummyFight.Type);
    }
}
