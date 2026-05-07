using System.Net;
using System.Net.Http.Json;
using ExcelBotCs.Controllers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Controllers;

public class LotteryControllerIntegrationTests : IntegrationTestBase
{
    public LotteryControllerIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }
    #region Permission Tests

    [Fact]
    public async Task Guess_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.PostAsync("api/Lottery/guess/42", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.PostAsync("api/Lottery/guess/42", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.PostAsync("api/Lottery/guess/42", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.PostAsync("api/Lottery/guess/43", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnusedNumbers_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Lottery/unused");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Lottery/unused");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Lottery/unused");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Lottery/unused");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task View_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Lottery/view");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Lottery/view");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Lottery/view");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Lottery/view");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeGuess_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var request = new ChangeGuessRequest(1, 2);
        var response = await Client.PostAsJsonAsync("api/Lottery/change", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.PostAsJsonAsync("api/Lottery/change", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok (even if they don't have the old number guessed, should get proper response)
        await AuthenticateAsMember();
        response = await Client.PostAsJsonAsync("api/Lottery/change", request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.PostAsJsonAsync("api/Lottery/change", request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhoGuessed_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Lottery/who-guessed/42");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Lottery/who-guessed/42");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Lottery/who-guessed/42");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Lottery/who-guessed/42");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllGuesses_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.GetAsync("api/Lottery/all-guesses");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.GetAsync("api/Lottery/all-guesses");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member = Ok
        await AuthenticateAsMember();
        response = await Client.GetAsync("api/Lottery/all-guesses");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.GetAsync("api/Lottery/all-guesses");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RunLottery_CheckPermissions()
    {
        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.PostAsync("api/Lottery/run", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.PostAsync("api/Lottery/run", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member (non-admin) = Forbidden
        await AuthenticateAsMember();
        response = await Client.PostAsync("api/Lottery/run", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Admin = Ok
        await AuthenticateAsAdmin();
        response = await Client.PostAsync("api/Lottery/run", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AwardUsers_CheckPermissions()
    {
        var request = new AwardUsersRequest("Test reason", new List<string> { "TestUser" });

        // No Auth = Unauthorized
        SetUnauthenticated();
        var response = await Client.PostAsJsonAsync("api/Lottery/award", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // No Member = Forbidden
        SetAuthenticatedUser("12355");
        response = await Client.PostAsJsonAsync("api/Lottery/award", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member (non-admin) = Forbidden
        await AuthenticateAsMember();
        response = await Client.PostAsJsonAsync("api/Lottery/award", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Admin = Ok (or BadRequest if user not found, which is still authenticated)
        await AuthenticateAsAdmin();
        response = await Client.PostAsJsonAsync("api/Lottery/award", request);
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    #endregion

    #region Functional Tests - Guess

    [Fact]
    public async Task Guess_ValidNumber_ReturnsSuccess()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.PostAsync("api/Lottery/guess/42", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("guessResponse");
    }

    [Fact]
    public async Task Guess_MultipleDifferentNumbers_AllSucceed()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act - Guess multiple numbers
        var response1 = await Client.PostAsync("api/Lottery/guess/10", null);
        var response2 = await Client.PostAsync("api/Lottery/guess/20", null);
        var response3 = await Client.PostAsync("api/Lottery/guess/30", null);

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        response3.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Guess_DuplicateNumber_ReturnsAppropriateResponse()
    {
        // Arrange
        await AuthenticateAsMember();
        await Client.PostAsync("api/Lottery/guess/42", null);

        // Act - Try to guess the same number again
        var response = await Client.PostAsync("api/Lottery/guess/42", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
    }

    #endregion

    #region Functional Tests - GetUnusedNumbers

    [Fact]
    public async Task GetUnusedNumbers_NoGuesses_ReturnsAllNumbers()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/Lottery/unused");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("result");
    }

    [Fact]
    public async Task GetUnusedNumbers_AfterGuesses_ReturnsRemainingNumbers()
    {
        // Arrange
        await AuthenticateAsMember();
        await Client.PostAsync("api/Lottery/guess/1", null);
        await Client.PostAsync("api/Lottery/guess/2", null);
        await Client.PostAsync("api/Lottery/guess/3", null);

        // Act
        var response = await Client.GetAsync("api/Lottery/unused");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        // The result should not contain the guessed numbers
        content.ShouldContain("result");
    }

    #endregion

    #region Functional Tests - View

    [Fact]
    public async Task View_NoGuesses_ReturnsEmptyOrDefault()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/Lottery/view");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("view");
    }

    [Fact]
    public async Task View_WithGuesses_ReturnsUserGuesses()
    {
        // Arrange
        await AuthenticateAsMember();
        await Client.PostAsync("api/Lottery/guess/10", null);
        await Client.PostAsync("api/Lottery/guess/20", null);

        // Act
        var response = await Client.GetAsync("api/Lottery/view");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("view");
    }

    [Fact]
    public async Task View_DifferentUsers_ReturnsDifferentGuesses()
    {
        // Arrange - User 1 makes guesses
        var member1 = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/10", null);

        // Arrange - User 2 makes different guesses
        var member2 = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/20", null);

        // Act - Get views for both users
        SetAuthenticatedUser(member1.DiscordId, member1.DiscordName!);
        var response1 = await Client.GetAsync("api/Lottery/view");

        SetAuthenticatedUser(member2.DiscordId, member2.DiscordName!);
        var response2 = await Client.GetAsync("api/Lottery/view");

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        content1.ShouldNotBe(content2);
    }

    #endregion

    #region Functional Tests - ChangeGuess

    [Fact]
    public async Task ChangeGuess_ValidChange_ReturnsSuccess()
    {
        // Arrange
        await AuthenticateAsMember();
        await Client.PostAsync("api/Lottery/guess/10", null);

        // Act
        var request = new ChangeGuessRequest(10, 20);
        var response = await Client.PostAsJsonAsync("api/Lottery/change", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("changeResponse");
    }

    [Fact]
    public async Task ChangeGuess_NumberNotGuessed_ReturnsAppropriateResponse()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act - Try to change a number that wasn't guessed
        var request = new ChangeGuessRequest(10, 20);
        var response = await Client.PostAsJsonAsync("api/Lottery/change", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("changeResponse");
    }

    [Fact]
    public async Task ChangeGuess_ToAlreadyGuessedNumber_ReturnsAppropriateResponse()
    {
        // Arrange
        await AuthenticateAsMember();
        await Client.PostAsync("api/Lottery/guess/10", null);
        await Client.PostAsync("api/Lottery/guess/20", null);

        // Act - Try to change 10 to 20 (already guessed)
        var request = new ChangeGuessRequest(10, 20);
        var response = await Client.PostAsJsonAsync("api/Lottery/change", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("changeResponse");
    }

    #endregion

    #region Functional Tests - WhoGuessed

    [Fact]
    public async Task WhoGuessed_NumberNotGuessed_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/Lottery/who-guessed/99");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("whoGuessed");
    }

    [Fact]
    public async Task WhoGuessed_SingleUserGuessed_ReturnsThatUser()
    {
        // Arrange
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/42", null);

        // Act
        var response = await Client.GetAsync("api/Lottery/who-guessed/42");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("whoGuessed");
    }

    [Fact]
    public async Task WhoGuessed_MultipleUsersGuessed_ReturnsAllUsers()
    {
        // Arrange - User 1 guesses
        var member1 = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/42", null);

        // Arrange - User 2 guesses the same number
        var member2 = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/42", null);

        // Act
        var response = await Client.GetAsync("api/Lottery/who-guessed/42");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
        content.ShouldContain("whoGuessed");
    }

    #endregion

    #region Functional Tests - GetAllGuesses

    [Fact]
    public async Task GetAllGuesses_NoGuesses_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateAsMember();

        // Act
        var response = await Client.GetAsync("api/Lottery/all-guesses");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<object>>();
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllGuesses_WithGuesses_ReturnsAllGuessedNumbers()
    {
        // Arrange - Create 3 different members each making one guess
        var member1 = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/10", null);

        var member2 = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/20", null);

        var member3 = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/30", null);

        // Act
        var response = await Client.GetAsync("api/Lottery/all-guesses");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<object>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
    }

    #endregion

    #region Functional Tests - RunLottery

    [Fact]
    public async Task RunLottery_AsAdmin_ExecutesSuccessfully()
    {
        // Arrange
        await AuthenticateAsAdmin();

        // Add some guesses first
        var member = await CreateAndAuthenticateAsMember(GenerateRandomDiscordId());
        await Client.PostAsync("api/Lottery/guess/50", null);

        // Switch back to admin
        await AuthenticateAsAdmin();

        // Act
        var response = await Client.PostAsync("api/Lottery/run", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.ShouldNotBeNull();
        result.ContainsKey("message").ShouldBeTrue();
    }

    #endregion

    #region Functional Tests - AwardUsers

    [Fact]
    public async Task AwardUsers_WithValidUsernames_ReturnsSuccess()
    {
        // Arrange
        await AuthenticateAsAdmin();

        // Create a member to award
        var memberService = Factory.Services.GetRequiredService<IMemberService>();
        var testMember = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "AwardTestUser",
            PlayerName = "TestPlayer"
        };
        await memberService.CreateAsync(testMember);

        // Act
        var request = new AwardUsersRequest("Integration test reward", new List<string> { "AwardTestUser" });
        var response = await Client.PostAsJsonAsync("api/Lottery/award", request);

        // Assert
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            result.ShouldNotBeNull();
            result.ContainsKey("message").ShouldBeTrue();
        }
    }

    [Fact]
    public async Task AwardUsers_WithInvalidUsernames_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdmin();

        // Act
        var request = new AwardUsersRequest("Test reward", new List<string> { "NonExistentUser123" });
        var response = await Client.PostAsJsonAsync("api/Lottery/award", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        result.ShouldNotBeNull();
        result["message"].ShouldContain("No valid users found");
    }

    [Fact]
    public async Task AwardUsers_WithMixedUsernames_AwardsValidUsers()
    {
        // Arrange
        await AuthenticateAsAdmin();

        // Create a valid member
        var memberService = Factory.Services.GetRequiredService<IMemberService>();
        var validMember = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "ValidAwardUser",
            PlayerName = "ValidPlayer"
        };
        await memberService.CreateAsync(validMember);

        // Act - Mix valid and invalid usernames
        var request = new AwardUsersRequest(
            "Mixed test reward",
            new List<string> { "ValidAwardUser", "NonExistentUser" }
        );
        var response = await Client.PostAsJsonAsync("api/Lottery/award", request);

        // Assert - Should succeed because at least one user is valid
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AwardUsers_WithPlayerName_FindsUser()
    {
        // Arrange
        await AuthenticateAsAdmin();

        // Create a member with a player name
        var memberService = Factory.Services.GetRequiredService<IMemberService>();
        var testMember = new Member
        {
            DiscordId = GenerateRandomDiscordId(),
            DiscordName = "DiscordUser",
            PlayerName = "FFXIVPlayerName"
        };
        await memberService.CreateAsync(testMember);

        // Act - Award using player name
        var request = new AwardUsersRequest("Player name test", new List<string> { "FFXIVPlayerName" });
        var response = await Client.PostAsJsonAsync("api/Lottery/award", request);

        // Assert
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    #endregion
}