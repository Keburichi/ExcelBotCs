using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

[TestFixture]
public class RaidplansControllerIntegrationTests : IntegrationTestBase
{
    private string _testFightId = null!;

    [SetUp]
    public new async Task SetUp()
    {
        base.SetUp();

        // Create a test fight for raidplans to be associated with
        var fightService = Factory.Services.GetRequiredService<IFightService>();
        var testFight = new Fight
        {
            Name = "Test Fight",
            Description = "Test fight for raidplans",
            ImageUrl = "https://example.com/fight.png",
            Raidplans = new List<Raidplan>()
        };
        await fightService.CreateAsync(testFight);
        _testFightId = testFight.Id;
    }

    #region Permission Tests

    [Test]
    public async Task GetRaidplans_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetRaidplan_CheckPermissions()
    {
        // Create a raidplan first
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "Test Raidplan",
            Description = "Test Description",
            Url = "https://example.com/raidplan",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan!.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CreateRaidplan_CheckPermissions()
    {
        var raidplanDto = new RaidplanDto
        {
            Name = "Test Raidplan",
            Description = "Test Description",
            Url = "https://example.com/raidplan",
            AuthorId = ""
        };

        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Created
        await AuthenticateAsMember();
        response = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    #endregion

    #region Functional Tests - Get

    [Test]
    public async Task GetRaidplans_NoRaidplans_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");

        // Assert
        response.EnsureSuccessStatusCode();
        var raidplans = await response.Content.ReadFromJsonAsync<List<RaidplanDto>>();
        Assert.That(raidplans, Is.Not.Null);
        Assert.That(raidplans, Is.Empty);
    }

    [Test]
    public async Task GetRaidplans_WithRaidplans_ReturnsAll()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());

        // Create multiple raidplans
        var raidplan1 = new RaidplanDto
        {
            Name = "Raidplan 1",
            Description = "Description 1",
            Url = "https://example.com/raidplan1",
            AuthorId = ""
        };
        var raidplan2 = new RaidplanDto
        {
            Name = "Raidplan 2",
            Description = "Description 2",
            Url = "https://example.com/raidplan2",
            AuthorId = ""
        };

