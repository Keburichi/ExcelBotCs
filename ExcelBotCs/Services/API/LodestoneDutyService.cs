using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class LodestoneDutyService : BaseEntityService<LodestoneDuty, ILodestoneDutyRepository>, ILodestoneDutyService
{
    public LodestoneDutyService(ILodestoneDutyRepository lodestoneDutyRepository) : base(lodestoneDutyRepository)
    {
    }

    public override async Task CreateAsync(LodestoneDuty entity)
    {
        entity.LastSyncTime = DateTime.UtcNow;
        await Repository.CreateAsync(entity);
    }

    public override async Task UpdateAsync(string id, LodestoneDuty updatedEntity)
    {
        updatedEntity.LastSyncTime = DateTime.UtcNow;
        await Repository.UpdateAsync(id, updatedEntity);
    }

    public async Task<List<LodestoneDuty>> GetByExpansionAndCategoryAsync(int expansionId, int categoryId)
    {
        return await Repository.GetByExpansionAndCategoryAsync(expansionId, categoryId);
    }

    public async Task<LodestoneDuty?> GetByLodestoneIdAsync(string lodestoneId)
    {
        return await Repository.GetByLodestoneIdAsync(lodestoneId);
    }

    public async Task<bool> HasDataAsync()
    {
        var count = await Repository.CountAsync();
        return count > 0;
    }

    public async Task<bool> HasDataForExpansionAndCategoryAsync(int expansionId, int categoryId)
    {
        return await Repository.HasDataForExpansionAndCategoryAsync(expansionId, categoryId);
    }

    public async Task BulkCreateAsync(List<LodestoneDuty> duties)
    {
        foreach (var duty in duties)
        {
            duty.LastSyncTime = DateTime.UtcNow;
            await Repository.CreateAsync(duty);
        }
    }
}
