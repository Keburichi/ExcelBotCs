using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IMemberRoleRepository : IBaseRepository<MemberRole>
{
    Task<MemberRole> GetByDiscordId(ulong discordId);
}