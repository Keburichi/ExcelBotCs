using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ExcelBotCs.Data;
public class Database
{
	private readonly IMongoDatabase _database;

	public Database(IOptions<DatabaseOptions> options)
	{
		var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
		settings.ServerApi = new ServerApi(ServerApiVersion.V1);
		settings.LinqProvider = LinqProvider.V3;

		var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
		BsonSerializer.RegisterSerializer(objectSerializer);

		var client = new MongoClient(settings);
		try
		{
			var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
			Console.WriteLine("Successfully connected to Mongodb");

			_database = client.GetDatabase(options.Value.DatabaseName);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			throw;
		}
	}

	public Repository<T> GetCollection<T>(string collection) where T : DatabaseObject
	{
		return new Repository<T>(_database.GetCollection<T>(collection));
	}
}