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

    public async Task<List<Fight>> GetFightsAsync()
    {
        var fights = await _fightRepository.GetAsync();

        if (fights is null)
            return new List<Fight>();

        // Since we are importing the fights from FFLogs and the FFLogs API is doing a horrible job
        // of properly classifying fights we need to filter out duplicates manually.
        // We do not restrict the import side since we need all the individual fight ids,
        // to check if someone cleared something new and sync progress
        var filteredFights = new List<Fight>();

        foreach (var fight in fights.OrderBy(x => x.FFLogsExpansionId))
        {
            HandleSpecialFights(fight);

            if (filteredFights.Any(x => x.Name.Equals(fight.Name)))
                continue;

            filteredFights.Add(fight);
        }

        return filteredFights
            .OrderByDescending(x => x.FFLogsZoneId)
            .ThenByDescending(x => x.FFLogsEncounterId).ToList();
    }

    private void HandleSpecialFights(Fight fight)
    {
        // Since some fights have inconsistent naming across expansions we need to fix them manually
        if (fight.Name.Equals("Bahamut Prime"))
            fight.Name = "The Unending Coil of Bahamut";

        if (fight.Name.Equals("The Ultima Weapon"))
            fight.Name = "The Weapon's Refrain";
    }

    public async Task<Fight?> GetFightAsync(string id)
    {
        return await _fightRepository.GetAsync(id);
    }

    public async Task CreateAsync(Fight fight)
    {
        await _fightRepository.CreateAsync(fight);
    }

    public async Task UpdateAsync(string id, Fight updatedFight)
    {
        await _fightRepository.UpdateAsync(id, updatedFight);
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
        fight.DateCreated = existing.DateCreated;
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