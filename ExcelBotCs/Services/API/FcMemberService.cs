using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class FcMemberService : BaseEntityService<FcMember, IFcMemberRepository>, IFcMemberService
{
    public FcMemberService(IFcMemberRepository repository) : base(repository)
    {
    }

    public async Task<FcMember> GetByCharacterId(string characterId)
    {
        return await Repository.GetByCharacterId(characterId);
    }
}
