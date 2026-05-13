using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Models.Database.Events;

public class EventGroup
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<EventParticipant> Participants { get; set; }
}