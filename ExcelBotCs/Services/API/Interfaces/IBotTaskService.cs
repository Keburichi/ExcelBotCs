using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IBotTaskService : IBaseEntityService<BotTask>
{
    Task EnqueueAsync<TPayload>(string taskType, TPayload payload, DateTime? scheduledAt = null);
}