        await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplan1);
        await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplan2);

        // Act
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");

        // Assert
        response.EnsureSuccessStatusCode();
        var raidplans = await response.Content.ReadFromJsonAsync<List<RaidplanDto>>();
        Assert.That(raidplans, Is.Not.Null);
        Assert.That(raidplans, Has.Count.EqualTo(2));
        Assert.That(raidplans.Any(r => r.Name == raidplan1.Name), Is.True);
        Assert.That(raidplans.Any(r => r.Name == raidplan2.Name), Is.True);
    }

    [Test]
    public async Task GetRaidplan_WhenExists_ReturnsRaidplan()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "Test Raidplan",
            Description = "Test Description",
            Url = "https://example.com/raidplan",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedRaidplan = await response.Content.ReadFromJsonAsync<RaidplanDto>();
        Assert.That(retrievedRaidplan, Is.Not.Null);
        Assert.That(retrievedRaidplan.Name, Is.EqualTo(raidplanDto.Name));
        Assert.That(retrievedRaidplan.Description, Is.EqualTo(raidplanDto.Description));
        Assert.That(retrievedRaidplan.Url, Is.EqualTo(raidplanDto.Url));
    }

    [Test]
    public async Task GetRaidplan_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion

    #region Functional Tests - Create

    [Test]
    public async Task CreateRaidplan_ValidData_CreatesRaidplan()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "New Raidplan",
            Description = "New Description",
            Url = "https://example.com/newraidplan",
            AuthorId = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var createdRaidplan = await response.Content.ReadFromJsonAsync<RaidplanDto>();
        Assert.That(createdRaidplan, Is.Not.Null);
        Assert.That(createdRaidplan.Id, Is.Not.Null);
        Assert.That(createdRaidplan.Name, Is.EqualTo(raidplanDto.Name));
        Assert.That(createdRaidplan.AuthorId, Is.EqualTo(member.Id));
    }

    [Test]
    public async Task CreateRaidplan_SetsAuthorToCurrentMember()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "Test Raidplan",
            Description = "Test Description",
            Url = "https://example.com/raidplan",
            AuthorId = "some-other-id" // Try to set a different author
        };

        // Act
        var response = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdRaidplan = await response.Content.ReadFromJsonAsync<RaidplanDto>();
        Assert.That(createdRaidplan!.AuthorId, Is.EqualTo(member.Id)); // Should override to current member
    }

    #endregion

    #region Functional Tests - Update

    [Test]
    public async Task UpdateRaidplan_AsOwner_Succeeds()
    {
        // Arrange - Create a raidplan as owner
        var owner = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "Original Name",
            Description = "Original Description",
            Url = "https://example.com/original",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act - Update as owner
        createdRaidplan!.Name = "Updated Name";
        createdRaidplan.Description = "Updated Description";
        var updateResponse = await Client.PutAsJsonAsync(
            $"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}",
            createdRaidplan);

        // Assert
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify the update
        var getResponse = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        var updatedRaidplan = await getResponse.Content.ReadFromJsonAsync<RaidplanDto>();
        Assert.That(updatedRaidplan!.Name, Is.EqualTo(createdRaidplan.Name));
        Assert.That(updatedRaidplan.Description, Is.EqualTo(createdRaidplan.Description));
    }

    [Test]
    public async Task UpdateRaidplan_AsAdmin_Succeeds()
    {
        // Arrange - Create a raidplan as a regular member
        var regularMember = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "Original Name",
            Description = "Original Description",
            Url = "https://example.com/original",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act - Update as admin (different user)
        await AuthenticateAsAdmin();
        createdRaidplan!.Name = "Admin Updated";
        var updateResponse = await Client.PutAsJsonAsync(
            $"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}",
            createdRaidplan);

        // Assert
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task UpdateRaidplan_AsNonOwnerNonAdmin_ReturnsForbidden()
    {
        // Arrange - Create a raidplan as one member
        var owner = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "Original Name",
            Description = "Original Description",
            Url = "https://example.com/original",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act - Try to update as a different non-admin member
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        createdRaidplan!.Name = "Unauthorized Update";
        var updateResponse = await Client.PutAsJsonAsync(
            $"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}",
            createdRaidplan);

        // Assert
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UpdateRaidplan_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";
        var raidplanDto = new RaidplanDto
        {
            Id = nonExistentId,
            Name = "Test",
            Description = "Test",
            Url = "https://example.com/test",
            AuthorId = ""
        };

        // Act
        var response = await Client.PutAsJsonAsync(
            $"api/fights/{_testFightId}/raidplans/{nonExistentId}",
            raidplanDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdateRaidplan_PreservesOriginalAuthor()
    {
        // Arrange - Create a raidplan as one member
        var originalOwner = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "Original Name",
            Description = "Original Description",
            Url = "https://example.com/original",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act - Update as admin and try to change author
        await AuthenticateAsAdmin();
        createdRaidplan!.AuthorId = "different-author-id";
        await Client.PutAsJsonAsync(
            $"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}",
            createdRaidplan);

        // Assert - Author should remain the original
        var getResponse = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        var updatedRaidplan = await getResponse.Content.ReadFromJsonAsync<RaidplanDto>();
        Assert.That(updatedRaidplan!.AuthorId, Is.EqualTo(originalOwner.Id));
    }

    #endregion

    #region Functional Tests - Delete

    [Test]
    public async Task DeleteRaidplan_AsAdmin_Succeeds()
    {
        // Arrange - Create a raidplan as a regular member
        var regularMember = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "To Delete",
            Description = "Will be deleted",
            Url = "https://example.com/delete",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act - Delete as admin
        await AuthenticateAsAdmin();
        var deleteResponse = await Client.DeleteAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan!.Id}");

        // Assert
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify deletion
        var getResponse = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteRaidplan_AsOwner_ReturnsForbidden()
    {
        // Arrange - Create a raidplan as owner
        var owner = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "To Delete",
            Description = "Will be deleted",
            Url = "https://example.com/delete",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act - Try to delete as owner (not admin)
        var deleteResponse = await Client.DeleteAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan!.Id}");

        // Assert - Only admins can delete
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task DeleteRaidplan_AsNonOwnerNonAdmin_ReturnsForbidden()
    {
        // Arrange - Create a raidplan as one member
        var owner = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var raidplanDto = new RaidplanDto
        {
            Name = "To Delete",
            Description = "Will be deleted",
            Url = "https://example.com/delete",
            AuthorId = ""
        };
        var createResponse = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        var createdRaidplan = await createResponse.Content.ReadFromJsonAsync<RaidplanDto>();

        // Act - Try to delete as a different non-admin member
        await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        var deleteResponse = await Client.DeleteAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan!.Id}");

        // Assert
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task DeleteRaidplan_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.DeleteAsync($"api/fights/{_testFightId}/raidplans/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion
}