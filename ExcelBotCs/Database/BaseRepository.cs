using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly IMongoClient Client;
    protected readonly IMongoCollection<T> Collection;
    protected readonly IMongoDatabase Database;

    protected BaseRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        Client = mongoClient;

        EnsureDatabaseExists(Client, databaseOptions.Value.DatabaseName, GetCollectionName());

        Database = Client.GetDatabase(databaseOptions.Value.DatabaseName);
        Collection = Database.GetCollection<T>(GetCollectionName());
    }

    public virtual async Task<List<T>> GetAsync()
    {
        return await Collection.Find(entity => true).ToListAsync();
    }

    public virtual async Task<T?> GetAsync(string id)
    {
        return await Collection.Find(entity => entity.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(T entity)
    {
        entity.DateCreated = DateTime.UtcNow;
        entity.DateModified = DateTime.UtcNow;
        await Collection.InsertOneAsync(entity);
    }

    public async Task UpdateAsync(string id, T updatedEntity)
    {
        updatedEntity.DateModified = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(entity => entity.Id == id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await Collection.DeleteOneAsync(entity => entity.Id == id);
    }

    public async Task UpsertAsync(T entity)
    {
        entity.DateModified = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(e => e.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = true });
    }

    private void EnsureDatabaseExists(IMongoClient client, string databaseName, string collectionName)
    {
        // Check if the database already exists by listing database names.
        var dbExists = client.ListDatabaseNames().ToList().Contains(databaseName);
        var db = client.GetDatabase(databaseName);

        if (!dbExists)
        {
            // Create the database by creating the initial collection.
            db.CreateCollection(collectionName);
            return;
        }

        // If the DB exists but the target collection is missing, create it as well.
        var existingCollections = db.ListCollectionNames().ToList();
        if (!existingCollections.Contains(collectionName)) db.CreateCollection(collectionName);
    }

    protected virtual string GetCollectionName()
    {
        return typeof(T).Name;
    }
}
