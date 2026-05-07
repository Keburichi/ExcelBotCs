using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;

namespace ExcelBotCs.Tests.Controllers;

public class AuthControllerIntegrationTests : IntegrationTestBase
{
    public AuthControllerIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }
    #region Permission Tests

    [Fact]
    public async Task Index_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMe_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Auth/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Auth/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Auth/me");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Auth/me");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Functional Tests - Index

    [Fact]
    public async Task Index_AsMember_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // HEAD requests should not have content
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBeEmpty();
    }

    [Fact]
    public async Task Index_AsAdmin_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsAdmin();

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // HEAD requests should not have content
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBeEmpty();
    }

    #endregion

    #region Functional Tests - GetMe

    [Fact]
    public async Task GetMe_AsMember_ReturnsCurrentMember()
    {
        // Arrange
        var discordId = GenerateRandomDiscordId();
        var member = await CreateAndAuthenticateAsMember(discordId);

        // Act
        var response = await Client.GetAsync("api/Auth/me");

        // Assert
        response.EnsureSuccessStatusCode();
        var returnedMember = await response.Content.ReadFromJsonAsync<Member>();
        returnedMember.ShouldNotBeNull();
        returnedMember.Id.ShouldBe(member.Id);
        returnedMember.DiscordId.ShouldBe(discordId);
        returnedMember.DiscordName.ShouldBe("Test Member");
        returnedMember.PlayerName.ShouldBe("Test Member");
    }

    [Fact]
    public async Task GetMe_AsAdmin_ReturnsCurrentMemberWithAdminFlag()
    {
        // Arrange
        var discordId = GenerateRandomDiscordId();
        var member = await CreateAndAuthenticateAsMember(discordId, true, true, false, "Admin User");

        // Act
        var response = await Client.GetAsync("api/Auth/me");

        // Assert
        response.EnsureSuccessStatusCode();
        var returnedMember = await response.Content.ReadFromJsonAsync<Member>();
        returnedMember.ShouldNotBeNull();
        returnedMember.Id.ShouldBe(member.Id);
        returnedMember.DiscordId.ShouldBe(discordId);
        returnedMember.IsAdmin.ShouldBe(true);
    }

    [Fact]
    public async Task GetMe_AsDeveloper_ReturnsCurrentMemberWithDeveloperFlag()
    {
        // Arrange
        var discordId = GenerateRandomDiscordId();
        var member = await CreateAndAuthenticateAsMember(discordId, false, true, true, "Developer User");

        // Act
        var response = await Client.GetAsync("api/Auth/me");

        // Assert
        response.EnsureSuccessStatusCode();
        var returnedMember = await response.Content.ReadFromJsonAsync<Member>();
        returnedMember.ShouldNotBeNull();
        returnedMember.Id.ShouldBe(member.Id);
        returnedMember.DiscordId.ShouldBe(discordId);
        returnedMember.IsDeveloper.ShouldBe(true);
    }

    [Fact]
    public async Task GetMe_MultipleCalls_ReturnsConsistentData()
    {
        // Arrange
        var discordId = GenerateRandomDiscordId();
        var member = await CreateAndAuthenticateAsMember(discordId, false, true, false, "Consistent Member");

        // Act - Make multiple calls
        var response1 = await Client.GetAsync("api/Auth/me");
        var response2 = await Client.GetAsync("api/Auth/me");
        var response3 = await Client.GetAsync("api/Auth/me");

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        response3.EnsureSuccessStatusCode();

        var member1 = await response1.Content.ReadFromJsonAsync<Member>();
        var member2 = await response2.Content.ReadFromJsonAsync<Member>();
        var member3 = await response3.Content.ReadFromJsonAsync<Member>();

        member1.ShouldNotBeNull();
        member2.ShouldNotBeNull();
        member3.ShouldNotBeNull();

        // All three calls should return the same member
        member1.Id.ShouldBe(member.Id);
        member2.Id.ShouldBe(member.Id);
        member3.Id.ShouldBe(member.Id);

        member1.DiscordId.ShouldBe(discordId);
        member2.DiscordId.ShouldBe(discordId);
        member3.DiscordId.ShouldBe(discordId);
    }

    [Fact]
    public async Task GetMe_DifferentMembers_ReturnsDifferentData()
    {
        // Arrange - Create first member
        var discordId1 = GenerateRandomDiscordId();
        var member1 = await CreateAndAuthenticateAsMember(discordId1, false, true, false, "Member One");

        // Act - Get first member
        var response1 = await Client.GetAsync("api/Auth/me");
        response1.EnsureSuccessStatusCode();
        var returnedMember1 = await response1.Content.ReadFromJsonAsync<Member>();

        // Arrange - Switch to second member
        var discordId2 = GenerateRandomDiscordId();
        var member2 = await CreateAndAuthenticateAsMember(discordId2, false, true, false, "Member Two");

        // Act - Get second member
        var response2 = await Client.GetAsync("api/Auth/me");
        response2.EnsureSuccessStatusCode();
        var returnedMember2 = await response2.Content.ReadFromJsonAsync<Member>();

        // Assert - Members should be different
        returnedMember1.ShouldNotBeNull();
        returnedMember2.ShouldNotBeNull();
        returnedMember1.Id.ShouldNotBe(returnedMember2.Id);
        returnedMember1.DiscordId.ShouldBe(discordId1);
        returnedMember2.DiscordId.ShouldBe(discordId2);
        returnedMember1.DiscordName.ShouldBe("Member One");
        returnedMember2.DiscordName.ShouldBe("Member Two");
    }

    #endregion
}