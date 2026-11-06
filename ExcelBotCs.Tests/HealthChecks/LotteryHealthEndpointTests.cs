using System.Net;
using System.Text.Json;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelBotCs.Tests.HealthChecks;

/// <summary>
///     <c>/health</c> after #115: the existing Discord and Mongo checks plus the <c>lottery</c> detail (spec §5.10
///     "Health", §14; criterion 17). The endpoint is what the container probe calls, so the lottery detail is reported
///     next to the core checks and a stuck round can only degrade it — never turn the probe into a failure.
/// </summary>
[Collection("MongoDB")]
public class LotteryHealthEndpointTests : IntegrationTestBase
{
    public LotteryHealthEndpointTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHealth_WithAnUnfinishedRound_DegradesTheLotteryDetailAndReturnsOk()
    {
        // Criterion 17: the stuck round the state machine reports as pending. The detail names it; the probe stays 200.
        await SeedStuckRoundAsync();

        var response = await Client.GetAsync("/health");
        var report = await ReadReportAsync(response);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        report.RootElement.GetProperty("status").GetString().ShouldBe("Degraded");

        var lottery = LotteryDetail(report);
        lottery.GetProperty("status").GetString().ShouldBe("Degraded");
        lottery.GetProperty("description").GetString().ShouldContain("2026-07-28");

        var data = lottery.GetProperty("data");
        data.GetProperty("degraded").GetBoolean().ShouldBeTrue();
        data.GetProperty("pendingRoundKey").GetString().ShouldBe("2026-07-28");
        data.GetProperty("pendingStatus").GetString().ShouldBe(LotteryRoundStatus.Drawn);
        data.GetProperty("failedStep").GetString().ShouldBe(LotteryFailedStep.Announcement);
        data.GetProperty("lastError").GetString().ShouldBe("Discord is unreachable");
    }

    [Fact]
    public async Task GetHealth_ReportsTheLotteryDetailNextToTheExistingChecks()
    {
        var response = await Client.GetAsync("/health");
        var report = await ReadReportAsync(response);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var checks = report.RootElement.GetProperty("checks");
        checks.TryGetProperty("discord-bot", out _).ShouldBeTrue();
        checks.TryGetProperty("mongodb", out _).ShouldBeTrue();
        checks.GetProperty("mongodb").GetProperty("status").GetString().ShouldBe("Healthy");

        // The test deployment leaves the deployment kill switch off, so the detail reports it as a degraded state
        // rather than pretending the automation is live.
        var data = LotteryDetail(report).GetProperty("data");
        data.GetProperty("state").GetString().ShouldBe(LotteryAutomationStatus.DisabledKind);
        data.GetProperty("schedulerEnabled").GetBoolean().ShouldBeFalse();
    }

    private async Task SeedStuckRoundAsync()
    {
        var rounds = Factory.Services.GetRequiredService<ILotteryRoundRepository>();
        await rounds.EnsureScheduledAsync("2026-07-28", new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc));
        await rounds.RegisterFailureAsync("2026-07-28", LotteryFailedStep.Announcement,
            LotteryRoundStatus.Drawn, 3, "Discord is unreachable",
            new DateTime(2026, 7, 28, 19, 0, 0, DateTimeKind.Utc), null);
    }

    private static JsonElement LotteryDetail(JsonDocument report)
    {
        return report.RootElement.GetProperty("checks").GetProperty("lottery");
    }

    private static async Task<JsonDocument> ReadReportAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }
}
