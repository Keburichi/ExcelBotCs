using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("Lodestone")]
public class LodestoneOptions
{
    public string FCId { get; set; }
}