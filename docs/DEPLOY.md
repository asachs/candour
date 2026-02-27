# Deployment Guide

Battle-tested, step-by-step instructions for deploying Candour to Azure. This guide reflects real deployment experience including common pitfalls and their solutions.

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| Azure CLI | Latest | `brew install azure-cli` or [docs](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) |
| .NET SDK | 9.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0) |
| Azure Functions Core Tools | v4 | `brew install azure-functions-core-tools@4` or [docs](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) |
| Node.js / npm | 18+ | Required for SWA CLI deployment |

Authenticate the Azure CLI before starting:

```bash
az login
az account set --subscription "your-subscription-id"
```

---

## Overview

Candour deploys as two separate Azure resources:

1. **Azure Functions (Flex Consumption)** — API backend (.NET 9 isolated worker)
2. **Azure Static Web Apps (Free tier)** — Blazor WASM frontend

Supporting infrastructure (Cosmos DB, Key Vault, App Insights, Log Analytics, Storage) is provisioned via Bicep.

---

## 1. Entra ID App Registration

Create a single app registration that serves both the API (JWT validation) and the SPA (MSAL login).

### 1.1 Register the App

1. Go to **Azure Portal → Entra ID → App registrations → New registration**
2. Name: `Candour`
3. Supported account types: **Accounts in this organizational directory only** (single tenant)
4. Redirect URI: platform **Single-page application (SPA)**, value `http://localhost:5000/authentication/login-callback`
5. Click **Register**

> **Important:** Choose **Single-page application** as the platform type, not **Web**. MSAL.js uses PKCE (Proof Key for Code Exchange) which requires the SPA platform. Using "Web" will cause `AADSTS50011` redirect URI mismatch errors.

### 1.2 Note the IDs

From the app's **Overview** page, copy:

| Value | Where it goes |
|-------|---------------|
| **Application (client) ID** | `CANDOUR_ENTRA_CLIENT_ID` in `.env`, `ClientId` in WASM `appsettings.json` |
| **Directory (tenant) ID** | Used in `Authority` URL in WASM `appsettings.json` |

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

### 1.5 Token Claims (Recommended)

1. Go to **Token configuration → Add optional claim**
2. Token type: **ID**
3. Select `email` and `preferred_username`
4. These claims allow the UI to display the logged-in user's name

### 1.6 Add Production Redirect URI (After Step 3)

Once you have your Static Web App URL from Step 3:

1. Go to **Authentication**
2. Under **Single-page application**, click **Add URI**
3. Add `https://{your-swa-domain}/authentication/login-callback`

> **Gotcha:** You must add the production URI as a **SPA** platform type. If you accidentally add it under **Web**, delete it and re-add it under the correct platform.

### 1.7 Adding External Admin Users

To grant admin access to users outside your tenant:

1. Go to **Entra ID → Users → Invite external user**
2. Enter their email address
3. Add their email to the `CANDOUR_ADMIN_EMAILS` environment variable

No code changes needed — guest users receive tokens from your tenant with valid claims.

---

## 2. Deploy Infrastructure (Bicep)

All Azure resources are defined as Infrastructure as Code in the `infra/` directory.

### 2.1 Create Environment File

```bash
cd infra
cp .env.example .env
```

Edit `.env` with your values:

```bash
CANDOUR_SUBSCRIPTION=your-azure-subscription-id
CANDOUR_LOCATION=northeurope          # See region notes below
CANDOUR_ENV=prod
CANDOUR_OWNER_ALIAS=youralias         # Used in resource group name + tags
CANDOUR_OWNER_EMAIL=you@example.com
CANDOUR_ADMIN_EMAILS=admin1@example.com;admin2@example.com
CANDOUR_API_KEY=                       # Leave empty to auto-generate
CANDOUR_ENTRA_CLIENT_ID=your-client-id-from-step-1
```

### 2.2 Run the Deployment Script

```bash
chmod +x deploy.sh
./deploy.sh
```

This script:
1. Loads `.env` and validates required variables
2. Creates or reuses an Entra ID app registration (if `CANDOUR_ENTRA_CLIENT_ID` is empty)
3. Generates an API key (if `CANDOUR_API_KEY` is empty)
4. Deploys all Bicep templates (subscription-scoped)
5. Publishes the .NET Functions app via zip deploy
6. Verifies the API is responding

### 2.3 What Gets Provisioned

