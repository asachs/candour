# Deployment Guide

Step-by-step instructions for deploying Candour to Azure.

---

## Prerequisites

- Azure subscription with permissions to create resources
- Azure CLI (`az`) installed and authenticated
- .NET 9 SDK
- Azure Functions Core Tools v4

---

## 1. Entra ID App Registration

Create a single app registration that serves both the API (JWT validation) and the SPA (MSAL login).

### 1.1 Register the App

1. Go to **Azure Portal → Entra ID → App registrations → New registration**
2. Name: `Candour`
3. Supported account types: **Accounts in this organizational directory only** (single tenant)
4. Redirect URI: platform **SPA**, value `http://localhost:5000/authentication/login-callback`
5. Click **Register**

### 1.2 Note the IDs

From the app's **Overview** page, copy:

| Value | Where it goes |
|-------|---------------|
| **Application (client) ID** | `ClientId` in both Functions and WASM config |
| **Directory (tenant) ID** | `TenantId` in Functions config, `Authority` URL in WASM config |

### 1.3 Expose an API Scope

1. Go to **Expose an API**
2. Set **Application ID URI** to `api://{client-id}`
3. Click **Add a scope**:
   - Scope name: `access_as_user`
   - Who can consent: **Admins and users**
   - Admin consent display name: `Access Candour as a user`
4. Save

### 1.4 Grant the SPA Access

1. Go to **API permissions → Add a permission**
2. Select **My APIs → Candour**
3. Check `access_as_user`
4. Click **Add permissions**
5. Click **Grant admin consent** for your organization

### 1.5 Optional: Token Claims

1. Go to **Token configuration → Add optional claim**
2. Token type: **ID**
3. Select `email` and `preferred_username`

### 1.6 Add Production Redirect URI

Once you have your production frontend URL:

1. Go to **Authentication**
2. Under **Single-page application**, click **Add URI**
3. Add `https://{your-frontend-domain}/authentication/login-callback`

### 1.7 Adding External Admin Users

To grant admin access to users outside your tenant, invite them as **B2B guests**:

1. Go to **Entra ID → Users → Invite external user**
2. Enter their email address
3. They sign in with their own credentials but authenticate against your tenant

No code changes needed — guest users receive tokens from your tenant with valid `tid` and `oid` claims.

---

## 2. Azure Infrastructure

### 2.1 Resource Group

```bash
az group create \
  --name rg-candour \
  --location australiaeast
```

### 2.2 Cosmos DB

```bash
# Create account
az cosmosdb create \
  --name cosmos-candour \
  --resource-group rg-candour \
  --kind GlobalDocumentDB \
  --default-consistency-level Session

# Create database
az cosmosdb sql database create \
  --account-name cosmos-candour \
  --resource-group rg-candour \
  --name candour

# Get connection string
az cosmosdb keys list \
  --name cosmos-candour \
  --resource-group rg-candour \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" \
  --output tsv
```

Containers (`surveys`, `responses`, `usedtokens`) are created automatically on first startup by `CosmosDbInitializer`.

### 2.3 Storage Account (for Functions)

```bash
az storage account create \
  --name stcandourfunc \
  --resource-group rg-candour \
  --location australiaeast \
  --sku Standard_LRS
```

### 2.4 Azure Functions App

```bash
az functionapp create \
  --name func-candour \
  --resource-group rg-candour \
  --storage-account stcandourfunc \
  --consumption-plan-location australiaeast \
  --runtime dotnet-isolated \
  --runtime-version 9 \
  --functions-version 4 \
  --os-type Linux
```

### 2.5 Static Web App (for Blazor WASM)

```bash
az staticwebapp create \
  --name swa-candour \
  --resource-group rg-candour \
  --location australiaeast
```

Alternatively, use Azure Blob Storage static website hosting with a CDN.

---

## 3. Configuration

### 3.1 Functions App Settings

```bash
az functionapp config appsettings set \
  --name func-candour \
  --resource-group rg-candour \
  --settings \
    "Candour__Auth__UseEntraId=true" \
    "Candour__Auth__TenantId={your-tenant-id}" \
    "Candour__Auth__ClientId={your-client-id}" \
    "Candour__Auth__Audience=api://{your-client-id}" \
    "Candour__ApiKey=" \
    "CosmosDb__ConnectionString={your-cosmos-connection-string}" \
    "CosmosDb__DatabaseName=candour"
```

### 3.2 CORS

```bash
az functionapp cors add \
  --name func-candour \
  --resource-group rg-candour \
  --allowed-origins "https://{your-frontend-domain}"

# Enable credentials for auth headers
az resource update \
  --resource-group rg-candour \
  --name func-candour/web \
  --resource-type Microsoft.Web/sites/config \
  --set properties.cors.supportCredentials=true
```

