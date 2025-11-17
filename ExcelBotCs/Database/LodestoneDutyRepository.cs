using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LodestoneDutyRepository : BaseRepository<LodestoneDuty>, ILodestoneDutyRepository
{
    public LodestoneDutyRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions)
    {
    }

    public async Task<List<LodestoneDuty>> GetByExpansionAndCategoryAsync(int expansionId, int categoryId)
    {
        return await Collection
            .Find(d => d.ExpansionId == expansionId && d.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<LodestoneDuty?> GetByLodestoneIdAsync(string lodestoneId)
    {
        return await Collection
            .Find(d => d.LodestoneId == lodestoneId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasDataForExpansionAndCategoryAsync(int expansionId, int categoryId)
    {
        var count = await Collection
            .CountDocumentsAsync(d => d.ExpansionId == expansionId && d.CategoryId == categoryId);
        return count > 0;
    }

    public async Task<long> CountAsync()
    {
        return await Collection.CountDocumentsAsync(d => true);
    }
}
