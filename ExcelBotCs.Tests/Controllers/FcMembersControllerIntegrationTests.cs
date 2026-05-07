using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

public class FcMembersControllerIntegrationTests : IntegrationTestBase
{
    public FcMembersControllerIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }
    #region Permission Tests

    [Fact]
    public async Task GetEntities_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/FcMembers");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/FcMembers");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/FcMembers");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/FcMembers");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Functional Tests - Get

    [Fact]
    public async Task GetEntities_NoFcMembers_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/FcMembers");

        // Assert
        response.EnsureSuccessStatusCode();
        var fcMembers = await response.Content.ReadFromJsonAsync<List<FcMemberDto>>();
        fcMembers.ShouldNotBeNull();
        fcMembers.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetEntities_WithFcMembers_ReturnsAll()
    {
        // Arrange
        await AuthenticateAsMember();
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();

        var fcMember1 = new FcMember
        {
            Name = "FC Member 1",
            CharacterId = "123456",
            Title = "Tank",
            FcRank = "Member",
            Avatar = "https://example.com/avatar1.png",
            Bio = "Bio 1"
        };
        var fcMember2 = new FcMember
        {
            Name = "FC Member 2",
            CharacterId = "789012",
            Title = "Healer",
            FcRank = "Officer",
            Avatar = "https://example.com/avatar2.png",
            Bio = "Bio 2"
        };

        await fcMemberService.CreateAsync(fcMember1);
        await fcMemberService.CreateAsync(fcMember2);

        // Act
        var response = await Client.GetAsync("api/FcMembers");

        // Assert
        response.EnsureSuccessStatusCode();
        var fcMembers = await response.Content.ReadFromJsonAsync<List<FcMemberDto>>();
        fcMembers.ShouldNotBeNull();
        fcMembers.Count.ShouldBe(2);
        fcMembers.Any(m => m.Name == fcMember1.Name).ShouldBeTrue();
        fcMembers.Any(m => m.Name == fcMember2.Name).ShouldBeTrue();
    }

    [Fact]
    public async Task GetEntity_WhenExists_ReturnsFcMember()
    {
        // Arrange
        await AuthenticateAsMember();
        var fcMemberService = Factory.Services.GetRequiredService<IFcMemberService>();

        var fcMember = new FcMember
        {
            Name = "Test FC Member",
            CharacterId = "123456",
            Title = "DPS",
            FcRank = "Member",
            Avatar = "https://example.com/avatar.png",
            Bio = "Test Bio"
        };
        await fcMemberService.CreateAsync(fcMember);

        // Act
        var response = await Client.GetAsync($"api/FcMembers/{fcMember.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedFcMember = await response.Content.ReadFromJsonAsync<FcMemberDto>();
        retrievedFcMember.ShouldNotBeNull();
        retrievedFcMember.Id.ShouldBe(fcMember.Id);
        retrievedFcMember.Name.ShouldBe(fcMember.Name);
        retrievedFcMember.CharacterId.ShouldBe(fcMember.CharacterId);
    }

    [Fact]
    public async Task GetEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.GetAsync($"api/FcMembers/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Functional Tests - Create

    [Fact]
    public async Task CreateEntity_ValidData_CreatesFcMember()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fcMemberDto = new FcMemberDto
        {
            Name = "New FC Member",
            CharacterId = "999888",
            Title = "Tank",
            FcRank = "Member",
            Avatar = "https://example.com/avatar.png",
            Bio = "New member bio"
        };

        // Act
        var response = await Client.PostAsJsonAsync("api/FcMembers", fcMemberDto);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdFcMember = await response.Content.ReadFromJsonAsync<FcMemberDto>();
        createdFcMember.ShouldNotBeNull();
        createdFcMember.Name.ShouldBe(fcMemberDto.Name);
        createdFcMember.CharacterId.ShouldBe(fcMemberDto.CharacterId);
    }

    [Fact]
    public async Task CreateEntity_PersistsToDatabase()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fcMemberDto = new FcMemberDto
        {
            Name = "Persistent FC Member",
            CharacterId = "111222",
            Title = "Healer",
            FcRank = "Officer",
            Avatar = "https://example.com/avatar.png",
            Bio = "Persistent bio"
        };

        // Act
        var createResponse = await Client.PostAsJsonAsync("api/FcMembers", fcMemberDto);
        createResponse.EnsureSuccessStatusCode();
        var createdFcMember = await createResponse.Content.ReadFromJsonAsync<FcMemberDto>();

        // Verify persistence by retrieving
        var getResponse = await Client.GetAsync($"api/FcMembers/{createdFcMember!.Id}");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var retrievedFcMember = await getResponse.Content.ReadFromJsonAsync<FcMemberDto>();
        retrievedFcMember.ShouldNotBeNull();
        retrievedFcMember.Name.ShouldBe(fcMemberDto.Name);
    }

    #endregion

    #region Functional Tests - Update

    [Fact]
    public async Task UpdateEntity_ValidData_UpdatesFcMember()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fcMemberDto = new FcMemberDto
        {
            Name = "Original Name",
            CharacterId = "333444",
            Title = "Tank",
            FcRank = "Member",
            Avatar = "https://example.com/avatar.png",
            Bio = "Original bio"
        };

        var createResponse = await Client.PostAsJsonAsync("api/FcMembers", fcMemberDto);
        var createdFcMember = await createResponse.Content.ReadFromJsonAsync<FcMemberDto>();

        // Modify the FC member
        createdFcMember!.Name = "Updated Name";
        createdFcMember.Title = "DPS";
        createdFcMember.Bio = "Updated bio";

        // Act
        var updateResponse = await Client.PutAsJsonAsync($"api/FcMembers/{createdFcMember.Id}", createdFcMember);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"api/FcMembers/{createdFcMember.Id}");
        var updatedFcMember = await getResponse.Content.ReadFromJsonAsync<FcMemberDto>();
        updatedFcMember.ShouldNotBeNull();
        updatedFcMember.Name.ShouldBe(createdFcMember.Name);
        updatedFcMember.Title.ShouldBe(createdFcMember.Title);
        updatedFcMember.Bio.ShouldBe(createdFcMember.Bio);
    }

    [Fact]
    public async Task UpdateEntity_WhenNotExists_ReturnsNoContent()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";
        var fcMemberDto = new FcMemberDto
        {
            Id = nonExistentId,
            Name = "Non-existent",
            CharacterId = "555666",
            Title = "Tank",
            FcRank = "Member",
            Avatar = "https://example.com/avatar.png",
            Bio = "Bio"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"api/FcMembers/{nonExistentId}", fcMemberDto);

        // Assert
        // Note: The controller doesn't check if entity exists before updating,
        // so it returns NoContent even if entity doesn't exist
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Functional Tests - Delete

    [Fact]
    public async Task DeleteEntity_WhenExists_DeletesFcMember()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var fcMemberDto = new FcMemberDto
        {
            Name = "To Delete",
            CharacterId = "777888",
            Title = "Healer",
            FcRank = "Member",
            Avatar = "https://example.com/avatar.png",
            Bio = "Will be deleted"
        };

        var createResponse = await Client.PostAsJsonAsync("api/FcMembers", fcMemberDto);
        var createdFcMember = await createResponse.Content.ReadFromJsonAsync<FcMemberDto>();

        // Act
        var deleteResponse = await Client.DeleteAsync($"api/FcMembers/{createdFcMember!.Id}");

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"api/FcMembers/{createdFcMember.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdmin();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.DeleteAsync($"api/FcMembers/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion
}