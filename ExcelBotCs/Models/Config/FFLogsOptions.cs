using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("FFLogs")]
public class FFLogsOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = "https://www.fflogs.com/oauth/token";
    public string ApiUrl { get; set; } = "https://www.fflogs.com/api/v2/client";

    /// <summary>
    /// Number of members to sync per wave (default: 20)
    /// </summary>
    public int MembersPerWave { get; set; } = 20;

    /// <summary>
    /// Delay in milliseconds between individual member queries (default: 500ms)
    /// </summary>
    public int DelayBetweenRequestsMs { get; set; } = 500;
}
