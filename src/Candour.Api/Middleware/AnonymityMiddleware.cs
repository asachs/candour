namespace Candour.Api.Middleware;

using System.Net;
using System.Text.RegularExpressions;

public partial class AnonymityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AnonymityMiddleware> _logger;

    // Compiled regex for respondent-facing routes that need IP stripping
    // Matches:
    //   /api/surveys/{guid}/responses  (submit response)
    //   /api/surveys/{guid}/results    (view results)
    //   /api/surveys/{guid}            (view survey — exactly, NOT /publish /analyze etc.)
    //   /survey/{id}                   (Blazor UI)
    [GeneratedRegex(@"^(?:/api/surveys/[^/]+/(?:responses|results)|/api/surveys/[^/]+|/survey/[^/]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RespondentRoutePattern();

    public AnonymityMiddleware(RequestDelegate next, ILogger<AnonymityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (RespondentRoutePattern().IsMatch(path))
        {
            StripIdentifyingInformation(context);
        }

        await _next(context);
    }

    private static void StripIdentifyingInformation(HttpContext context)
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

        // Register callback to strip Set-Cookie before response headers are sent
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("Set-Cookie");
            return Task.CompletedTask;
        });
    }
}
