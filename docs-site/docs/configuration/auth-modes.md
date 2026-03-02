# Authentication Modes

Candour supports three authentication modes for admin routes. The mode is determined by two configuration settings: `Candour__Auth__UseEntraId` and `Candour__ApiKey`.

## Mode Summary

| Mode | `UseEntraId` | `ApiKey` | Admin Route Behavior |
|------|-------------|----------|---------------------|
| **Entra ID** | `true` | (ignored) | Requires `Authorization: Bearer <jwt>` header |
| **API Key** | `false` | `"secret"` | Requires `X-Api-Key: secret` header |
| **Disabled** | `false` | `""` (empty) | Admin routes are open with no credentials |

!!! warning "Disabled mode is for local development only"
    When authentication is disabled, anyone who can reach the API can create, publish, and manage surveys. Never deploy to a shared or public environment with auth disabled.

## Entra ID Mode (Production)

Entra ID mode uses Microsoft Entra ID (Azure Active Directory) for OAuth 2.0 / OpenID Connect authentication. This is the recommended mode for all deployed environments.

### When to Use

- Production deployments
- Staging/test environments that share an Entra ID tenant
- Any environment where admin access must be restricted to specific users

### How It Works

1. The Blazor WASM frontend uses MSAL to redirect the user to the Microsoft login page.
2. After authentication, MSAL receives a JWT with the user's identity claims.
3. The frontend includes this JWT as a `Bearer` token on every API request to admin routes.
4. The `AuthenticationMiddleware` validates the JWT by:
    - Fetching the tenant's OpenID Connect discovery document from `https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration`
    - Validating the token's signature, issuer, audience, and expiration
    - Optionally checking the user's email against the admin allowlist

### Configuration

**Functions API (`local.settings.json` or Azure App Settings):**

```json
{
  "Values": {
    "Candour__Auth__UseEntraId": "true",
    "Candour__Auth__TenantId": "your-tenant-id",
    "Candour__Auth__ClientId": "your-client-id",
    "Candour__Auth__Audience": "api://your-client-id",
    "Candour__Auth__AdminEmails": "admin@app.candour.example;lead@app.candour.example"
  }
}
```

**Blazor WASM (`wwwroot/appsettings.json`):**

```json
{
  "AzureAd": {
    "Enabled": true,
    "Authority": "https://login.microsoftonline.com/your-tenant-id",
    "ClientId": "your-client-id",
    "ApiScope": "api://your-client-id/access_as_user"
  }
}
```

### Admin Email Allowlist

The `Candour__Auth__AdminEmails` setting restricts admin access to a specific set of email addresses. Emails are semicolon-separated and compared case-insensitively.

```
Candour__Auth__AdminEmails=admin@app.candour.example;lead@app.candour.example
```

The middleware resolves the user's email from JWT claims in this priority order:

1. `email` claim
2. `preferred_username` claim
3. `upn` (User Principal Name) claim

When the allowlist is empty, any valid JWT from the configured tenant grants admin access.

!!! tip "Guest users"
    External users invited as B2B guests in your Entra ID tenant receive tokens from your tenant with valid claims. Add their email to the allowlist to grant them admin access. No code changes are needed.

## API Key Mode (Testing)

API key mode provides a simple shared-secret authentication mechanism. It is intended for automated testing, CI/CD pipelines, and integration test suites that need to call admin endpoints without an Entra ID login flow.

### When to Use

- Automated integration tests
- CI/CD pipeline smoke tests
- Development environments where Entra ID is not available but some access control is desired

### How It Works

1. The `AuthenticationMiddleware` detects that `UseEntraId` is `false`.
2. It validates the request's `X-Api-Key` header against the configured `Candour__ApiKey` value.
3. If the key matches, the request proceeds. If not, a `401 Unauthorized` response is returned.

### Configuration

**Functions API:**

```json
{
  "Values": {
    "Candour__Auth__UseEntraId": "false",
    "Candour__ApiKey": "your-test-api-key"
  }
}
```

**Client usage:**

```bash
curl -H "X-Api-Key: your-test-api-key" \
  https://api.candour.example/api/surveys
```

!!! note "WASM frontend in API key mode"
    When the Functions API uses API key mode, set `AzureAd:Enabled` to `false` in the WASM `appsettings.json`. This disables the MSAL login flow in the browser. The WASM frontend will need to be modified or extended to include the API key header if admin features are required in this mode.

## Disabled Mode (Local Development)

Disabled mode removes all authentication from admin routes. This is the fastest way to develop locally without any identity infrastructure.

### When to Use

- Local development with the Cosmos DB Emulator
- Quick prototyping and debugging
- Environments where you are the only user

### How It Works

1. The `AuthenticationMiddleware` detects that `UseEntraId` is `false`.
2. It checks the `Candour__ApiKey` value, which is empty.
3. An empty API key is treated as a bypass -- all admin requests are allowed without credentials.

### Configuration

**Functions API (`local.settings.json`):**

```json
{
  "Values": {
    "Candour__Auth__UseEntraId": "false",
    "Candour__ApiKey": ""
  }
}
```

**Blazor WASM (`wwwroot/appsettings.json`):**

```json
{
  "AzureAd": {
    "Enabled": false
  }
}
```

This is the default configuration shipped in the repository's `local.settings.json`.

## Switching Between Modes

### Local to Production

To move from local development (disabled mode) to production (Entra ID mode):

1. Register an app in Entra ID (see the [Deployment Guide](../deployment/guide.md) for full instructions).
2. Set `Candour__Auth__UseEntraId=true` and provide the tenant/client/audience values in the Functions configuration.
3. Set `AzureAd:Enabled=true` and provide the authority/client/scope values in the WASM `appsettings.json`.
4. Optionally configure `Candour__Auth__AdminEmails` to restrict access to specific users.

### Production to API Key (for Testing)

To temporarily switch a deployed environment to API key mode (e.g., for automated testing):

1. Set `Candour__Auth__UseEntraId=false` in the Function App settings.
2. Set `Candour__ApiKey` to a strong random value.
3. Update test scripts to include the `X-Api-Key` header.

!!! warning "Restart required"
    The `UseEntraId` setting determines which `IJwtTokenValidator` implementation is registered at startup. Changing this setting requires a Function App restart for the new mode to take effect. In Azure, updating app settings triggers an automatic restart.

### JWT Validator Implementations

The authentication mode determines which JWT validator is injected at startup:

| Mode | Validator Class | Behavior |
|------|----------------|----------|
| Entra ID (`UseEntraId=true`) | `EntraIdJwtTokenValidator` | Full JWT validation against Entra ID OIDC metadata |
| API Key / Disabled (`UseEntraId=false`) | `NoOpJwtTokenValidator` | No JWT validation; falls back to API key check |

This selection happens once at application startup in `Program.cs`:

```csharp
var useEntraId = builder.Configuration.GetValue<bool>("Candour:Auth:UseEntraId");
if (useEntraId)
{
    builder.Services.AddSingleton<IJwtTokenValidator, EntraIdJwtTokenValidator>();
}
else
{
    builder.Services.AddSingleton<IJwtTokenValidator, NoOpJwtTokenValidator>();
}
```
