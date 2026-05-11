namespace ExcelBotCs.Models.Database;

public class BotTask : BaseEntity
{
    public string TaskType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public BotTaskStatus Status { get; set; } = BotTaskStatus.Pending;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
