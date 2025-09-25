using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Data;
public abstract class DatabaseObject
{
	[BsonId]
	public ObjectId Id { get; set; }
	public DateTime DateCreated { get; set; }
	public DateTime DateModified { get; set; }

	public DatabaseObject()
	{
		Id = ObjectId.GenerateNewId();
		DateCreated = DateTime.UtcNow;
		DateModified = DateCreated;
	}
}