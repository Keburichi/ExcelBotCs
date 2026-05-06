using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("Lodestone")]
public class LodestoneOptions
{
    public string FCId { get; set; }
    public string BaseUrl { get; set; } = "https://na.finalfantasyxiv.com";
    public int RequestDelayMs { get; set; } = 1000;
}