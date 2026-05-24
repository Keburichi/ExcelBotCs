using ExcelBotCs.HealthChecks;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.HealthChecks;

public class DiscordBotHealthCheckTests
{
    private readonly Mock<IBotConnectionMonitor> _monitor = new();
    private readonly HealthCheckContext _context;
    private readonly DiscordBotHealthCheck _check;

    public DiscordBotHealthCheckTests()
    {
        var options = Options.Create(new DiscordBotOptions
        {
            DisconnectHealthThreshold = TimeSpan.FromMinutes(5)
        });
        _check = new DiscordBotHealthCheck(_monitor.Object, options);
        _context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("discord-bot", _ => _check, null, null)
        };
    }

    [Fact]
    public async Task CheckHealthAsync_WhenMonitorIsHealthy_ReturnsHealthy()
    {
        _monitor.Setup(m => m.IsHealthy(It.IsAny<TimeSpan>())).Returns(true);

        var result = await _check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenMonitorIsUnhealthy_ReturnsUnhealthy()
    {
        _monitor.Setup(m => m.IsHealthy(It.IsAny<TimeSpan>())).Returns(false);

        var result = await _check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnhealthy_IncludesDescriptiveMessage()
    {
        _monitor.Setup(m => m.IsHealthy(It.IsAny<TimeSpan>())).Returns(false);

        var result = await _check.CheckHealthAsync(_context);

        result.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CheckHealthAsync_PassesConfiguredThresholdToMonitor()
    {
        var threshold = TimeSpan.FromMinutes(10);
        var options = Options.Create(new DiscordBotOptions { DisconnectHealthThreshold = threshold });
        var check = new DiscordBotHealthCheck(_monitor.Object, options);
        _monitor.Setup(m => m.IsHealthy(threshold)).Returns(true);

        await check.CheckHealthAsync(_context);

        _monitor.Verify(m => m.IsHealthy(threshold), Times.Once);
    }
}