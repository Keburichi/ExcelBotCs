using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Exceptions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Utils;
using Moq;

namespace ExcelBotCs.Tests.Services.API;

[TestFixture]
public class MemberServiceTests
{
    private IMemberService _memberService;
    private Mock<IMemberRepository> _memberRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _memberService = new MemberService(_memberRepositoryMock.Object);
    }

    [Test]
    public async Task GetAsync_ReturnsNull()
    {
        // Arrange
        _memberRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((List<Member>)null);

        // Act
        var result = await _memberService.GetAsync();

        // Assert
        Assert.That(result, Is.Null);

        _memberRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ReturnsList()
    {
        // Arrange
        var members = new List<Member>().PopulateWithRandomData();
        _memberRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(members);

        // Act
        var result = await _memberService.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EquivalentTo(members));

        _memberRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _memberRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Member)null);

        // Act
        var result = await _memberService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Null);

        _memberRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task GetAsync_ById_ReturnsItem()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var member = new Member().PopulateWithRandomData();
        _memberRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(member);

        // Act
        var result = await _memberService.GetAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(member));

        _memberRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
    }

    [Test]
    public async Task CreateAsync_CallsRepository()
    {
        // Arrange
        var member = new Member().PopulateWithRandomData();
        _memberRepositoryMock.Setup(x => x.CreateAsync(member)).Returns(Task.CompletedTask);

        // Act
        await _memberService.CreateAsync(member);

        // Assert
        _memberRepositoryMock.Verify(x => x.CreateAsync(member), Times.Once());
    }

    [Test]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var existingMember = new Member().PopulateWithRandomData();
        existingMember.LodestoneId = "existing-lodestone-id";
        existingMember.LodestoneVerificationToken = "existing-token";

        var updatedMember = new Member().PopulateWithRandomData();
        updatedMember.LodestoneId = "new-lodestone-id";
        updatedMember.LodestoneVerificationToken = "new-token";

        _memberRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync(existingMember);
        _memberRepositoryMock.Setup(x => x.UpdateAsync(id, It.IsAny<Member>())).Returns(Task.CompletedTask);

        // Act
        await _memberService.UpdateAsync(id, updatedMember);

        // Assert
        Assert.That(updatedMember.LodestoneId, Is.EqualTo("existing-lodestone-id"));
        Assert.That(updatedMember.LodestoneVerificationToken, Is.EqualTo("existing-token"));

        _memberRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _memberRepositoryMock.Verify(x => x.UpdateAsync(id, updatedMember), Times.Once());
    }

    [Test]
    public void UpdateAsync_ThrowsNotFoundException_WhenMemberDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var updatedMember = new Member().PopulateWithRandomData();

        _memberRepositoryMock.Setup(x => x.GetAsync(id)).ReturnsAsync((Member)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () => await _memberService.UpdateAsync(id, updatedMember));

        _memberRepositoryMock.Verify(x => x.GetAsync(id), Times.Once());
        _memberRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Member>()), Times.Never());
    }

    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _memberRepositoryMock.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _memberService.DeleteAsync(id);

        // Assert
        _memberRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once());
    }

    [Test]
    public async Task GetByDiscordId_String_ReturnsNull()
    {
        // Arrange
        var discordId = Guid.NewGuid().ToString();
        _memberRepositoryMock.Setup(x => x.GetByDiscordId(discordId)).ReturnsAsync((Member)null);

        // Act
        var result = await _memberService.GetByDiscordId(discordId);

        // Assert
        Assert.That(result, Is.Null);

        _memberRepositoryMock.Verify(x => x.GetByDiscordId(discordId), Times.Once());
    }

    [Test]
    public async Task GetByDiscordId_String_ReturnsItem()
    {
        // Arrange
        var discordId = Guid.NewGuid().ToString();
        var member = new Member().PopulateWithRandomData();
        _memberRepositoryMock.Setup(x => x.GetByDiscordId(discordId)).ReturnsAsync(member);

        // Act
        var result = await _memberService.GetByDiscordId(discordId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(member));

        _memberRepositoryMock.Verify(x => x.GetByDiscordId(discordId), Times.Once());
    }

    [Test]
    public async Task GetByDiscordId_Ulong_ReturnsItem()
    {
        // Arrange
        ulong discordId = 123456789;
        var member = new Member().PopulateWithRandomData();
        _memberRepositoryMock.Setup(x => x.GetByDiscordId(discordId.ToString())).ReturnsAsync(member);

        // Act
        var result = await _memberService.GetByDiscordId(discordId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(member));

        _memberRepositoryMock.Verify(x => x.GetByDiscordId(discordId.ToString()), Times.Once());
    }

    [Test]
    public async Task GetByDiscordIds_ReturnsList()
    {
        // Arrange
        var discordIds = new List<ulong> { 123456789, 987654321, 555555555 };
        var allMembers = new List<Member>
        {
            new Member { DiscordId = "123456789" }.PopulateWithRandomData(),
            new Member { DiscordId = "987654321" }.PopulateWithRandomData(),
            new Member { DiscordId = "999999999" }.PopulateWithRandomData()
        };

        _memberRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(allMembers);

        // Act
        var result = await _memberService.GetByDiscordIds(discordIds);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(m => m.DiscordId == "123456789"), Is.True);
        Assert.That(result.Any(m => m.DiscordId == "987654321"), Is.True);

        _memberRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }

    [Test]
    public async Task GetByLodestoneId_ReturnsNull()
    {
        // Arrange
        var lodestoneId = Guid.NewGuid().ToString();
        _memberRepositoryMock.Setup(x => x.GetByLodestoneId(lodestoneId)).ReturnsAsync((Member)null);

        // Act
        var result = await _memberService.GetByLodestoneId(lodestoneId);

        // Assert
        Assert.That(result, Is.Null);

        _memberRepositoryMock.Verify(x => x.GetByLodestoneId(lodestoneId), Times.Once());
    }

    [Test]
    public async Task GetByLodestoneId_ReturnsItem()
    {
        // Arrange
        var lodestoneId = Guid.NewGuid().ToString();
        var member = new Member().PopulateWithRandomData();
        _memberRepositoryMock.Setup(x => x.GetByLodestoneId(lodestoneId)).ReturnsAsync(member);

        // Act
        var result = await _memberService.GetByLodestoneId(lodestoneId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(member));

        _memberRepositoryMock.Verify(x => x.GetByLodestoneId(lodestoneId), Times.Once());
    }

    [Test]
    public async Task GetFcMembers_ReturnsOnlyFcMembers()
    {
        // Arrange
        var allMembers = new List<Member>
        {
            new Member
            {
                Roles = new List<MemberRole>
                {
                    new()
                    {
                        IsMember = true
                    }
                }
            }.PopulateWithRandomData(),
            new Member
            {
                Roles = new List<MemberRole>
                {
                    new()
                    {
                        IsMember = false
                    }
                }
            }.PopulateWithRandomData(),
            new Member
            {
                Roles = new List<MemberRole>
                {
                    new()
                    {
                        IsAdmin = true
                    }
                }
            }.PopulateWithRandomData(),
            new Member
            {
                Roles = new List<MemberRole>
                {
                    new()
                    {
                        IsDeveloper = true
                    }
                }
            }.PopulateWithRandomData()
        };

        _memberRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(allMembers);

        // Act
        var result = await _memberService.GetFcMembers();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(m => m.IsMember == true), Is.True);

        _memberRepositoryMock.Verify(x => x.GetAsync(), Times.Once());
    }
}