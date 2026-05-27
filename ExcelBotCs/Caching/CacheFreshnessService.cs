using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Caching;

public class CacheFreshnessService : BackgroundService
{
    private readonly IEntityCacheService _cacheService;
    private readonly IOptions<CacheOptions> _options;
    private readonly ILogger<CacheFreshnessService> _logger;

    public CacheFreshnessService(
        IServiceScopeFactory scopeFactory,
        IEntityCacheService cacheService,
        IOptions<CacheOptions> options,
        ILogger<CacheFreshnessService> logger) : base(scopeFactory)
    {
        _cacheService = cacheService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Warm cache on startup with retry
        var retryCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _cacheService.WarmAllAsync();
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= 5)
                {
                    _logger.LogError(ex, "Failed to warm cache after {RetryCount} attempts", retryCount);
                    break;
                }

                _logger.LogWarning(ex, "Failed to warm cache, retrying in 5 seconds (attempt {Attempt}/5)",
                    retryCount);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        var interval = TimeSpan.FromSeconds(_options.Value.FreshnessCheckIntervalSeconds);
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _cacheService.RefreshIfStaleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache freshness check");
            }
        }
    }
}
