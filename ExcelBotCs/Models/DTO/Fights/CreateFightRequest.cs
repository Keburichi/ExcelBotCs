using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO.Fights;

public class CreateFightRequest
{
    public string Name { get; set; }
    public FightType Type { get; set; }
    public string? BossId { get; set; }
    public int? FFLogsEncounterId { get; set; }
    public int? FFLogsZoneId { get; set; }
    public string? FFLogsZoneName { get; set; }
    public int? FFLogsDifficultyId { get; set; }
    public int? FFLogsExpansionId { get; set; }
    public string? FFLogsExpansionName { get; set; }
}
