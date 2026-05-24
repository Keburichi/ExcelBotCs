namespace ExcelBotCs.HealthChecks;

public interface IBotConnectionMonitor
{
    void NotifyConnected();
    void NotifyDisconnected(Exception? reason);
    void NotifyAuthFailure();
    bool IsHealthy(TimeSpan disconnectThreshold);
}