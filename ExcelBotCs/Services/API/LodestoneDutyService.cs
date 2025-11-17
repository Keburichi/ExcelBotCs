using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class LodestoneDutyService : ILodestoneDutyService
{
    private readonly ILodestoneDutyRepository _lodestoneDutyRepository;

    public LodestoneDutyService(ILodestoneDutyRepository lodestoneDutyRepository)
    {
        _lodestoneDutyRepository = lodestoneDutyRepository;
    }

    public async Task<List<LodestoneDuty>> GetAsync()
    {
        return await _lodestoneDutyRepository.GetAsync();
    }

    public async Task<LodestoneDuty> GetAsync(string id)
    {
        return await _lodestoneDutyRepository.GetAsync(id);
    }

    public async Task CreateAsync(LodestoneDuty entity)
    {
        entity.LastSyncTime = DateTime.UtcNow;
        await _lodestoneDutyRepository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, LodestoneDuty updatedEntity)
    {
        updatedEntity.LastSyncTime = DateTime.UtcNow;
        await _lodestoneDutyRepository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _lodestoneDutyRepository.DeleteAsync(id);
    }

    public async Task<List<LodestoneDuty>> GetByExpansionAndCategoryAsync(int expansionId, int categoryId)
    {
        return await _lodestoneDutyRepository.GetByExpansionAndCategoryAsync(expansionId, categoryId);
    }

    public async Task<LodestoneDuty?> GetByLodestoneIdAsync(string lodestoneId)
    {
        return await _lodestoneDutyRepository.GetByLodestoneIdAsync(lodestoneId);
    }

    public async Task<bool> HasDataAsync()
    {
        var count = await _lodestoneDutyRepository.CountAsync();
        return count > 0;
    }

    public async Task<bool> HasDataForExpansionAndCategoryAsync(int expansionId, int categoryId)
    {
        return await _lodestoneDutyRepository.HasDataForExpansionAndCategoryAsync(expansionId, categoryId);
    }

    public async Task BulkCreateAsync(List<LodestoneDuty> duties)
    {
        foreach (var duty in duties)
        {
            duty.LastSyncTime = DateTime.UtcNow;
            await _lodestoneDutyRepository.CreateAsync(duty);
        }
    }
}
