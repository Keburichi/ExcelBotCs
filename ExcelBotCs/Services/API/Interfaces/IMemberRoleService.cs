using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IMemberRoleService : IBaseEntityService<MemberRole>
{
    Task<MemberRole> GetByDiscordId(string discordId);
}