using ExcelBotCs.HealthChecks;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExcelBotCs.Tests.HealthChecks;

/// <summary>
///     The <c>lottery</c> detail of <c>/health</c> (spec §5.10, §14; criterion 17). The mapping is the accepted one from
///     #105: healthy when no round is pending and the startup check passed, degraded when a round is pending or failed
///     or the startup check is degraded. The automation flags (dry run, paused, disabled) are reported informationally
///     in the detail and never flip the status; the check never returns unhealthy, so a stuck round or a slow database
///     cannot fail the container's probe.
/// </summary>
public class LotteryHealthCheckTests
{
    private static readonly DateTime NextRun = new(2026, 11, 17, 19, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILotteryRoundRunner> _runner = new();
    private HealthCheckContext _context = null!;

    public LotteryHealthCheckTests()
    {
        _context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(LotteryHealthCheck.Name, _ => null!, null, null)
        };
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAutomationIsLiveAndNoRoundIsPending_IsHealthy()
    {
        var check = CreateCheck(Snapshot(), await StartupCheckAsync(LotteryStartupResult.Healthy));

        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldContain("live");
        result.Data["state"].ShouldBe(LotteryAutomationStatus.LiveKind);
        result.Data["degraded"].ShouldBe(false);
        result.Data["startupCheck"].ShouldBe("healthy");
        result.Data["nextRoundKey"].ShouldBe("2026-11-17");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTheAnnouncementIsStillPending_DegradesTheDetailWithTheRound()
    {
        // The stuck-round case of criterion 17: a drawn round whose post never went out. The detail says which round
        // and why, and the aggregation (covered by the endpoint tests) keeps the container healthy.
        var pending = new LotteryRound
        {
            RoundKey = "2026-11-10",
            Status = LotteryRoundStatus.Drawn,
            FailedStep = LotteryFailedStep.Announcement,
            Attempts = 3,
            LastError = "Discord is unreachable",
            NextRetryAt = new DateTime(2026, 11, 10, 19, 30, 0, DateTimeKind.Utc)
        };

        var check = CreateCheck(Snapshot(pendingRound: pending, lastError: "Discord is unreachable"),
            await StartupCheckAsync(LotteryStartupResult.Healthy));

        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("2026-11-10");
        result.Description.ShouldContain("announcement");
        // The anonymous report never carries the dependency's raw error text, only a coarse reason.
        result.Description.ShouldNotContain("Discord is unreachable");
        result.Data.ContainsKey("lastError").ShouldBeFalse();
        result.Data["lastErrorReason"].ShouldBe("a Discord error is recorded");
        result.Data["degraded"].ShouldBe(true);
        result.Data["pendingRoundKey"].ShouldBe("2026-11-10");
        result.Data["pendingStatus"].ShouldBe(LotteryRoundStatus.Drawn);
        result.Data["failedStep"].ShouldBe(LotteryFailedStep.Announcement);
        result.Data["attempts"].ShouldBe(3);
        result.Data["nextRetryAtUtc"].ShouldBe("2026-11-10T19:30:00.0000000Z");
    }

    [Theory]
    [InlineData(LotteryAutomationStatus.DryRunKind)]
    [InlineData(LotteryAutomationStatus.PausedKind)]
    [InlineData(LotteryAutomationStatus.DisabledKind)]
    public async Task CheckHealthAsync_WhenTheSchedulerIsNotLive_StaysHealthyAndReportsTheFlagInformationally(string kind)
    {
        // The accepted rule from #105: the round state decides the status, not the rollout flags. A dry run, a paused
        // scheduler or the kill switch is an operator decision, so the detail reports it without degrading the probe.
        var check = CreateCheck(Snapshot(kind: kind, schedulerEnabled: kind == LotteryAutomationStatus.DryRunKind),
            await StartupCheckAsync(LotteryStartupResult.Healthy));

        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["state"].ShouldBe(kind);
        result.Data["degraded"].ShouldBe(false);
        result.Data["automationNote"].ShouldBeOfType<string>().ShouldNotBeNullOrWhiteSpace();

        // The reason is still readable for an operator, just not a degradation.
        result.Description.ShouldContain((string)result.Data["automationNote"]);
        result.Description.ShouldContain("The next round is 2026-11-17.");
    }

    [Theory]
    [InlineData(LotteryAutomationStatus.LiveKind)]
    [InlineData(LotteryAutomationStatus.DryRunKind)]
    [InlineData(LotteryAutomationStatus.PausedKind)]
    [InlineData(LotteryAutomationStatus.DisabledKind)]
    public async Task CheckHealthAsync_WithAPendingRound_DegradesWhicheverAutomationFlagIsSet(string kind)
    {
        // The other half of the accepted rule: a pending or failed round degrades the detail in every rollout state.
        var pending = new LotteryRound
        {
            RoundKey = "2026-11-10",
            Status = LotteryRoundStatus.Failed,
            FailedStep = LotteryFailedStep.Draw,
            Attempts = 5
        };

        var check = CreateCheck(Snapshot(kind: kind, schedulerEnabled: kind == LotteryAutomationStatus.LiveKind,
            pendingRound: pending), await StartupCheckAsync(LotteryStartupResult.Healthy));

        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["degraded"].ShouldBe(true);
        result.Data["pendingRoundKey"].ShouldBe("2026-11-10");
        result.Description.ShouldContain("2026-11-10");
        result.Description.ShouldContain("draw");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTheStartupCheckIsDegraded_ReportsTheCountNotTheProblems()
    {
        // Spec §5.10/§14 + the anonymous-endpoint rule: the problem texts can name ids and URLs, so the report carries
        // the count; the admin status endpoint keeps the detail.
        var startupResult = LotteryStartupResult.Degraded([
            "DiscordBot:GuildId is not configured.",
            "WebBaseUrl 'http://localhost:5000' is not an absolute http(s) URL."
        ]);

        var check = CreateCheck(Snapshot(), await StartupCheckAsync(startupResult));

        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotContain("GuildId");
        result.Description.ShouldContain("2 problem(s)");
        result.Data["startupCheck"].ShouldBe("degraded");
        result.Data["problemCount"].ShouldBe(2);
        result.Data.ContainsKey("problems").ShouldBeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WhileTheStartupCheckIsStillRunning_DegradesTheDetail()
    {
        // The scheduler schedules nothing until the check finishes, so the detail says so instead of claiming "live".
        var startup = new LotteryStartupCheckService(new HangingStartupCheck(),
            NullLogger<LotteryStartupCheckService>.Instance);
        await startup.StartAsync(CancellationToken.None);

        var check = CreateCheck(Snapshot(), startup);
        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["startupCheck"].ShouldBe("pending");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTheStatusReadFails_IsDegradedAndNeverUnhealthy()
    {
        _runner.Setup(runner => runner.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("MongoDB is unreachable"));
        var check = new LotteryHealthCheck(_runner.Object,
            await StartupCheckAsync(LotteryStartupResult.Healthy));

        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        // The anonymous detail stays coarse; the raw driver message never leaves the authenticated admin surface.
        result.Description.ShouldNotContain("MongoDB is unreachable");
        result.Description.ShouldContain("could not be read");
        result.Data["state"].ShouldBe("unknown");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTheStatusReadHangs_GivesUpAndIsDegraded()
    {
        _runner.Setup(runner => runner.GetStatusAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return Snapshot();
            });

        var check = new LotteryHealthCheck(_runner.Object,
            await StartupCheckAsync(LotteryStartupResult.Healthy), TimeSpan.FromMilliseconds(100));

        var result = await check.CheckHealthAsync(_context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("within");
    }

    [Fact]
    public async Task CheckHealthAsync_EverySnapshotState_IsHealthyOrDegradedNeverUnhealthy()
    {
        var pending = new LotteryRound
        {
            RoundKey = "2026-11-10",
            Status = LotteryRoundStatus.Failed,
            FailedStep = LotteryFailedStep.Draw,
            Attempts = 5
        };

        foreach (var kind in new[]
                 {
                     LotteryAutomationStatus.LiveKind, LotteryAutomationStatus.DryRunKind,
                     LotteryAutomationStatus.PausedKind, LotteryAutomationStatus.DisabledKind
                 })
        foreach (var startupResult in new LotteryStartupResult?[]
                 {
                     LotteryStartupResult.Healthy, LotteryStartupResult.Degraded(["bad"]), null
                 })
        foreach (var snapshot in new[] { Snapshot(kind: kind), Snapshot(kind: kind, pendingRound: pending) })
        {
            var check = CreateCheck(snapshot, startupResult is null
                ? new LotteryStartupCheckService(new HangingStartupCheck(),
                    NullLogger<LotteryStartupCheckService>.Instance)
                : await StartupCheckAsync(startupResult));

            var result = await check.CheckHealthAsync(_context);

            result.Status.ShouldNotBe(HealthStatus.Unhealthy);
        }
    }

    private LotteryHealthCheck CreateCheck(LotteryStatusSnapshot snapshot, LotteryStartupCheckService startup,
        TimeSpan? readTimeout = null)
    {
        _runner.Setup(runner => runner.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);
        return new LotteryHealthCheck(_runner.Object, startup, readTimeout);
    }

    private static async Task<LotteryStartupCheckService> StartupCheckAsync(LotteryStartupResult result)
    {
        var check = new Mock<ILotteryStartupCheck>();
        check.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>())).ReturnsAsync(result);

        var service = new LotteryStartupCheckService(check.Object,
            NullLogger<LotteryStartupCheckService>.Instance);
        await service.StartAsync(CancellationToken.None);
        await service.Completion;
        return service;
    }

    private static LotteryStatusSnapshot Snapshot(
        string kind = LotteryAutomationStatus.LiveKind,
        bool schedulerEnabled = true,
        LotteryRound? pendingRound = null,
        string? lastError = null)
    {
        return new LotteryStatusSnapshot(kind, schedulerEnabled, 1_000_000, null, pendingRound, NextRun,
            "2026-11-17", lastError);
    }

    /// <summary>A startup check that never answers, so the service keeps <c>Result == null</c> like a slow start.</summary>
    private sealed class HangingStartupCheck : ILotteryStartupCheck
    {
        public Task<LotteryStartupResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            return new TaskCompletionSource<LotteryStartupResult>(TaskCreationOptions.RunContinuationsAsynchronously)
                .Task;
        }
    }
}

