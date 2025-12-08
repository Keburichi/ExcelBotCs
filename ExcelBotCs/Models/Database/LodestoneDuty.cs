namespace ExcelBotCs.Models.Database;

public class LodestoneDuty : BaseEntity
{
    public string Name { get; set; }
    public string LodestoneId { get; set; }
    public List<string> BossNames { get; set; } = new();
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    // Cache metadata
    public int ExpansionId { get; set; }
    public int CategoryId { get; set; }
    public FightType FightType { get; set; }
    public DateTime LastSyncTime { get; set; }

    public string Url => $"https://na.finalfantasyxiv.com/lodestone/playguide/db/duty/{LodestoneId}/";
}
