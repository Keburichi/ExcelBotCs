using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IMemberRoleService
{
    Task<List<MemberRole>> GetAsync();
    Task<MemberRole?> GetAsync(string id);
    Task CreateAsync(MemberRole memberRoles);
    Task UpdateAsync(string id, MemberRole updatedMemberRole);
    Task DeleteAsync(string id);

    Task<MemberRole> GetByDiscordId(string discordId);
}