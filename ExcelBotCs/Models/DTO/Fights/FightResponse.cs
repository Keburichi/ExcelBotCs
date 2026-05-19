using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Resources;

namespace ExcelBotCs.Models.DTO.Fights;

public class FightResponse : BaseDto
{
    public string Name { get; set; }
    public FightType Type { get; set; }
    public string? BossId { get; set; }
    public string? BossName { get; set; }
    public string? ImageUrl { get; set; }

    // FFLogs metadata
    public int? FFLogsEncounterId { get; set; }
    public int? FFLogsZoneId { get; set; }
    public string? FFLogsZoneName { get; set; }
    public int? FFLogsExpansionId { get; set; }
    public string? FFLogsExpansionName { get; set; }
    public bool IsFrozen { get; set; }

    public List<ResourceResponse> Resources { get; set; } = new();
}
