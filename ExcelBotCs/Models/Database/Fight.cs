namespace ExcelBotCs.Models.Database;

public class Fight : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public FightType Type { get; set; }
    public List<Raidplan> Raidplans { get; set; }
}