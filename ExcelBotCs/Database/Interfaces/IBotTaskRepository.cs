using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Database.Interfaces;

public interface IBotTaskRepository : IBaseRepository<BotTask>
{
    Task<List<BotTask>> GetPendingTasksAsync();
    Task MarkInProgressAsync(string taskId);
    Task MarkCompletedAsync(string taskId);
    Task MarkFailedAsync(string taskId, string errorMessage);
}
