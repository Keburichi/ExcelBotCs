using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.FreeCompany;
using NetStone.Model.Parseables.FreeCompany.Members;

namespace ExcelBotCs.Services.API.Interfaces;

public interface ILodestoneClient
{
    LodestoneClient GetLodestoneClient();
    Task<LodestoneCharacter?> GetCharacter(string id);
    Task<LodestoneFreeCompany?> GetFreeCompany(string id);
    Task<FreeCompanyMembers?> GetFreeCompanyMembers(string id);
}