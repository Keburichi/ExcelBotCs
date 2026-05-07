using ExcelBotCs.Database;
using ExcelBotCs.Models.Config;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.TestFramework.Utils;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class BaseRepositoryTests : MongoDbTest
{
    private TestRepository _testRepository;
    private IMongoClient _mongoClient;

    public BaseRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _mongoClient = mongoClient;

        // By initializing multiple repositories we test that the base repository is correctly creating missing collections
        var unused = new EventRepository(mongoClient, databaseOptions);
        var memberRepository = new MemberRepository(mongoClient, databaseOptions);

        _testRepository = new TestRepository(mongoClient, databaseOptions);
    }

    [Fact]
    public async Task CollectionCreationTest()
    {
        var database = _mongoClient.GetDatabase("TestDatabase");
        var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
        collections.ShouldContain("TestDatabaseEntity");
    }

    [Fact]
    public async Task GetAsync_ReturnsEmptyList_WhenNoDocumentsExist()
    {
        var result = await _testRepository.GetAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_ReturnsList_WhenDocumentsExist()
    {
        for (var i = 0; i < 10; i++)
            await _testRepository.CreateAsync(new TestDatabaseEntity().PopulateWithRandomData());

        var result = await _testRepository.GetAsync();

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(10);
    }

    [Fact]
    public async Task GetAsync_ReturnNull_WhenDocumentDoesNotExist()
    {
        // Use a valid MongoDB ObjectId format (24-digit hex string)
        var result = await _testRepository.GetAsync("507f1f77bcf86cd799439011");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnDocument_WhenDocumentExists()
    {
        var entity = new TestDatabaseEntity().PopulateWithRandomData();
        await _testRepository.CreateAsync(entity);

        var allEntries = await _testRepository.GetAsync();

        var result = await _testRepository.GetAsync(allEntries.First(x => x.Name.Equals(entity.Name)).Id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(entity.Name);
    }

    [Fact]
    public async Task CreateAsync_CreatesDocument_WhenValidEntity()
    {
        var before = await _testRepository.GetAsync();
        before.ShouldBeEmpty();

        var entity = new TestDatabaseEntity().PopulateWithRandomData();
        await _testRepository.CreateAsync(entity);

        var allEntries = await _testRepository.GetAsync();
        allEntries.Count.ShouldBe(1);
        allEntries.First().Name.ShouldBe(entity.Name);
        allEntries.First().DateCreated.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        allEntries.First().DateModified.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDocument_WhenValidEntity()
    {
        var entity = new TestDatabaseEntity().PopulateWithRandomData();
        await _testRepository.CreateAsync(entity);

        // Wait a short moment so that we can validate that the update method actually
        // updates the edit date
        await Task.Delay(TimeSpan.FromSeconds(2));

        var dbEntity = _testRepository.GetAsync().Result.FirstOrDefault(x => x.Name.Equals(entity.Name));
        dbEntity.Name = "Updated Name";
        dbEntity.Description = "Updated Description";
        var originalEditDate = dbEntity.DateModified;

        await _testRepository.UpdateAsync(dbEntity.Id, dbEntity);

        var updatedEntity = await _testRepository.GetAsync(dbEntity.Id);

        updatedEntity.Name.ShouldBe(dbEntity.Name);
        updatedEntity.Description.ShouldBe(dbEntity.Description);
        updatedEntity.DateModified.ShouldBeGreaterThan(originalEditDate);
    }

    [Fact]
    public async Task DeleteAsync_DeletesDocument_WhenValidId()
    {
        var entity = new TestDatabaseEntity().PopulateWithRandomData();
        await _testRepository.CreateAsync(entity);

        var before = await _testRepository.GetAsync();
        before.Count.ShouldBe(1);

        await _testRepository.DeleteAsync(entity.Id);

        var after = await _testRepository.GetAsync();
        after.ShouldBeEmpty();
    }
}