using System.Text.Json;
using ExcelBotCs.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExcelBotCs.Tests.HealthChecks;

/// <summary>
///     The <c>/health</c> report is anonymous (§5.10, §14), so the writer redacts the data keys that can carry raw
///     dependency errors. The lottery detail already reports a coarse reason instead of its <c>lastError</c>, and this
///     pins the writer's own guard so a future check cannot leak one by adding it to <c>Data</c>.
/// </summary>
public class HealthResponseWriterTests
{
	[Fact]
	public async Task WriteAsync_RedactsSensitiveDataKeysAndKeepsTheStructuredFacts()
	{
		var data = new Dictionary<string, object>
		{
			["pendingRoundKey"] = "2026-11-10",
			["pendingStatus"] = "drawn",
			["attempts"] = 3,
			["lastError"] = "Discord is unreachable (token abc)",
			["problems"] = new[] { "DiscordBot:GuildId is not configured (0)." }
		};

		var entry = new HealthReportEntry(HealthStatus.Degraded, "A round is stuck", TimeSpan.FromMilliseconds(2),
			exception: null, data: data);

		var report = new HealthReport(
			new Dictionary<string, HealthReportEntry> { [LotteryHealthCheck.Name] = entry },
			TimeSpan.FromMilliseconds(5));

		var context = new DefaultHttpContext();
		var stream = new MemoryStream();
		context.Response.Body = stream;

		await HealthResponseWriter.WriteAsync(context, report);

		stream.Position = 0;
		using var json = await JsonDocument.ParseAsync(stream);
		var jsonData = json.RootElement.GetProperty("checks").GetProperty(LotteryHealthCheck.Name)
			.GetProperty("data");

		jsonData.GetProperty("pendingRoundKey").GetString().ShouldBe("2026-11-10");
		jsonData.GetProperty("pendingStatus").GetString().ShouldBe("drawn");
		jsonData.GetProperty("attempts").GetInt32().ShouldBe(3);
		jsonData.GetProperty("lastError").GetString().ShouldBe(HealthResponseWriter.Redacted);
		jsonData.GetProperty("problems").GetString().ShouldBe(HealthResponseWriter.Redacted);

		stream.Position = 0;
		var body = await new StreamReader(stream).ReadToEndAsync();
		body.ShouldNotContain("unreachable");
		body.ShouldNotContain("GuildId");
	}

	[Fact]
	public void CoarseReason_ClassifiesWithoutQuotingTheMessage()
	{
		LotteryHealthCheck.CoarseReason("Discord is unreachable").ShouldBe("a Discord error is recorded");
		LotteryHealthCheck.CoarseReason("MongoDB connection timeout").ShouldBe("a database error is recorded");
		LotteryHealthCheck.CoarseReason("something odd").ShouldBe("an error is recorded");
	}
}
