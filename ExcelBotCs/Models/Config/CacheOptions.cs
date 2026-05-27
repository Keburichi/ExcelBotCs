using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("Cache")]
public class CacheOptions
{
    public string Provider { get; set; } = "InMemory";
    public string? RedisConnectionString { get; set; }
    public int FreshnessCheckIntervalSeconds { get; set; } = 60;
}
