using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Caching;

public interface ICacheAccessor<T> where T : BaseEntity
{
    List<T> GetAll();
    T? GetById(string id);
    void Update(T entity);
    void Remove(string id);
    bool IsPopulated { get; }
}
