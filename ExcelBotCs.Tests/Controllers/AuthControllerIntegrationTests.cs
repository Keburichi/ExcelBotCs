using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Tests.Utils;

namespace ExcelBotCs.Tests.Controllers;

[TestFixture]
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    #region Permission Tests

    [Test]
    public async Task Index_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetMe_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Auth/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Auth/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Auth/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Auth/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Functional Tests - Index

    [Test]
    public async Task Index_AsMember_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // HEAD requests should not have content
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Empty);
    }

    [Test]
    public async Task Index_AsAdmin_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsAdmin();

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "api/Auth"));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // HEAD requests should not have content
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Empty);
    }

    #endregion

    #region Functional Tests - GetMe

    [Test]
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
        Assert.That(returnedMember, Is.Not.Null);
        Assert.That(returnedMember.Id, Is.EqualTo(member.Id));
        Assert.That(returnedMember.DiscordId, Is.EqualTo(discordId));
        Assert.That(returnedMember.DiscordName, Is.EqualTo("Test Member"));
        Assert.That(returnedMember.PlayerName, Is.EqualTo("Test Member"));
    }

    [Test]
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
        Assert.That(returnedMember, Is.Not.Null);
        Assert.That(returnedMember.Id, Is.EqualTo(member.Id));
        Assert.That(returnedMember.DiscordId, Is.EqualTo(discordId));
        Assert.That(returnedMember.IsAdmin, Is.True);
    }

    [Test]
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
        Assert.That(returnedMember, Is.Not.Null);
        Assert.That(returnedMember.Id, Is.EqualTo(member.Id));
        Assert.That(returnedMember.DiscordId, Is.EqualTo(discordId));
        Assert.That(returnedMember.IsDeveloper, Is.True);
    }

    [Test]
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

        Assert.That(member1, Is.Not.Null);
        Assert.That(member2, Is.Not.Null);
        Assert.That(member3, Is.Not.Null);

        // All three calls should return the same member
        Assert.That(member1.Id, Is.EqualTo(member.Id));
        Assert.That(member2.Id, Is.EqualTo(member.Id));
        Assert.That(member3.Id, Is.EqualTo(member.Id));

        Assert.That(member1.DiscordId, Is.EqualTo(discordId));
        Assert.That(member2.DiscordId, Is.EqualTo(discordId));
        Assert.That(member3.DiscordId, Is.EqualTo(discordId));
    }

    [Test]
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
        Assert.That(returnedMember1, Is.Not.Null);
        Assert.That(returnedMember2, Is.Not.Null);
        Assert.That(returnedMember1.Id, Is.Not.EqualTo(returnedMember2.Id));
        Assert.That(returnedMember1.DiscordId, Is.EqualTo(discordId1));
        Assert.That(returnedMember2.DiscordId, Is.EqualTo(discordId2));
        Assert.That(returnedMember1.DiscordName, Is.EqualTo("Member One"));
        Assert.That(returnedMember2.DiscordName, Is.EqualTo("Member Two"));
    }

    #endregion
}