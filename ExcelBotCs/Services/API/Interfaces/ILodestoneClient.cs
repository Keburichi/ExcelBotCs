using ExcelBotCs.Models.LodestoneClient;
using NetStone;

namespace ExcelBotCs.Services.API.Interfaces;

public interface ILodestoneClient
{
    LodestoneClient GetLodestoneClient();
    Task<LodestoneCharacter?> GetCharacter(string id);
    Task<LodestoneFreeCompany?> GetFreeCompany(string id);
    Task<List<FcMemberEntry>> GetFreeCompanyMembers(string id);
}