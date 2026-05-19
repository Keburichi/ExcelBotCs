using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IBossService
{
    Task<List<Boss>> GetBossesAsync();
    Task<Boss?> GetBossAsync(string id);
    Task<Boss?> GetByNormalizationKeyAsync(string normalizationKey);
    Task CreateAsync(Boss boss);
    Task UpdateAsync(string id, Boss updatedBoss);
    Task DeleteAsync(string id);
    Task<Boss> GetOrCreateAsync(string encounterName, int? expansionId, bool isUltimate);
}
