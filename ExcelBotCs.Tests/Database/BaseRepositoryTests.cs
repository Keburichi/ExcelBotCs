using ExcelBotCs.Database;
using ExcelBotCs.Models.Config;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.TestFramework.Utils;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[TestFixture]
public class BaseRepositoryTests : MongoDbTest
{
    private TestRepository _testRepository;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        // By initializing multiple repositories we test that the base repository is correctly creating missing collections
        var unused = new EventRepository(mongoClient, databaseOptions);
        var memberRepository = new MemberRepository(mongoClient, databaseOptions);

        _testRepository = new TestRepository(mongoClient, databaseOptions);
    }

    [Test]
    public void CollectionCreationTest()
    {
        // Nothing to do here, the actual test is in InitializeRepository
        Assert.Pass();
    }

    [Test]
    public async Task GetAsync_ReturnsEmptyList_WhenNoDocumentsExist()
    {
        var result = await _testRepository.GetAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAsync_ReturnsList_WhenDocumentsExist()
    {
        for (var i = 0; i < 10; i++)
            await _testRepository.CreateAsync(new TestDatabaseEntity().PopulateWithRandomData());

        var result = await _testRepository.GetAsync();

        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Count, Is.EqualTo(10));
    }

    [Test]
    public async Task GetAsync_ReturnNull_WhenDocumentDoesNotExist()
    {
        // Use a valid MongoDB ObjectId format (24-digit hex string)
        var result = await _testRepository.GetAsync("507f1f77bcf86cd799439011");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAsync_ReturnDocument_WhenDocumentExists()
    {
        var entity = new TestDatabaseEntity().PopulateWithRandomData();
        await _testRepository.CreateAsync(entity);

        var allEntries = await _testRepository.GetAsync();

        var result = await _testRepository.GetAsync(allEntries.First(x => x.Name.Equals(entity.Name)).Id);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(entity.Name));
    }

    [Test]
    public async Task CreateAsync_CreatesDocument_WhenValidEntity()
    {
        var before = await _testRepository.GetAsync();
        Assert.That(before, Is.Empty);

        var entity = new TestDatabaseEntity().PopulateWithRandomData();
        await _testRepository.CreateAsync(entity);

        var allEntries = await _testRepository.GetAsync();
        Assert.That(allEntries, Has.Count.EqualTo(1));
        Assert.That(allEntries.First().Name, Is.EqualTo(entity.Name));
        Assert.That(allEntries.First().CreateDate, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(allEntries.First().EditDate, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
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
        var originalEditDate = dbEntity.EditDate;

        await _testRepository.UpdateAsync(dbEntity.Id, dbEntity);

        var updatedEntity = await _testRepository.GetAsync(dbEntity.Id);

        Assert.That(updatedEntity.Name, Is.EqualTo(dbEntity.Name));
        Assert.That(updatedEntity.Description, Is.EqualTo(dbEntity.Description));
        Assert.That(updatedEntity.EditDate, Is.GreaterThan(originalEditDate));
    }

    [Test]
    public async Task DeleteAsync_DeletesDocument_WhenValidId()
    {
        var entity = new TestDatabaseEntity().PopulateWithRandomData();
        await _testRepository.CreateAsync(entity);

        var before = await _testRepository.GetAsync();
        Assert.That(before, Has.Count.EqualTo(1));

        await _testRepository.DeleteAsync(entity.Id);

        var after = await _testRepository.GetAsync();
        Assert.That(after, Is.Empty);
    }
}