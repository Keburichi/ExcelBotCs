using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The hosted scheduler shell: it ticks through the injected clock and delegates to the round engine, and a
///     throwing engine can never take the host down.
/// </summary>
public class LotterySchedulerServiceTests
{
	private readonly Mock<ILotteryRoundRunner> _runner = new();

	private LotterySchedulerService CreateService()
	{
		return new LotterySchedulerService(Mock.Of<IServiceScopeFactory>(), _runner.Object,
			new FakeTimeProvider(), Mock.Of<ILogger<LotterySchedulerService>>());
	}

	[Fact]
	public async Task TickOnceAsync_DelegatesToTheRoundEngine()
	{
		_runner.Setup(runner => runner.TickAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new LotteryTickResult(LotteryTickAction.Nothing, "nothing due"));

		var result = await CreateService().TickOnceAsync();

		result.Action.ShouldBe(LotteryTickAction.Nothing);
		_runner.Verify(runner => runner.TickAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task TickOnceAsync_WhenTheEngineThrows_DoesNotPropagate()
	{
		_runner.Setup(runner => runner.TickAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("boom"));

		var result = await CreateService().TickOnceAsync();

		result.Action.ShouldBe(LotteryTickAction.Nothing);
		result.Message.ShouldContain("boom");
	}

	[Fact]
	public void Interval_IsOneMinute()
	{
		LotterySchedulerService.Interval.ShouldBe(TimeSpan.FromMinutes(1));
	}
}
