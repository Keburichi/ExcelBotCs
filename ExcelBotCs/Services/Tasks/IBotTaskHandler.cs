namespace ExcelBotCs.Services.Tasks;

public interface IBotTaskHandler
{
    string TaskType { get; }
    Task ExecuteAsync(string payload, CancellationToken cancellationToken = default);
}
