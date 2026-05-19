using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Fights;
using ExcelBotCs.Models.DTO.Resources;
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
        var fights = await response.Content.ReadFromJsonAsync<List<FightResponse>>();
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
            Type = FightType.Normal,
            Raidplans = new List<Raidplan>()
        };
        var fight2 = new Fight
        {
            Name = "Fight 2",
            Type = FightType.Savage,
            Raidplans = new List<Raidplan>()
        };

        await fightService.CreateAsync(fight1);
        await fightService.CreateAsync(fight2);

        // Act
        var response = await Client.GetAsync("api/Fights");

        // Assert
        response.EnsureSuccessStatusCode();
        var fights = await response.Content.ReadFromJsonAsync<List<FightResponse>>();
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
            Type = FightType.Extreme,
            Raidplans = new List<Raidplan>()
        };
        await fightService.CreateAsync(fight);

        // Act
        var response = await Client.GetAsync($"api/Fights/{fight.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedFight = await response.Content.ReadFromJsonAsync<FightResponse>();
        retrievedFight.ShouldNotBeNull();
        retrievedFight.Id.ShouldBe(fight.Id);
        retrievedFight.Name.ShouldBe(fight.Name);
        retrievedFight.Type.ShouldBe(fight.Type);
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
        var request = new CreateFightRequest
        {
            Name = "New Fight",
            Type = FightType.Savage
        };

        // Act
        var response = await Client.PostAsJsonAsync("api/Fights", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdFight = await response.Content.ReadFromJsonAsync<FightResponse>();
        createdFight.ShouldNotBeNull();
        createdFight.Name.ShouldBe(request.Name);
        createdFight.Type.ShouldBe(request.Type);
    }

    [Fact]
    public async Task CreateEntity_PersistsToDatabase()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var request = new CreateFightRequest
        {
            Name = "Persistent Fight",
            Type = FightType.Ultimate
        };

        // Act
        var createResponse = await Client.PostAsJsonAsync("api/Fights", request);
        createResponse.EnsureSuccessStatusCode();
        var createdFight = await createResponse.Content.ReadFromJsonAsync<FightResponse>();

        // Verify persistence by retrieving
        var getResponse = await Client.GetAsync($"api/Fights/{createdFight!.Id}");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var retrievedFight = await getResponse.Content.ReadFromJsonAsync<FightResponse>();
        retrievedFight.ShouldNotBeNull();
        retrievedFight.Name.ShouldBe(request.Name);
    }

    #endregion

    #region Functional Tests - Update

    [Fact]
    public async Task UpdateEntity_ValidData_UpdatesFight()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var createRequest = new CreateFightRequest
        {
            Name = "Original Name",
            Type = FightType.Normal
        };

        var createResponse = await Client.PostAsJsonAsync("api/Fights", createRequest);
        var createdFight = await createResponse.Content.ReadFromJsonAsync<FightResponse>();

        var updateRequest = new UpdateFightRequest
        {
            Name = "Updated Name",
            Type = FightType.Savage
        };

        // Act
        var updateResponse = await Client.PutAsJsonAsync($"api/Fights/{createdFight!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"api/Fights/{createdFight.Id}");
        var updatedFight = await getResponse.Content.ReadFromJsonAsync<FightResponse>();
        updatedFight.ShouldNotBeNull();
        updatedFight.Name.ShouldBe("Updated Name");
        updatedFight.Type.ShouldBe(FightType.Savage);
    }

    [Fact]
    public async Task UpdateEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";
        var updateRequest = new UpdateFightRequest
        {
            Name = "Non-existent"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"api/Fights/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Functional Tests - Delete

    [Fact]
    public async Task DeleteEntity_WhenExists_DeletesFight()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var request = new CreateFightRequest
        {
            Name = "To Delete",
            Type = FightType.Chaotic
        };

        var createResponse = await Client.PostAsJsonAsync("api/Fights", request);
        var createdFight = await createResponse.Content.ReadFromJsonAsync<FightResponse>();

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

    #region Integration Tests - Fights with Resources

    [Fact]
    public async Task GetEntity_WithResources_ReturnsResourcesList()
    {
        // Arrange
        await AuthenticateAsMember();
        var fightService = Factory.Services.GetRequiredService<IFightService>();

        var fight = new Fight
        {
            Name = "Fight with Resources",
            Type = FightType.Savage,
            Raidplans = new List<Raidplan>()
        };
        await fightService.CreateAsync(fight);

        // Act
        var response = await Client.GetAsync($"api/Fights/{fight.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedFight = await response.Content.ReadFromJsonAsync<FightResponse>();
        retrievedFight.ShouldNotBeNull();
        retrievedFight.Resources.ShouldNotBeNull();
        retrievedFight.Resources.ShouldBeEmpty();
    }

    #endregion
}
