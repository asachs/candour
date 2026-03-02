# Middleware Pipeline

Candour's Azure Functions API uses three middleware layers that process every incoming HTTP request. The middleware is registered in `Program.cs` and executes in a fixed order. The ordering is deliberate -- changing it would break either security or anonymity guarantees.

## Pipeline Order

Every HTTP request passes through the following stages:

```
Incoming Request
  |
  1. AuthenticationMiddleware
  |    - Checks if route is admin-protected
  |    - Validates JWT (Entra ID) or API key
  |    - Returns 401/403 if unauthorized
  |
  2. RateLimitingMiddleware
  |    - Checks if route is rate-limited
  |    - Reads X-Forwarded-For to derive IP key
  |    - Enforces per-endpoint request limits
  |    - Returns 429 if limit exceeded
  |
  3. AnonymityMiddleware
  |    - Checks if route is respondent-facing
  |    - Strips all identifying headers from request
  |    - Strips Set-Cookie from response
  |
  4. Function Handler (MediatR CQRS)
       - Processes the request with zero access to client identity
```

## AuthenticationMiddleware

The authentication middleware protects admin routes while allowing respondent routes to pass through unauthenticated. It uses a compiled regex to match admin paths (`/api/surveys`, `.../publish`, `.../analyze`, `.../results`, `.../export`) and skips all respondent-facing routes such as `GET /api/surveys/{id}`, `POST .../validate-token`, and `POST .../responses`.

The middleware supports two modes: **Entra ID** (JWT validation against Microsoft Entra ID) for production, and **API key** (shared secret via the `X-Api-Key` header) for testing and local development. In either mode, unauthorized requests receive `401` or `403` responses, and `OPTIONS` requests are passed through for CORS preflight.

For full details on authentication modes, claim resolution, the admin email allowlist, and configuration, see [Authentication Modes](../configuration/auth-modes.md).

## RateLimitingMiddleware

The rate limiting middleware protects public endpoints from abuse. It uses Cosmos DB as a distributed counter store with TTL-based auto-cleanup.

### Which Endpoints Are Rate-Limited

Rate limits are applied per-endpoint with configurable windows and request caps. See [Rate Limiting Configuration](../configuration/rate-limiting.md#default-policies) for the current per-endpoint policies.

Admin routes are **not** rate-limited. They are already protected by authentication.

### How Keys Are Derived

All rate limiting uses IP-based keys. The middleware reads the `X-Forwarded-For` header (taking the first IP from a comma-separated list) and constructs a composite key:

```
ip:{client-ip}:{policy-name}
```

For example: `ip:203.0.113.42:submit-response`

If no `X-Forwarded-For` header is present (e.g., direct connections without a proxy), the request is not rate-limited.

!!! info "Why IP-based keys only"
    Token-based rate limiting keys were considered but removed. Azure Functions isolated worker body streams are not seekable -- reading the request body in middleware prevents downstream handlers from deserializing it. IP-based keys provide sufficient protection without this limitation.

### Cosmos DB Counter Storage

Each rate limit key maps to a document in the `rateLimits` container:

```json
{
  "key": "ip:203.0.113.42:submit-response",
  "count": 3,
  "windowStart": "2026-03-02T10:30:00Z",
  "ttl": 60
}
```

When a request arrives:

1. The middleware looks up the counter document by key.
2. If the document exists and the window has not expired, it checks whether `count >= maxRequests`.
3. If the limit is exceeded, it returns `429 Too Many Requests` with a `Retry-After` header.
4. Otherwise, it increments the counter and upserts the document.
5. If the window has expired, the counter resets to 1 with a new window start.
6. The document's `ttl` is set to the policy's window duration, so Cosmos DB automatically deletes expired counters.

!!! tip "Fail-open design"
    If the Cosmos DB rate limit repository is unavailable (network error, throttling, etc.), the middleware logs an error and **allows the request through**. Rate limiting should never cause a total outage of public endpoints.

### 429 Response Format

When rate-limited, the API returns `429 Too Many Requests` with `Retry-After` and `X-RateLimit-*` headers. See [Rate Limiting Configuration](../configuration/rate-limiting.md#429-response-format) for the full response format.

## AnonymityMiddleware

The anonymity middleware is the core of Candour's privacy architecture. It strips all identifying information from requests on respondent-facing routes, ensuring that no downstream handler, telemetry system, or log can access the client's identity.

### Which Routes It Applies To

The middleware uses a compiled regex to match respondent routes:

```
^(?:/api/surveys/[^/]+/(?:responses|results|validate-token)|/api/surveys/[^/]+)$
```

This matches:

- `GET /api/surveys/{id}` -- Survey retrieval
- `POST /api/surveys/{id}/validate-token` -- Token validation
- `POST /api/surveys/{id}/responses` -- Response submission
- `GET /api/surveys/{id}/results` -- Results viewing

Admin routes (`/api/surveys`, `.../publish`, `.../analyze`, `.../export`) are **not** matched -- admin requests retain their full headers.

!!! note "Why `results` is included"
    The `results` route is included for defence-in-depth -- although it requires Entra ID authentication (making IP stripping redundant), applying the middleware uniformly avoids accidental exposure if authentication is misconfigured.

### Headers Stripped From Requests

The middleware removes the following headers before the request reaches any handler:

| Header | Purpose | Why It Is Removed |
|--------|---------|-------------------|
| `X-Forwarded-For` | Client IP address chain from reverse proxies | Direct client identification |
| `X-Real-IP` | Client IP from Nginx-style proxies | Direct client identification |
| `X-Forwarded-Host` | Original host requested by the client | Can reveal client network |
| `X-Client-IP` | Client IP from some load balancers | Direct client identification |
| `CF-Connecting-IP` | Client IP from Cloudflare | Direct client identification |
| `True-Client-IP` | Client IP from Akamai/Cloudflare Enterprise | Direct client identification |

### Response Header Stripping

After the handler executes, the middleware also removes `Set-Cookie` from the response on respondent routes. This prevents any downstream component from inadvertently setting a tracking cookie on anonymous respondents.

!!! warning "Both request and response"
    The anonymity middleware operates on **both** the request (stripping identifying headers before the handler runs) and the response (stripping Set-Cookie after the handler runs). This bidirectional stripping ensures that anonymity is maintained even if a handler or framework component attempts to set a tracking cookie.

## Why This Order?

The three middleware layers must execute in the order: Authentication, Rate Limiting, Anonymity. Here is why:

### 1. Authentication first

Authentication must run before anything else because it determines whether the caller is authorized. An unauthenticated request to an admin route should be rejected immediately with a `401`, before consuming rate limit quota or any other processing.

### 2. Rate limiting second

Rate limiting must run **after** authentication (so that authenticated admin requests skip it) but **before** anonymity stripping. The rate limiting middleware reads the `X-Forwarded-For` header to derive the client IP for its counter key. If anonymity stripping ran first, the IP header would already be removed and rate limiting would have no key to work with.

### 3. Anonymity last

Anonymity stripping must be the final middleware before the handler. After rate limiting has read the IP for its counter key, the anonymity middleware removes all identifying headers. By the time the MediatR handler executes, there is no client identity information available in the request -- not in headers, not in context, not anywhere.

```
Authentication  -->  Rate Limiting  -->  Anonymity  -->  Handler
     |                    |                  |              |
  "Who are you?"    "How fast?"     "Now forget who"   "Process data"
```

!!! warning "Do not reorder"
    Swapping rate limiting and anonymity would break rate limiting (no IP to key on). Swapping authentication and rate limiting would waste rate limit quota on unauthorized requests. The current order is the only correct sequence.
