using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IMemberRepository : IBaseRepository<Member>
{
    Task<Member> GetByDiscordId(string discordId);
}