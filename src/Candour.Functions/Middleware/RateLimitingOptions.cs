namespace Candour.Functions.Middleware;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new()
    {
        ["get-survey"] = new RateLimitPolicy { WindowSeconds = 60, MaxRequests = 30 },
        ["validate-token"] = new RateLimitPolicy { WindowSeconds = 60, MaxRequests = 10 },
        ["submit-response"] = new RateLimitPolicy { WindowSeconds = 60, MaxRequests = 5 }
    };
}

public class RateLimitPolicy
{
    public int WindowSeconds { get; set; } = 60;
    public int MaxRequests { get; set; } = 10;
}
