# Rate Limiting Configuration

Candour uses distributed rate limiting backed by Cosmos DB to protect public endpoints from abuse. Rate limit counters are stored as documents with TTL-based auto-expiration, requiring no background cleanup process.

## Default Policies

The following policies are applied out of the box. All policies use IP-based keys derived from the `X-Forwarded-For` header.

| Endpoint | Policy Name | Window (seconds) | Max Requests | Purpose |
|----------|------------|-------------------|--------------|---------|
| `GET /api/surveys/{id}` | `get-survey` | 60 | 30 | Prevent rapid survey scraping |
| `POST /api/surveys/{id}/validate-token` | `validate-token` | 60 | 10 | Prevent token brute-force |
| `POST /api/surveys/{id}/responses` | `submit-response` | 60 | 5 | Prevent response flooding |

!!! note "Admin routes are not rate-limited"
    Rate limiting applies only to public, unauthenticated endpoints. Admin routes are already protected by JWT/API key authentication and are excluded from rate limiting.

## Configuration Format

Rate limiting policies are configured under the `RateLimiting:Policies` section. Each policy has a name, a window duration in seconds, and a maximum number of requests per window.

### JSON Format (local.settings.json)

```json
{
  "Values": {
    "RateLimiting__Policies__get-survey__WindowSeconds": "120",
    "RateLimiting__Policies__get-survey__MaxRequests": "60",
    "RateLimiting__Policies__validate-token__WindowSeconds": "120",
    "RateLimiting__Policies__validate-token__MaxRequests": "20",
    "RateLimiting__Policies__submit-response__WindowSeconds": "120",
    "RateLimiting__Policies__submit-response__MaxRequests": "10"
  }
}
```

### Azure App Settings

Set individual policy values using the double-underscore notation:

```bash
az functionapp config appsettings set \
  --name func-candour \
  --resource-group rg-candour \
  --settings \
    "RateLimiting__Policies__get-survey__WindowSeconds=120" \
    "RateLimiting__Policies__get-survey__MaxRequests=60" \
    "RateLimiting__Policies__validate-token__WindowSeconds=120" \
    "RateLimiting__Policies__validate-token__MaxRequests=20" \
    "RateLimiting__Policies__submit-response__WindowSeconds=120" \
    "RateLimiting__Policies__submit-response__MaxRequests=10"
```

### Options Class

The configuration binds to `RateLimitingOptions`:

```csharp
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
```

Default values are defined in the options class itself. Configuration overrides only the values you explicitly set -- unspecified policies retain their defaults.

## Disabling Rate Limiting

There is no global toggle to disable rate limiting entirely. However, you can effectively disable it by setting very high limits:

```bash
az functionapp config appsettings set \
  --name func-candour \
  --resource-group rg-candour \
  --settings \
    "RateLimiting__Policies__get-survey__MaxRequests=999999" \
    "RateLimiting__Policies__validate-token__MaxRequests=999999" \
    "RateLimiting__Policies__submit-response__MaxRequests=999999"
```

!!! warning "Do not disable rate limiting in production"
    Rate limiting is a defense against abuse on unauthenticated endpoints. Disabling it exposes the system to token brute-force attacks and response flooding. Only raise limits for load testing or specific operational needs.

### Fail-Open Behavior

If the Cosmos DB rate limit repository is unavailable (e.g., Cosmos DB throttling, network error), the middleware logs an error and allows the request through. This fail-open design ensures that a transient infrastructure issue does not cause a complete outage of public endpoints.

## 429 Response Format

When a request exceeds the rate limit, the API returns a `429 Too Many Requests` response with the following structure:

### Response Headers

| Header | Description |
|--------|-------------|
| `Retry-After` | Number of seconds until the current rate limit window expires |
| `X-RateLimit-Limit` | Maximum number of requests allowed in the window |
| `X-RateLimit-Remaining` | Always `0` when the response is 429 |

### Response Body

```json
{
  "error": "Rate limit exceeded. Try again in 45 seconds."
}
```

The `Retry-After` value is calculated as the number of seconds remaining until the current window expires. Clients should use this value to schedule their retry rather than immediately retrying.

### Example

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 45
X-RateLimit-Limit: 5
X-RateLimit-Remaining: 0
Content-Type: application/json

{
  "error": "Rate limit exceeded. Try again in 45 seconds."
}
```

## How Counters Work

Rate limit counters are stored in the `rateLimits` Cosmos DB container, partitioned by the composite key.

Each counter document has the following shape:

```json
{
  "key": "ip:203.0.113.42:submit-response",
  "count": 3,
  "windowStart": "2026-03-02T10:30:00Z",
  "ttl": 60
}
```

**Window lifecycle:**

1. **First request** -- A new counter document is created with `count: 1` and `ttl` set to the policy's `WindowSeconds`.
2. **Subsequent requests** -- The counter is incremented. If `count >= MaxRequests` and the window has not expired, the request is rejected.
3. **Window expiration** -- When the window expires (current time >= `windowStart + windowSeconds`), the next request resets the counter to 1 with a new window start.
4. **TTL cleanup** -- Cosmos DB automatically deletes the document after the TTL expires, keeping the container clean without a background job.

!!! info "IP key derivation"
    The client IP is extracted from the first value in the `X-Forwarded-For` header. This header is populated by Azure's infrastructure (load balancers, Static Web Apps proxy) before the request reaches the Function App. If no `X-Forwarded-For` header is present, the request is not rate-limited.
