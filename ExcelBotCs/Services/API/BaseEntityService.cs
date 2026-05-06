using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public abstract class BaseEntityService<TEntity, TRepository> : IBaseEntityService<TEntity>
    where TEntity : BaseEntity
    where TRepository : IBaseRepository<TEntity>
{
    protected readonly TRepository Repository;

    protected BaseEntityService(TRepository repository)
    {
        Repository = repository;
    }

    public virtual async Task<List<TEntity>> GetAsync()
    {
        return await Repository.GetAsync();
    }

    public virtual async Task<TEntity> GetAsync(string id)
    {
        return await Repository.GetAsync(id);
    }

    public virtual async Task CreateAsync(TEntity entity)
    {
        await Repository.CreateAsync(entity);
    }

    public virtual async Task UpdateAsync(string id, TEntity updatedEntity)
    {
        await Repository.UpdateAsync(id, updatedEntity);
    }

    public virtual async Task DeleteAsync(string id)
    {
        await Repository.DeleteAsync(id);
    }
}