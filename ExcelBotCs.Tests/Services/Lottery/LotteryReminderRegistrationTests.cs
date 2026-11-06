using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The reminder services and the scheduler that drives them resolve from the host container. A missing
///     registration would only surface on a Monday evening, so it is pinned here.
/// </summary>
[Collection("MongoDB")]
public class LotteryReminderRegistrationTests : IntegrationTestBase
{
	public LotteryReminderRegistrationTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public void TheReminderServiceResolvesFromTheHostContainer()
	{
		Factory.Services.GetRequiredService<ILotteryReminderService>()
			.ShouldBeOfType<LotteryReminderService>();
	}

	[Fact]
	public void TheSchedulerResolvesWithItsReminderDependency()
	{
		Factory.Services.GetRequiredService<LotterySchedulerService>().ShouldNotBeNull();
	}
}
