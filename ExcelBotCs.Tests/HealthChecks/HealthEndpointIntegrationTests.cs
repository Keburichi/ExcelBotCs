using System.Net;
using System.Text.Json;
using ExcelBotCs.TestFramework.Database;
using ExcelBotCs.Tests.Utils;

namespace ExcelBotCs.Tests.HealthChecks;

[Collection("MongoDB")]
public class HealthEndpointIntegrationTests : IntegrationTestBase
{
    public HealthEndpointIntegrationTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_DoesNotRequireAuthentication()
    {
        SetUnauthenticated();

        var response = await Client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsTheJsonReportOfEveryCheck()
    {
        var response = await Client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // #115: the body is the per-check report, so the lottery detail sits next to the core checks. The status code
        // is the probe contract (asserted above); the body is diagnostics.
        using var report = JsonDocument.Parse(body);
        var checks = report.RootElement.GetProperty("checks");

        checks.TryGetProperty("discord-bot", out _).ShouldBeTrue();
        checks.TryGetProperty("mongodb", out _).ShouldBeTrue();
        checks.TryGetProperty("lottery", out var lottery).ShouldBeTrue();
        lottery.GetProperty("status").GetString().ShouldBeOneOf("Healthy", "Degraded");
        lottery.TryGetProperty("data", out _).ShouldBeTrue();
    }

    // --- Request ID header tests (use /health as a no-auth endpoint for convenience) ---

    [Fact]
    public async Task AnyRequest_ResponseContainsXRequestIdHeader()
    {
        var response = await Client.GetAsync("/health");

        response.Headers.Contains("X-Request-ID").ShouldBeTrue();
    }

    [Fact]
    public async Task AnyRequest_WithXRequestIdHeader_EchoesBackSameId()
    {
        const string requestId = "my-test-correlation-id";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Request-ID", requestId);

        var response = await Client.SendAsync(request);

        response.Headers.TryGetValues("X-Request-ID", out var values);
        values.ShouldNotBeNull();
        values!.Single().ShouldBe(requestId);
    }

    [Fact]
    public async Task AnyRequest_WithoutXRequestIdHeader_GeneratesUniqueIdPerRequest()
    {
        var response1 = await Client.GetAsync("/health");
        var response2 = await Client.GetAsync("/health");

        response1.Headers.TryGetValues("X-Request-ID", out var values1);
        response2.Headers.TryGetValues("X-Request-ID", out var values2);

        values1.ShouldNotBeNull();
        values2.ShouldNotBeNull();
        values1!.Single().ShouldNotBe(values2!.Single());
    }

    [Fact]
    public async Task AnyRequest_GeneratedXRequestId_Is32CharHex()
    {
        var response = await Client.GetAsync("/health");

        response.Headers.TryGetValues("X-Request-ID", out var values);
        var id = values!.Single();

        id.ShouldMatch("^[0-9a-f]{32}$");
    }
}