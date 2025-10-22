using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class MemberRepository : BaseRepository<Member>, IMemberRepository
{
    public MemberRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    public async Task<Member> GetByDiscordId(string discordId)
    {
        return await Collection.Find(x => x.DiscordId == discordId).FirstOrDefaultAsync();
    }

    public async Task<Member> GetByDiscordId(ulong discordId) 
        => await GetByDiscordId(discordId.ToString());
}