namespace ExcelBotCs.Models.Discord;

public class DiscordUser
{
    // Minimal fields from /users/@me when using 'identify' scope
    public string id { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string global_name { get; set; } = string.Empty; // may be null
    public string discriminator { get; set; } = string.Empty; // historically used
    public string avatar { get; set; } = string.Empty; // hash
}