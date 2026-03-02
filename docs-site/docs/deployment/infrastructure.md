# Infrastructure as Code

Candour's Azure infrastructure is defined as Bicep templates and deployed through a single shell script. This page documents the templates, parameters, resources, and deployment process.

---

## Overview

The infrastructure is split across two Bicep files and one deployment script:

| File | Purpose |
|------|---------|
| `infra/main.bicep` | Subscription-level entry point; creates the resource group and invokes the resources module |
| `infra/resources.bicep` | Defines all Azure resources within the resource group |
| `infra/deploy.sh` | End-to-end deployment script: Entra ID registration, Bicep deployment, code publish, and verification |

The Bicep templates target a serverless architecture. Cosmos DB uses the serverless capacity mode, Functions run on a Flex Consumption plan, and the Static Web App uses the Free tier. This keeps the baseline cost near zero for low-traffic deployments.

---

## Parameters

### main.bicep

The top-level template accepts these parameters:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `location` | string | `westeurope` | Azure region for all resources |
| `environmentName` | string | `prod` | Environment name used in resource group naming |
| `apiKey` | secureString | (required) | API key for non-Entra admin authentication |
| `entraIdTenantId` | string | Subscription tenant ID | Entra ID tenant ID |
| `entraIdClientId` | string | (required) | Entra ID app registration client ID |
| `entraIdAudience` | string | `''` | Token audience; defaults to `clientId` when empty |
| `adminEmails` | array | `[]` | Email addresses of admin users |
| `tags` | object | `{}` | Tags applied to all resources |

!!! note "Scope"
    `main.bicep` targets `subscription` scope. It creates the resource group and delegates resource creation to the `resources.bicep` module.

### resources.bicep

The resource module receives these parameters from the parent template:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `location` | string | (required) | Azure region |
| `uniqueSuffix` | string | (required) | Generated suffix for globally unique resource names |
| `apiKey` | secureString | (required) | API key |
| `entraIdTenantId` | string | (required) | Entra ID tenant ID |
| `entraIdClientId` | string | (required) | App registration client ID |
| `entraIdAudience` | string | (required) | Token audience |
| `adminEmails` | array | `[]` | Admin email addresses (semicolon-delimited in app settings) |
| `staticWebAppUrl` | string | `''` | Override SWA URL for CORS; auto-detected from the SWA resource when empty |
| `deployRbac` | bool | `false` | Whether to deploy RBAC role assignments for Key Vault |
| `tags` | object | `{}` | Tags applied to all resources |

---

## Resources Created

The `resources.bicep` template provisions the following resources:

| Resource | Naming Convention | Purpose |
|----------|-------------------|---------|
| Log Analytics Workspace | `law-candour-{suffix}` | Centralized log collection |
| Application Insights | `appi-candour-{suffix}` | Application performance monitoring and telemetry |
| Storage Account | `stcandour{suffix}` | Function App runtime state and deployment packages |
| Blob Container | `deploymentpackage` | Flex Consumption deployment artifact storage |
| Cosmos DB Account | `cosmos-candour-{suffix}` | Serverless document database |
| Cosmos DB Database | `candour` | Application database |
| Cosmos DB Container | `surveys` | Survey documents (partition key: `/id`) |
| Cosmos DB Container | `responses` | Response documents (partition key: `/surveyId`) |
| Cosmos DB Container | `usedTokens` | Token tracking with unique key on `/tokenHash` (partition key: `/surveyId`) |
| Cosmos DB Container | `rateLimits` | Rate limiting counters with TTL (partition key: `/key`) |
| Key Vault | `kv-candour-{suffix}` | Secrets storage and batch encryption key |
| Key Vault Key | `candour-batch-secret` | RSA 2048-bit key for batch operations |
| Static Web App | `swa-candour-{suffix}` | Blazor WASM frontend hosting (Free tier) |
| App Service Plan | `asp-candour-{suffix}` | Flex Consumption hosting plan for Functions |
| Function App | `func-candour-{suffix}` | .NET 9 isolated worker API backend |
| RBAC Assignment | (auto-named) | Storage Blob Data Owner for Function App identity |
| RBAC Assignment | (conditional) | Key Vault Crypto User for Function App identity (when `deployRbac=true`) |

