using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.HealthChecks;

public class DiscordBotHealthCheck(IBotConnectionMonitor monitor, IOptions<DiscordBotOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var threshold = options.Value.DisconnectHealthThreshold;
        return Task.FromResult(monitor.IsHealthy(threshold)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy(
                "Discord bot has been disconnected for longer than the configured threshold"));
    }
}