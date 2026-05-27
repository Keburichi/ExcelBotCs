using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Caching;

public class CacheAccessor<T> : ICacheAccessor<T> where T : BaseEntity
{
    private readonly IEntityCacheStore<T> _store;

    public CacheAccessor(IEntityCacheStore<T> store)
    {
        _store = store;
    }

    public List<T> GetAll() => _store.IsPopulated ? _store.GetAll() : [];

    public T? GetById(string id) => _store.IsPopulated ? _store.GetById(id) : null;

    public void Update(T entity) => _store.Set(entity);

    public void Remove(string id) => _store.Remove(id);

    public bool IsPopulated => _store.IsPopulated;
}
