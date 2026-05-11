using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class BotTaskRepository : BaseRepository<BotTask>, IBotTaskRepository
{
    public BotTaskRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
        : base(mongoClient, databaseOptions) { }

    public async Task<List<BotTask>> GetPendingTasksAsync()
    {
        var now = DateTime.UtcNow;
        return await Collection.Find(t =>
            t.Status == BotTaskStatus.Pending &&
            (t.ScheduledAt == null || t.ScheduledAt <= now)
        ).ToListAsync();
    }

    public async Task MarkInProgressAsync(string taskId)
    {
        var filter = Builders<BotTask>.Filter.Eq(t => t.Id, taskId);
        var update = Builders<BotTask>.Update
            .Set(t => t.Status, BotTaskStatus.InProgress)
            .Set(t => t.DateModified, DateTime.UtcNow);
        await Collection.UpdateOneAsync(filter, update);
    }

    public async Task MarkCompletedAsync(string taskId)
    {
        var filter = Builders<BotTask>.Filter.Eq(t => t.Id, taskId);
        var update = Builders<BotTask>.Update
            .Set(t => t.Status, BotTaskStatus.Completed)
            .Set(t => t.ExecutedAt, DateTime.UtcNow)
            .Set(t => t.DateModified, DateTime.UtcNow);
        await Collection.UpdateOneAsync(filter, update);
    }

    public async Task MarkFailedAsync(string taskId, string errorMessage)
    {
        var filter = Builders<BotTask>.Filter.Eq(t => t.Id, taskId);
        var update = Builders<BotTask>.Update
            .Set(t => t.Status, BotTaskStatus.Failed)
            .Set(t => t.ErrorMessage, errorMessage)
            .Set(t => t.ExecutedAt, DateTime.UtcNow)
            .Set(t => t.DateModified, DateTime.UtcNow);
        await Collection.UpdateOneAsync(filter, update);
    }
}
