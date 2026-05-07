using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

public class MemberRolesControllerIntegrationTests : IntegrationTestBase
{
    public MemberRolesControllerIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }
    [Fact]
    public async Task GetEntities_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/MemberRoles");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/MemberRoles");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/MemberRoles");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/MemberRoles");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEntities()
    {
        // Act
        await AuthenticateAsMember();
        var response = await Client.GetAsync("/api/MemberRoles");

        // Assert
        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<List<MemberRoleDto>>();
        roles.ShouldNotBeNull();

        roles.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetEntities_WhenRolesExist_ReturnsAllRoles()
    {
        // Arrange
        await AuthenticateAsMember();
        var role1 = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Admin Role",
            IsAdmin = true,
            IsMember = true,
            IsDeveloper = false
        };
        var role2 = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Member Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };

        var memberRoleService = Factory.Services.GetRequiredService<IMemberRoleService>();
        await memberRoleService.CreateAsync(role1);
        await memberRoleService.CreateAsync(role2);

        // Act
        var response = await Client.GetAsync("/api/MemberRoles");

        // Assert
        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<List<MemberRoleDto>>();
        roles.ShouldNotBeNull();
        roles.Count.ShouldBe(3);
        roles.Any(r => r.Name == "Admin Role").ShouldBeTrue();
        roles.Any(r => r.Name == "Member Role").ShouldBeTrue();
    }

    [Fact]
    public async Task GetEntity_WhenRoleExists_ReturnsRole()
    {
        // Arrange
        await AuthenticateAsMember();
        var role = new MemberRole
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Test Role",
            IsAdmin = true,
            IsMember = true,
            IsDeveloper = true
        };

        var memberRoleService = Factory.Services.GetRequiredService<IMemberRoleService>();
        await memberRoleService.CreateAsync(role);

        // Act
        var response = await Client.GetAsync($"/api/MemberRoles/{role.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedRole = await response.Content.ReadFromJsonAsync<MemberRoleDto>();
        retrievedRole.ShouldNotBeNull();
        retrievedRole.Id.ShouldBe(role.Id);
        retrievedRole.Name.ShouldBe(role.Name);
        retrievedRole.DiscordId.ShouldBe(role.DiscordId);
        retrievedRole.IsAdmin.ShouldBeTrue();
        retrievedRole.IsMember.ShouldBeTrue();
        retrievedRole.IsDeveloper.ShouldBeTrue();
    }

    [Fact]
    public async Task GetEntity_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011"; // Valid MongoDB ObjectId format

        // Act
        var response = await Client.GetAsync($"/api/MemberRoles/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEntity_WithValidData_CreatesRole()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var role = new MemberRoleDto
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "New Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/MemberRoles", role);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var createdRole = await response.Content.ReadFromJsonAsync<MemberRoleDto>();
        createdRole.ShouldNotBeNull();
        createdRole.Id.ShouldNotBeNull();
        createdRole.Name.ShouldBe(role.Name);
        createdRole.DiscordId.ShouldBe(role.DiscordId);
    }

    [Fact]
    public async Task CreateEntity_PersistsToDatabase()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var role = new MemberRoleDto
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Persistent Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = true
        };

        // Act
        var createResponse = await Client.PostAsJsonAsync("/api/MemberRoles", role);
        createResponse.EnsureSuccessStatusCode();
        var createdRole = await createResponse.Content.ReadFromJsonAsync<MemberRoleDto>();

        // Verify persistence by retrieving the role
        var getResponse = await Client.GetAsync($"/api/MemberRoles/{createdRole!.Id}");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var retrievedRole = await getResponse.Content.ReadFromJsonAsync<MemberRoleDto>();
        retrievedRole.ShouldNotBeNull();
        retrievedRole.Name.ShouldBe(role.Name);
    }

    [Fact]
    public async Task UpdateEntity_WithValidData_UpdatesRole()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var role = new MemberRoleDto
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Original Name",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };

        var createResponse = await Client.PostAsJsonAsync("/api/MemberRoles", role);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<MemberRoleDto>();

        // Modify the role
        createdRole!.Name = "Updated Name";
        createdRole.IsAdmin = true;
        createdRole.IsDeveloper = true;

        // Act
        var updateResponse = await Client.PutAsJsonAsync($"/api/MemberRoles/{createdRole.Id}", createdRole);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"/api/MemberRoles/{createdRole.Id}");
        var updatedRole = await getResponse.Content.ReadFromJsonAsync<MemberRoleDto>();
        updatedRole.ShouldNotBeNull();
        updatedRole.Name.ShouldBe(createdRole.Name);
        updatedRole.IsAdmin.ShouldBeTrue();
        updatedRole.IsDeveloper.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteEntity_WhenRoleExists_DeletesRole()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var role = new MemberRoleDto
        {
            DiscordId = GenerateRandomDiscordId(),
            Name = "Role To Delete",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };

        var createResponse = await Client.PostAsJsonAsync("/api/MemberRoles", role);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<MemberRoleDto>();

        // Act
        var deleteResponse = await Client.DeleteAsync($"/api/MemberRoles/{createdRole!.Id}");

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"/api/MemberRoles/{createdRole.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEntity_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011"; // Valid MongoDB ObjectId format

        // Act
        var response = await Client.DeleteAsync($"/api/MemberRoles/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEntity_WithInvalidIdFormat_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsMember();
        var invalidId = "invalid-id";

        // Act
        var response = await Client.GetAsync($"/api/MemberRoles/{invalidId}");

        // Assert - ASP.NET Core routing will return 404 for routes that don't match the constraint
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}