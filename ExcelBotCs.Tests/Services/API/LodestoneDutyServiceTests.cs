using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

[TestFixture]
public class LodestoneDutyServiceTests
{
    private ILodestoneDutyService _lodestoneDutyService;
    private Mock<ILodestoneDutyRepository> _lodestoneDutyRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _lodestoneDutyRepositoryMock = new Mock<ILodestoneDutyRepository>();
        _lodestoneDutyService = new LodestoneDutyService(_lodestoneDutyRepositoryMock.Object);
    }

    [Test]
    public async Task GetAsync_ReturnsNull()
    {
        // Arrange
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<LodestoneDuty>)null);

        // Act
        var result = await _lodestoneDutyService.GetAsync();

        // Assert
        Assert.That(result, Is.Null);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ReturnsList()
    {
        // Arrange
        var duties = new List<LodestoneDuty>().PopulateWithRandomData();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(duties);

        // Act
        var result = await _lodestoneDutyService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EquivalentTo(duties));

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((LodestoneDuty)null);

        // Act
        var result = await _lodestoneDutyService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Null);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var duty = new LodestoneDuty().PopulateWithRandomData();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(duty);

        // Act
        var result = await _lodestoneDutyService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(duty));

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task CreateAsync_SetsLastSyncTimeAndCallsRepository()
    {
        // Arrange
        var duty = new LodestoneDuty().PopulateWithRandomData();
        var beforeCreate = DateTime.UtcNow;
        _lodestoneDutyRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<LodestoneDuty>())).Returns(Task.CompletedTask);

        // Act
        await _lodestoneDutyService.CreateAsync(duty);
        var afterCreate = DateTime.UtcNow;

        // Assert
        Assert.That(duty.LastSyncTime, Is.GreaterThanOrEqualTo(beforeCreate));
        Assert.That(duty.LastSyncTime, Is.LessThanOrEqualTo(afterCreate));

        _lodestoneDutyRepositoryMock.Verify(x => x.CreateAsync(duty), Times.Once());
    }

    [Test]
    public async Task UpdateAsync_SetsLastSyncTimeAndCallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var duty = new LodestoneDuty().PopulateWithRandomData();
        var beforeUpdate = DateTime.UtcNow;
        _lodestoneDutyRepositoryMock.Setup(x => x.UpdateAsync(id, It.IsAny<LodestoneDuty>()))
            .Returns(Task.CompletedTask);

        // Act
        await _lodestoneDutyService.UpdateAsync(id, duty);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.That(duty.LastSyncTime, Is.GreaterThanOrEqualTo(beforeUpdate));
        Assert.That(duty.LastSyncTime, Is.LessThanOrEqualTo(afterUpdate));

        _lodestoneDutyRepositoryMock.Verify(x => x.UpdateAsync(id, duty), Times.Once());
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _lodestoneDutyRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _lodestoneDutyService.DeleteAsync(id);

        // Assert
        _lodestoneDutyRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Test]
    public async Task GetByExpansionAndCategoryAsync_ReturnsNull()
    {
        // Arrange
        var expansionId = 5;
        var categoryId = 3;
        _lodestoneDutyRepositoryMock.Setup(x => x.GetByExpansionAndCategoryAsync(expansionId, categoryId))
            .ReturnsAsync((List<LodestoneDuty>)null);

        // Act
        var result = await _lodestoneDutyService.GetByExpansionAndCategoryAsync(expansionId, categoryId);

        // Assert
        Assert.That(result, Is.Null);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Test]
    public async Task GetByExpansionAndCategoryAsync_ReturnsList()
    {
        // Arrange
        var expansionId = 5;
        var categoryId = 3;
        var duties = new List<LodestoneDuty>().PopulateWithRandomData();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetByExpansionAndCategoryAsync(expansionId, categoryId))
            .ReturnsAsync(duties);

        // Act
        var result = await _lodestoneDutyService.GetByExpansionAndCategoryAsync(expansionId, categoryId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EquivalentTo(duties));

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Test]
    public async Task GetByLodestoneIdAsync_ReturnsNull()
    {
        // Arrange
        var lodestoneId = Guid.NewGuid().ToString();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetByLodestoneIdAsync(lodestoneId)).ReturnsAsync((LodestoneDuty)null);

        // Act
        var result = await _lodestoneDutyService.GetByLodestoneIdAsync(lodestoneId);

        // Assert
        Assert.That(result, Is.Null);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByLodestoneIdAsync(lodestoneId), Times.Once());
    }

    [Test]
    public async Task GetByLodestoneIdAsync_ReturnsItem()
    {
        // Arrange
        var lodestoneId = Guid.NewGuid().ToString();
        var duty = new LodestoneDuty().PopulateWithRandomData();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetByLodestoneIdAsync(lodestoneId)).ReturnsAsync(duty);

        // Act
        var result = await _lodestoneDutyService.GetByLodestoneIdAsync(lodestoneId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(duty));

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByLodestoneIdAsync(lodestoneId), Times.Once());
    }

    [Test]
    public async Task HasDataAsync_ReturnsFalse_WhenCountIsZero()
    {
        // Arrange
        _lodestoneDutyRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);

        // Act
        var result = await _lodestoneDutyService.HasDataAsync();

        // Assert
        Assert.That(result, Is.False);

        _lodestoneDutyRepositoryMock.Verify(x => x.CountAsync(), Times.Once());
    }

    [Test]
    public async Task HasDataAsync_ReturnsTrue_WhenCountIsGreaterThanZero()
    {
        // Arrange
        _lodestoneDutyRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(5);

        // Act
        var result = await _lodestoneDutyService.HasDataAsync();

        // Assert
        Assert.That(result, Is.True);

        _lodestoneDutyRepositoryMock.Verify(x => x.CountAsync(), Times.Once());
    }

    [Test]
    public async Task HasDataForExpansionAndCategoryAsync_ReturnsFalse()
    {
        // Arrange
        var expansionId = 5;
        var categoryId = 3;
        _lodestoneDutyRepositoryMock.Setup(x => x.HasDataForExpansionAndCategoryAsync(expansionId, categoryId))
            .ReturnsAsync(false);

        // Act
        var result = await _lodestoneDutyService.HasDataForExpansionAndCategoryAsync(expansionId, categoryId);

        // Assert
        Assert.That(result, Is.False);

        _lodestoneDutyRepositoryMock.Verify(x => x.HasDataForExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Test]
    public async Task HasDataForExpansionAndCategoryAsync_ReturnsTrue()
    {
        // Arrange
        var expansionId = 5;
        var categoryId = 3;
        _lodestoneDutyRepositoryMock.Setup(x => x.HasDataForExpansionAndCategoryAsync(expansionId, categoryId))
            .ReturnsAsync(true);

        // Act
        var result = await _lodestoneDutyService.HasDataForExpansionAndCategoryAsync(expansionId, categoryId);

        // Assert
        Assert.That(result, Is.True);

        _lodestoneDutyRepositoryMock.Verify(x => x.HasDataForExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Test]
    public async Task BulkCreateAsync_SetsLastSyncTimeAndCallsRepositoryForEachDuty()
    {
        // Arrange
        var duties = new List<LodestoneDuty>
        {
            new LodestoneDuty().PopulateWithRandomData(),
            new LodestoneDuty().PopulateWithRandomData(),
            new LodestoneDuty().PopulateWithRandomData()
        };
        var beforeCreate = DateTime.UtcNow;
        _lodestoneDutyRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<LodestoneDuty>())).Returns(Task.CompletedTask);

        // Act
        await _lodestoneDutyService.BulkCreateAsync(duties);
        var afterCreate = DateTime.UtcNow;

        // Assert
        foreach (var duty in duties)
        {
            Assert.That(duty.LastSyncTime, Is.GreaterThanOrEqualTo(beforeCreate));
            Assert.That(duty.LastSyncTime, Is.LessThanOrEqualTo(afterCreate));
        }

        _lodestoneDutyRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<LodestoneDuty>()), Times.Exactly(3));
    }
}