using System.Text.Json;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class BotTaskService : BaseEntityService<BotTask, IBotTaskRepository>, IBotTaskService
{
    public BotTaskService(IBotTaskRepository repository) : base(repository) { }

    public async Task EnqueueAsync<TPayload>(string taskType, TPayload payload, DateTime? scheduledAt = null)
    {
        var task = new BotTask
        {
            TaskType = taskType,
            Payload = JsonSerializer.Serialize(payload),
            Status = BotTaskStatus.Pending,
            ScheduledAt = scheduledAt
        };
        await Repository.CreateAsync(task);
    }
}
