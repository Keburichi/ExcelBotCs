namespace ExcelBotCs.Models.Database;

public class FFLogsImportLog : BaseEntity
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public FFLogsImportType ImportType { get; set; } // Fights, MemberActivity
    public int ItemsProcessed { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsSkipped { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ApiRequestCount { get; set; }
}

public enum FFLogsImportType
{
    FightImport,
    MemberActivitySync
}
