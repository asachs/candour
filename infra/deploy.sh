#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# ── Load environment config ──
# Usage: ./deploy.sh [env]   (default: prod)
# Loads .env.{env} for non-secret configuration
TARGET_ENV="${1:-prod}"
ENV_FILE="$SCRIPT_DIR/.env.$TARGET_ENV"
if [ -f "$ENV_FILE" ]; then
    echo "==> Loading $ENV_FILE..."
    set -a
    source "$ENV_FILE"
    set +a
else
    echo "ERROR: $ENV_FILE not found. Create it from .env.example." >&2
    exit 1
fi

# ── Validate required env vars ──
: "${CANDOUR_SUBSCRIPTION:?Set CANDOUR_SUBSCRIPTION in .env or environment}"
: "${CANDOUR_OWNER_ALIAS:?Set CANDOUR_OWNER_ALIAS in .env or environment}"
: "${CANDOUR_OWNER_EMAIL:?Set CANDOUR_OWNER_EMAIL in .env or environment}"
: "${CANDOUR_ADMIN_EMAILS:?Set CANDOUR_ADMIN_EMAILS in .env or environment (semicolon-delimited)}"

LOCATION="${CANDOUR_LOCATION:-westeurope}"
ENV_NAME="${CANDOUR_ENV:-prod}"

echo "==> Setting subscription..."
az account set --subscription "$CANDOUR_SUBSCRIPTION"

# ── Step 1: Entra ID App Registration ──
if [ -n "${CANDOUR_ENTRA_CLIENT_ID:-}" ]; then
    echo "==> Using pre-configured Entra ID app: $CANDOUR_ENTRA_CLIENT_ID"
else
    echo "==> Checking Entra ID app registration..."
    APP_NAME="candour-api-${ENV_NAME}"
    EXISTING_APP=$(az ad app list --display-name "$APP_NAME" --query '[0].appId' -o tsv 2>/dev/null || true)

    if [ -z "$EXISTING_APP" ]; then
        echo "==> Creating Entra ID app registration: $APP_NAME"
        CANDOUR_ENTRA_CLIENT_ID=$(az ad app create \
            --display-name "$APP_NAME" \
            --sign-in-audience AzureADMyOrg \
            --query appId -o tsv)
        echo "==> Created app registration: $CANDOUR_ENTRA_CLIENT_ID"
    else
        CANDOUR_ENTRA_CLIENT_ID="$EXISTING_APP"
        echo "==> Using existing app registration: $CANDOUR_ENTRA_CLIENT_ID"
    fi
fi

# ── Step 2: Resolve API key ──
# Priority: Key Vault → environment variable → generate new
KV_NAME_PREFIX="kv-candour-"
RESOURCE_GROUP_NAME="ansachs-rg-candour-${ENV_NAME}"
EXISTING_KV=$(az keyvault list --resource-group "$RESOURCE_GROUP_NAME" --query '[0].name' -o tsv 2>/dev/null || true)

if [ -n "$EXISTING_KV" ]; then
    KV_KEY=$(az keyvault secret show --vault-name "$EXISTING_KV" --name "candour-api-key" --query value -o tsv 2>/dev/null || true)
    if [ -n "$KV_KEY" ]; then
        CANDOUR_API_KEY="$KV_KEY"
        echo "==> API key loaded from Key Vault ($EXISTING_KV)"
    fi
fi

if [ -z "${CANDOUR_API_KEY:-}" ]; then
    CANDOUR_API_KEY=$(openssl rand -base64 32)
    echo "==> Generated new API key (will be stored in Key Vault after deployment)"
    STORE_KEY_IN_KV=true
fi

export CANDOUR_API_KEY
export CANDOUR_ENTRA_CLIENT_ID

# ── Step 3: Deploy Bicep ──
echo "==> Deploying infrastructure..."
DEPLOYMENT_NAME="candour-$(date +%Y%m%d%H%M%S)"
az deployment sub create \
    --location "$LOCATION" \
    --template-file "$SCRIPT_DIR/main.bicep" \
    --parameters "$SCRIPT_DIR/main.bicepparam" \
    --name "$DEPLOYMENT_NAME" \
    -o none

RESOURCE_GROUP=$(az deployment sub show --name "$DEPLOYMENT_NAME" --query properties.outputs.resourceGroupName.value -o tsv)
FUNCTION_APP_NAME=$(az deployment sub show --name "$DEPLOYMENT_NAME" --query properties.outputs.functionAppName.value -o tsv)
FUNCTION_APP_URL=$(az deployment sub show --name "$DEPLOYMENT_NAME" --query properties.outputs.functionAppUrl.value -o tsv)
SWA_NAME=$(az deployment sub show --name "$DEPLOYMENT_NAME" --query properties.outputs.staticWebAppName.value -o tsv)
SWA_URL=$(az deployment sub show --name "$DEPLOYMENT_NAME" --query properties.outputs.staticWebAppUrl.value -o tsv)

