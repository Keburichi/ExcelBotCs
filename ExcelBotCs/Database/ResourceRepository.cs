using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class ResourceRepository : BaseRepository<Resource>, IResourceRepository
{
    public ResourceRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    public async Task<List<Resource>> GetByFightIdAsync(string fightId)
    {
        return await Collection.Find(r => r.FightId == fightId).ToListAsync();
    }
}
