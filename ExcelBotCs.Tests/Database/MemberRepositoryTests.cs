using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[TestFixture]
public class MemberRepositoryTests : MongoDbTest
{
    private IMemberRepository _memberRepository = null!;
    private IMemberRoleRepository _memberRoleRepository = null!;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _memberRepository = new MemberRepository(mongoClient, databaseOptions);
        _memberRoleRepository = new MemberRoleRepository(mongoClient, databaseOptions);
    }

    [Test]
    public async Task GetAsync_ReturnsEmptyList_WhenNoMembersExist()
    {
        var result = await _memberRepository.GetAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAsync_ReturnsMember_WhenMemberExists()
    {
        // Arrange
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string>(),
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].DiscordName, Is.EqualTo(member.DiscordName));
    }

    [Test]
    public async Task GetAsync_LoadsRoles_WhenMemberHasRoles()
    {
        // Arrange - Create MemberRole
        var memberRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Test Role",
            IsAdmin = true,
            IsMember = true,
            IsDeveloper = false
        };
        await _memberRoleRepository.CreateAsync(memberRole);

        // Create Member with reference to the role
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string> { memberRole.Id! },
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Empty);
        var loadedMember = result[0];
        Assert.That(loadedMember.Roles, Is.Not.Null);
        Assert.That(loadedMember.Roles, Is.Not.Empty);
        Assert.That(loadedMember.Roles.Count, Is.EqualTo(1));
        Assert.That(loadedMember.Roles[0].Name, Is.EqualTo("Test Role"));
        Assert.That(loadedMember.Roles[0].IsAdmin, Is.True);
    }

    [Test]
    public async Task GetAsync_LoadsMultipleRoles_WhenMemberHasMultipleRoles()
    {
        // Arrange - Create multiple MemberRoles
        var adminRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Admin Role",
            IsAdmin = true,
            IsMember = true,
            IsDeveloper = false
        };
        await _memberRoleRepository.CreateAsync(adminRole);

        var developerRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Developer Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = true
        };
        await _memberRoleRepository.CreateAsync(developerRole);

        // Create Member with references to both roles
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string> { adminRole.Id!, developerRole.Id! },
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Empty);
        var loadedMember = result[0];
        Assert.That(loadedMember.Roles, Is.Not.Null);
        Assert.That(loadedMember.Roles, Has.Count.EqualTo(2));
        Assert.That(loadedMember.Roles.Any(r => r.Name == "Admin Role"), Is.True);
        Assert.That(loadedMember.Roles.Any(r => r.Name == "Developer Role"), Is.True);
        Assert.That(loadedMember.IsAdmin, Is.True);
        Assert.That(loadedMember.IsDeveloper, Is.True);
    }

    [Test]
    public async Task GetAsync_ReturnsEmptyRoles_WhenMemberHasNoRoles()
    {
        // Arrange
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string>(),
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetAsync();

        // Assert
        Assert.That(result, Is.Not.Empty);
        var loadedMember = result[0];
        Assert.That(loadedMember.Roles, Is.Not.Null);
        Assert.That(loadedMember.Roles, Is.Empty);
        Assert.That(loadedMember.IsAdmin, Is.False.Or.Null);
        Assert.That(loadedMember.IsMember, Is.False.Or.Null);
    }

    [Test]
    public async Task GetAsyncById_LoadsRoles_WhenMemberHasRoles()
    {
        // Arrange
        var memberRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Test Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };
        await _memberRoleRepository.CreateAsync(memberRole);

        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string> { memberRole.Id! },
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetAsync(member.Id!);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Roles, Is.Not.Null);
        Assert.That(result.Roles, Is.Not.Empty);
        Assert.That(result.Roles[0].Name, Is.EqualTo("Test Role"));
    }

    [Test]
    public async Task GetByDiscordId_ReturnsNull_WhenMemberDoesNotExist()
    {
        // Act
        var result = await _memberRepository.GetByDiscordId(GenerateRandomDiscordId());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByDiscordId_ReturnsMember_WhenMemberExists()
    {
        // Arrange
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string>(),
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetByDiscordId(member.DiscordId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DiscordName, Is.EqualTo(member.DiscordName));
    }

    [Test]
    public async Task GetByDiscordId_LoadsRoles_WhenMemberHasRoles()
    {
        // Arrange
        var memberRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Member Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };
        await _memberRoleRepository.CreateAsync(memberRole);

        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string> { memberRole.Id! },
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetByDiscordId(member.DiscordId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Roles, Is.Not.Null);
        Assert.That(result.Roles, Is.Not.Empty);
        Assert.That(result.Roles[0].Name, Is.EqualTo(memberRole.Name));
        Assert.That(result.IsMember, Is.True);
    }

    [Test]
    public async Task GetByLodestoneId_ReturnsNull_WhenMemberDoesNotExist()
    {
        // Act
        var result = await _memberRepository.GetByLodestoneId("12345678");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByLodestoneId_ReturnsMember_WhenMemberExists()
    {
        // Arrange
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            LodestoneId = "12345678",
            RoleIds = new List<string>(),
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetByLodestoneId(member.LodestoneId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DiscordName, Is.EqualTo(member.DiscordName));
        Assert.That(result.LodestoneId, Is.EqualTo(member.LodestoneId));
    }

    [Test]
    public async Task GetByLodestoneId_LoadsRoles_WhenMemberHasRoles()
    {
        // Arrange
        var memberRole = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Lodestone Test Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };
        await _memberRoleRepository.CreateAsync(memberRole);

        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            LodestoneId = "87654321",
            RoleIds = new List<string> { memberRole.Id! },
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        // Act
        var result = await _memberRepository.GetByLodestoneId(member.LodestoneId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Roles, Is.Not.Null);
        Assert.That(result.Roles, Is.Not.Empty);
        Assert.That(result.Roles[0].Name, Is.EqualTo(memberRole.Name));
        Assert.That(result.IsMember, Is.True);
    }

    [Test]
    public async Task CreateAsync_CreatesPopulatesIdAndTimestamps()
    {
        // Arrange
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string>(),
            ExperienceIds = new List<string>()
        };

        // Act
        await _memberRepository.CreateAsync(member);

        // Assert
        Assert.That(member.Id, Is.Not.Null);
        Assert.That(member.Id, Is.Not.Empty);
        Assert.That(member.DateCreated, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(member.DateModified, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task UpdateAsync_UpdatesEditDate()
    {
        // Arrange
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string>(),
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Act
        member.DiscordName = "Updated Name";
        await _memberRepository.UpdateAsync(member.Id!, member);

        // Assert
        var updated = await _memberRepository.GetAsync(member.Id!);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.DiscordName, Is.EqualTo(member.DiscordName));
        Assert.That(updated.DateModified, Is.GreaterThan(updated.DateCreated));
    }

    [Test]
    public async Task DeleteAsync_RemovesMember()
    {
        // Arrange
        var member = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "TestUser",
            PlayerName = "Test Player",
            RoleIds = new List<string>(),
            ExperienceIds = new List<string>()
        };
        await _memberRepository.CreateAsync(member);

        var before = await _memberRepository.GetAsync();
        Assert.That(before, Has.Count.EqualTo(1));

        // Act
        await _memberRepository.DeleteAsync(member.Id!);

        // Assert
        var after = await _memberRepository.GetAsync();
        Assert.That(after, Is.Empty);
    }
}