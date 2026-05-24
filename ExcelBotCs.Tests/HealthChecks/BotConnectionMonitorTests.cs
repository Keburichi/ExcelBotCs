using System.Net;
using Discord.Net;
using ExcelBotCs.HealthChecks;

namespace ExcelBotCs.Tests.HealthChecks;

public class BotConnectionMonitorTests
{
    private readonly BotConnectionMonitor _monitor = new();

    [Fact]
    public void IsHealthy_BeforeAnyEvent_ReturnsTrue()
    {
        _monitor.IsHealthy(TimeSpan.FromMinutes(5)).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_AfterConnected_ReturnsTrue()
    {
        _monitor.NotifyConnected();

        _monitor.IsHealthy(TimeSpan.FromMinutes(5)).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_AfterDisconnect_WithinThreshold_ReturnsTrue()
    {
        _monitor.NotifyConnected();
        _monitor.NotifyDisconnected(null);

        _monitor.IsHealthy(TimeSpan.FromHours(1)).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_AfterDisconnect_BeyondThreshold_ReturnsFalse()
    {
        _monitor.NotifyConnected();
        _monitor.NotifyDisconnected(null);

        // TimeSpan.Zero: any elapsed time exceeds the threshold
        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeFalse();
    }

    [Fact]
    public void IsHealthy_AfterReconnect_ReturnsTrue()
    {
        _monitor.NotifyConnected();
        _monitor.NotifyDisconnected(null);
        _monitor.NotifyConnected();

        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_AfterAuthFailure_AlwaysReturnsTrue()
    {
        _monitor.NotifyAuthFailure();

        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_AfterConnectThenAuthFailure_AlwaysReturnsTrue()
    {
        _monitor.NotifyConnected();
        _monitor.NotifyAuthFailure();

        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_DisconnectedWithUnauthorizedException_TreatsAsAuthFailure()
    {
        _monitor.NotifyConnected();
        var ex = new HttpException(HttpStatusCode.Unauthorized, null);
        _monitor.NotifyDisconnected(ex);

        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_DisconnectedWithForbiddenException_TreatsAsAuthFailure()
    {
        _monitor.NotifyConnected();
        var ex = new HttpException(HttpStatusCode.Forbidden, null);
        _monitor.NotifyDisconnected(ex);

        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_DisconnectedWithGenericException_TreatsAsNetworkFailure()
    {
        _monitor.NotifyConnected();
        _monitor.NotifyDisconnected(new Exception("Network timeout"));

        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeFalse();
    }

    [Fact]
    public void IsHealthy_NeverConnected_DisconnectNotTracked()
    {
        // Calling NotifyDisconnected without a prior NotifyConnected should not set disconnect time
        // because IsHealthy returns true when !_hasEverConnected
        _monitor.NotifyDisconnected(null);

        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeTrue();
    }

    [Fact]
    public void NotifyConnected_MultipleTimes_RemainsHealthy()
    {
        _monitor.NotifyConnected();
        _monitor.NotifyConnected();
        _monitor.NotifyConnected();

        _monitor.IsHealthy(TimeSpan.FromMinutes(5)).ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_DisconnectedThenReconnectedThenDisconnectedAgain_UsesLatestDisconnect()
    {
        _monitor.NotifyConnected();
        _monitor.NotifyDisconnected(null);
        _monitor.NotifyConnected();
        _monitor.NotifyDisconnected(null);

        // Still disconnected beyond zero threshold
        _monitor.IsHealthy(TimeSpan.Zero).ShouldBeFalse();
    }
}