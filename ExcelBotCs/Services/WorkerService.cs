using ExcelBotCs.Services.FFLogs;
using ExcelBotCs.Services.Import;

namespace ExcelBotCs.Services;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private readonly ImportService _importService;
    private readonly LodestoneService _lodestoneService;
    private readonly FFLogsSyncService _ffLogsSyncService;

    public WorkerService(IServiceScopeFactory scopeFactory, ILogger<WorkerService> logger,
        ImportService importService, LodestoneService lodestoneService, FFLogsSyncService ffLogsSyncService) : base(scopeFactory)
    {
        _logger = logger;
        _importService = importService;
        _lodestoneService = lodestoneService;
        _ffLogsSyncService = ffLogsSyncService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run first import
        await RunImportAsync(stoppingToken);

        // Then run periodically every 5 minutes
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunImportAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker service task was cancelled");
        }
    }

    private async Task RunImportAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Importing external entities");

            await _importService.ImportMembers(); // Importing discord members automatically imports roles as well
            await _lodestoneService.ImportMembers();

            // FFLogs imports
            await _ffLogsSyncService.SyncFightsAsync(); // Daily check
            await _ffLogsSyncService.SyncMemberActivityAsync(); // Wave-based sync

            _logger.LogInformation("Imported external entities");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing external entities");
        }
    }
}