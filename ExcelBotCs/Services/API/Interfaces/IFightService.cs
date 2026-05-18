using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IFightService
{
    Task<List<Fight>> GetFightsAsync();
    Task<Fight?> GetFightAsync(string id);
    Task CreateAsync(Fight fight);
    Task UpdateAsync(string id, Fight updatedFight);
    Task DeleteAsync(string id);

    Task<bool> UpsertAsync(Fight fight);
    Task<Fight?> GetByNameAndTypeAsync(string name, FightType type);
    Task<(int inserted, int updated)> BulkUpsertAsync(IEnumerable<Fight> fights);
}