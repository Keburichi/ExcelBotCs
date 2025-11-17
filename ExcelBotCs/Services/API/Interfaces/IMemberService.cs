using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IMemberService : IBaseEntityService<Member>
{
    Task<Member> GetByDiscordId(string discordId);
    Task<Member> GetByDiscordId(ulong discordId);
    Task<Member> GetByLodestoneId(string lodestoneId);
    Task<List<Member>> GetFcMembers();
}