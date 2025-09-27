using ExcelBotCs.Database.DTO;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Services;

public class FcMemberService : BaseDatabaseService<FcMember>
{
    public FcMemberService(IOptions<DatabaseOptions> databaseOptions) : base(databaseOptions)
    {
    }

    public async Task<FcMember> GetByCharacterId(string characterId)
    {
        return await Collection.Find(x => x.CharacterId == characterId).FirstOrDefaultAsync();
    }
}