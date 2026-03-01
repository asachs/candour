namespace Candour.Functions.Middleware;

using System.Net;
using System.Text.RegularExpressions;
using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public partial class RateLimitingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IRateLimitRepository _repository;
    private readonly RateLimitingOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // GET /api/surveys/{id} — but NOT /api/surveys (admin list)
    [GeneratedRegex(@"^/api/surveys/[^/]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GetSurveyPattern();

    // POST /api/surveys/{id}/validate-token
    [GeneratedRegex(@"^/api/surveys/[^/]+/validate-token$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ValidateTokenPattern();

    // POST /api/surveys/{id}/responses
    [GeneratedRegex(@"^/api/surveys/[^/]+/responses$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SubmitResponsePattern();

    public RateLimitingMiddleware(
        IRateLimitRepository repository,
        IOptions<RateLimitingOptions> options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _repository = repository;
        _options = options.Value;
        _logger = logger;
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
        var method = requestData.Method.ToUpperInvariant();

        var (policyName, key) = await ResolvePolicy(path, method, requestData);

        if (policyName == null || key == null)
        {
            await next(context);
            return;
        }

        if (!_options.Policies.TryGetValue(policyName, out var policy))
        {
            await next(context);
            return;
        }

        try
        {
            var now = DateTime.UtcNow;
            var entry = await _repository.GetAsync(key, context.CancellationToken);

            if (entry != null)
            {
                var windowEnd = entry.WindowStart.AddSeconds(policy.WindowSeconds);
                if (now < windowEnd && entry.Count >= policy.MaxRequests)
                {
                    var retryAfter = (int)Math.Ceiling((windowEnd - now).TotalSeconds);
                    _logger.LogWarning("Rate limit exceeded for endpoint {Endpoint}, window {WindowSeconds}s",
                        policyName, policy.WindowSeconds);

                    var response = requestData.CreateResponse(HttpStatusCode.TooManyRequests);
                    response.Headers.Add("Retry-After", retryAfter.ToString());
                    response.Headers.Add("X-RateLimit-Limit", policy.MaxRequests.ToString());
                    response.Headers.Add("X-RateLimit-Remaining", "0");
                    await response.WriteAsJsonAsync(new
                    {
                        error = $"Rate limit exceeded. Try again in {retryAfter} seconds."
                    }, context.CancellationToken);
                    context.GetInvocationResult().Value = response;
                    return;
                }

                if (now >= windowEnd)
                {
                    // Window expired — reset
                    entry.Count = 1;
                    entry.WindowStart = now;
                    entry.TtlSeconds = policy.WindowSeconds;
                }
                else
                {
                    entry.Count++;
                }
            }
            else
            {
                entry = new RateLimitEntry
                {
                    Key = key,
                    Count = 1,
                    WindowStart = now,
                    TtlSeconds = policy.WindowSeconds
                };
            }

            await _repository.UpsertAsync(entry, context.CancellationToken);
        }
        catch (Exception ex)
        {
            // Rate limiting should not block requests if the repository is unavailable
            _logger.LogError(ex, "Rate limiting check failed for endpoint {Endpoint}, allowing request", policyName);
        }

        await next(context);
    }

    private Task<(string? policyName, string? key)> ResolvePolicy(
        string path, string method, HttpRequestData request)
    {
        // All rate limiting uses IP-based keys. Token-hash keys were removed because
        // Azure Functions isolated worker body streams are not seekable — reading the
        // body in middleware prevents downstream handlers from deserializing it.
        if (method == "GET" && GetSurveyPattern().IsMatch(path))
        {
            var ip = ExtractIpAddress(request);
            return Task.FromResult<(string?, string?)>(("get-survey", ip != null ? $"ip:{ip}:get-survey" : null));
        }

        if (method == "POST" && ValidateTokenPattern().IsMatch(path))
        {
            var ip = ExtractIpAddress(request);
            return Task.FromResult<(string?, string?)>(("validate-token", ip != null ? $"ip:{ip}:validate-token" : null));
        }

        if (method == "POST" && SubmitResponsePattern().IsMatch(path))
        {
            var ip = ExtractIpAddress(request);
            return Task.FromResult<(string?, string?)>(("submit-response", ip != null ? $"ip:{ip}:submit-response" : null));
        }

        return Task.FromResult<(string?, string?)>((null, null));
    }

    private static string? ExtractIpAddress(HttpRequestData request)
    {
        // Read X-Forwarded-For before AnonymityMiddleware strips it
        if (request.Headers.TryGetValues("X-Forwarded-For", out var values))
        {
            var forwarded = values.FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                // Take first IP (client IP) from comma-separated list
                return forwarded.Split(',', StringSplitOptions.TrimEntries)[0];
            }
        }

        return null;
    }

}
