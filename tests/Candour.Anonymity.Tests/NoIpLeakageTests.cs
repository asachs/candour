namespace Candour.Anonymity.Tests;

using System.Net;
using Candour.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

public class NoIpLeakageTests
{
    [Fact]
    public async Task AnonymityMiddleware_StripsIpHeaders_OnResponseRoutes()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/responses";
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1";
        context.Request.Headers["X-Real-IP"] = "10.0.0.1";
        context.Request.Headers["X-Client-IP"] = "172.16.0.1";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");

        var middleware = new AnonymityMiddleware(
            next: (ctx) => Task.CompletedTask,
            logger: NullLogger<AnonymityMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-For"));
        Assert.False(context.Request.Headers.ContainsKey("X-Real-IP"));
        Assert.False(context.Request.Headers.ContainsKey("X-Client-IP"));
        Assert.Equal(IPAddress.None, context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task AnonymityMiddleware_StripsIpHeaders_OnSurveyRoutes()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/survey/abc123";
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        var middleware = new AnonymityMiddleware(
            next: (ctx) => Task.CompletedTask,
            logger: NullLogger<AnonymityMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-For"));
        Assert.Equal(IPAddress.None, context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task AnonymityMiddleware_DoesNotStrip_OnAdminRoutes()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/dashboard";
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");

        var middleware = new AnonymityMiddleware(
            next: (ctx) => Task.CompletedTask,
            logger: NullLogger<AnonymityMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        // Admin routes should NOT have IP stripped
        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-For"));
        Assert.Equal(IPAddress.Parse("192.168.1.1"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task AnonymityMiddleware_RemovesCookies_OnResponseRoutes()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/responses";

        var middleware = new AnonymityMiddleware(
            next: (ctx) =>
            {
                ctx.Response.Headers.Append("Set-Cookie", "session=abc123");
                return Task.CompletedTask;
            },
            logger: NullLogger<AnonymityMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }
}
