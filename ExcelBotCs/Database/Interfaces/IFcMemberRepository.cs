using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IFcMemberRepository : IBaseRepository<FcMember>
{
    Task<FcMember> GetByCharacterId(string characterId);
}