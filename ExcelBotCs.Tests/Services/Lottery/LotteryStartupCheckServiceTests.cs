using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The background wrapper around the startup check (spec §5.10, criterion 19): it must publish the verdict for the
///     scheduler and the health detail, never throw out of the fire-and-forget start, and never delay the host.
/// </summary>
public class LotteryStartupCheckServiceTests
{
    [Fact]
    public async Task StartAsync_PublishesTheCheckResultWithoutBlockingTheHost()
    {
        var check = new Mock<ILotteryStartupCheck>();
        var verdict = new TaskCompletionSource<LotteryStartupResult>();
        check.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>())).Returns(verdict.Task);

        var service = new LotteryStartupCheckService(check.Object,
            NullLogger<LotteryStartupCheckService>.Instance);

        var start = service.StartAsync(CancellationToken.None);

        // StartAsync returns immediately: a slow database may not hold up the web server or the Discord gateway.
        start.IsCompleted.ShouldBeTrue();
        service.Result.ShouldBeNull();

        verdict.SetResult(LotteryStartupResult.Healthy);
        await service.Completion;

        service.Result.ShouldBe(LotteryStartupResult.Healthy);
    }

    [Fact]
    public async Task StartAsync_WhenTheCheckThrows_StartsDegradedInsteadOfCrashing()
    {
        // ILotteryStartupCheck promises not to throw; the wrapper is the last line of defence so an unobserved task
        // exception can never take the host down (criterion 19).
        var check = new Mock<ILotteryStartupCheck>();
        check.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("MongoDB is unreachable"));

        var service = new LotteryStartupCheckService(check.Object,
            NullLogger<LotteryStartupCheckService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.Completion;

        service.Result.ShouldNotBeNull();
        service.Result!.IsDegraded.ShouldBeTrue();
        service.Result.SchedulerEnabled.ShouldBeFalse();
        service.Result.Problems.ShouldHaveSingleItem().ShouldContain("MongoDB is unreachable");
    }

    [Fact]
    public async Task StartAsync_LogsExactlyOneErrorWhenTheCheckThrows()
    {
        var logger = new Mock<ILogger<LotteryStartupCheckService>>();
        var check = new Mock<ILotteryStartupCheck>();
        check.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var service = new LotteryStartupCheckService(check.Object, logger.Object);
        await service.StartAsync(CancellationToken.None);
        await service.Completion;

        // The single alert of criterion 19: one error log line, which DiscordLogger mirrors into the log channel.
        logger.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_IsANoOp()
    {
        var service = new LotteryStartupCheckService(Mock.Of<ILotteryStartupCheck>(),
            NullLogger<LotteryStartupCheckService>.Instance);

        await Should.NotThrowAsync(() => service.StopAsync(CancellationToken.None));
    }
}
