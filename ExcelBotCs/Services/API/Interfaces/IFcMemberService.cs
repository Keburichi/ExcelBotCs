using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IFcMemberService : IBaseEntityService<FcMember>
{
    Task<FcMember> GetByCharacterId(string characterId);
}