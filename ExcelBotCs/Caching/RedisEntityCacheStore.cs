using System.Text.Json;
using ExcelBotCs.Models.Database;
using StackExchange.Redis;

namespace ExcelBotCs.Caching;

public class RedisEntityCacheStore<T> : IEntityCacheStore<T> where T : BaseEntity
{
    private readonly IDatabase _db;
    private readonly string _hashKey;
    private readonly string _metaKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisEntityCacheStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _hashKey = $"cache:{typeof(T).Name}";
        _metaKey = $"cache:{typeof(T).Name}:meta";
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = null };
    }

    public List<T> GetAll()
    {
        var entries = _db.HashGetAll(_hashKey);
        return entries
            .Select(e => JsonSerializer.Deserialize<T>(e.Value!, _jsonOptions)!)
            .ToList();
    }

    public T? GetById(string id)
    {
        var value = _db.HashGet(_hashKey, id);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
    }

    public void SetAll(List<T> entities)
    {
        var batch = _db.CreateBatch();
        batch.KeyDeleteAsync(_hashKey);

        if (entities.Count > 0)
        {
            var entries = entities.Select(e =>
                new HashEntry(e.Id, JsonSerializer.Serialize(e, _jsonOptions))).ToArray();
            batch.HashSetAsync(_hashKey, entries);
        }

        var now = DateTime.UtcNow;
        batch.HashSetAsync(_metaKey, [
            new HashEntry("LastRefreshed", now.Ticks.ToString()),
            new HashEntry("MaxDateModified", entities.Count > 0
                ? entities.Max(e => e.DateModified).Ticks.ToString()
                : "")
        ]);

        batch.Execute();
    }

    public void Set(T entity)
    {
        _db.HashSet(_hashKey, entity.Id, JsonSerializer.Serialize(entity, _jsonOptions));
    }

    public void Remove(string id)
    {
        _db.HashDelete(_hashKey, id);
    }

    public void Clear()
    {
        _db.KeyDelete(_hashKey);
        _db.KeyDelete(_metaKey);
    }

    public DateTime? GetMaxDateModified()
    {
        var value = _db.HashGet(_metaKey, "MaxDateModified");
        if (value.IsNullOrEmpty || string.IsNullOrEmpty(value.ToString())) return null;
        return new DateTime(long.Parse(value!), DateTimeKind.Utc);
    }

    public int Count => (int)_db.HashLength(_hashKey);

    public DateTime? LastRefreshed
    {
        get
        {
            var value = _db.HashGet(_metaKey, "LastRefreshed");
            if (value.IsNullOrEmpty) return null;
            return new DateTime(long.Parse(value!), DateTimeKind.Utc);
        }
    }

    public bool IsPopulated => _db.HashLength(_hashKey) > 0;
}
