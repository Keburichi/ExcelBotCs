using ExcelBotCs.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExcelBotCs.Tests.Middleware;

public class RequestIdMiddlewareTests
{
    private RequestIdMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new RequestIdMiddleware(next, NullLogger<RequestIdMiddleware>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_WithXRequestIdHeader_SetsTraceIdentifierToHeaderValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "my-trace-id";

        await CreateMiddleware().InvokeAsync(context);

        context.TraceIdentifier.ShouldBe("my-trace-id");
    }

    [Fact]
    public async Task InvokeAsync_WithoutXRequestIdHeader_SetsTraceIdentifier()
    {
        var context = new DefaultHttpContext();

        await CreateMiddleware().InvokeAsync(context);

        context.TraceIdentifier.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_WithoutXRequestIdHeader_GeneratesValid32CharHexString()
    {
        var context = new DefaultHttpContext();

        await CreateMiddleware().InvokeAsync(context);

        context.TraceIdentifier.ShouldMatch("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task InvokeAsync_WithoutXRequestIdHeader_GeneratesUniqueIdPerRequest()
    {
        var ctx1 = new DefaultHttpContext();
        var ctx2 = new DefaultHttpContext();
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(ctx1);
        await middleware.InvokeAsync(ctx2);

        ctx1.TraceIdentifier.ShouldNotBe(ctx2.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithXRequestIdHeader_StillCallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "some-id";

        await middleware.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
    }
}