### Security Configuration

The templates enforce several security defaults:

- **Storage Account:** `allowSharedKeyAccess` is disabled; the Function App accesses storage through its managed identity
- **Storage Account:** `minimumTlsVersion` is set to TLS 1.2; public blob access is disabled
- **Function App:** HTTPS-only is enforced
- **Key Vault:** RBAC authorization is enabled; soft delete is active with a 7-day retention
- **CORS:** Configured with credentials support; localhost origins included for development

---

## Outputs

The Bicep deployment produces these outputs, used by the deployment script for subsequent steps:

| Output | Description |
|--------|-------------|
| `resourceGroupName` | Name of the created resource group |
| `functionAppName` | Function App resource name |
| `functionAppUrl` | Function App HTTPS URL |
| `cosmosDbAccountName` | Cosmos DB account name |
| `keyVaultName` | Key Vault resource name |
| `appInsightsName` | Application Insights resource name |
| `staticWebAppName` | Static Web App resource name |
| `staticWebAppUrl` | Static Web App default hostname URL |

---

## Deployment Script

The `infra/deploy.sh` script automates the full deployment lifecycle.

### Usage

```bash
./infra/deploy.sh [environment]
```

The environment argument defaults to `prod`. The script loads configuration from `infra/.env.{environment}`.

### What the Script Does

The script executes six steps in sequence:

1. **Entra ID App Registration** -- Creates or reuses an app registration. Skipped if `CANDOUR_ENTRA_CLIENT_ID` is already set in the environment.
2. **API Key Resolution** -- Retrieves the API key from Key Vault if infrastructure already exists. Otherwise, generates a new key and stores it in Key Vault after deployment.
3. **Bicep Deployment** -- Runs `az deployment sub create` with the `main.bicep` template and reads outputs (resource names, URLs).
4. **Function App Deployment** -- Builds `Candour.Functions` in Release mode, creates a zip package, and deploys via `az functionapp deployment source config-zip`.
5. **Static Web App Deployment** -- Builds `Candour.Web` in Release mode, generates an environment-specific `appsettings.json`, and deploys using the SWA CLI.
6. **Verification** -- Tests that the API and SWA endpoints respond with expected HTTP status codes.

### Environment Configuration

Create an `infra/.env.prod` file (or `.env.{environment}` for other environments) with the following variables:

| Variable | Required | Description |
|----------|----------|-------------|
| `CANDOUR_SUBSCRIPTION` | Yes | Azure subscription ID |
| `CANDOUR_OWNER_ALIAS` | Yes | Owner alias for resource tagging |
| `CANDOUR_OWNER_EMAIL` | Yes | Owner email for resource tagging |
| `CANDOUR_ADMIN_EMAILS` | Yes | Semicolon-delimited admin email addresses |
| `CANDOUR_LOCATION` | No | Azure region (default: `westeurope`) |
| `CANDOUR_ENV` | No | Environment name (default: `prod`) |
| `CANDOUR_ENTRA_CLIENT_ID` | No | Pre-existing app registration client ID; created automatically if empty |
| `CANDOUR_ENTRA_TENANT_ID` | No | Entra ID tenant ID; detected from current Azure session if empty |
| `CANDOUR_API_KEY` | No | Pre-existing API key; generated automatically if empty |

!!! danger "Sensitive values"
    The `.env` files may contain secrets. They should be listed in `.gitignore` and never committed to version control.

### Example Run

```bash
# Deploy to the production environment
./infra/deploy.sh prod

# Deploy to a staging environment
./infra/deploy.sh staging
```

The script prints resource URLs and a test command on completion:

```
=== DEPLOYMENT COMPLETE ===
Function App URL: https://api.candour.example
Static Web App:   https://app.candour.example
Entra ID App ID:  xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

Test with:
  curl https://api.candour.example/api/surveys
```
