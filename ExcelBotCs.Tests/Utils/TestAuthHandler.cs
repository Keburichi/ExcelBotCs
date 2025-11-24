using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Tests.Utils;

/// <summary>
///     Configurable authentication handler for testing different user scenarios.
///     Set TestAuthHandlerOptions.TestUser before making requests to control authentication behavior.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<TestAuthHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var testUser = Options.TestUser;

        // If no test user is configured, fail authentication (unauthenticated scenario)
        if (testUser == null) return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, testUser.UserId),
            new(ClaimTypes.Name, testUser.UserName),
            new("discord:Id", testUser.DiscordId)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TestAuthHandlerOptions : AuthenticationSchemeOptions
{
    /// <summary>
    ///     Set this to control which user is authenticated for the next request.
    ///     Set to null to simulate an unauthenticated user.
    /// </summary>
    public TestUser? TestUser { get; set; }
}

/// <summary>
///     Represents a test user with configurable identity.
/// </summary>
public class TestUser
{
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string DiscordId { get; set; } = null!;

    public static TestUser Create(string discordId, string userName = "Test User")
    {
        return new TestUser
        {
            UserId = discordId,
            UserName = userName,
            DiscordId = discordId
        };
    }
}