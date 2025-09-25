using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ExcelBotCs.Utilities;

/// <summary>
/// IXmlRepository implementation that stores Data Protection XML keys in MongoDB.
/// </summary>
public class MongoXmlRepository : IXmlRepository
{
    private readonly IMongoCollection<DataProtectionKeyDocument> _collection;

    public MongoXmlRepository(string connectionString, string databaseName,
        string collectionName = "DataProtectionKeys")
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(databaseName);
        _collection = db.GetCollection<DataProtectionKeyDocument>(collectionName);
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var docs = _collection.Find(Builders<DataProtectionKeyDocument>.Filter.Empty).ToList();
        var elements = new List<XElement>(docs.Count);
        foreach (var doc in docs)
        {
            if (string.IsNullOrWhiteSpace(doc.Xml))
                continue;
            elements.Add(XElement.Parse(doc.Xml));
        }

        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        var xml = element.ToString(SaveOptions.DisableFormatting);
        var filter = Builders<DataProtectionKeyDocument>.Filter.Eq(x => x.FriendlyName, friendlyName);
        var update = Builders<DataProtectionKeyDocument>.Update
            .Set(x => x.FriendlyName, friendlyName)
            .Set(x => x.Xml, xml)
            .SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
            .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow);

        _collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
    }

    private class DataProtectionKeyDocument
    {
        [BsonId] public ObjectId Id { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
        public string Xml { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}