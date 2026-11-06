using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The reminder services and the scheduler that drives them resolve from the host container, and the host wires the
///     startup verdict into the reminder. A missing registration would only surface on a Monday evening, so it is
///     pinned here.
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

	[Fact]
	public async Task TheHostWiresTheStartupVerdictIntoTheReminderTick()
	{
		// The integration host removes the background startup check, so the verdict stays "pending". If the host did
		// not inject it, the tick would fall through to the reminder toggle below and report `reminders-disabled`.
		var outcome = await Factory.Services.GetRequiredService<ILotteryReminderService>().TickAsync();

		outcome.Disposition.ShouldBe(LotteryReminderDisposition.StartupPending);
	}
}
