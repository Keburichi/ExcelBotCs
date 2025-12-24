using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

[TestFixture]
public class FightsControllerIntegrationTests : IntegrationTestBase
{
    #region Permission Tests

    [Test]
    public async Task GetEntities_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Fights");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Fights");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Fights");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Fights");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Functional Tests - Get

    [Test]
    public async Task GetEntities_NoFights_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/Fights");

        // Assert
        response.EnsureSuccessStatusCode();
        var fights = await response.Content.ReadFromJsonAsync<List<FightDto>>();
        Assert.That(fights, Is.Not.Null);
        Assert.That(fights, Is.Empty);
    }

    [Test]
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
        Assert.That(fights, Is.Not.Null);
        Assert.That(fights, Has.Count.AtLeast(2));
        Assert.That(fights.Any(f => f.Name == fight1.Name), Is.True);
        Assert.That(fights.Any(f => f.Name == fight2.Name), Is.True);
    }

    [Test]
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
        Assert.That(retrievedFight, Is.Not.Null);
        Assert.That(retrievedFight.Id, Is.EqualTo(fight.Id));
        Assert.That(retrievedFight.Name, Is.EqualTo(fight.Name));
        Assert.That(retrievedFight.Description, Is.EqualTo(fight.Description));
    }

    [Test]
    public async Task GetEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.GetAsync($"api/Fights/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion

    #region Functional Tests - Create

    [Test]
    public async Task CreateEntity_ValidData_CreatesFight()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var createdFight = await response.Content.ReadFromJsonAsync<FightDto>();
        Assert.That(createdFight, Is.Not.Null);
        Assert.That(createdFight.Name, Is.EqualTo(fightDto.Name));
        Assert.That(createdFight.Description, Is.EqualTo(fightDto.Description));
    }

    [Test]
    public async Task CreateEntity_PersistsToDatabase()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(retrievedFight, Is.Not.Null);
        Assert.That(retrievedFight.Name, Is.EqualTo(fightDto.Name));
    }

    #endregion

    #region Functional Tests - Update

    [Test]
    public async Task UpdateEntity_ValidData_UpdatesFight()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify the update
        var getResponse = await Client.GetAsync($"api/Fights/{createdFight.Id}");
        var updatedFight = await getResponse.Content.ReadFromJsonAsync<FightDto>();
        Assert.That(updatedFight, Is.Not.Null);
        Assert.That(updatedFight.Name, Is.EqualTo(createdFight.Name));
        Assert.That(updatedFight.Description, Is.EqualTo(createdFight.Description));
        Assert.That(updatedFight.ImageUrl, Is.EqualTo(createdFight.ImageUrl));
    }

    [Test]
    public async Task UpdateEntity_WhenNotExists_ReturnsNoContent()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    #endregion

    #region Functional Tests - Delete

    [Test]
    public async Task DeleteEntity_WhenExists_DeletesFight()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify deletion
        var getResponse = await Client.GetAsync($"api/Fights/{createdFight.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.DeleteAsync($"api/Fights/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion

    #region Integration Tests - Fights with Raidplans

    [Test]
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
        Assert.That(retrievedFight, Is.Not.Null);
        Assert.That(retrievedFight.Raidplans, Is.Not.Null);
        Assert.That(retrievedFight.Raidplans, Is.Empty); // Initially no raidplans
    }

    #endregion
}