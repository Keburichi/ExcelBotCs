using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IMemberService : IBaseEntityService<Member>
{
    Task UpdateMemberProfileAsync(string id, UpdateMemberRequest request);
    Task<Member> GetByDiscordId(string discordId);
    Task<Member> GetByDiscordId(ulong discordId);
    Task<List<Member>> GetByDiscordIds(IEnumerable<ulong> discordIds);
    Task<Member> GetByLodestoneId(string lodestoneId);
    Task<List<Member>> GetFcMembers();
    Task UpdateDiscordRoles(string id, List<string> roleIds);
}