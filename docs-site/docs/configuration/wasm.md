# Blazor WASM Configuration

The Candour Blazor WASM frontend is configured through `wwwroot/appsettings.json`. Since Blazor WASM runs entirely in the browser, this file is served as a static asset and parsed at runtime by the .NET WebAssembly host.

## Configuration Schema

The `appsettings.json` file has the following structure:

```json
{
  "ApiBaseUrl": "https://api.candour.example",
  "EngineeringMode": true,
  "AzureAd": {
    "Enabled": true,
    "Authority": "https://login.microsoftonline.com/{tenant-id}",
    "ClientId": "{client-id}",
    "ApiScope": "api://{client-id}/access_as_user"
  }
}
```

### Settings Reference

| Setting | Type | Default | Required | Description |
|---------|------|---------|----------|-------------|
| `ApiBaseUrl` | `string` | WASM base address | No | Base URL for the Functions API. When omitted, defaults to the origin the WASM app is served from. |
| `EngineeringMode` | `bool` | `true` | No | When enabled, shows respondents the exact Cosmos DB document stored after submission and an explicit list of what was *not* stored (IP, user agent, token, identity, cookies). |
| `AzureAd:Enabled` | `bool` | `true` | Yes | Enables MSAL authentication for admin features. Set to `false` for local development without Entra ID. |
| `AzureAd:Authority` | `string` | — | When `Enabled=true` | The Entra ID authority URL. Format: `https://login.microsoftonline.com/{tenant-id}` |
| `AzureAd:ClientId` | `string` | — | When `Enabled=true` | The app registration client ID. Must match the Functions API `Candour:Auth:ClientId` setting. |
| `AzureAd:ApiScope` | `string` | — | When `Enabled=true` | The API scope to request in tokens. Format: `api://{client-id}/access_as_user` |

!!! warning "Client ID must match"
    The `AzureAd:ClientId` in the WASM config and the `Candour__Auth__ClientId` in the Functions config must reference the same Entra ID app registration. A mismatch will cause JWT validation to fail with `401 Unauthorized` on admin API calls.

## Local Development Configuration

During local development, the WASM app uses the default `appsettings.json` at `src/Candour.Web/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:5001",
  "EngineeringMode": true,
  "AzureAd": {
    "Enabled": false,
    "Authority": "https://login.microsoftonline.com/{tenant-id}",
    "ClientId": "{client-id}",
    "ApiScope": "api://{client-id}/access_as_user"
  }
}
```

Key differences from production:

- **`ApiBaseUrl`** points to the local Functions dev server.
- **`AzureAd:Enabled`** is `false`, disabling MSAL authentication. Admin features are accessible without login.
- **`EngineeringMode`** is `true` for development visibility.

!!! tip "No appsettings.Development.json"
    Blazor WASM does not support `appsettings.Development.json` in the same way as server-side ASP.NET. The WASM host reads `appsettings.json` as a static file from `wwwroot/`. To use different settings locally, edit `wwwroot/appsettings.json` directly. This file is not committed to source control with production values -- the CI/CD pipeline overwrites it during deployment.

## CI/CD Configuration Injection

The deploy workflow generates the production `appsettings.json` at build time, replacing the local development file with environment-specific values. This happens in the `deploy-web` job of the GitHub Actions pipeline.

The workflow step:

```yaml
- name: Generate environment appsettings.json
  run: |
    cat > ./publish-web/wwwroot/appsettings.json <<EOF
    {
      "ApiBaseUrl": "${{ vars.API_BASE_URL }}",
      "EngineeringMode": true,
      "AzureAd": {
        "Enabled": true,
        "Authority": "https://login.microsoftonline.com/${{ vars.TEST_ENTRA_TENANT_ID }}",
        "ClientId": "${{ vars.TEST_ENTRA_CLIENT_ID }}",
        "ApiScope": "api://${{ vars.TEST_ENTRA_CLIENT_ID }}/access_as_user"
      }
    }
    EOF
    rm -f ./publish-web/wwwroot/appsettings.json.br ./publish-web/wwwroot/appsettings.json.gz
```

The pipeline:

1. Publishes the Blazor WASM project with `dotnet publish`.
2. Overwrites the `appsettings.json` in the published `wwwroot/` directory with production values sourced from GitHub repository variables.
3. Removes pre-compressed `.br` and `.gz` versions of the file so that Azure Static Web Apps re-compresses the updated file.
4. Deploys the `wwwroot/` folder to Azure Static Web Apps.

### Required GitHub Variables

| Variable | Description |
|----------|-------------|
| `API_BASE_URL` | Production Functions API base URL (e.g., `https://api.candour.example`) |
| `TEST_ENTRA_TENANT_ID` | Entra ID tenant ID for the target environment |
| `TEST_ENTRA_CLIENT_ID` | Entra ID app registration client ID |

!!! info "Why variables, not secrets?"
    The WASM `appsettings.json` is served as a public static file -- its contents are visible to anyone who opens the browser's DevTools. Tenant ID, client ID, and API scope are not secrets. They are public identifiers required for the MSAL login flow. Actual secrets (connection strings, API keys) live only in the Functions API configuration.
