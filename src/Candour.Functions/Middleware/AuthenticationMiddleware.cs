namespace Candour.Functions.Middleware;

using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Candour.Functions.Auth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

public partial class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IJwtTokenValidator _jwtValidator;
    private readonly IConfiguration _configuration;
    private readonly bool _useEntraId;

    // Admin routes: /api/surveys (list+create), /api/surveys/{id}/publish, /api/surveys/{id}/analyze
    [GeneratedRegex(@"^/api/surveys(?:/[^/]+/(?:publish|analyze))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AdminRoutePattern();

    public AuthenticationMiddleware(IJwtTokenValidator jwtValidator, IConfiguration configuration)
    {
        _jwtValidator = jwtValidator;
        _configuration = configuration;
        _useEntraId = configuration.GetValue<bool>("Candour:Auth:UseEntraId");
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData == null)
        {
            await next(context);
            return;
        }

        var path = requestData.Url.AbsolutePath;

        // Only protect admin routes
        if (!AdminRoutePattern().IsMatch(path))
        {
            await next(context);
            return;
        }

        // Skip CORS preflight
        if (string.Equals(requestData.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (_useEntraId)
        {
            var principal = await ValidateBearerToken(requestData);
            if (principal == null)
            {
                var response = requestData.CreateResponse(HttpStatusCode.Unauthorized);
                context.GetInvocationResult().Value = response;
                return;
            }

            context.Items["User"] = principal;
        }
        else
        {
            // Dev mode: fall back to API key validation
            if (!AuthHelper.ValidateApiKey(requestData, _configuration))
            {
                var response = requestData.CreateResponse(HttpStatusCode.Unauthorized);
                context.GetInvocationResult().Value = response;
                return;
            }
        }

        await next(context);
    }

    private async Task<ClaimsPrincipal?> ValidateBearerToken(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("Authorization", out var values))
            return null;

        var authHeader = values.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authHeader["Bearer ".Length..];
        return await _jwtValidator.ValidateTokenAsync(token);
    }
}
