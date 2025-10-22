using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class FcMemberRepository : BaseRepository<FcMember>, IFcMemberRepository
{
    public FcMemberRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    public async Task<FcMember> GetByCharacterId(string characterId)
    {
        return await Collection.Find(x => x.CharacterId == characterId).FirstOrDefaultAsync();
    }
}