### 3.3 Blazor WASM Configuration

Update `src/Candour.Web/wwwroot/appsettings.json` before publishing:

```json
{
  "AzureAd": {
    "Enabled": true,
    "Authority": "https://login.microsoftonline.com/{your-tenant-id}",
    "ClientId": "{your-client-id}",
    "ApiScope": "api://{your-client-id}/access_as_user"
  },
  "ApiBaseUrl": "https://func-candour.azurewebsites.net"
}
```

---

## 4. Deploy

### 4.1 Publish Functions

```bash
cd src/Candour.Functions
dotnet publish -c Release -o ./publish
cd publish
func azure functionapp publish func-candour
```

### 4.2 Publish Blazor WASM

```bash
cd src/Candour.Web
dotnet publish -c Release -o ./publish

# The publishable output is in:
# ./publish/wwwroot/
```

Deploy the `wwwroot` folder to your static hosting:

**Static Web Apps:**
```bash
# Via GitHub Actions (recommended) or:
az staticwebapp deploy \
  --name swa-candour \
  --app-location ./publish/wwwroot
```

**Blob Storage static website:**
```bash
az storage blob upload-batch \
  --account-name stcandourweb \
  --destination '$web' \
  --source ./publish/wwwroot
```

---

## 5. Verify

### 5.1 Health Check

```bash
# Functions API responds
curl -s -o /dev/null -w "%{http_code}" https://func-candour.azurewebsites.net/api/surveys

# Should return 401 (auth required for list endpoint)
```

### 5.2 Auth Flow

1. Open the frontend URL in a browser
2. Click **Login** — should redirect to Microsoft login
3. After sign-in, click **Admin** — should load the dashboard
4. Create a survey — `CreatorId` should be your Entra ID object ID (visible in Cosmos DB)

### 5.3 Respondent Flow

1. Publish a survey and copy a token link
2. Open the link in an incognito window (no auth required)
3. Submit a response — should succeed without login

---

## 6. CI/CD Pipeline

The workflow is at [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml). It runs on every push to `master`:

1. **`test`** — restores, builds, and runs `dotnet test` across all projects
2. **`deploy-functions`** — publishes `Candour.Functions` and deploys to Azure Functions (runs in parallel with `deploy-web`, both gated on `test`)
3. **`deploy-web`** — publishes `Candour.Web` and deploys the WASM output to Azure Static Web Apps

Pull requests run the `test` job only (no deploy).

### 6.1 GitHub Secrets Setup

Two secrets must be configured in **GitHub → Settings → Secrets and variables → Actions**:

#### `AZURE_FUNCTIONS_PUBLISH_PROFILE`

1. Go to **Azure Portal → func-candour → Overview**
2. Click **Get publish profile** (top bar)
3. Copy the entire XML contents of the downloaded `.PublishSettings` file
4. Paste as the secret value in GitHub

#### `AZURE_STATIC_WEB_APPS_TOKEN`

1. Go to **Azure Portal → swa-candour → Overview**
2. Click **Manage deployment token**
3. Copy the token
4. Paste as the secret value in GitHub

---

## Configuration Reference

### Functions App Settings

| Key | Required | Default | Description |
|-----|----------|---------|-------------|
| `Candour__Auth__UseEntraId` | Yes | `true` | Enable Entra ID JWT validation |
| `Candour__Auth__TenantId` | When Entra enabled | — | Azure AD tenant ID |
| `Candour__Auth__ClientId` | When Entra enabled | — | App registration client ID |
| `Candour__Auth__Audience` | When Entra enabled | — | Token audience (`api://{client-id}`) |
| `Candour__ApiKey` | No | `""` | API key for dev mode (empty = bypass) |
| `CosmosDb__ConnectionString` | Yes | — | Cosmos DB connection string |
| `CosmosDb__DatabaseName` | Yes | `candour` | Cosmos DB database name |

### WASM `appsettings.json`

| Key | Required | Default | Description |
|-----|----------|---------|-------------|
| `AzureAd:Enabled` | Yes | `true` | Enable MSAL authentication |
| `AzureAd:Authority` | When enabled | — | `https://login.microsoftonline.com/{tenant-id}` |
| `AzureAd:ClientId` | When enabled | — | App registration client ID |
| `AzureAd:ApiScope` | When enabled | — | `api://{client-id}/access_as_user` |
| `ApiBaseUrl` | No | WASM base address | Functions API base URL |

### Auth Modes

| `UseEntraId` | `ApiKey` | Behavior |
|--------------|----------|----------|
| `true` | (ignored) | Admin routes require `Authorization: Bearer <jwt>` |
| `false` | `""` (empty) | Admin routes are open (dev mode) |
| `false` | `"secret"` | Admin routes require `X-Api-Key: secret` header |
