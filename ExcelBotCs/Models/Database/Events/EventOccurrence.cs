using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Models.Database.Events;

/// <summary>
///     Represents a single occurrence of an event (embedded subdocument, not a separate collection)
/// </summary>
[BsonIgnoreExtraElements]
public class EventOccurrence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurrenceDate { get; set; }
    public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Scheduled;
}