using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Database.DTO;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonRepresentation(BsonType.DateTime)]
    [JsonIgnore]
    public DateTime CreateDate { get; set; }

    [JsonIgnore]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime EditDate { get; set; }
}