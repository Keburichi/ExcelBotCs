using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class MemberRepositoryTests : MongoDbTest
{
    private IMemberRepository _memberRepository = null!;
    private IMemberRoleRepository _memberRoleRepository = null!;

    public MemberRepositoryTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _memberRepository = new MemberRepository(mongoClient, databaseOptions);
        _memberRoleRepository = new MemberRoleRepository(mongoClient, databaseOptions);
    }

    [Fact]
    public async Task GetAsync_ReturnsEmptyList_WhenNoMembersExist()
    {
        var result = await _memberRepository.GetAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
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
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);
        result[0].DiscordName.ShouldBe(member.DiscordName);
    }

    [Fact]
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
        result.ShouldNotBeEmpty();
        var loadedMember = result[0];
        loadedMember.Roles.ShouldNotBeNull();
        loadedMember.Roles.ShouldNotBeEmpty();
        loadedMember.Roles.Count.ShouldBe(1);
        loadedMember.Roles[0].Name.ShouldBe("Test Role");
        loadedMember.Roles[0].IsAdmin.ShouldBe(true);
    }

    [Fact]
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
        result.ShouldNotBeEmpty();
        var loadedMember = result[0];
        loadedMember.Roles.ShouldNotBeNull();
        loadedMember.Roles.Count.ShouldBe(2);
        loadedMember.Roles.Any(r => r.Name == "Admin Role").ShouldBe(true);
        loadedMember.Roles.Any(r => r.Name == "Developer Role").ShouldBe(true);
        loadedMember.IsAdmin.ShouldBe(true);
        loadedMember.IsDeveloper.ShouldBe(true);
    }

    [Fact]
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
        result.ShouldNotBeEmpty();
        var loadedMember = result[0];
        loadedMember.Roles.ShouldNotBeNull();
        loadedMember.Roles.ShouldBeEmpty();
        (loadedMember.IsAdmin ?? false).ShouldBe(false);
        (loadedMember.IsMember ?? false).ShouldBe(false);
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Roles.ShouldNotBeNull();
        result.Roles.ShouldNotBeEmpty();
        result.Roles[0].Name.ShouldBe("Test Role");
    }

    [Fact]
    public async Task GetByDiscordId_ReturnsNull_WhenMemberDoesNotExist()
    {
        // Act
        var result = await _memberRepository.GetByDiscordId(GenerateRandomDiscordId());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result.DiscordName.ShouldBe(member.DiscordName);
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result.Roles.ShouldNotBeNull();
        result.Roles.ShouldNotBeEmpty();
        result.Roles[0].Name.ShouldBe(memberRole.Name);
        result.IsMember.ShouldBe(true);
    }

    [Fact]
    public async Task GetByLodestoneId_ReturnsNull_WhenMemberDoesNotExist()
    {
        // Act
        var result = await _memberRepository.GetByLodestoneId("12345678");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result.DiscordName.ShouldBe(member.DiscordName);
        result.LodestoneId.ShouldBe(member.LodestoneId);
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result.Roles.ShouldNotBeNull();
        result.Roles.ShouldNotBeEmpty();
        result.Roles[0].Name.ShouldBe(memberRole.Name);
        result.IsMember.ShouldBe(true);
    }

    [Fact]
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
        member.Id.ShouldNotBeNull();
        member.Id.ShouldNotBeEmpty();
        member.DateCreated.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        member.DateModified.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
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
        updated.ShouldNotBeNull();
        updated!.DiscordName.ShouldBe(member.DiscordName);
        updated.DateModified.ShouldBeGreaterThan(updated.DateCreated);
    }

    [Fact]
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
        before.Count.ShouldBe(1);

        // Act
        await _memberRepository.DeleteAsync(member.Id!);

        // Assert
        var after = await _memberRepository.GetAsync();
        after.ShouldBeEmpty();
    }
}
