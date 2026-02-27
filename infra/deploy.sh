#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# ── Load .env if present ──
if [ -f "$SCRIPT_DIR/.env" ]; then
    echo "==> Loading .env..."
    set -a
    source "$SCRIPT_DIR/.env"
    set +a
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

# ── Step 2: Generate API key if not set ──
if [ -z "${CANDOUR_API_KEY:-}" ]; then
    CANDOUR_API_KEY=$(openssl rand -base64 32)
    echo "==> Generated API key (save this): $CANDOUR_API_KEY"
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

echo "==> Infrastructure deployed:"
echo "    Resource Group: $RESOURCE_GROUP"
echo "    Function App:   $FUNCTION_APP_NAME"
echo "    URL:            $FUNCTION_APP_URL"

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

# ── Step 5: Verify ──
echo "==> Waiting for cold start..."
sleep 10

echo "==> Testing endpoint..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$FUNCTION_APP_URL/api/surveys")
echo "    GET /api/surveys → HTTP $HTTP_STATUS"

echo ""
echo "=== DEPLOYMENT COMPLETE ==="
echo "Function App URL: $FUNCTION_APP_URL"
echo "Entra ID App ID:  $CANDOUR_ENTRA_CLIENT_ID"
echo "API Key:          $CANDOUR_API_KEY"
echo ""
echo "Test with:"
echo "  curl $FUNCTION_APP_URL/api/surveys"
echo "  curl -H 'X-Api-Key: $CANDOUR_API_KEY' -X POST $FUNCTION_APP_URL/api/surveys -d '{...}'"
