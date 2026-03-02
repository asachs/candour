# Deployment Guide

This guide walks through deploying Candour to Azure from scratch. It covers identity configuration, infrastructure provisioning, application deployment, and post-deployment verification.

---

## Prerequisites

Before starting, ensure the following tools are installed and authenticated:

| Tool | Version | Purpose |
|------|---------|---------|
| Azure CLI (`az`) | Latest | Infrastructure provisioning and deployment |
| .NET SDK | 9.0+ | Building Functions and Blazor WASM projects |
| Azure Functions Core Tools | v4 | Local Functions testing and deployment |

You also need an Azure subscription with permissions to create resource groups, Cosmos DB accounts, Function Apps, and Static Web Apps.

!!! note "Automated alternative"
    The `infra/deploy.sh` script automates steps 2 through 5 below. See the [Infrastructure as Code](infrastructure.md) page for details on the automated deployment path.

---

## 1. Entra ID App Registration

Candour uses a single Entra ID app registration for both the API (JWT validation) and the SPA (MSAL login).

### 1.1 Register the Application

1. Navigate to **Azure Portal > Entra ID > App registrations > New registration**
2. Set the following values:

    | Field | Value |
    |-------|-------|
    | Name | `Candour` |
    | Supported account types | Accounts in this organizational directory only (single tenant) |
    | Redirect URI -- Platform | SPA |
    | Redirect URI -- Value | `http://localhost:5000/authentication/login-callback` |

3. Click **Register**

### 1.2 Record the Application IDs

From the app registration's **Overview** page, copy two values:

| Value | Where it is used |
|-------|------------------|
| **Application (client) ID** | `ClientId` in both Functions and WASM configuration |
| **Directory (tenant) ID** | `TenantId` in Functions configuration; part of the `Authority` URL in WASM configuration |

!!! tip
    Keep these IDs accessible. They are referenced in multiple configuration steps below.

### 1.3 Expose an API Scope

1. Go to **Expose an API**
2. Set **Application ID URI** to `api://{client-id}`
3. Click **Add a scope** with the following values:

    | Field | Value |
    |-------|-------|
    | Scope name | `access_as_user` |
    | Who can consent | Admins and users |
    | Admin consent display name | Access Candour as a user |

4. Save the scope

### 1.4 Grant the SPA Access to the API

1. Go to **API permissions > Add a permission**
2. Select **My APIs > Candour**
3. Check the `access_as_user` scope
4. Click **Add permissions**
5. Click **Grant admin consent** for your organization

### 1.5 Optional: Token Claims

To include user identity information in tokens:

1. Go to **Token configuration > Add optional claim**
2. Token type: **ID**
3. Select `email` and `preferred_username`

### 1.6 Add Production Redirect URI

Once you know your production frontend URL:

1. Go to **Authentication**
2. Under **Single-page application**, click **Add URI**
3. Add `https://app.candour.example/authentication/login-callback`

### 1.7 Adding External Admin Users

To grant admin access to users outside your tenant, invite them as B2B guests:

1. Go to **Entra ID > Users > Invite external user**
2. Enter their email address
3. They sign in with their own credentials but authenticate against your tenant

No code changes are required. Guest users receive tokens from your tenant with valid `tid` and `oid` claims.

---

## 2. Azure Infrastructure

!!! note "Region selection"
    The examples use `australiaeast` as a placeholder. Replace with the Azure
    region closest to your users. The [Infrastructure as Code](infrastructure.md)
    templates default to `westeurope`.

### 2.1 Resource Group

```bash
az group create \
  --name rg-candour-example \
  --location australiaeast
```

### 2.2 Cosmos DB

```bash
# Create account (serverless)
az cosmosdb create \
  --name cosmos-candour-example \
  --resource-group rg-candour-example \
  --kind GlobalDocumentDB \
  --default-consistency-level Session \
  --capabilities '[{"name": "EnableServerless"}]'

# Create database
az cosmosdb sql database create \
  --account-name cosmos-candour-example \
  --resource-group rg-candour-example \
  --name candour

# Retrieve connection string
az cosmosdb keys list \
  --name cosmos-candour-example \
  --resource-group rg-candour-example \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" \
  --output tsv
```

!!! note "Automatic container creation"
    The `surveys`, `responses`, `usedTokens`, and `rateLimits` containers are created automatically on first startup by `CosmosDbInitializer`. No manual container creation is necessary.

### 2.3 Storage Account

The Function App requires a storage account for runtime state:

```bash
az storage account create \
  --name stcandourexample \
  --resource-group rg-candour-example \
  --location australiaeast \
  --sku Standard_LRS
```

### 2.4 Azure Functions App

