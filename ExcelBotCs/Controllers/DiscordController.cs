using AspNet.Security.OAuth.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Import;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscordController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly JwtOptions _jwtOptions;
    private readonly RsaKeyService _rsaKeyService;
    private readonly ImportService _importService;
    private readonly IOptions<DiscordBotOptions> _discordBotOptions;

    public DiscordController(IMemberService memberService, IOptions<JwtOptions> jwtOptions, RsaKeyService rsaKeyService,
        ImportService importService, IOptions<DiscordBotOptions> discbordBotOptions)
    {
        _memberService = memberService;
        _jwtOptions = jwtOptions.Value;
        _rsaKeyService = rsaKeyService;
        _importService = importService;
        _discordBotOptions = discbordBotOptions;
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

        // If the user doesn't already exist in the database, create it
        // and sync their roles so they don't have to wait until the next scheduled sync
        // to access the website
        if (member is null)
        {
            await _memberService.CreateAsync(new Member
            {
                DiscordId = discordId,
                DiscordName = discordName,
                DiscordAvatar = discordAvatar
            });
        }

        // only import the member if there are no roles assigned yet
        if (member is null || member.Roles.Count == 0)
            await _importService.ImportMembers(_discordBotOptions.Value.GuildId);

        // Redirect to SPA home where the cookie will authorize API calls
        return Results.Redirect("/");
    }
}