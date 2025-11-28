using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

[TestFixture]
public class FightServiceTests
{
    private IFightService _fightService;
    private Mock<IFightRepository> _fightRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _fightRepositoryMock = new Mock<IFightRepository>();
        _fightService = new FightService(_fightRepositoryMock.Object);
    }

    [Test]
    public async Task GetAsync_ReturnsEmptyList()
    {
        // Arrange
        _fightRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<Fight>)null);

        // Act
        var result = await _fightService.GetAsync();

        // Assert
        Assert.That(result, Is.Empty);

        _fightRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ReturnsList_WithFiltering()
    {
        // Arrange
        var fights = new List<Fight>
        {
            new Fight
            {
                Name = "Fight1", Type = FightType.Savage, FFLogsZoneId = 100, FFLogsEncounterId = 1,
                FFLogsExpansionId = 1
            }.PopulateWithRandomData(),
            new Fight
            {
                Name = "Fight2", Type = FightType.LegacySavage, FFLogsZoneId = 90, FFLogsEncounterId = 2,
                FFLogsExpansionId = 2
            }.PopulateWithRandomData(),
            new Fight
            {
                Name = "Fight1", Type = FightType.Savage, FFLogsZoneId = 80, FFLogsEncounterId = 3,
                FFLogsExpansionId = 1
            }.PopulateWithRandomData()
        };
        _fightRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(fights);

        // Act
        var result = await _fightService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2)); // Fight1 should be deduplicated
        Assert.That(result[0].Name, Is.EqualTo("Fight1"));
        Assert.That(result[0].Type, Is.EqualTo(FightType.Savage)); // LegacySavage should be converted to Savage
        Assert.That(result[1].Name, Is.EqualTo("Fight2"));

        _fightRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_RenamesBahamutPrime()
    {
        // Arrange
        var fights = new List<Fight>
        {
            new Fight { Name = "Bahamut Prime", Type = FightType.Ultimate, FFLogsZoneId = 100, FFLogsEncounterId = 1 }
                .PopulateWithRandomData()
        };
        _fightRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(fights);

        // Act
        var result = await _fightService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("The Unending Coil of Bahamut"));

        _fightRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_RenamesUltimaWeapon()
    {
        // Arrange
        var fights = new List<Fight>
        {
            new Fight
            {
                Name = "The Ultima Weapon", Type = FightType.Ultimate, FFLogsZoneId = 100, FFLogsEncounterId = 1
            }.PopulateWithRandomData()
        };
        _fightRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(fights);

        // Act
        var result = await _fightService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("The Weapon's Refrain"));

        _fightRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _fightRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Fight)null);

        // Act
        var result = await _fightService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Null);

        _fightRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var fight = new Fight().PopulateWithRandomData();
        _fightRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(fight);

        // Act
        var result = await _fightService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(fight));

        _fightRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task CreateAsync_CallsRepository()
    {
        // Arrange
        var fight = new Fight().PopulateWithRandomData();
        _fightRepositoryMock.Setup(x => x.CreateAsync(fight)).Returns(Task.CompletedTask);

        // Act
        await _fightService.CreateAsync(fight);

        // Assert
        _fightRepositoryMock.Verify(x => x.CreateAsync(fight), Times.Once());
    }

    [Test]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var fight = new Fight().PopulateWithRandomData();
        _fightRepositoryMock.Setup(x => x.UpdateAsync(id, fight)).Returns(Task.CompletedTask);

        // Act
        await _fightService.UpdateAsync(id, fight);

        // Assert
        _fightRepositoryMock.Verify(x => x.UpdateAsync(id, fight), Times.Once());
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _fightRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _fightService.DeleteAsync(id);

        // Assert
        _fightRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Test]
    public async Task GetByNameAndTypeAsync_ReturnsNull()
    {
        // Arrange
        var name = "Test Fight";
        var type = FightType.Savage;
        _fightRepositoryMock.Setup(x => x.GetByNameAndTypeAsync(name, type)).ReturnsAsync((Fight)null);

        // Act
        var result = await _fightService.GetByNameAndTypeAsync(name, type);

        // Assert
        Assert.That(result, Is.Null);

        _fightRepositoryMock.Verify(x => x.GetByNameAndTypeAsync(name, type), Times.Once());
    }

    [Test]
    public async Task GetByNameAndTypeAsync_ReturnsItem()
    {
        // Arrange
        var name = "Test Fight";
        var type = FightType.Savage;
        var fight = new Fight().PopulateWithRandomData();
        _fightRepositoryMock.Setup(x => x.GetByNameAndTypeAsync(name, type)).ReturnsAsync(fight);

        // Act
        var result = await _fightService.GetByNameAndTypeAsync(name, type);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(fight));

        _fightRepositoryMock.Verify(x => x.GetByNameAndTypeAsync(name, type), Times.Once());
    }

    [Test]
    public async Task UpsertAsync_CreatesNewFight_WhenNotExists()
    {
        // Arrange
        var fight = new Fight { Name = "New Fight", Type = FightType.Savage }.PopulateWithRandomData();
        _fightRepositoryMock.Setup(x => x.GetByNameAndTypeAsync(fight.Name, fight.Type)).ReturnsAsync((Fight)null);
        _fightRepositoryMock.Setup(x => x.CreateAsync(fight)).Returns(Task.CompletedTask);

        // Act
        var result = await _fightService.UpsertAsync(fight);

        // Assert
        Assert.That(result, Is.True); // true means inserted

        _fightRepositoryMock.Verify(x => x.GetByNameAndTypeAsync(fight.Name, fight.Type), Times.Once());
        _fightRepositoryMock.Verify(x => x.CreateAsync(fight), Times.Once());
        _fightRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Fight>()), Times.Never());
    }

    [Test]
    public async Task UpsertAsync_UpdatesExistingFight_WhenExists()
    {
        // Arrange
        var existingFight = new Fight
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Existing Fight",
            Type = FightType.Savage,
            CreateDate = DateTime.UtcNow.AddDays(-7)
        }.PopulateWithRandomData();

        var updatedFight = new Fight
        {
            Name = "Existing Fight",
            Type = FightType.Savage
        }.PopulateWithRandomData();

        _fightRepositoryMock.Setup(x => x.GetByNameAndTypeAsync(updatedFight.Name, updatedFight.Type))
            .ReturnsAsync(existingFight);
        _fightRepositoryMock.Setup(x => x.UpdateAsync(existingFight.Id, It.IsAny<Fight>())).Returns(Task.CompletedTask);

        // Act
        var result = await _fightService.UpsertAsync(updatedFight);

        // Assert
        Assert.That(result, Is.False); // false means updated
        Assert.That(updatedFight.Id, Is.EqualTo(existingFight.Id));
        Assert.That(updatedFight.CreateDate, Is.EqualTo(existingFight.CreateDate));

        _fightRepositoryMock.Verify(x => x.GetByNameAndTypeAsync(updatedFight.Name, updatedFight.Type), Times.Once());
        _fightRepositoryMock.Verify(x => x.UpdateAsync(existingFight.Id, updatedFight), Times.Once());
        _fightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Fight>()), Times.Never());
    }

    [Test]
    public async Task BulkUpsertAsync_InsertsAndUpdatesMultipleFights()
    {
        // Arrange
        var existingFight = new Fight
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Existing Fight",
            Type = FightType.Savage,
            CreateDate = DateTime.UtcNow.AddDays(-7)
        }.PopulateWithRandomData();

        var fights = new List<Fight>
        {
            new Fight { Name = "New Fight 1", Type = FightType.Savage }.PopulateWithRandomData(),
            new Fight { Name = "Existing Fight", Type = FightType.Savage }.PopulateWithRandomData(),
            new Fight { Name = "New Fight 2", Type = FightType.Ultimate }.PopulateWithRandomData()
        };

        _fightRepositoryMock.Setup(x => x.GetByNameAndTypeAsync("New Fight 1", FightType.Savage))
            .ReturnsAsync((Fight)null);
        _fightRepositoryMock.Setup(x => x.GetByNameAndTypeAsync("Existing Fight", FightType.Savage))
            .ReturnsAsync(existingFight);
        _fightRepositoryMock.Setup(x => x.GetByNameAndTypeAsync("New Fight 2", FightType.Ultimate))
            .ReturnsAsync((Fight)null);

        _fightRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Fight>())).Returns(Task.CompletedTask);
        _fightRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Fight>()))
            .Returns(Task.CompletedTask);

        // Act
        var (inserted, updated) = await _fightService.BulkUpsertAsync(fights);

        // Assert
        Assert.That(inserted, Is.EqualTo(2));
        Assert.That(updated, Is.EqualTo(1));

        _fightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Fight>()), Times.Exactly(2));
        _fightRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Fight>()), Times.Once());
    }
}