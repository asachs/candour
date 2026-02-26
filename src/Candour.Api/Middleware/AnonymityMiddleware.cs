namespace Candour.Api.Middleware;

using System.Net;

public class AnonymityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AnonymityMiddleware> _logger;

    public AnonymityMiddleware(RequestDelegate next, ILogger<AnonymityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Strip IP info on ALL survey response routes
        if (path.Contains("/api/responses") || path.Contains("/survey/"))
        {
            // Remove forwarded headers that could contain IP
            context.Request.Headers.Remove("X-Forwarded-For");
            context.Request.Headers.Remove("X-Real-IP");
            context.Request.Headers.Remove("X-Forwarded-Host");
            context.Request.Headers.Remove("X-Client-IP");
            context.Request.Headers.Remove("CF-Connecting-IP");
            context.Request.Headers.Remove("True-Client-IP");

            // Overwrite connection remote IP
            context.Connection.RemoteIpAddress = IPAddress.None;
        }

        await _next(context);

        // Ensure no cookies set for respondent routes
        if (path.Contains("/api/responses") || path.Contains("/survey/"))
        {
            context.Response.Headers.Remove("Set-Cookie");
        }
    }
}
