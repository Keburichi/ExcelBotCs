using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IFightRepository : IBaseRepository<Fight>
{
    Task<Fight?> GetByNameAndTypeAsync(string name, FightType type);
}