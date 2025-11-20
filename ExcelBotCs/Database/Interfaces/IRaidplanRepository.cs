using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IRaidplanRepository
{
    Task<List<Raidplan>> GetAsync();
    Task<Raidplan?> GetAsync(string id);
    Task<List<Raidplan>> GetByFightIdAsync(string fightId);
    Task CreateAsync(string fightId, Raidplan entity);
    Task UpdateAsync(string fightId, string id, Raidplan updatedEntity);
    Task DeleteAsync(string fightId, string id);
}