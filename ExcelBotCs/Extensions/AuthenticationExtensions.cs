using System.Globalization;
using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using ExcelBotCs.Data;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Discord;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExcelBotCs.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
        });

        auth.AddDiscord(options =>
        {
            var oauthProviders = configuration.GetSection("OAuthProviders").Get<OAuthProviders>();
            var discordOptions = oauthProviders!.Providers["Discord"];
            options.ClientId = discordOptions.ClientId;
            options.ClientSecret = discordOptions.ClientSecret;
            options.CallbackPath = discordOptions.CallBack;
            options.SaveTokens = true;

            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

            options.ClaimActions.MapCustomJson("urn:discord:avatar:url", user =>
                string.Format(
                    CultureInfo.InvariantCulture,
                    "https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
                    user.GetString("id"),
                    user.GetString("avatar"),
                    user.GetString("avatar")!.StartsWith("a_") ? "gif" : "png"));

            options.Scope.Add("identify");
            options.Scope.Add("guilds");
            options.Scope.Add("guilds.members.read");

            options.Events = new OAuthEvents
            {
                OnTicketReceived = context =>
                {
                    Console.WriteLine("Ticket received from Discord");
                    var claims = context.Principal?.Claims ?? Array.Empty<Claim>();
                    foreach (var claim in claims) Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                    return Task.CompletedTask;
                }
            };
        });

        auth.AddCookie(options =>
        {
            options.Cookie.Name = "DiscordAuth";
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.ExpireTimeSpan = TimeSpan.FromDays(24);

            // Keep the user logged in as long as they are active
            options.SlidingExpiration = true;

            options.Cookie.SameSite = SameSiteMode.Lax;
            // Use SameAsRequest to allow cookies over HTTP in local dev; consider Always for production behind HTTPS
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    // For API/AJAX, return 401; for normal browser navigations, redirect to root
                    var accept = ctx.Request.Headers["Accept"].ToString();
                    if (ctx.Request.Path.StartsWithSegments("/api") ||
                        accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    ctx.Response.Redirect("/");
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = ctx =>
                {
                    var accept = ctx.Request.Headers["Accept"].ToString();
                    if (ctx.Request.Path.StartsWithSegments("/api") ||
                        accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    ctx.Response.Redirect("/");
                    return Task.CompletedTask;
                }
            };
        });

        auth.AddJwtBearer(options =>
        {
            var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();
            if (jwtOptions is null) throw new InvalidOperationException("JwtOptions is not configured");

            // Ensure RSA key pair exists in MongoDB and use it for JWT validation
            var dbCfgLocal = configuration.GetSection("ExcelDatabase").Get<DatabaseOptions>()
                             ?? throw new InvalidOperationException("ExcelDatabase configuration missing");
            var rsaService = new RsaKeyService(new OptionsWrapper<DatabaseOptions>(dbCfgLocal));
            rsaService.EnsureRsaKeysPresent(jwtOptions, AppContext.BaseDirectory);
            var publicRsa = rsaService.GetPublicRsa();

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new RsaSecurityKey(publicRsa),
                RequireSignedTokens = true
            };
        });

        return services;
    }
}