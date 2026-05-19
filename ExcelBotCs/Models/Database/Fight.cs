namespace ExcelBotCs.Models.Database;

public class Fight : BaseEntity
{
    public string Name { get; set; }
    public FightType Type { get; set; }
    public List<Raidplan> Raidplans { get; set; }
    public string? BossId { get; set; }

    // FFLogs Integration Fields
    public int? FFLogsEncounterId { get; set; }      // Primary identifier from FFLogs
    public int? FFLogsZoneId { get; set; }           // Zone/raid tier ID
    public string? FFLogsZoneName { get; set; }      // Zone name (e.g., "Abyssos")
    public int? FFLogsDifficultyId { get; set; }
    public int? FFLogsExpansionId { get; set; }      // Expansion identifier
    public string? FFLogsExpansionName { get; set; } // Expansion name
    public bool IsFrozen { get; set; }               // If zone is archived

    public override string ToString()
    {
        return $"{Name}";
    }

    public bool IsFightDifficult()
    {
        return Type switch
        {
            FightType.Normal => false,
            FightType.Extreme or FightType.Savage or FightType.Ultimate or FightType.Chaotic
                or FightType.Unreal => true,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}