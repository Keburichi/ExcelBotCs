namespace ExcelBotCs.Models.Discord;

public class OAuthProviders
{
    public required Dictionary<string, OAuthConfig> Providers { get; set; }
}