echo "==> Infrastructure deployed:"
echo "    Resource Group: $RESOURCE_GROUP"
echo "    Function App:   $FUNCTION_APP_NAME"
echo "    API URL:        $FUNCTION_APP_URL"
echo "    SWA Name:       $SWA_NAME"
echo "    SWA URL:        $SWA_URL"

# Store API key in Key Vault if newly generated
if [ "${STORE_KEY_IN_KV:-}" = "true" ]; then
    KV_NAME=$(az keyvault list --resource-group "$RESOURCE_GROUP" --query '[0].name' -o tsv)
    if [ -n "$KV_NAME" ]; then
        az keyvault secret set --vault-name "$KV_NAME" --name "candour-api-key" --value "$CANDOUR_API_KEY" -o none 2>/dev/null \
            && echo "==> API key stored in Key Vault ($KV_NAME)" \
            || echo "WARN: Could not store API key in Key Vault. Assign Key Vault Secrets Officer role and re-run."
    fi
fi

# ── Step 4: Deploy Function Code ──
echo "==> Building Candour.Functions..."
PUBLISH_DIR="$REPO_ROOT/.publish"
rm -rf "$PUBLISH_DIR"
dotnet publish "$REPO_ROOT/src/Candour.Functions/Candour.Functions.csproj" \
    -c Release \
    -o "$PUBLISH_DIR" \
    --no-self-contained

echo "==> Deploying to Azure Functions..."
cd "$PUBLISH_DIR"
zip -r "$REPO_ROOT/.publish.zip" . > /dev/null
cd "$REPO_ROOT"

az functionapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP" \
    --name "$FUNCTION_APP_NAME" \
    --src "$REPO_ROOT/.publish.zip"

rm -rf "$PUBLISH_DIR" "$REPO_ROOT/.publish.zip"

# ── Step 5: Deploy Web App (Blazor WASM → Static Web App) ──
echo "==> Building Candour.Web..."
WEB_PUBLISH_DIR="$REPO_ROOT/.publish-web"
rm -rf "$WEB_PUBLISH_DIR"
dotnet publish "$REPO_ROOT/src/Candour.Web/Candour.Web.csproj" \
    -c Release \
    -o "$WEB_PUBLISH_DIR"

# Generate environment-specific appsettings.json
TENANT_ID="${CANDOUR_ENTRA_TENANT_ID:-$(az account show --query tenantId -o tsv)}"
cat > "$WEB_PUBLISH_DIR/wwwroot/appsettings.json" <<APPSETTINGS
{
  "ApiBaseUrl": "$FUNCTION_APP_URL",
  "AzureAd": {
    "Enabled": true,
    "Authority": "https://login.microsoftonline.com/$TENANT_ID",
    "ClientId": "$CANDOUR_ENTRA_CLIENT_ID",
    "ApiScope": "api://$CANDOUR_ENTRA_CLIENT_ID/access_as_user"
  }
}
APPSETTINGS
# Remove stale pre-compressed versions so SWA serves the updated file
rm -f "$WEB_PUBLISH_DIR/wwwroot/appsettings.json.br" "$WEB_PUBLISH_DIR/wwwroot/appsettings.json.gz"
echo "==> Generated appsettings.json (ApiBaseUrl=$FUNCTION_APP_URL)"

# Get SWA deployment token and deploy
echo "==> Deploying to Static Web App..."
SWA_TOKEN=$(az staticwebapp secrets list --name "$SWA_NAME" --resource-group "$RESOURCE_GROUP" --query properties.apiKey -o tsv)
bunx @azure/static-web-apps-cli deploy \
    "$WEB_PUBLISH_DIR/wwwroot" \
    --deployment-token "$SWA_TOKEN" \
    --env production

rm -rf "$WEB_PUBLISH_DIR"

# ── Step 6: Verify ──
echo "==> Waiting for cold start..."
sleep 10

echo "==> Testing API endpoint..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$FUNCTION_APP_URL/api/surveys")
echo "    GET /api/surveys → HTTP $HTTP_STATUS"

echo "==> Testing SWA..."
SWA_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SWA_URL")
echo "    GET $SWA_URL → HTTP $SWA_STATUS"

echo ""
echo "=== DEPLOYMENT COMPLETE ==="
echo "Function App URL: $FUNCTION_APP_URL"
echo "Static Web App:   $SWA_URL"
echo "Entra ID App ID:  $CANDOUR_ENTRA_CLIENT_ID"
echo ""
echo "Test with:"
echo "  curl $FUNCTION_APP_URL/api/surveys"
