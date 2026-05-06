using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class MemberRoleService : BaseEntityService<MemberRole, IMemberRoleRepository>, IMemberRoleService
{
    public MemberRoleService(IMemberRoleRepository repository) : base(repository)
    {
    }

    public async Task<MemberRole> GetByDiscordId(string discordId)
    {
        return await Repository.GetByDiscordId(discordId);
    }
}
