using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class BossRepository : BaseRepository<Boss>, IBossRepository
{
    public BossRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    public async Task<Boss?> GetByNormalizationKeyAsync(string normalizationKey)
    {
        return await Collection.Find(b => b.NormalizationKey == normalizationKey).FirstOrDefaultAsync();
    }
}
