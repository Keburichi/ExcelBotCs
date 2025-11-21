namespace ExcelBotCs.Models.Database;

/// <summary>
///     Represents a single occurrence of an event (embedded subdocument, not a separate collection)
/// </summary>
public class EventOccurrence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurrenceDate { get; set; }
    public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Scheduled;
    public string? DiscordMessageId { get; set; }

    /// <summary>
    ///     Signups for this specific occurrence (for tracking and statistics)
    /// </summary>
    public List<EventSignup> Signups { get; set; } = new();

    /// <summary>
    ///     Selected participants for this specific occurrence
    /// </summary>
    public List<EventParticipant> Participants { get; set; } = new();
}