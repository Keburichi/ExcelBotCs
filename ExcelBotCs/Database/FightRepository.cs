using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class FightRepository : BaseRepository<Fight>, IFightRepository
{
    public FightRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    public async Task<Fight?> GetByNameAndTypeAsync(string name, FightType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await Collection.Find(x => x.Name == name && x.Type == type).FirstOrDefaultAsync();
    }
}