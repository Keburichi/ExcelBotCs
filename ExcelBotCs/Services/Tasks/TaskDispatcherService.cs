using ExcelBotCs.Database.Interfaces;

namespace ExcelBotCs.Services.Tasks;

public class TaskDispatcherService : BackgroundService
{
    private readonly IBotTaskRepository _botTaskRepository;
    private readonly Dictionary<string, IBotTaskHandler> _handlers;
    private readonly ILogger<TaskDispatcherService> _logger;

    public TaskDispatcherService(
        IServiceScopeFactory scopeFactory,
        IBotTaskRepository botTaskRepository,
        IEnumerable<IBotTaskHandler> handlers,
        ILogger<TaskDispatcherService> logger) : base(scopeFactory)
    {
        _botTaskRepository = botTaskRepository;
        _logger = logger;

        var handlerList = handlers.ToList();
        var distinct = handlerList.Select(h => h.TaskType).Distinct().Count();
        if (distinct != handlerList.Count)
            throw new InvalidOperationException("Duplicate IBotTaskHandler TaskType registrations detected.");

        _handlers = handlerList.ToDictionary(h => h.TaskType);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await DispatchPendingTasksAsync(stoppingToken);
        }
        catch (OperationCanceledException) { }
    }

    private async Task DispatchPendingTasksAsync(CancellationToken cancellationToken)
    {
        var pending = await _botTaskRepository.GetPendingTasksAsync();
        foreach (var task in pending)
        {
            if (!_handlers.TryGetValue(task.TaskType, out var handler))
            {
                _logger.LogWarning("No handler registered for task type {TaskType}", task.TaskType);
                continue;
            }

            await _botTaskRepository.MarkInProgressAsync(task.Id);
            try
            {
                await handler.ExecuteAsync(task.Payload, cancellationToken);
                await _botTaskRepository.MarkCompletedAsync(task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {TaskId} ({TaskType}) failed: {Message}", task.Id, task.TaskType, ex.Message);
                await _botTaskRepository.MarkFailedAsync(task.Id, ex.Message);
            }
        }
    }
}
