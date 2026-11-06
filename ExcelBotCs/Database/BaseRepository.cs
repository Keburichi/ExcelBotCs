using System.Linq.Expressions;
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

    /// <summary>
    ///     The indexes this collection needs. Empty by default; collections that need indexes override it.
    /// </summary>
    public virtual IReadOnlyList<CreateIndexModel<T>> GetIndexModels()
    {
        return [];
    }

    /// <summary>
    ///     Creates the collection's indexes if they are missing. Idempotent: creating an index that already exists
    ///     with the same name and key is a no-op, so this is safe to run on every deployment.
    /// </summary>
    public async Task EnsureIndexesAsync()
    {
        var indexModels = GetIndexModels();
        if (indexModels.Count == 0) return;

        await Collection.Indexes.CreateManyAsync(indexModels);
    }

    /// <summary>Builds an ascending key definition; use <see cref="Combine" /> for compound indexes.</summary>
    protected static IndexKeysDefinition<T> Ascending(Expression<Func<T, object>> field)
    {
        return Builders<T>.IndexKeys.Ascending(field);
    }

    /// <summary>Builds a descending key definition; use <see cref="Combine" /> for compound indexes.</summary>
    protected static IndexKeysDefinition<T> Descending(Expression<Func<T, object>> field)
    {
        return Builders<T>.IndexKeys.Descending(field);
    }

    /// <summary>Combines key definitions into a compound key, in the order given.</summary>
    protected static IndexKeysDefinition<T> Combine(params IndexKeysDefinition<T>[] keys)
    {
        return Builders<T>.IndexKeys.Combine(keys);
    }

    /// <summary>Builds an index model with an explicit, deterministic name.</summary>
    protected static CreateIndexModel<T> Index(string name, IndexKeysDefinition<T> keys, bool unique = false)
    {
        return new CreateIndexModel<T>(keys, new CreateIndexOptions { Name = name, Unique = unique });
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
