using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Models.Database;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonRepresentation(BsonType.DateTime)]
    [JsonIgnore]
    public DateTime DateCreated { get; set; }

    [JsonIgnore]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime DateModified { get; set; }

    protected BaseEntity()
    {
        Id = ObjectId.GenerateNewId().ToString();
        DateCreated = DateModified = DateTime.UtcNow;
    }
}
