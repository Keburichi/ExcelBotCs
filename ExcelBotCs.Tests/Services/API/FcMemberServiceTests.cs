using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

public class FcMemberServiceTests
{
    private readonly IFcMemberService _fcMemberService;
    private readonly Mock<IFcMemberRepository> _fcMemberRepositoryMock;

    public FcMemberServiceTests()
    {
        _fcMemberRepositoryMock = new Mock<IFcMemberRepository>();
        _fcMemberService = new FcMemberService(_fcMemberRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull()
    {
        // Arrange
        _fcMemberRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<FcMember>)null);

        // Act
        var result = await _fcMemberService.GetAsync();

        // Assert
        result.ShouldBeNull();

        _fcMemberRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ReturnsList()
    {
        // Arrange
        var fcMembers = new List<FcMember>().PopulateWithRandomData();
        _fcMemberRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(fcMembers);

        // Act
        var result = await _fcMemberService.GetAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(fcMembers);

        _fcMemberRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _fcMemberRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((FcMember)null);

        // Act
        var result = await _fcMemberService.GetAsync(id);

        // Assert
        result.ShouldBeNull();

        _fcMemberRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var fcMember = new FcMember().PopulateWithRandomData();
        _fcMemberRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(fcMember);

        // Act
        var result = await _fcMemberService.GetAsync(id);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(fcMember);

        _fcMemberRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_CallsRepository()
    {
        // Arrange
        var fcMember = new FcMember().PopulateWithRandomData();
        _fcMemberRepositoryMock.Setup(x => x.CreateAsync(fcMember)).Returns(Task.CompletedTask);

        // Act
        await _fcMemberService.CreateAsync(fcMember);

        // Assert
        _fcMemberRepositoryMock.Verify(x => x.CreateAsync(fcMember), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var fcMember = new FcMember().PopulateWithRandomData();
        _fcMemberRepositoryMock.Setup(x => x.UpdateAsync(id, fcMember)).Returns(Task.CompletedTask);

        // Act
        await _fcMemberService.UpdateAsync(id, fcMember);

        // Assert
        _fcMemberRepositoryMock.Verify(x => x.UpdateAsync(id, fcMember), Times.Once());
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _fcMemberRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _fcMemberService.DeleteAsync(id);

        // Assert
        _fcMemberRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Fact]
    public async Task GetByCharacterId_ReturnsNull()
    {
        // Arrange
        var characterId = Guid.NewGuid().ToString();
        _fcMemberRepositoryMock.Setup(x => x.GetByCharacterId(characterId)).ReturnsAsync((FcMember)null);

        // Act
        var result = await _fcMemberService.GetByCharacterId(characterId);

        // Assert
        result.ShouldBeNull();

        _fcMemberRepositoryMock.Verify(x => x.GetByCharacterId(characterId), Times.Once());
    }

    [Fact]
    public async Task GetByCharacterId_ReturnsItem()
    {
        // Arrange
        var characterId = Guid.NewGuid().ToString();
        var fcMember = new FcMember().PopulateWithRandomData();
        _fcMemberRepositoryMock.Setup(x => x.GetByCharacterId(characterId)).ReturnsAsync(fcMember);

        // Act
        var result = await _fcMemberService.GetByCharacterId(characterId);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(fcMember);

        _fcMemberRepositoryMock.Verify(x => x.GetByCharacterId(characterId), Times.Once());
    }
}