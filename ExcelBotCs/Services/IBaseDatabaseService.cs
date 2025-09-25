using ExcelBotCs.Database.DTO;

namespace ExcelBotCs.Services;

public interface IBaseDatabaseService<T> where T : BaseEntity
{
    Task<List<T>> GetAsync();
    Task<T> GetAsync(string id);
    Task CreateAsync(T entity);
    Task UpdateAsync(string id, T updatedEntity);
    Task DeleteAsync(string id);
}