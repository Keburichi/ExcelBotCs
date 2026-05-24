using System.Net;
using Discord.Net;

namespace ExcelBotCs.HealthChecks;

public class BotConnectionMonitor : IBotConnectionMonitor
{
    private volatile bool _hasEverConnected;
    private volatile bool _isAuthFailure;
    private DateTimeOffset? _disconnectedAt;
    private readonly object _lock = new();

    public void NotifyConnected()
    {
        lock (_lock)
        {
            _hasEverConnected = true;
            _disconnectedAt = null;
        }
    }

    public void NotifyDisconnected(Exception? reason)
    {
        if (reason is HttpException { HttpCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden })
        {
            NotifyAuthFailure();
            return;
        }

        lock (_lock)
        {
            _disconnectedAt ??= DateTimeOffset.UtcNow;
        }
    }

    public void NotifyAuthFailure()
    {
        _isAuthFailure = true;
    }

    public bool IsHealthy(TimeSpan disconnectThreshold)
    {
        if (_isAuthFailure) return true;
        if (!_hasEverConnected) return true;

        DateTimeOffset? disconnectedAt;
        lock (_lock)
        {
            disconnectedAt = _disconnectedAt;
        }

        if (disconnectedAt is null) return true;
        return DateTimeOffset.UtcNow - disconnectedAt.Value < disconnectThreshold;
    }
}