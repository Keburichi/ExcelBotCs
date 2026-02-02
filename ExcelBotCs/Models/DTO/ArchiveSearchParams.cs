using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO;

public class ArchiveSearchParams
{
    /// <summary>
    ///     Optional text search for event name
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    ///     Optional start date filter
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    ///     Optional end date filter
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    ///     Optional event type filter
    /// </summary>
    public EventType? EventType { get; set; }
}