using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

[TestFixture]
public class RaidplanServiceTests
{
    private IRaidplanService _raidplanService;
    private Mock<IRaidplanRepository> _raidplanRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _raidplanRepositoryMock = new Mock<IRaidplanRepository>();
        _raidplanService = new RaidplanService(_raidplanRepositoryMock.Object);
    }

    [Test]
    public async Task GetAsync_ReturnsNull()
    {
        // Arrange
        _raidplanRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<Raidplan>)null);

        // Act
        var result = await _raidplanService.GetAsync();

        // Assert
        Assert.That(result, Is.Null);

        _raidplanRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ReturnsList()
    {
        // Arrange
        var raidplans = new List<Raidplan>().PopulateWithRandomData();
        _raidplanRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(raidplans);

        // Act
        var result = await _raidplanService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EquivalentTo(raidplans));

        _raidplanRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _raidplanRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Raidplan)null);

        // Act
        var result = await _raidplanService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Null);

        _raidplanRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var raidplan = new Raidplan().PopulateWithRandomData();
        _raidplanRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(raidplan);

        // Act
        var result = await _raidplanService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(raidplan));

        _raidplanRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task GetByFightIdAsync_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _raidplanRepositoryMock.Setup(x => x.GetByFightIdAsync(id)).ReturnsAsync((List<Raidplan>)null);

        // Act
        var result = await _raidplanService.GetByFightIdAsync(id);

        // Assert
        Assert.That(result, Is.Null);

        _raidplanRepositoryMock.Verify(x => x.GetByFightIdAsync(id), Times.Once());
    }

    [Test]
    public async Task GetByFightIdAsync_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var raidplans = new List<Raidplan>().PopulateWithRandomData();
        _raidplanRepositoryMock.Setup(x => x.GetByFightIdAsync(id)).ReturnsAsync(raidplans);

        // Act
        var result = await _raidplanService.GetByFightIdAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EquivalentTo(raidplans));

        _raidplanRepositoryMock.Verify(x => x.GetByFightIdAsync(id), Times.Once());
    }

    [Test]
    public async Task CreateAsync_CallsRepository()
    {
        // Arrange
        var fightId = Guid.NewGuid().ToString();
        var raidplan = new Raidplan().PopulateWithRandomData();
        _raidplanRepositoryMock.Setup(x => x.CreateAsync(fightId, raidplan)).Returns(Task.CompletedTask);

        // Act
        await _raidplanService.CreateAsync(fightId, raidplan);

        // Assert
        _raidplanRepositoryMock.Verify(x => x.CreateAsync(fightId, raidplan), Times.Once());
    }

    [Test]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var fightId = Guid.NewGuid().ToString();
        var id = Guid.NewGuid().ToString();
        var raidplan = new Raidplan().PopulateWithRandomData();
        _raidplanRepositoryMock.Setup(x => x.UpdateAsync(fightId, id, raidplan)).Returns(Task.CompletedTask);

        // Act
        await _raidplanService.UpdateAsync(fightId, id, raidplan);

        // Assert
        _raidplanRepositoryMock.Verify(x => x.UpdateAsync(fightId, id, raidplan), Times.Once());
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        var fightId = Guid.NewGuid().ToString();
        var id = Guid.NewGuid().ToString();
        _raidplanRepositoryMock.Setup(x => x.DeleteAsync(fightId, id)).Returns(Task.CompletedTask);

        // Act
        await _raidplanService.DeleteAsync(fightId, id);

        // Assert
        _raidplanRepositoryMock.Verify(x => x.DeleteAsync(fightId, id), Times.Once());
    }
}