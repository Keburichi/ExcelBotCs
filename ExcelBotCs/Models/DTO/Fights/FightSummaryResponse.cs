using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO.Fights;

public class FightSummaryResponse : BaseDto
{
    public string Name { get; set; }
    public FightType Type { get; set; }
    public int? FFLogsEncounterId { get; set; }
    public bool IsFrozen { get; set; }
}
