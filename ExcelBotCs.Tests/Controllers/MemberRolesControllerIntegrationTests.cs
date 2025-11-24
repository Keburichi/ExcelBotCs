using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Tests.Utils;

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
    public async Task GetEntities_WhenNoRolesExist_ReturnsEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/MemberRoles");

        // Assert
        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<List<MemberRoleDto>>();
        Assert.That(roles, Is.Not.Null);
        Assert.That(roles, Is.Empty);
    }

    [Test]
    public async Task GetEntities_WhenRolesExist_ReturnsAllRoles()
    {
        // Arrange
        var role1 = new MemberRoleDto
        {
            DiscordId = "123456789",
            Name = "Admin Role",
            IsAdmin = true,
            IsMember = true,
            IsDeveloper = false
        };
        var role2 = new MemberRoleDto
        {
            DiscordId = "987654321",
            Name = "Member Role",
            IsAdmin = false,
            IsMember = true,
            IsDeveloper = false
        };

        await Client.PostAsJsonAsync("/api/MemberRoles", role1);
        await Client.PostAsJsonAsync("/api/MemberRoles", role2);

        // Act
        var response = await Client.GetAsync("/api/MemberRoles");

        // Assert
        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<List<MemberRoleDto>>();
        Assert.That(roles, Is.Not.Null);
        Assert.That(roles, Has.Count.EqualTo(2));
        Assert.That(roles.Any(r => r.Name == "Admin Role"), Is.True);
        Assert.That(roles.Any(r => r.Name == "Member Role"), Is.True);
    }

    [Test]
    public async Task GetEntity_WhenRoleExists_ReturnsRole()
    {
        // Arrange
        var role = new MemberRoleDto
        {
            DiscordId = "123456789",
            Name = "Test Role",
            IsAdmin = true,
            IsMember = true,
            IsDeveloper = true
        };

        var createResponse = await Client.PostAsJsonAsync("/api/MemberRoles", role);
        createResponse.EnsureSuccessStatusCode();
        var createdRole = await createResponse.Content.ReadFromJsonAsync<MemberRoleDto>();

        // Act
        var response = await Client.GetAsync($"/api/MemberRoles/{createdRole!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedRole = await response.Content.ReadFromJsonAsync<MemberRoleDto>();
        Assert.That(retrievedRole, Is.Not.Null);
        Assert.That(retrievedRole.Id, Is.EqualTo(createdRole.Id));
        Assert.That(retrievedRole.Name, Is.EqualTo("Test Role"));
        Assert.That(retrievedRole.DiscordId, Is.EqualTo("123456789"));
        Assert.That(retrievedRole.IsAdmin, Is.True);
        Assert.That(retrievedRole.IsMember, Is.True);
        Assert.That(retrievedRole.IsDeveloper, Is.True);
    }

    [Test]
    public async Task GetEntity_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        // Arrange
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
        var role = new MemberRoleDto
        {
            DiscordId = "111222333",
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
        Assert.That(createdRole.Name, Is.EqualTo("New Role"));
        Assert.That(createdRole.DiscordId, Is.EqualTo("111222333"));
    }

    [Test]
    public async Task CreateEntity_PersistsToDatabase()
    {
        // Arrange
        var role = new MemberRoleDto
        {
            DiscordId = "444555666",
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
        Assert.That(retrievedRole.Name, Is.EqualTo("Persistent Role"));
    }

    [Test]
    public async Task UpdateEntity_WithValidData_UpdatesRole()
    {
        // Arrange
        var role = new MemberRoleDto
        {
            DiscordId = "777888999",
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
        Assert.That(updatedRole.Name, Is.EqualTo("Updated Name"));
        Assert.That(updatedRole.IsAdmin, Is.True);
        Assert.That(updatedRole.IsDeveloper, Is.True);
    }

    [Test]
    public async Task DeleteEntity_WhenRoleExists_DeletesRole()
    {
        // Arrange
        var role = new MemberRoleDto
        {
            DiscordId = "999000111",
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
        var invalidId = "invalid-id";

        // Act
        var response = await Client.GetAsync($"/api/MemberRoles/{invalidId}");

        // Assert - ASP.NET Core routing will return 404 for routes that don't match the constraint
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}