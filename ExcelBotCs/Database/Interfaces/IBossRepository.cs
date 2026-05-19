using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IBossRepository : IBaseRepository<Boss>
{
    Task<Boss?> GetByNormalizationKeyAsync(string normalizationKey);
}