| Resource | Name Pattern | Notes |
|----------|-------------|-------|
| Resource Group | `{alias}-rg-candour-{env}` | Subscription-scoped deployment creates this |
| Cosmos DB (Serverless) | `cosmos-candour-{hash}` | Session consistency, auto-created containers |
| Key Vault | `kv-candour-{hash}` | RSA key for batch secret encryption |
| Storage Account | `stcandour{hash}` | Functions runtime (blob deployment) |
| App Service Plan | `asp-candour-{hash}` | Flex Consumption (FC1) |
| Function App | `func-candour-{hash}` | .NET 9 isolated worker, Linux |
| Application Insights | `appi-candour-{hash}` | Operational telemetry only (no PII) |
| Log Analytics Workspace | `law-candour-{hash}` | Backing store for App Insights |

> **Region Note:** Not all regions support Flex Consumption (FC1) plans. If you get a `LocationNotAvailableForResourceType` error, try `northeurope`, `westeurope`, `eastus`, or `westus2`. Some organizations may have Azure Policy restrictions on regions — check with your admin.

### 2.4 RBAC Assignment (Manual Step)

The Function App needs **Key Vault Crypto User** role to use the batch secret key. The Bicep template includes this but it's gated behind `deployRbac=false` by default (requires `Microsoft.Authorization/roleAssignments/write` permission).

**If you have RBAC write permissions:**

Re-deploy with `deployRbac=true` in the Bicep parameters.

**If you don't (most common):**

1. Go to **Azure Portal → Key Vault → Access control (IAM)**
2. Click **Add → Add role assignment**
3. Role: **Key Vault Crypto User**
4. Members: Select the Function App's managed identity
5. Save

### 2.5 Verify API Deployment

After `deploy.sh` completes:

```bash
# Should return 401 (admin routes require auth)
curl -s -o /dev/null -w "%{http_code}" https://func-candour-{hash}.azurewebsites.net/api/surveys

# Should return 404 or empty (no surveys exist yet, but proves the app is running)
curl https://func-candour-{hash}.azurewebsites.net/api/surveys/nonexistent
```

---

## 3. Deploy Frontend (Static Web Apps)

The Blazor WASM frontend is deployed to Azure Static Web Apps separately.

### 3.1 Configure the Frontend

Update `src/Candour.Web/wwwroot/appsettings.json` with your production values:

```json
{
  "ApiBaseUrl": "https://func-candour-{hash}.azurewebsites.net",
  "AzureAd": {
    "Enabled": true,
    "Authority": "https://login.microsoftonline.com/{your-tenant-id}",
    "ClientId": "{your-client-id}",
    "ApiScope": "api://{your-client-id}/access_as_user"
  }
}
```

> **Important:** This file is embedded in the published WASM output. The committed version in the repo should contain placeholders (not real values) to avoid leaking tenant/client IDs in a public repository. Configure the real values before publishing, then revert before committing.

### 3.2 Publish the Blazor WASM App

```bash
dotnet publish src/Candour.Web -c Release -o .publish-web
```

### 3.3 Add SPA Routing Configuration

Blazor WASM is a single-page application — all routes must fall back to `index.html`. Create this file in the publish output:

```bash
cat > .publish-web/wwwroot/staticwebapp.config.json << 'EOF'
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/_framework/*", "/_content/*", "/lib/*", "*.css", "*.js", "*.png", "*.ico", "*.json"]
  }
}
EOF
```

> **Why?** Without this, navigating directly to `/admin` or `/survey/123` returns a 404 from the static host. The `exclude` list ensures actual static files (framework DLLs, CSS, images) are served directly.

### 3.4 Create the Static Web App

```bash
az staticwebapp create \
  --name swa-candour \
  --resource-group {your-resource-group} \
  --location westeurope \
  --sku Free
```

> **Region Note:** Static Web Apps are only available in a few regions: `westus2`, `centralus`, `eastus2`, `westeurope`, `eastasia`. This doesn't affect performance — SWA content is served via a global CDN regardless of the "home" region.

Get the deployment token:

```bash
az staticwebapp secrets list \
  --name swa-candour \
  --resource-group {your-resource-group} \
  --query "properties.apiKey" -o tsv
```

### 3.5 Deploy with SWA CLI

```bash
npx @azure/static-web-apps-cli deploy \
  .publish-web/wwwroot \
  --deployment-token "{your-swa-deployment-token}" \
  --env production
```

> **Gotcha:** If you get `EACCES` errors from npm cache, use a temp cache: `npm_config_cache=$(mktemp -d) npx @azure/static-web-apps-cli deploy ...`

Note the URL from the output (e.g., `https://delightful-forest-005427103.4.azurestaticapps.net`).

### 3.6 Configure CORS on the Function App

The Function App must accept requests from the Static Web App origin:

```bash
az functionapp cors add \
  --name func-candour-{hash} \
  --resource-group {your-resource-group} \
  --allowed-origins "https://{your-swa-domain}"

# Enable credentials (required for auth headers)
az resource update \
  --resource-group {your-resource-group} \
  --name func-candour-{hash}/web \
  --resource-type Microsoft.Web/sites/config \
  --set properties.cors.supportCredentials=true
```

