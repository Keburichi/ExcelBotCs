using ExcelBotCs.Models.Database;
using MongoDB.Driver;

namespace ExcelBotCs.Database.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<List<T>> GetAsync();
    Task<T?> GetAsync(string id);
    Task CreateAsync(T entity);
    Task UpdateAsync(string id, T updatedEntity);
    Task DeleteAsync(string id);
    Task UpsertAsync(T entity);

    /// <summary>The indexes this collection needs; empty when the collection needs none.</summary>
    IReadOnlyList<CreateIndexModel<T>> GetIndexModels();

    /// <summary>Creates the collection's indexes if they are missing. Idempotent.</summary>
    Task EnsureIndexesAsync();
}
