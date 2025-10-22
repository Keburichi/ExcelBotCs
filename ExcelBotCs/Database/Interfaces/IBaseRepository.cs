using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<List<T>> GetAsync();
    Task<T> GetAsync(string id);
    Task CreateAsync(T entity);
    Task UpdateAsync(string id, T updatedEntity);
    Task DeleteAsync(string id);
}