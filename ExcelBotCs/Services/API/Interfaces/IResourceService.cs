using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IResourceService
{
    Task<List<Resource>> GetByFightIdAsync(string fightId);
    Task<Resource?> GetAsync(string id);
    Task CreateAsync(Resource resource);
    Task UpdateAsync(string id, Resource updatedResource);
    Task DeleteAsync(string id);
}
