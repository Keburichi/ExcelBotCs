using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Models.Database;

public class Resource : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Url { get; set; }
    public ResourceType Type { get; set; }
    public string FightId { get; set; }
    public string AuthorId { get; set; }

    [BsonIgnore] public Member? Author { get; set; }
}
