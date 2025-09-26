using ExcelBotCs.Data;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Services;

public class MemberRoleService : BaseDatabaseService<MemberRole>
{
    public MemberRoleService(IOptions<DatabaseOptions> databaseOptions) : base(databaseOptions)
    {
    }

    public async Task<MemberRole> GetByDiscordId(ulong discordId)
    {
        return await Collection.Find(x => x.DiscordId == discordId).FirstOrDefaultAsync();
    }
}