using ExcelBotCs.Data;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Services;

public class MemberService : BaseDatabaseService<Member>
{
    public MemberService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }

    public async Task<Member> GetByDiscordId(string discordId)
    {
        return await Collection.Find(x => x.DiscordId == discordId).FirstOrDefaultAsync();
    }

    public async Task<Member> GetByDiscordId(ulong discordId) 
        => await GetByDiscordId(discordId.ToString());
}