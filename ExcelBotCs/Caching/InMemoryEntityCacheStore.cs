using System.Collections.Concurrent;
using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Caching;

public class InMemoryEntityCacheStore<T> : IEntityCacheStore<T> where T : BaseEntity
{
    private volatile ConcurrentDictionary<string, T> _store = new();
    private DateTime? _lastRefreshed;
    private bool _isPopulated;

    public List<T> GetAll() => _store.Values.ToList();

    public T? GetById(string id) => _store.GetValueOrDefault(id);

    public void SetAll(List<T> entities)
    {
        var newStore = new ConcurrentDictionary<string, T>(
            entities.ToDictionary(e => e.Id));
        Interlocked.Exchange(ref _store, newStore);
        _lastRefreshed = DateTime.UtcNow;
        _isPopulated = true;
    }

    public void Set(T entity) => _store[entity.Id] = entity;

    public void Remove(string id) => _store.TryRemove(id, out _);

    public void Clear()
    {
        _store.Clear();
        _isPopulated = false;
    }

    public DateTime? GetMaxDateModified()
    {
        if (_store.IsEmpty) return null;
        return _store.Values.Max(e => e.DateModified);
    }

    public int Count => _store.Count;
    public DateTime? LastRefreshed => _lastRefreshed;
    public bool IsPopulated => _isPopulated;
}
