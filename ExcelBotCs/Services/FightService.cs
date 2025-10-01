using ExcelBotCs.Data;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Services;

public class FightService : BaseDatabaseService<Fight>
{
    public FightService(IOptions<DatabaseOptions> databaseConfig) : base(databaseConfig)
    {
    }

    public async Task<Fight?> GetByNameAndTypeAsync(string name, FightType type)
    {
        return await Collection.Find(x => x.Name == name && x.Type == type).FirstOrDefaultAsync();
    }

    public async Task<bool> UpsertAsync(Fight fight)
    {
        // try find existing by unique key (Name + Type)
        var existing = await GetByNameAndTypeAsync(fight.Name, fight.Type);
        if (existing == null)
        {
            await CreateAsync(fight);
            return true; // inserted
        }

        // preserve immutable fields
        fight.Id = existing.Id;
        fight.CreateDate = existing.CreateDate;
        await UpdateAsync(existing.Id, fight);
        return false; // updated
    }

    public async Task<(int inserted, int updated)> BulkUpsertAsync(IEnumerable<Fight> fights)
    {
        int inserted = 0, updated = 0;
        foreach (var fight in fights)
        {
            var wasInserted = await UpsertAsync(fight);
            if (wasInserted) inserted++;
            else updated++;
        }

        return (inserted, updated);
    }
}