### 3.7 Add Redirect URI to Entra ID

Go back to **Step 1.6** and add the production redirect URI:

```
https://{your-swa-domain}/authentication/login-callback
```

### 3.8 Revert appsettings.json

If this is a public repository, revert the production values before committing:

```bash
git checkout -- src/Candour.Web/wwwroot/appsettings.json
```

---

## 4. Verify End-to-End

### 4.1 API Health

```bash
# Admin route should require auth
curl -s -o /dev/null -w "%{http_code}" https://func-candour-{hash}.azurewebsites.net/api/surveys
# Expected: 401

# Results route should require auth
curl -s -o /dev/null -w "%{http_code}" https://func-candour-{hash}.azurewebsites.net/api/surveys/test/results
# Expected: 401
```

### 4.2 Frontend + Auth

1. Open `https://{your-swa-domain}` in a browser
2. Click **Login** — should redirect to Microsoft sign-in
3. After sign-in, your name should appear in the navigation bar
4. Click **Admin** — should load the dashboard (if your email is in the admin allowlist)

### 4.3 Create and Test a Survey

1. In the Admin dashboard, create a new survey
2. Publish the survey to generate token URLs
3. Copy a token URL and open it in an **incognito window** (no auth required)
4. Submit a response
5. Back in the admin view, check results (requires minimum response threshold)

---

## 5. Troubleshooting

### Function App Won't Start

**Symptom:** 500 errors or "host not found" after deployment.

**Check logs:**
```bash
az functionapp log tail \
  --name func-candour-{hash} \
  --resource-group {your-resource-group}
```

**Common causes:**

| Error | Solution |
|-------|----------|
| `Could not load file or assembly 'Newtonsoft.Json'` | Ensure `Candour.Functions.csproj` has `<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />`. The Azure Functions Worker SDK declares it in a sidecar manifest but doesn't always copy the DLL. |
| `Cosmos DB connection failed` | Check `CosmosDb__ConnectionString` app setting. Verify the Cosmos DB account exists and the connection string is current. |
| `Key Vault access denied` | The Function App's managed identity needs **Key Vault Crypto User** role (see Step 2.4). |

### Frontend Shows Blank Page

**Symptom:** White screen, no content loads.

**Check browser dev tools (F12 → Console):**

| Error | Solution |
|-------|----------|
| `AuthenticationService.init is undefined` | Ensure `index.html` includes `<script src="_content/Microsoft.Authentication.WebAssembly.Msal/AuthenticationService.js"></script>` before the Blazor script. |
| `Failed to fetch _framework/*.dll` | SPA routing not configured. Ensure `staticwebapp.config.json` with `navigationFallback` is in the published output (see Step 3.3). |
| CORS errors in console | The Function App's CORS settings must include the SWA origin (see Step 3.6). |

### Auth Errors

| Error | Solution |
|-------|----------|
| `AADSTS50011: Reply URL does not match` | The redirect URI must be registered as **SPA** platform type (not Web) in the Entra ID app registration. Check both `http://localhost:5000/authentication/login-callback` (dev) and `https://{swa-domain}/authentication/login-callback` (prod) are present under **Single-page application**. |
| `AADSTS700054: response_type 'code' is not enabled` | Same cause — the redirect URI is registered under **Web** instead of **SPA**. Delete and re-add under the correct platform. |
| `401 on admin routes after successful login` | Check that your email is in the `Candour__Auth__AdminEmails` app setting (semicolon-delimited). |

---

## 6. CI/CD Pipeline

Example GitHub Actions workflow for automated deployment:

```yaml
name: Deploy Candour

on:
  push:
    branches: [master]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet test

  deploy-functions:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet publish src/Candour.Functions -c Release -o ./publish
      - uses: Azure/functions-action@v1
        with:
          app-name: func-candour-{hash}
          package: ./publish
          publish-profile: ${{ secrets.AZURE_FUNCTIONS_PUBLISH_PROFILE }}

  deploy-web:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Configure production settings
        run: |
          cat > src/Candour.Web/wwwroot/appsettings.json << EOF
          {
            "ApiBaseUrl": "${{ secrets.API_BASE_URL }}",
            "AzureAd": {
              "Enabled": true,
              "Authority": "https://login.microsoftonline.com/${{ secrets.ENTRA_TENANT_ID }}",
              "ClientId": "${{ secrets.ENTRA_CLIENT_ID }}",
              "ApiScope": "api://${{ secrets.ENTRA_CLIENT_ID }}/access_as_user"
            }
          }
          EOF
      - run: dotnet publish src/Candour.Web -c Release -o ./publish-web
      - name: Add SPA routing config
        run: |
          cat > ./publish-web/wwwroot/staticwebapp.config.json << 'EOF'
          {
            "navigationFallback": {
              "rewrite": "/index.html",
              "exclude": ["/_framework/*", "/_content/*", "/lib/*", "*.css", "*.js", "*.png", "*.ico", "*.json"]
            }
          }
          EOF
      - uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_TOKEN }}
          app_location: ./publish-web/wwwroot
          skip_app_build: true
```

