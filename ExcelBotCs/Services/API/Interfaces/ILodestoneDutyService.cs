using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface ILodestoneDutyService : IBaseEntityService<LodestoneDuty>
{
    Task<List<LodestoneDuty>> GetByExpansionAndCategoryAsync(int expansionId, int categoryId);
    Task<LodestoneDuty?> GetByLodestoneIdAsync(string lodestoneId);
    Task<bool> HasDataAsync();
    Task<bool> HasDataForExpansionAndCategoryAsync(int expansionId, int categoryId);
    Task BulkCreateAsync(List<LodestoneDuty> duties);
}
