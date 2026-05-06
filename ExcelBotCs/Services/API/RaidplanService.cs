using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class RaidplanService : IRaidplanService
{
    private readonly IRaidplanRepository _raidplanRepository;

    public RaidplanService(IRaidplanRepository raidplanRepository)
    {
        _raidplanRepository = raidplanRepository;
    }

    public async Task<List<Raidplan>> GetAsync()
    {
        return await _raidplanRepository.GetAsync();
    }

    public async Task<Raidplan?> GetAsync(string id)
    {
        return await _raidplanRepository.GetAsync(id);
    }

    public async Task<List<Raidplan>> GetByFightIdAsync(string fightId)
    {
        return await _raidplanRepository.GetByFightIdAsync(fightId);
    }

    public async Task CreateAsync(string fightId, Raidplan entity)
    {
        await _raidplanRepository.CreateAsync(fightId, entity);
    }

    public async Task UpdateAsync(string fightId, string id, Raidplan updatedEntity)
    {
        await _raidplanRepository.UpdateAsync(fightId, id, updatedEntity);
    }

    public async Task DeleteAsync(string fightId, string id)
    {
        await _raidplanRepository.DeleteAsync(fightId, id);
    }
}