**Required GitHub Secrets:**

| Secret | Value |
|--------|-------|
| `AZURE_FUNCTIONS_PUBLISH_PROFILE` | Download from Azure Portal → Function App → Get publish profile |
| `AZURE_STATIC_WEB_APPS_TOKEN` | From `az staticwebapp secrets list` (Step 3.4) |
| `API_BASE_URL` | `https://func-candour-{hash}.azurewebsites.net` |
| `ENTRA_TENANT_ID` | Your Azure AD tenant ID |
| `ENTRA_CLIENT_ID` | Your app registration client ID |

---

## Configuration Reference

### Functions App Settings

| Key | Required | Default | Description |
|-----|----------|---------|-------------|
| `Candour__Auth__UseEntraId` | Yes | `true` | Enable Entra ID JWT validation |
| `Candour__Auth__TenantId` | When Entra enabled | — | Azure AD tenant ID |
| `Candour__Auth__ClientId` | When Entra enabled | — | App registration client ID |
| `Candour__Auth__Audience` | When Entra enabled | `{ClientId}` | Token audience (`api://{client-id}` or just `{client-id}`) |
| `Candour__Auth__AdminEmails` | When Entra enabled | — | Semicolon-delimited admin email addresses |
| `Candour__ApiKey` | No | `""` | API key for dev mode (empty = open admin routes) |
| `CosmosDb__ConnectionString` | Yes | — | Cosmos DB connection string |
| `CosmosDb__DatabaseName` | Yes | `candour` | Cosmos DB database name |
| `KeyVault__Uri` | Yes | — | Key Vault URI (e.g., `https://kv-candour-{hash}.vault.azure.net/`) |
| `KeyVault__KeyName` | Yes | `candour-batch-secret` | Key Vault key name for batch encryption |
| `Candour__FrontendBaseUrl` | No | `""` | Frontend URL for FQDN token links (e.g., `https://your-swa.azurestaticapps.net`) |

### WASM `appsettings.json`

| Key | Required | Default | Description |
|-----|----------|---------|-------------|
| `AzureAd:Enabled` | Yes | `false` | Enable MSAL authentication |
| `AzureAd:Authority` | When enabled | — | `https://login.microsoftonline.com/{tenant-id}` |
| `AzureAd:ClientId` | When enabled | — | App registration client ID |
| `AzureAd:ApiScope` | When enabled | — | `api://{client-id}/access_as_user` |
| `ApiBaseUrl` | Yes | `https://localhost:5001` | Functions API base URL |

### Auth Modes

| `UseEntraId` | `ApiKey` | Behavior |
|--------------|----------|----------|
| `true` | (ignored) | Admin routes require `Authorization: Bearer <jwt>` from Entra ID |
| `false` | `""` (empty) | Admin routes are open — **dev mode only** |
| `false` | `"secret"` | Admin routes require `X-Api-Key: secret` header |

### Admin-Protected Routes

The `AuthenticationMiddleware` protects these routes (regex pattern match):

| Route | Method | Purpose |
|-------|--------|---------|
| `GET /api/surveys` | GET | List all surveys |
| `POST /api/surveys` | POST | Create a survey |
| `POST /api/surveys/{id}/publish` | POST | Publish + generate tokens |
| `POST /api/surveys/{id}/analyze` | POST | Run AI analysis |
| `GET /api/surveys/{id}/results` | GET | View aggregate results |

### Public Routes (No Auth)

| Route | Method | Purpose |
|-------|--------|---------|
| `GET /api/surveys/{id}` | GET | View survey questions |
| `POST /api/surveys/{id}/responses` | POST | Submit response (blind token) |
| `POST /api/surveys/{id}/validate-token` | POST | Check token validity |

---

## Infrastructure Files

| File | Purpose |
|------|---------|
| `infra/main.bicep` | Subscription-scoped entry point, creates resource group |
| `infra/resources.bicep` | All Azure resources (Cosmos DB, Functions, Key Vault, etc.) |
| `infra/main.bicepparam` | Parameter file, reads from environment variables |
| `infra/deploy.sh` | End-to-end deployment script (infra + code) |
| `infra/.env.example` | Template for environment variables |
| `infra/.env` | Your actual values (gitignored) |
