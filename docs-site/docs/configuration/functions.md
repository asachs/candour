# Functions API Configuration

The Candour Functions API is configured through application settings. In Azure, these are set as Function App application settings. In local development, they are defined in `local.settings.json`.

## Settings Reference

### Authentication

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `Candour__Auth__UseEntraId` | Enable Entra ID JWT validation for admin routes | `true` | Yes |
| `Candour__Auth__TenantId` | Azure AD tenant ID for JWT validation | `""` | When `UseEntraId=true` |
| `Candour__Auth__ClientId` | Entra ID app registration client ID | `""` | When `UseEntraId=true` |
| `Candour__Auth__Audience` | Expected JWT audience claim (typically `api://{client-id}`) | `""` | When `UseEntraId=true` |
| `Candour__Auth__AdminEmails` | Semicolon-separated list of allowed admin email addresses | `""` | No |
| `Candour__ApiKey` | API key for dev/test mode (when `UseEntraId=false`) | `""` | No |

!!! tip "Empty API key = open access"
    When `UseEntraId` is `false` and `ApiKey` is empty, all admin routes are open without any credentials. This is intended for local development only. See [Authentication Modes](auth-modes.md) for details.

### Cosmos DB

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `CosmosDb__ConnectionString` | Cosmos DB connection string | — | Yes |
| `CosmosDb__DatabaseName` | Database name within the Cosmos account | `candour` | Yes |

Cosmos DB containers (`surveys`, `responses`, `usedTokens`, `rateLimits`) are created automatically on first startup by the `CosmosDbInitializer`. No manual container setup is required.

### Key Vault

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `KeyVault__Uri` | Azure Key Vault URI (e.g., `https://kv-candour.vault.azure.net`) | — | No |
| `KeyVault__KeyName` | RSA key name within the vault for batch secret encryption | — | No |

!!! info "Key Vault fallback"
    When Key Vault settings are not configured, the system falls back to ASP.NET Data Protection for batch secret encryption. This fallback is suitable for local development but should not be used in production.

### AI Analysis (Optional)

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `Candour__AI__Provider` | AI provider for survey analysis (`ollama` or `none`) | `none` | No |
| `Candour__AI__Endpoint` | Ollama API endpoint | `http://localhost:11434` | When provider is `ollama` |
| `Candour__AI__Model` | Ollama model name | `llama3` | When provider is `ollama` |

### General

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `Candour__FrontendBaseUrl` | Base URL of the frontend application, used to construct shareable token links in the publish response (e.g., `https://app.candour.example`) | `""` | No |

### Azure Functions Runtime

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `AzureWebJobsStorage` | Storage account connection string for the Functions runtime | — | Yes |
| `FUNCTIONS_WORKER_RUNTIME` | Must be set to `dotnet-isolated` | `dotnet-isolated` | Yes |

### Rate Limiting

Rate limiting is configured under the `RateLimiting` section. See [Rate Limiting Configuration](rate-limiting.md) for full details.

## Setting Values

### Azure Portal

1. Navigate to your Function App in the Azure Portal.
2. Go to **Configuration** under **Settings**.
3. Add or edit application settings using the double-underscore (`__`) notation for nested keys.

For example, `Candour:Auth:TenantId` in code becomes `Candour__Auth__TenantId` as an application setting.

### Azure CLI

```bash
az functionapp config appsettings set \
  --name func-candour \
  --resource-group rg-candour \
  --settings \
    "Candour__Auth__UseEntraId=true" \
    "Candour__Auth__TenantId=your-tenant-id" \
    "Candour__Auth__ClientId=your-client-id" \
    "Candour__Auth__Audience=api://your-client-id" \
    "Candour__Auth__AdminEmails=admin@app.candour.example" \
    "CosmosDb__ConnectionString=your-connection-string" \
    "CosmosDb__DatabaseName=candour"
```

### Local Development (`local.settings.json`)

For local development, settings are stored in `src/Candour.Functions/local.settings.json`. This file is `.gitignore`d and should never be committed.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDb__ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=...",
    "CosmosDb__DatabaseName": "candour",
    "Candour__ApiKey": "",
    "Candour__Auth__UseEntraId": "false",
    "Candour__Auth__TenantId": "",
    "Candour__Auth__ClientId": "",
    "Candour__Auth__Audience": ""
  },
  "Host": {
    "CORS": "http://localhost:5000,http://localhost:5001,https://localhost:5001",
    "CORSCredentials": true
  }
}
```

!!! note "CORS in local development"
    The `Host.CORS` setting enables cross-origin requests from the Blazor WASM dev server. In production, CORS is configured at the Azure Function App level via `az functionapp cors add`.

### Configuration Binding

Settings are bound to strongly-typed options classes using the .NET configuration system:

- `Candour:Auth:*` binds to `EntraIdOptions`
- `CosmosDb:*` binds to `CosmosDbOptions`
- `RateLimiting:*` binds to `RateLimitingOptions`

The double-underscore (`__`) separator in environment variables and Azure app settings is automatically translated to the colon (`:`) separator used in .NET configuration.
