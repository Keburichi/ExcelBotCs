using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Models.Database;

public class Raidplan : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }

    // Store only the reference to the author
    public string AuthorId { get; set; }

    // Ignored by MongoDB, populated at runtime when needed
    [BsonIgnore] public Member? Author { get; set; }
}