!!! warning "Flex Consumption requires IaC"
    Flex Consumption plans cannot be provisioned with a single CLI command.
    Use the [Infrastructure as Code](infrastructure.md) approach instead,
    or create the plan separately:

    ```bash
    az functionapp plan create \
      --name asp-candour-example \
      --resource-group rg-candour-example \
      --location australiaeast \
      --sku FC1 --is-linux

    az functionapp create \
      --name func-candour-example \
      --resource-group rg-candour-example \
      --storage-account stcandourexample \
      --plan asp-candour-example \
      --runtime dotnet-isolated \
      --runtime-version 9 \
      --functions-version 4
    ```

### 2.5 Static Web App

```bash
az staticwebapp create \
  --name swa-candour-example \
  --resource-group rg-candour-example \
  --location australiaeast
```

---

## 3. Configuration

### 3.1 Functions App Settings

```bash
az functionapp config appsettings set \
  --name func-candour-example \
  --resource-group rg-candour-example \
  --settings \
    "Candour__Auth__UseEntraId=true" \
    "Candour__Auth__TenantId={your-tenant-id}" \
    "Candour__Auth__ClientId={your-client-id}" \
    "Candour__Auth__Audience=api://{your-client-id}" \
    "Candour__ApiKey=" \
    "CosmosDb__ConnectionString={your-cosmos-connection-string}" \
    "CosmosDb__DatabaseName=candour"
```

See the [Configuration Reference](../configuration/functions.md) for a full table of all supported app settings.

### 3.2 CORS

```bash
az functionapp cors add \
  --name func-candour-example \
  --resource-group rg-candour-example \
  --allowed-origins "https://app.candour.example"

# Enable credentials for auth headers
az resource update \
  --resource-group rg-candour-example \
  --name func-candour-example/web \
  --resource-type Microsoft.Web/sites/config \
  --set properties.cors.supportCredentials=true
```

!!! warning
    CORS must include credentials support (`supportCredentials=true`) for authentication headers to pass through from the Blazor WASM frontend.

### 3.3 Blazor WASM Configuration

Update the Blazor WASM configuration as described in [Blazor WASM Configuration](../configuration/wasm.md). Key values to set: `Authority` (with your tenant ID), `ClientId`, `ApiScope`, and `ApiBaseUrl` (your Functions API URL).

!!! note "Placeholder values"
    Replace `{your-tenant-id}` and `{your-client-id}` with the values recorded in step 1.2.

!!! tip
    The `deploy.sh` script generates this file automatically from environment variables during deployment.

---

## 4. Deploy

### 4.1 Publish Functions

```bash
cd src/Candour.Functions
dotnet publish -c Release -o ./publish
cd publish
func azure functionapp publish func-candour-example
```

### 4.2 Publish Blazor WASM

```bash
cd src/Candour.Web
dotnet publish -c Release -o ./publish
```

The publishable output is in `./publish/wwwroot/`.

**Deploy to Static Web Apps:**

```bash
az staticwebapp deploy \
  --name swa-candour-example \
  --app-location ./publish/wwwroot
```

**Alternative -- Blob Storage static website:**

```bash
az storage blob upload-batch \
  --account-name stcandourweb \
  --destination '$web' \
  --source ./publish/wwwroot
```

---

## 5. Verification

### 5.1 Health Check

```bash
# Verify Functions API responds
curl -s -o /dev/null -w "%{http_code}" https://api.candour.example/api/surveys
# Expected: 401 (authentication required for the list endpoint)
```

### 5.2 Authentication Flow

1. Open the frontend URL (`https://app.candour.example`) in a browser
2. Click **Login** -- the browser should redirect to the Microsoft login page
3. After sign-in, navigate to **Admin** -- the dashboard should load
4. Create a survey -- the `CreatorId` stored in Cosmos DB should match your Entra ID object ID

### 5.3 Respondent Flow

1. Publish a survey and copy a token link
2. Open the link in an incognito browser window (no authentication required)
3. Submit a response -- the submission should succeed without login

### Verification Checklist

Use this checklist to confirm a successful deployment:

- [ ] `GET /api/surveys` returns `401 Unauthorized`
- [ ] `GET /api/surveys/{id}` returns `404 Not Found` for a non-existent survey ID
- [ ] `POST /api/surveys/{id}/validate-token` returns `200` with `{"valid":false}` for a fake token
- [ ] Frontend loads at the SWA URL without errors
- [ ] Login redirects to Entra ID and returns to the app
- [ ] Admin dashboard is accessible after login
- [ ] Survey creation, publishing, and response submission all complete end-to-end
- [ ] CORS headers are present on API responses from the frontend origin
