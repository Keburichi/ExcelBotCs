using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IFightService : IBaseEntityService<Fight>
{
    Task<bool> UpsertAsync(Fight fight);
    Task<Fight?> GetByNameAndTypeAsync(string name, FightType type);
    Task<(int inserted, int updated)> BulkUpsertAsync(IEnumerable<Fight> fights);
}