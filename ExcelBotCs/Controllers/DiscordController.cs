using AspNet.Security.OAuth.Discord;
using Discord.WebSocket;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscordController : ControllerBase
{
    private readonly MemberService _memberService;
    private readonly JwtOptions _jwtOptions;
    private readonly RsaKeyService _rsaKeyService;

    public DiscordController(MemberService memberService, IOptions<JwtOptions> jwtOptions, RsaKeyService rsaKeyService)
    {
        _memberService = memberService;
        _jwtOptions = jwtOptions.Value;
        _rsaKeyService = rsaKeyService;
    }

    [HttpGet]
    [Route("login")]
    public IResult Login()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/discord/get-token", // The URL to redirect to after successful authentication
            IsPersistent =
                true // Ensures the session persists across requests (the authentication cookie will be stored)
        };

        // Triggers the OAuth challenge and redirects the user to Discord's authorization page
        return Results.Challenge(properties, [DiscordAuthenticationDefaults.AuthenticationScheme]);
    }

    [HttpGet]
    [Route("get-token")]
    public async Task<IResult> GetToken()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
        {
            return Results.Unauthorized();
        }

        var claims = result.Principal.Claims.ToList();

        // Ensure RSA keys are present (kept for future JWT use if needed)
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, AppContext.BaseDirectory);

        // IMPORTANT: Keep the cookie session; do not sign out. This allows cookie-authenticated API access.
        // await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Create the user if it doesn't already exist in the database
        var discordId = claims
            .First(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
        var discordName = claims.First(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
            .Value;
        var discordAvatar = claims.First(c => c.Type == "urn:discord:avatar:url").Value;

        var member = await _memberService.GetByDiscordId(discordId);

        if (member is null)
            _memberService.CreateAsync(new Member()
            {
                DiscordId = discordId,
                DiscordName = discordName,
                DiscordAvatar = discordAvatar
            });

        // Redirect to SPA home where the cookie will authorize API calls
        return Results.Redirect("/");
    }

    [Route("callback")]
    [HttpPost]
    public IActionResult AuthCallback()
    {
        return Ok("Hello wold");
    }

    [Route("callback")]
    [HttpGet]
    public IActionResult GetCallback()
    {
        // Excract code from query string
        Request.Query.TryGetValue("code", out var code);

        return Ok("Hello wold");
    }
}