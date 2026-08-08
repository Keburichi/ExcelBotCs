using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("Minecraft")]
public class MinecraftOptions
{
    public string RconHost { get; set; } = string.Empty;
    public int RconPort { get; set; } = 25575;
    public string RconPassword { get; set; } = string.Empty;
}
