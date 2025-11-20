using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using MongoDB.Bson;

namespace ExcelBotCs.Database;

public class RaidplanRepository : IRaidplanRepository
{
    private readonly IFightRepository _fightRepository;

    public RaidplanRepository(IFightRepository fightRepository)
    {
        _fightRepository = fightRepository;
    }

    public async Task<List<Raidplan>> GetAsync()
    {
        // Get all raidplans from all fights
        var fights = await _fightRepository.GetAsync();
        return fights.SelectMany(f => f.Raidplans ?? new List<Raidplan>()).ToList();
    }

    public async Task<Raidplan?> GetAsync(string id)
    {
        var fights = await _fightRepository.GetAsync();
        return fights
            .SelectMany(f => f.Raidplans ?? new List<Raidplan>())
            .FirstOrDefault(r => r.Id == id);
    }

    public async Task<List<Raidplan>> GetByFightIdAsync(string fightId)
    {
        var fight = await _fightRepository.GetAsync(fightId);
        return fight?.Raidplans ?? new List<Raidplan>();
    }

    public async Task CreateAsync(string fightId, Raidplan entity)
    {
        var fight = await _fightRepository.GetAsync(fightId);
        if (fight == null) return;

        entity.CreateDate = DateTime.UtcNow;
        entity.EditDate = DateTime.UtcNow;
        entity.Id = ObjectId.GenerateNewId().ToString();

        fight.Raidplans ??= new List<Raidplan>();
        fight.Raidplans.Add(entity);

        await _fightRepository.UpdateAsync(fightId, fight);
    }

    public async Task UpdateAsync(string fightId, string id, Raidplan updatedEntity)
    {
        var fight = await _fightRepository.GetAsync(fightId);
        if (fight?.Raidplans == null) return;

        var index = fight.Raidplans.FindIndex(r => r.Id == id);
        if (index == -1) return;

        updatedEntity.EditDate = DateTime.UtcNow;
        updatedEntity.Id = id; // Preserve the ID
        updatedEntity.CreateDate = fight.Raidplans[index].CreateDate; // Preserve CreateDate

        fight.Raidplans[index] = updatedEntity;
        await _fightRepository.UpdateAsync(fightId, fight);
    }

    public async Task DeleteAsync(string fightId, string id)
    {
        var fight = await _fightRepository.GetAsync(fightId);
        if (fight?.Raidplans == null) return;

        fight.Raidplans.RemoveAll(r => r.Id == id);
        await _fightRepository.UpdateAsync(fightId, fight);
    }
}