using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface ILodestoneDutyService
{
    Task<List<LodestoneDuty>> GetAsync();
    Task<LodestoneDuty?> GetAsync(string id);
    Task CreateAsync(LodestoneDuty entity);
    Task UpdateAsync(string id, LodestoneDuty updatedEntity);
    Task DeleteAsync(string id);

    Task<List<LodestoneDuty>> GetByExpansionAndCategoryAsync(int expansionId, int categoryId);
    Task<LodestoneDuty?> GetByLodestoneIdAsync(string lodestoneId);
    Task<bool> HasDataAsync();
    Task<bool> HasDataForExpansionAndCategoryAsync(int expansionId, int categoryId);
    Task BulkCreateAsync(List<LodestoneDuty> duties);
}