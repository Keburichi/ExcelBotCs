using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

public class LodestoneDutyServiceTests
{
    private readonly ILodestoneDutyService _lodestoneDutyService;
    private readonly Mock<ILodestoneDutyRepository> _lodestoneDutyRepositoryMock;

    public LodestoneDutyServiceTests()
    {
        _lodestoneDutyRepositoryMock = new Mock<ILodestoneDutyRepository>();
        _lodestoneDutyService = new LodestoneDutyService(_lodestoneDutyRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull()
    {
        // Arrange
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<LodestoneDuty>)null);

        // Act
        var result = await _lodestoneDutyService.GetAsync();

        // Assert
        result.ShouldBeNull();

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ReturnsList()
    {
        // Arrange
        var duties = new List<LodestoneDuty>().PopulateWithRandomData();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(duties);

        // Act
        var result = await _lodestoneDutyService.GetAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(duties);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((LodestoneDuty)null);

        // Act
        var result = await _lodestoneDutyService.GetAsync(id);

        // Assert
        result.ShouldBeNull();

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var duty = new LodestoneDuty().PopulateWithRandomData();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(duty);

        // Act
        var result = await _lodestoneDutyService.GetAsync(id);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(duty);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Fact]
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
        duty.LastSyncTime.ShouldBeGreaterThanOrEqualTo(beforeCreate);
        duty.LastSyncTime.ShouldBeLessThanOrEqualTo(afterCreate);

        _lodestoneDutyRepositoryMock.Verify(x => x.CreateAsync(duty), Times.Once());
    }

    [Fact]
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
        duty.LastSyncTime.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        duty.LastSyncTime.ShouldBeLessThanOrEqualTo(afterUpdate);

        _lodestoneDutyRepositoryMock.Verify(x => x.UpdateAsync(id, duty), Times.Once());
    }

    [Fact]
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

    [Fact]
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
        result.ShouldBeNull();

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result.ShouldBe(duties);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Fact]
    public async Task GetByLodestoneIdAsync_ReturnsNull()
    {
        // Arrange
        var lodestoneId = Guid.NewGuid().ToString();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetByLodestoneIdAsync(lodestoneId)).ReturnsAsync((LodestoneDuty)null);

        // Act
        var result = await _lodestoneDutyService.GetByLodestoneIdAsync(lodestoneId);

        // Assert
        result.ShouldBeNull();

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByLodestoneIdAsync(lodestoneId), Times.Once());
    }

    [Fact]
    public async Task GetByLodestoneIdAsync_ReturnsItem()
    {
        // Arrange
        var lodestoneId = Guid.NewGuid().ToString();
        var duty = new LodestoneDuty().PopulateWithRandomData();
        _lodestoneDutyRepositoryMock.Setup(x => x.GetByLodestoneIdAsync(lodestoneId)).ReturnsAsync(duty);

        // Act
        var result = await _lodestoneDutyService.GetByLodestoneIdAsync(lodestoneId);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(duty);

        _lodestoneDutyRepositoryMock.Verify(x => x.GetByLodestoneIdAsync(lodestoneId), Times.Once());
    }

    [Fact]
    public async Task HasDataAsync_ReturnsFalse_WhenCountIsZero()
    {
        // Arrange
        _lodestoneDutyRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);

        // Act
        var result = await _lodestoneDutyService.HasDataAsync();

        // Assert
        result.ShouldBeFalse();

        _lodestoneDutyRepositoryMock.Verify(x => x.CountAsync(), Times.Once());
    }

    [Fact]
    public async Task HasDataAsync_ReturnsTrue_WhenCountIsGreaterThanZero()
    {
        // Arrange
        _lodestoneDutyRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(5);

        // Act
        var result = await _lodestoneDutyService.HasDataAsync();

        // Assert
        result.ShouldBeTrue();

        _lodestoneDutyRepositoryMock.Verify(x => x.CountAsync(), Times.Once());
    }

    [Fact]
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
        result.ShouldBeFalse();

        _lodestoneDutyRepositoryMock.Verify(x => x.HasDataForExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Fact]
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
        result.ShouldBeTrue();

        _lodestoneDutyRepositoryMock.Verify(x => x.HasDataForExpansionAndCategoryAsync(expansionId, categoryId),
            Times.Once());
    }

    [Fact]
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
            duty.LastSyncTime.ShouldBeGreaterThanOrEqualTo(beforeCreate);
            duty.LastSyncTime.ShouldBeLessThanOrEqualTo(afterCreate);
        }

        _lodestoneDutyRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<LodestoneDuty>()), Times.Exactly(3));
    }
}