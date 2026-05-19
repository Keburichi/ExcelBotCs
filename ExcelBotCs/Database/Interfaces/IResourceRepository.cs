using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IResourceRepository : IBaseRepository<Resource>
{
    Task<List<Resource>> GetByFightIdAsync(string fightId);
}
