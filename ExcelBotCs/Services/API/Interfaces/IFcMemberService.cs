using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IFcMemberService
{
    Task<List<FcMember>> GetAsync();
    Task<FcMember?> GetAsync(string id);
    Task CreateAsync(FcMember fcMember);
    Task UpdateAsync(string id, FcMember updatedFcMember);
    Task DeleteAsync(string id);
    Task<FcMember> GetByCharacterId(string characterId);
}