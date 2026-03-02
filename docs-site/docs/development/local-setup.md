# Local Development Setup

This guide walks through setting up a local Candour development environment from scratch, including the Azure Functions API backend, Blazor WebAssembly frontend, and Cosmos DB Emulator.

## Prerequisites

Ensure you have the [required software](../getting-started/prerequisites.md) installed before proceeding.

## Clone and Restore

```bash
git clone https://github.com/asachs/candour.git
cd candour
dotnet restore
```

The `dotnet restore` command pulls all NuGet packages for every project in the solution.

## Cosmos DB Emulator Setup

Candour stores all data in Azure Cosmos DB. For local development, you need either the Cosmos DB Emulator or a Docker-based alternative.

=== "Cosmos DB Emulator (Native)"

    1. Install the [Azure Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator) for your platform.
    2. Start the emulator. It runs on `https://localhost:8081` by default.
    3. The default connection string is pre-configured in `local.settings.json`:

    ```
    AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
    ```

    !!! note
        This is the well-known emulator key used by all Cosmos DB Emulators. It is not a secret and is safe to commit.

=== "Docker"

    If you prefer Docker or are on Linux (where the native emulator is not available):

    ```bash
    docker pull mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

    docker run -d --name cosmos-emulator \
      -p 8081:8081 \
      -p 10250-10255:10250-10255 \
      mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    ```

    Wait approximately 30 seconds for the emulator to start, then verify:

    ```bash
    curl -k https://localhost:8081/_explorer/emulator.pem
    ```

    !!! tip
        The Linux emulator uses a self-signed certificate. You may need to import the emulator's certificate or set `CosmosClientOptions.HttpClientFactory` to bypass SSL validation in development.

The database and containers are created automatically on first run by the `CosmosDbInitializer` in the `Candour.Infrastructure.Cosmos` project.

## Configuration

The API uses `local.settings.json` for local configuration. The default `local.settings.json` at `src/Candour.Functions/local.settings.json` is pre-configured for local development with auth disabled and the Cosmos DB Emulator connection string. See the [Functions API Configuration](../configuration/functions.md#local-development-localsettingsjson) for the full settings reference.

Key configuration notes:

- **`Candour__Auth__UseEntraId`** is set to `false` for local development. This enables API key authentication instead of Entra ID JWT validation.
- **`Candour__ApiKey`** is empty, which means admin routes are open (no auth required) in development mode.
- **CORS** is configured to allow the Blazor WASM frontend running on `localhost:5000` and `localhost:5001`.

!!! warning "Do Not Use Empty API Keys in Production"
    The empty API key configuration is strictly for local development convenience. See the [Deployment Guide](../deployment/guide.md) for production authentication configuration.

## Starting the Application

You need two terminal windows -- one for the API backend and one for the frontend.

### Terminal 1: Start the Functions API

```bash
cd src/Candour.Functions
func start
```

The API starts on `http://localhost:7071`. You should see output listing all registered HTTP endpoints:

```
Functions:

        CreateSurvey: [POST] http://localhost:7071/api/surveys
        ExportCsv: [GET] http://localhost:7071/api/surveys/{surveyId}/export
        GetResults: [GET] http://localhost:7071/api/surveys/{surveyId}/results
        GetSurvey: [GET] http://localhost:7071/api/surveys/{surveyId}
        ListSurveys: [GET] http://localhost:7071/api/surveys
        PublishSurvey: [POST] http://localhost:7071/api/surveys/{surveyId}/publish
        SubmitResponse: [POST] http://localhost:7071/api/surveys/{surveyId}/responses
        ValidateToken: [POST] http://localhost:7071/api/surveys/{surveyId}/validate-token
```

### Terminal 2: Start the Blazor WASM Frontend

```bash
cd src/Candour.Web
dotnet run
```

The frontend starts on `http://localhost:5000` (HTTP) and `https://localhost:5001` (HTTPS).

## Verify It Works

1. Open `http://localhost:5000` in your browser. You should see the Candour home page with the "Truth needs no name" tagline.
2. Navigate to `http://localhost:5000/admin`. In local development mode (Entra ID disabled), you should see the admin dashboard directly.
3. Verify the API responds: `http://localhost:7071/api/surveys` should return an empty JSON array `[]` (or a list of surveys if any exist).

## Running Tests

```bash
# From the repository root
dotnet test
```

This discovers and runs all test projects in the solution. See the [Testing](testing.md) guide for details on the test suite.

## Common Issues

### "Cosmos DB Emulator is not running"

**Symptom:** The API starts but throws connection errors when you try to create or list surveys.

**Solution:** Ensure the Cosmos DB Emulator is running and accessible at `https://localhost:8081`. If using Docker, check that the container is healthy:

```bash
docker ps --filter name=cosmos-emulator
```

### "CORS error in browser console"

**Symptom:** The frontend loads but API calls fail with CORS errors in the browser developer tools.

**Solution:** Verify that the `Host.CORS` setting in `local.settings.json` includes the exact origin your frontend is running on (including the port). Restart the Functions host after changing the setting.

### "func: command not found"

**Symptom:** Azure Functions Core Tools are not installed or not on your PATH.

**Solution:** Install Azure Functions Core Tools v4:

```bash
# macOS
brew tap azure/functions
brew install azure-functions-core-tools@4

# Windows
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Linux (Ubuntu/Debian)
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-get update
sudo apt-get install azure-functions-core-tools-4
```

### "Port 7071 is already in use"

**Symptom:** `func start` fails because another process is using port 7071.

**Solution:** Find and stop the conflicting process, or start on a different port:

```bash
func start --port 7072
```

If you change the API port, update the frontend's API base URL configuration accordingly.

### SSL Certificate Errors with Cosmos Emulator

**Symptom:** The API fails to connect to the Cosmos DB Emulator with SSL/TLS errors.

**Solution:** The emulator uses a self-signed certificate. Import the emulator's certificate into your system trust store, or configure the Cosmos client to accept the emulator certificate in development mode. The `CosmosDbInitializer` handles this automatically for the well-known emulator endpoint.
