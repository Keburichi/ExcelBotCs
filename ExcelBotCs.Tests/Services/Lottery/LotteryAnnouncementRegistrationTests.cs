using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The announcement services have real dependencies (two repositories, the settings service, two option sets and
///     the alert seam), and the publisher is only resolved when the scheduler first posts. This test resolves them
///     from the host container so a missing or misordered registration fails here instead of on a Tuesday evening.
/// </summary>
[Collection("MongoDB")]
public class LotteryAnnouncementRegistrationTests : IntegrationTestBase
{
	public LotteryAnnouncementRegistrationTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public void TheAnnouncementServicesResolveFromTheHostContainer()
	{
		Factory.Services.GetRequiredService<ILotteryAnnouncementPublisher>()
			.ShouldBeOfType<LotteryAnnouncementPublisher>();
		Factory.Services.GetRequiredService<ILotteryAnnouncementPreviewer>()
			.ShouldBeOfType<LotteryAnnouncementPreviewer>();
		Factory.Services.GetRequiredService<ILotteryAlertService>().ShouldBeOfType<LotteryAlertService>();
	}
}
