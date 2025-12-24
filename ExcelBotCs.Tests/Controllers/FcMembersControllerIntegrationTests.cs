using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

[TestFixture]
public class FcMembersControllerIntegrationTests : IntegrationTestBase
{
    #region Permission Tests

    [Test]
    public async Task GetEntities_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/FcMembers");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/FcMembers");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/FcMembers");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/FcMembers");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Functional Tests - Get

    [Test]
    public async Task GetEntities_NoFcMembers_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/FcMembers");

        // Assert
        response.EnsureSuccessStatusCode();
        var fcMembers = await response.Content.ReadFromJsonAsync<List<FcMemberDto>>();
        Assert.That(fcMembers, Is.Not.Null);
        Assert.That(fcMembers, Is.Empty);
    }

    [Test]
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
        Assert.That(fcMembers, Is.Not.Null);
        Assert.That(fcMembers, Has.Count.AtLeast(2));
        Assert.That(fcMembers.Any(m => m.Name == fcMember1.Name), Is.True);
        Assert.That(fcMembers.Any(m => m.Name == fcMember2.Name), Is.True);
    }

    [Test]
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
        Assert.That(retrievedFcMember, Is.Not.Null);
        Assert.That(retrievedFcMember.Id, Is.EqualTo(fcMember.Id));
        Assert.That(retrievedFcMember.Name, Is.EqualTo(fcMember.Name));
        Assert.That(retrievedFcMember.CharacterId, Is.EqualTo(fcMember.CharacterId));
    }

    [Test]
    public async Task GetEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.GetAsync($"api/FcMembers/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion

    #region Functional Tests - Create

    [Test]
    public async Task CreateEntity_ValidData_CreatesFcMember()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var createdFcMember = await response.Content.ReadFromJsonAsync<FcMemberDto>();
        Assert.That(createdFcMember, Is.Not.Null);
        Assert.That(createdFcMember.Name, Is.EqualTo(fcMemberDto.Name));
        Assert.That(createdFcMember.CharacterId, Is.EqualTo(fcMemberDto.CharacterId));
    }

    [Test]
    public async Task CreateEntity_PersistsToDatabase()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(retrievedFcMember, Is.Not.Null);
        Assert.That(retrievedFcMember.Name, Is.EqualTo(fcMemberDto.Name));
    }

    #endregion

    #region Functional Tests - Update

    [Test]
    public async Task UpdateEntity_ValidData_UpdatesFcMember()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify the update
        var getResponse = await Client.GetAsync($"api/FcMembers/{createdFcMember.Id}");
        var updatedFcMember = await getResponse.Content.ReadFromJsonAsync<FcMemberDto>();
        Assert.That(updatedFcMember, Is.Not.Null);
        Assert.That(updatedFcMember.Name, Is.EqualTo(createdFcMember.Name));
        Assert.That(updatedFcMember.Title, Is.EqualTo(createdFcMember.Title));
        Assert.That(updatedFcMember.Bio, Is.EqualTo(createdFcMember.Bio));
    }

    [Test]
    public async Task UpdateEntity_WhenNotExists_ReturnsNoContent()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    #endregion

    #region Functional Tests - Delete

    [Test]
    public async Task DeleteEntity_WhenExists_DeletesFcMember()
    {
        // Arrange
        await AuthenticateAsMember();
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
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify deletion
        var getResponse = await Client.GetAsync($"api/FcMembers/{createdFcMember.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteEntity_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsMember();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var response = await Client.DeleteAsync($"api/FcMembers/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion
}