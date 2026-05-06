using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

[TestFixture]
public class MemberRoleServiceTests
{
    private IMemberRoleService _memberRoleService;
    private Mock<IMemberRoleRepository> _memberRoleRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _memberRoleRepositoryMock = new Mock<IMemberRoleRepository>();
        _memberRoleService = new MemberRoleService(_memberRoleRepositoryMock.Object);
    }

    [Test]
    public async Task GetAsync_ReturnsNull()
    {
        // Arrange
        _memberRoleRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<MemberRole>)null);

        // Act
        var result = await _memberRoleService.GetAsync();

        // Assert
        Assert.That(result, Is.Null);

        _memberRoleRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ReturnsList()
    {
        // Arrange
        var memberRoles = new List<MemberRole>().PopulateWithRandomData();
        _memberRoleRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(memberRoles);

        // Act
        var result = await _memberRoleService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EquivalentTo(memberRoles));

        _memberRoleRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _memberRoleRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((MemberRole)null);

        // Act
        var result = await _memberRoleService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Null);

        _memberRoleRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var memberRole = new MemberRole().PopulateWithRandomData();
        _memberRoleRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(memberRole);

        // Act
        var result = await _memberRoleService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(memberRole));

        _memberRoleRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task CreateAsync_CallsRepository()
    {
        // Arrange
        var memberRole = new MemberRole().PopulateWithRandomData();
        _memberRoleRepositoryMock.Setup(x => x.CreateAsync(memberRole)).Returns(Task.CompletedTask);

        // Act
        await _memberRoleService.CreateAsync(memberRole);

        // Assert
        _memberRoleRepositoryMock.Verify(x => x.CreateAsync(memberRole), Times.Once());
    }

    [Test]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var memberRole = new MemberRole().PopulateWithRandomData();
        _memberRoleRepositoryMock.Setup(x => x.UpdateAsync(id, memberRole)).Returns(Task.CompletedTask);

        // Act
        await _memberRoleService.UpdateAsync(id, memberRole);

        // Assert
        _memberRoleRepositoryMock.Verify(x => x.UpdateAsync(id, memberRole), Times.Once());
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _memberRoleRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _memberRoleService.DeleteAsync(id);

        // Assert
        _memberRoleRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Test]
    public async Task GetByDiscordId_ReturnsNull()
    {
        // Arrange
        var discordId = Guid.NewGuid().ToString();
        _memberRoleRepositoryMock.Setup(x => x.GetByDiscordId(discordId)).ReturnsAsync((MemberRole)null);

        // Act
        var result = await _memberRoleService.GetByDiscordId(discordId);

        // Assert
        Assert.That(result, Is.Null);

        _memberRoleRepositoryMock.Verify(x => x.GetByDiscordId(discordId), Times.Once());
    }

    [Test]
    public async Task GetByDiscordId_ReturnsItem()
    {
        // Arrange
        var discordId = Guid.NewGuid().ToString();
        var memberRole = new MemberRole().PopulateWithRandomData();
        _memberRoleRepositoryMock.Setup(x => x.GetByDiscordId(discordId)).ReturnsAsync(memberRole);

        // Act
        var result = await _memberRoleService.GetByDiscordId(discordId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(memberRole));

        _memberRoleRepositoryMock.Verify(x => x.GetByDiscordId(discordId), Times.Once());
    }
}