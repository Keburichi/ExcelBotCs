namespace ExcelBotCs.Models.Domain;

public class LodestoneDuty
{
    public string Name { get; set; }
    public string LodestoneId { get; set; }
    public List<string> BossNames { get; set; } = new();
    public string Url => $"https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/{LodestoneId}/";
}
