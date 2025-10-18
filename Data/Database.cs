using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ExcelBotCs.Data;
public class Database
{
	private readonly IMongoDatabase _database;

	public Database(DatabaseOptions options)
	{
		var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
		settings.ServerApi = new ServerApi(ServerApiVersion.V1);

		var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
		BsonSerializer.RegisterSerializer(objectSerializer);

		var client = new MongoClient(settings);
		try
		{
			var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
			Console.WriteLine("Successfully connected to Mongodb");

			_database = client.GetDatabase(options.DatabaseName);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			throw;
		}
	}

	public Repository<T> GetCollection<T>(string collection) where T : DatabaseObject => new(_database.GetCollection<T>(collection));
}