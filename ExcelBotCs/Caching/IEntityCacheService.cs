using ExcelBotCs.Models.DTO.Cache;

namespace ExcelBotCs.Caching;

public interface IEntityCacheService
{
    Task WarmAllAsync();
    Task RefreshIfStaleAsync();
    CacheStatusResponse GetStatus();
    Task ClearAsync(string entityType);
    Task FillAsync(string entityType);
    Task ClearAllAsync();
    Task FillAllAsync();
    object GetAllEntities(string entityType);
    IReadOnlyList<string> EntityTypes { get; }
}
