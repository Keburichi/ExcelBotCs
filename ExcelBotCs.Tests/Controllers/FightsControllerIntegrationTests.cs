using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

public class FightsControllerIntegrationTests : IntegrationTestBase
{
    public FightsControllerIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }
    #region Permission Tests

    [Fact]
    public async Task GetEntities_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Fights");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Fights");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Fights");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Fights");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Functional Tests - Get

    [Fact]
    public async Task GetEntities_NoFights_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/Fights");

        // Assert
        response.EnsureSuccessStatusCode();
        var fights = await response.Content.ReadFromJsonAsync<List<FightDto>>();
        fights.ShouldNotBeNull();
        fights.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetEntities_WithFights_ReturnsAll()
    {
        // Arrange
        await AuthenticateAsMember();
        var fightService = Factory.Services.GetRequiredService<IFightService>();

        var fight1 = new Fight
        {
            Name = "Fight 1",
            Description = "Description 1",
            ImageUrl = "https://example.com/fight1.png",
            Type = FightType.Normal,
            Raidplans = new List<Raidplan>()
        };
        var fight2 = new Fight
        {
            Name = "Fight 2",
            Description = "Description 2",
            ImageUrl = "https://example.com/fight2.png",
            Type = FightType.Savage,
            Raidplans = new List<Raidplan>()
        };

        await fightService.CreateAsync(fight1);
        await fightService.CreateAsync(fight2);

        // Act
        var response = await Client.GetAsync("api/Fights");

        // Assert
        response.EnsureSuccessStatusCode();
        var fights = await response.Content.ReadFromJsonAsync<List<FightDto>>();
        fights.ShouldNotBeNull();
        fights.Count.ShouldBe(2);
        fights.Any(f => f.Name == fight1.Name).ShouldBeTrue();
        fights.Any(f => f.Name == fight2.Name).ShouldBeTrue();
    }

    [Fact]
    public async Task GetEntity_WhenExists_ReturnsFight()
    {
        // Arrange
        await AuthenticateAsMember();
        var fightService = Factory.Services.GetRequiredService<IFightService>();

        var fight = new Fight
        {
            Name = "Test Fight",
            Description = "Test Description",
            ImageUrl = "https://example.com/test-fight.png",
            Type = FightType.Extreme,
            Raidplans = new List<Raidplan>()
        };
        await fightService.CreateAsync(fight);

        // Act
        var response = await Client.GetAsync($"api/Fights/{fight.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedFight = await response.Content.ReadFromJsonAsync<FightDto>();
        retrievedFight.ShouldNotBeNull();
        retrievedFight.Id.ShouldBe(fight.Id);
        retrievedFight.Name.ShouldBe(fight.Name);
        retrievedFight.Description.ShouldBe(fight.Description);
    }

    [Fact]
    public async Task GetEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.GetAsync($"api/Fights/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Functional Tests - Create

    [Fact]
    public async Task CreateEntity_ValidData_CreatesFight()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fightDto = new FightDto
        {
            Name = "New Fight",
            Description = "New Description",
            ImageUrl = "https://example.com/new-fight.png",
            Type = FightType.Savage,
            Raidplans = new List<RaidplanDto>()
        };

        // Act
        var response = await Client.PostAsJsonAsync("api/Fights", fightDto);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdFight = await response.Content.ReadFromJsonAsync<FightDto>();
        createdFight.ShouldNotBeNull();
        createdFight.Name.ShouldBe(fightDto.Name);
        createdFight.Description.ShouldBe(fightDto.Description);
    }

    [Fact]
    public async Task CreateEntity_PersistsToDatabase()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fightDto = new FightDto
        {
            Name = "Persistent Fight",
            Description = "Persistent Description",
            ImageUrl = "https://example.com/persistent.png",
            Type = FightType.Ultimate,
            Raidplans = new List<RaidplanDto>()
        };

        // Act
        var createResponse = await Client.PostAsJsonAsync("api/Fights", fightDto);
        createResponse.EnsureSuccessStatusCode();
        var createdFight = await createResponse.Content.ReadFromJsonAsync<FightDto>();

        // Verify persistence by retrieving
        var getResponse = await Client.GetAsync($"api/Fights/{createdFight!.Id}");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var retrievedFight = await getResponse.Content.ReadFromJsonAsync<FightDto>();
        retrievedFight.ShouldNotBeNull();
        retrievedFight.Name.ShouldBe(fightDto.Name);
    }

    #endregion

    #region Functional Tests - Update

    [Fact]
    public async Task UpdateEntity_ValidData_UpdatesFight()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fightDto = new FightDto
        {
            Name = "Original Name",
            Description = "Original Description",
            ImageUrl = "https://example.com/original.png",
            Type = FightType.Normal,
            Raidplans = new List<RaidplanDto>()
        };

        var createResponse = await Client.PostAsJsonAsync("api/Fights", fightDto);
        var createdFight = await createResponse.Content.ReadFromJsonAsync<FightDto>();

        // Modify the fight
        createdFight!.Name = "Updated Name";
        createdFight.Description = "Updated Description";
        createdFight.ImageUrl = "https://example.com/updated.png";

        // Act
        var updateResponse = await Client.PutAsJsonAsync($"api/Fights/{createdFight.Id}", createdFight);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"api/Fights/{createdFight.Id}");
        var updatedFight = await getResponse.Content.ReadFromJsonAsync<FightDto>();
        updatedFight.ShouldNotBeNull();
        updatedFight.Name.ShouldBe(createdFight.Name);
        updatedFight.Description.ShouldBe(createdFight.Description);
        updatedFight.ImageUrl.ShouldBe(createdFight.ImageUrl);
    }

    [Fact]
    public async Task UpdateEntity_WhenNotExists_ReturnsNoContent()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";
        var fightDto = new FightDto
        {
            Id = nonExistentId,
            Name = "Non-existent",
            Description = "Description",
            ImageUrl = "https://example.com/non-existent.png",
            Type = FightType.Extreme,
            Raidplans = new List<RaidplanDto>()
        };

        // Act
        var response = await Client.PutAsJsonAsync($"api/Fights/{nonExistentId}", fightDto);

        // Assert
        // Note: The controller doesn't check if entity exists before updating,
        // so it returns NoContent even if entity doesn't exist
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Functional Tests - Delete

    [Fact]
    public async Task DeleteEntity_WhenExists_DeletesFight()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fightDto = new FightDto
        {
            Name = "To Delete",
            Description = "Will be deleted",
            ImageUrl = "https://example.com/delete.png",
            Type = FightType.Chaotic,
            Raidplans = new List<RaidplanDto>()
        };

        var createResponse = await Client.PostAsJsonAsync("api/Fights", fightDto);
        var createdFight = await createResponse.Content.ReadFromJsonAsync<FightDto>();

        // Act
        var deleteResponse = await Client.DeleteAsync($"api/Fights/{createdFight!.Id}");

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"api/Fights/{createdFight.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.DeleteAsync($"api/Fights/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Integration Tests - Fights with Raidplans

    [Fact]
    public async Task GetEntity_WithRaidplans_ReturnsRaidplansList()
    {
        // Arrange
        await AuthenticateAsMember();
        var fightService = Factory.Services.GetRequiredService<IFightService>();

        var fight = new Fight
        {
            Name = "Fight with Raidplans",
            Description = "Has raidplans",
            ImageUrl = "https://example.com/fight.png",
            Type = FightType.Savage,
            Raidplans = new List<Raidplan>()
        };
        await fightService.CreateAsync(fight);

        // Act
        var response = await Client.GetAsync($"api/Fights/{fight.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedFight = await response.Content.ReadFromJsonAsync<FightDto>();
        retrievedFight.ShouldNotBeNull();
        retrievedFight.Raidplans.ShouldNotBeNull();
        retrievedFight.Raidplans.ShouldBeEmpty(); // Initially no raidplans
    }

    #endregion
}