using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

public class RaidplansControllerIntegrationTests : IntegrationTestBase
{
    private string _testFightId = null!;

    public RaidplansControllerIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnAfterIntegrationSetupAsync()
    {
        await base.OnAfterIntegrationSetupAsync();

        // Create a test fight for raidplans to be associated with
        var fightService = Factory.Services.GetRequiredService<IFightService>();
        var testFight = new Fight
        {
            Name = "Test Fight",
            Raidplans = new List<Raidplan>()
        };
        await fightService.CreateAsync(testFight);
        _testFightId = testFight.Id;
    }

    #region Permission Tests

    [Fact]
    public async Task GetRaidplans_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Created
        await AuthenticateAsMember();
        response = await Client.PostAsJsonAsync($"api/fights/{_testFightId}/raidplans", raidplanDto);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    #endregion

    #region Functional Tests - Get

    [Fact]
    public async Task GetRaidplans_NoRaidplans_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans");

        // Assert
        response.EnsureSuccessStatusCode();
        var raidplans = await response.Content.ReadFromJsonAsync<List<RaidplanDto>>();
        raidplans.ShouldNotBeNull();
        raidplans.ShouldBeEmpty();
    }

    [Fact]
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
        raidplans.ShouldNotBeNull();
        raidplans.Count.ShouldBe(2);
        raidplans.Any(r => r.Name == raidplan1.Name).ShouldBeTrue();
        raidplans.Any(r => r.Name == raidplan2.Name).ShouldBeTrue();
    }

    [Fact]
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
        retrievedRaidplan.ShouldNotBeNull();
        retrievedRaidplan.Name.ShouldBe(raidplanDto.Name);
        retrievedRaidplan.Description.ShouldBe(raidplanDto.Description);
        retrievedRaidplan.Url.ShouldBe(raidplanDto.Url);
    }

    [Fact]
    public async Task GetRaidplan_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Functional Tests - Create

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdRaidplan = await response.Content.ReadFromJsonAsync<RaidplanDto>();
        createdRaidplan.ShouldNotBeNull();
        createdRaidplan.Id.ShouldNotBeNull();
        createdRaidplan.Name.ShouldBe(raidplanDto.Name);
        createdRaidplan.AuthorId.ShouldBe(member.Id);
    }

    [Fact]
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
        createdRaidplan!.AuthorId.ShouldBe(member.Id); // Should override to current member
    }

    #endregion

    #region Functional Tests - Update

    [Fact]
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
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        var updatedRaidplan = await getResponse.Content.ReadFromJsonAsync<RaidplanDto>();
        updatedRaidplan!.Name.ShouldBe(createdRaidplan.Name);
        updatedRaidplan.Description.ShouldBe(createdRaidplan.Description);
    }

    [Fact]
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
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
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
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
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
        updatedRaidplan!.AuthorId.ShouldBe(originalOwner.Id);
    }

    #endregion

    #region Functional Tests - Delete

    [Fact]
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
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"api/fights/{_testFightId}/raidplans/{createdRaidplan.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
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
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
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
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRaidplan_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.DeleteAsync($"api/fights/{_testFightId}/raidplans/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion
}