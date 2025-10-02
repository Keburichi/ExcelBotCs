namespace ExcelBotCs.Extensions;

public static class DateTimeExtensions
{
    public static string ToShortDiscordTime(this DateTime datetime)
        => $"<t:{((DateTimeOffset)datetime).ToUnixTimeSeconds()}:t>";
    
    public static string ToLongDiscordTime(this DateTime datetime)
        => $"<t:{((DateTimeOffset)datetime).ToUnixTimeSeconds()}:T>";
    
    public static string ToShortDiscordDate(this DateTime datetime)
        => $"<t:{((DateTimeOffset)datetime).ToUnixTimeSeconds()}:d>";
    
    public static string ToLongDiscordDate(this DateTime datetime)
        => $"<t:{((DateTimeOffset)datetime).ToUnixTimeSeconds()}:D>";
    
    public static string ToLongDiscordDateShortTime(this DateTime datetime)
        => $"<t:{((DateTimeOffset)datetime).ToUnixTimeSeconds()}:f>";
    
    public static string ToLongDiscordDateLongTime(this DateTime datetime)
        => $"<t:{((DateTimeOffset)datetime).ToUnixTimeSeconds()}:F>";
    
    public static string ToRelativeDiscordTime(this DateTime datetime)
        => $"<t:{((DateTimeOffset)datetime).ToUnixTimeSeconds()}:R>";
}