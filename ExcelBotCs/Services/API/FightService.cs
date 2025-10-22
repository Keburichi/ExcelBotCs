using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class FightService : IFightService
{
    private readonly IFightRepository _fightRepository;

    public FightService(IFightRepository fightRepository)
    {
        _fightRepository = fightRepository;
    }

    public async Task<List<Fight>> GetAsync()
    {
        return await _fightRepository.GetAsync();
    }

    public async Task<Fight> GetAsync(string id)
    {
        return await _fightRepository.GetAsync(id);
    }

    public async Task CreateAsync(Fight entity)
    {
        await _fightRepository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, Fight updatedEntity)
    {
        await _fightRepository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _fightRepository.DeleteAsync(id);
    }

    public async Task<Fight?> GetByNameAndTypeAsync(string name, FightType type)
    {
        return await _fightRepository.GetByNameAndTypeAsync(name, type);
    }

    public async Task<bool> UpsertAsync(Fight fight)
    {
        // try find existing by unique key (Name + Type)
        var existing = await GetByNameAndTypeAsync(fight.Name, fight.Type);
        if (existing == null)
        {
            await CreateAsync(fight);
            return true; // inserted
        }

        // preserve immutable fields
        fight.Id = existing.Id;
        fight.CreateDate = existing.CreateDate;
        await UpdateAsync(existing.Id, fight);
        return false; // updated
    }

    public async Task<(int inserted, int updated)> BulkUpsertAsync(IEnumerable<Fight> fights)
    {
        int inserted = 0, updated = 0;
        foreach (var fight in fights)
        {
            var wasInserted = await UpsertAsync(fight);
            if (wasInserted) 
                inserted++;
            else 
                updated++;
        }

        return (inserted, updated);
    }
}