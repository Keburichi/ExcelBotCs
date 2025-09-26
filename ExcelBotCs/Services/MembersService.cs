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

    /// <summary>
    /// Get all members that are members of the fc.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Member>> GetFcMembers()
    {
        var allMembers = await GetAsync();
        return allMembers.Where(x => x.IsMember.HasValue && x.IsMember.Value).ToList();
    }
}