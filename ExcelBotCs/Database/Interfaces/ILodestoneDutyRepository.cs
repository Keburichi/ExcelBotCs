using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface ILodestoneDutyRepository : IBaseRepository<LodestoneDuty>
{
    Task<List<LodestoneDuty>> GetByExpansionAndCategoryAsync(int expansionId, int categoryId);
    Task<LodestoneDuty?> GetByLodestoneIdAsync(string lodestoneId);
    Task<bool> HasDataForExpansionAndCategoryAsync(int expansionId, int categoryId);
    Task<long> CountAsync();
}
