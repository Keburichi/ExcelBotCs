using ExcelBotCs.Services.Lottery;

namespace ExcelBotCs.Tests.Services.Lottery;

public class LotteryMaintenanceRequestTests
{
	[Fact]
	public void Parse_IsNotRequested_ForAnOrdinaryLaunch()
	{
		var request = LotteryMaintenanceRequest.Parse(["urls=http://localhost:8080"]);

		request.IsRequested.ShouldBeFalse();
	}

	[Fact]
	public void Parse_RequestsTheBackfill()
	{
		var request = LotteryMaintenanceRequest.Parse(["--lottery-backfill"]);

		request.IsRequested.ShouldBeTrue();
		request.Backfill.ShouldBeTrue();
		request.EnsureIndexes.ShouldBeFalse();
	}

	[Fact]
	public void Parse_RequestsTheIndexMigration()
	{
		var request = LotteryMaintenanceRequest.Parse(["--lottery-ensure-indexes"]);

		request.IsRequested.ShouldBeTrue();
		request.EnsureIndexes.ShouldBeTrue();
		request.Backfill.ShouldBeFalse();
	}

	[Fact]
	public void Parse_AcceptsBothFlags()
	{
		var request = LotteryMaintenanceRequest.Parse(["--lottery-ensure-indexes", "--lottery-backfill"]);

		request.EnsureIndexes.ShouldBeTrue();
		request.Backfill.ShouldBeTrue();
	}
}
