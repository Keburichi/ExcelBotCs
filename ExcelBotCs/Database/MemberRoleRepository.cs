using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class MemberRoleRepository : BaseRepository<MemberRole>, IMemberRoleRepository
{
    public MemberRoleRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    public async Task<MemberRole> GetByDiscordId(string discordId)
    {
        return await Collection.Find(x => x.DiscordId == discordId).FirstOrDefaultAsync();
    }
}