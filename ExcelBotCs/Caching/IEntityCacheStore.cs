using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Caching;

public interface IEntityCacheStore<T> where T : BaseEntity
{
    List<T> GetAll();
    T? GetById(string id);
    void SetAll(List<T> entities);
    void Set(T entity);
    void Remove(string id);
    void Clear();
    DateTime? GetMaxDateModified();
    int Count { get; }
    DateTime? LastRefreshed { get; }
    bool IsPopulated { get; }
}
