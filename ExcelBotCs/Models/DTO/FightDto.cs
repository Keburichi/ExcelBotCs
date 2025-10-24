using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO;

public class FightDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public FightType Type { get; set; }
    public List<Raidplan> Raidplans { get; set; }

    // FFLogs Integration Fields
    public int? FFLogsEncounterId { get; set; }      // Primary identifier from FFLogs
    public int? FFLogsZoneId { get; set; }           // Zone/raid tier ID
    public string? FFLogsZoneName { get; set; }      // Zone name (e.g., "Abyssos")
    public int? FFLogsDifficultyId { get; set; }
    public int? FFLogsExpansionId { get; set; }      // Expansion identifier
    public string? FFLogsExpansionName { get; set; } // Expansion name
    public bool IsFrozen { get; set; }               // If zone is archived
}