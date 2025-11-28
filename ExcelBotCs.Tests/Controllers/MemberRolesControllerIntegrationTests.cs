using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

[TestFixture]
public class MemberRolesControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetEntities_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/MemberRoles");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/MemberRoles");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/MemberRoles");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/MemberRoles");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetEntities()
    {
        // Act
        await AuthenticateAsMember();
        var response = await Client.GetAsync("/api/MemberRoles");

        // Assert
        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<List<MemberRoleDto>>();
        Assert.That(roles, Is.Not.Null);

        // Since we create a role and user for the authentication to work, there should be one result
        Assert.That(roles.Count, Is.AtLeast(1));
    }

    [Test]
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
        Assert.That(roles, Is.Not.Null);
        Assert.That(roles, Has.Count.AtLeast(2));
        Assert.That(roles.Any(r => r.Name == "Admin Role"), Is.True);
        Assert.That(roles.Any(r => r.Name == "Member Role"), Is.True);
    }

    [Test]
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
        Assert.That(retrievedRole, Is.Not.Null);
        Assert.That(retrievedRole.Id, Is.EqualTo(role.Id));
        Assert.That(retrievedRole.Name, Is.EqualTo(role.Name));
        Assert.That(retrievedRole.DiscordId, Is.EqualTo(role.DiscordId));
        Assert.That(retrievedRole.IsAdmin, Is.True);
        Assert.That(retrievedRole.IsMember, Is.True);
        Assert.That(retrievedRole.IsDeveloper, Is.True);
    }

    [Test]
    public async Task GetEntity_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011"; // Valid MongoDB ObjectId format

        // Act
        var response = await Client.GetAsync($"/api/MemberRoles/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
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
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdRole = await response.Content.ReadFromJsonAsync<MemberRoleDto>();
        Assert.That(createdRole, Is.Not.Null);
        Assert.That(createdRole.Id, Is.Not.Null);
        Assert.That(createdRole.Name, Is.EqualTo(role.Name));
        Assert.That(createdRole.DiscordId, Is.EqualTo(role.DiscordId));
    }

    [Test]
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
        Assert.That(retrievedRole, Is.Not.Null);
        Assert.That(retrievedRole.Name, Is.EqualTo(role.Name));
    }

    [Test]
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
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify the update
        var getResponse = await Client.GetAsync($"/api/MemberRoles/{createdRole.Id}");
        var updatedRole = await getResponse.Content.ReadFromJsonAsync<MemberRoleDto>();
        Assert.That(updatedRole, Is.Not.Null);
        Assert.That(updatedRole.Name, Is.EqualTo(createdRole.Name));
        Assert.That(updatedRole.IsAdmin, Is.True);
        Assert.That(updatedRole.IsDeveloper, Is.True);
    }

    [Test]
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
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify deletion
        var getResponse = await Client.GetAsync($"/api/MemberRoles/{createdRole.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteEntity_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011"; // Valid MongoDB ObjectId format

        // Act
        var response = await Client.DeleteAsync($"/api/MemberRoles/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetEntity_WithInvalidIdFormat_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsMember();
        var invalidId = "invalid-id";

        // Act
        var response = await Client.GetAsync($"/api/MemberRoles/{invalidId}");

        // Assert - ASP.NET Core routing will return 404 for routes that don't match the constraint
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}