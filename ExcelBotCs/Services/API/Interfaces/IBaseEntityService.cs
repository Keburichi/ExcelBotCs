using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IBaseEntityService<TEntity> where TEntity : BaseEntity
{
    Task<List<TEntity>> GetAsync();
    Task<TEntity> GetAsync(string id);
    Task CreateAsync(TEntity entity);
    Task UpdateAsync(string id, TEntity updatedEntity);
    Task DeleteAsync(string id);
}