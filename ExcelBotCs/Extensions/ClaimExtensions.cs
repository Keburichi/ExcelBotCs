using System.Security.Claims;

namespace ExcelBotCs.Extensions;

public static class ClaimExtensions
{
    public static string DiscordIdClaimType => "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
    public static string DiscordNameClaimType => "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
    public static string DiscordAvatarClaimType => "urn:discord:avatar:url";

    public static string GetDiscordId(this List<Claim> claims)
    {
        return claims
            .FirstOrDefault(x => x.Type == DiscordIdClaimType)
            ?.Value ?? string.Empty;
    }

    public static string GetDiscordName(this List<Claim> claims)
    {
        return claims
            .FirstOrDefault(x => x.Type == DiscordNameClaimType)
            ?.Value ?? string.Empty;
    }

    public static string GetDiscordAvatar(this List<Claim> claims)
    {
        return claims
            .FirstOrDefault(x => x.Type == DiscordAvatarClaimType)
            ?.Value ?? string.Empty;
    }
}