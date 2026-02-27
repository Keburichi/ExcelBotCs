using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ExcelBotCs.Data;
public class Database
{
	private readonly IMongoDatabase _database;

	public Database(IOptions<DatabaseOptions> options, ILogger<Database> logger)
	{
		var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
		settings.ServerApi = new ServerApi(ServerApiVersion.V1);

		var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);

		if (!BsonSerializer.TryRegisterSerializer(objectSerializer))
			logger.LogWarning("Serializer was already registered");

		var client = new MongoClient(settings);
		try
		{
			var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
			logger.LogInformation("Successfully connected to MongoDB");

			_database = client.GetDatabase(options.Value.DatabaseName);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to connect to MongoDB");
			throw;
		}
	}

	public Repository<T> GetCollection<T>(string collection) where T : DatabaseObject
	{
		return new Repository<T>(_database.GetCollection<T>(collection));
	}
}