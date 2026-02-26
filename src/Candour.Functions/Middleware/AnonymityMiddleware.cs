namespace Candour.Functions.Middleware;

using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

public partial class AnonymityMiddleware : IFunctionsWorkerMiddleware
{
    // Respondent-facing routes that need IP stripping
    [GeneratedRegex(@"^(?:/api/surveys/[^/]+/(?:responses|results|validate-token)|/api/surveys/[^/]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RespondentRoutePattern();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData != null)
        {
            var path = requestData.Url.AbsolutePath;
            if (RespondentRoutePattern().IsMatch(path))
            {
                StripIdentifyingHeaders(requestData);
            }
        }

        await next(context);

        // Strip Set-Cookie from response on respondent routes
        if (requestData != null && RespondentRoutePattern().IsMatch(requestData.Url.AbsolutePath))
        {
            var response = context.GetHttpResponseData();
            if (response != null)
            {
                response.Headers.Remove("Set-Cookie");
            }
        }
    }

    private static void StripIdentifyingHeaders(HttpRequestData request)
    {
        var headersToRemove = new[]
        {
            "X-Forwarded-For", "X-Real-IP", "X-Forwarded-Host",
            "X-Client-IP", "CF-Connecting-IP", "True-Client-IP"
        };

        foreach (var header in headersToRemove)
        {
            request.Headers.Remove(header);
        }
    }
}
