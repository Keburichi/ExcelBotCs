using ExcelBotCs.Models.DTO.Fights;

namespace ExcelBotCs.Models.DTO.Bosses;

public class BossResponse : BaseDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsUltimate { get; set; }
    public int? FFLogsExpansionId { get; set; }
    public List<FightSummaryResponse> Fights { get; set; } = new();
}
