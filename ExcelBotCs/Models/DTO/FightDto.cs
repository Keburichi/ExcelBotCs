using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO;

public class FightDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public FightType Type { get; set; }
    public List<Raidplan> Raidplans { get; set; }
}