# CI/CD Pipeline

Candour uses GitHub Actions for continuous integration and deployment. Two workflow files manage the application and documentation pipelines.

---

## Pipeline Overview

The deployment pipeline runs on every push to `master`. Pull requests run the test job only -- no deployment occurs.

```
push to master
    |
    v
  [test]
  restore > build > dotnet test
    |
    +----------------------------+
    |                            |
    v                            v
  [deploy-functions]        [deploy-web]
  publish > zip > deploy    publish > generate config > deploy
    |                            |
    +----------------------------+
    |
    +----------------------------+
    |                            |
    v                            v
  [smoke-test]              [e2e-test]
  curl API endpoints        Playwright browser tests
  curl SWA health check     admin login + survey flows
```

The `deploy-functions` and `deploy-web` jobs run in parallel after tests pass. Both the `smoke-test` and `e2e-test` jobs wait for both deployment jobs to complete before running.

---

## Deploy Workflow

**File:** `.github/workflows/deploy.yml`

**Trigger:** Push to `master`

### test

Runs the full .NET test suite across all projects:

1. Checkout code
2. Set up .NET 9 SDK
3. `dotnet restore`
4. `dotnet build --no-restore`
5. `dotnet test --no-build`

This job gates all subsequent deployment jobs. If any test fails, deployment does not proceed.

### deploy-functions

Deploys the API backend to Azure Functions:

1. Checkout code and set up .NET 9 SDK
2. `dotnet publish src/Candour.Functions -c Release -o ./publish`
3. Create a zip package from the publish output
4. Authenticate with Azure using the `AZURE_CREDENTIALS` secret
5. Deploy via `az functionapp deployment source config-zip` to the Function App specified in `TEST_FUNCTION_APP_NAME`

### deploy-web

Deploys the Blazor WASM frontend to Azure Static Web Apps:

1. Checkout code and set up .NET 9 SDK
2. Install the `wasm-tools` workload
3. `dotnet publish src/Candour.Web -c Release -o ./publish-web`
4. Generate an environment-specific `appsettings.json` from GitHub variables (API URL, Entra ID tenant/client IDs)
5. Remove stale pre-compressed versions (`appsettings.json.br`, `appsettings.json.gz`)
6. Deploy to Azure Static Web Apps using the `SWA_DEPLOYMENT_TOKEN` secret

!!! note "appsettings.json generation"
    The WASM `appsettings.json` is generated at deploy time, not committed to the repository. This allows the same codebase to target different environments by changing GitHub variables.

### smoke-test

Runs after both deployment jobs complete. Validates that deployed endpoints respond correctly:

- **Public endpoints:** `GET /api/surveys/{id}` expects `404`; `POST /api/surveys/{id}/validate-token` confirms the endpoint is reachable
- **Admin endpoints:** `GET /api/surveys`, `POST /api/surveys`, and `GET /api/surveys/{id}/results` all expect `401 Unauthorized`
- **SWA health check:** `GET` against the Static Web App base URL expects `200`

### e2e-test

Runs Playwright browser tests against the live deployment:

1. Checkout code and set up Node.js 20
2. Install Playwright with Chromium
3. Run the test suite against the deployed SWA URL
4. Upload Playwright report as a build artifact on failure (retained for 7 days)

!!! tip "Test survey seeding"
    E2E tests require a pre-seeded test survey. The survey ID is stored in the `TEST_SURVEY_ID` repository variable. Ensure the survey exists and has unused tokens before running the pipeline.

---

## GitHub Secrets

Configure these in **GitHub > Settings > Secrets and variables > Actions > Secrets**:

| Secret | Purpose | How to obtain |
|--------|---------|---------------|
| `AZURE_CREDENTIALS` | Azure service principal credentials for CLI authentication | `az ad sp create-for-rbac --name "github-deploy" --role contributor --scopes /subscriptions/{sub-id}` |
| `SWA_DEPLOYMENT_TOKEN` | Static Web App deployment token | Azure Portal > Static Web App > Manage deployment token |
| `TEST_ADMIN_EMAIL` | Entra ID test user email for E2E tests | Create a test user in your Entra ID tenant |
| `TEST_ADMIN_PASSWORD` | Entra ID test user password for E2E tests | Set during test user creation |

!!! danger "Credential handling"
    The `AZURE_CREDENTIALS` secret contains a service principal with Contributor access to your subscription. Rotate these credentials periodically and scope them to the narrowest possible resource group.

### Obtaining the Azure Credentials Secret

```bash
az ad sp create-for-rbac \
  --name "github-deploy-candour" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-candour-example \
  --json-auth
```

Copy the entire JSON output and paste it as the `AZURE_CREDENTIALS` secret value.

### Obtaining the SWA Deployment Token

1. Navigate to **Azure Portal > swa-candour-example > Overview**
2. Click **Manage deployment token**
3. Copy the token value
4. Paste it as the `SWA_DEPLOYMENT_TOKEN` secret in GitHub

---

## GitHub Variables

Configure these in **GitHub > Settings > Secrets and variables > Actions > Variables**:

| Variable | Purpose | Example value |
|----------|---------|---------------|
| `TEST_RESOURCE_GROUP` | Resource group for the deployment target | `rg-candour-example` |
| `TEST_FUNCTION_APP_NAME` | Function App resource name | `func-candour-example` |
| `API_BASE_URL` | Function App public URL (used by smoke tests) | `https://api.candour.example` |
| `SWA_BASE_URL` | Static Web App public URL (used by smoke and E2E tests) | `https://app.candour.example` |
| `TEST_ENTRA_TENANT_ID` | Entra ID tenant ID for appsettings generation | `{tenant-id}` |
| `TEST_ENTRA_CLIENT_ID` | Entra ID client ID for appsettings generation | `{client-id}` |
| `TEST_SURVEY_ID` | UUID of a pre-seeded test survey (for E2E tests) | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |

!!! note "Variables vs. Secrets"
    URLs and resource names are stored as variables because they are not sensitive. Credentials and tokens are stored as secrets.

---

## Documentation Workflow

**File:** `.github/workflows/docs.yml`

**Trigger:** Push to `master` that modifies files under `docs-site/` or the workflow file itself. Also supports manual dispatch via `workflow_dispatch`.

This workflow builds and deploys the MkDocs Material documentation site to GitHub Pages:

1. **Build job:** Checks out the repo, installs Python 3.12 and MkDocs Material dependencies from `docs-site/requirements.txt`, and runs `mkdocs build --strict`
2. **Deploy job:** Uploads the built site to GitHub Pages using the `deploy-pages` action

The workflow uses GitHub Pages' built-in concurrency control (`cancel-in-progress: false`) to prevent conflicting deployments.

!!! note "Permissions"
    The docs workflow requires `contents: read`, `pages: write`, and `id-token: write` permissions. These are configured at the workflow level.
