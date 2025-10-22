using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IMemberService : IBaseEntityService<Member>
{
    Task<Member> GetByDiscordId(string discordId);
    Task<Member> GetByDiscordId(ulong discordId);
    Task<List<Member>> GetFcMembers();
}