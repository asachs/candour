# Quick Start

Get Candour running locally in under five minutes. This guide covers cloning the repository, starting the API and frontend, and verifying everything works.

!!! note "Prerequisites"
    This guide assumes you have the required software installed. If you have not set up your environment yet, see [Prerequisites](prerequisites.md) first.

---

## 1. Clone the Repository

```bash
git clone https://github.com/asachs/candour.git
cd candour
```

## 2. Restore Dependencies

```bash
dotnet restore
```

This restores NuGet packages for all projects in the solution, including the Functions API, Blazor WASM frontend, and test projects.

## 3. Start the Cosmos DB Emulator

Candour uses Azure Cosmos DB for storage. For local development, start the Cosmos DB Emulator before launching the API.

=== "Windows (native)"

    Launch the Azure Cosmos DB Emulator from the Start menu or system tray. The emulator listens on `https://localhost:8081` by default.

=== "Docker"

    ```bash
    docker run -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 \
      -m 3g --cpus=2.0 \
      -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
      -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
      mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
    ```

!!! tip "Emulator Connection String"
    The default local settings already include the emulator connection string. No configuration changes are needed for local development.

## 4. Start the Functions API

Open a terminal and start the Azure Functions backend:

```bash
cd src/Candour.Functions
func start
```

The API starts on `http://localhost:7071`. You should see output indicating the function endpoints are registered.

!!! info "Local Auth Mode"
    Local development defaults to API key authentication with an empty key (`Candour__Auth__UseEntraId=false`). This means admin routes are open for development without any authentication setup.

## 5. Start the Blazor Frontend

Open a second terminal and start the Blazor WebAssembly frontend:

```bash
cd src/Candour.Web
dotnet run
```

The frontend starts on `http://localhost:5000`.

## 6. Verify It Works

Open your browser and confirm the following:

| URL | Expected Behavior |
|-----|-------------------|
| `http://localhost:5000` | Candour home page loads with the branded UI |
| `http://localhost:5000/admin` | Admin dashboard loads (empty survey list) |
| `http://localhost:7071/api/surveys` | API returns an empty JSON array `[]` |

You should be able to:

1. Navigate to the admin dashboard
2. Create a new survey with questions
3. Publish the survey and generate tokens
4. Open a token link in an incognito window
5. Submit a response as an anonymous respondent
6. View aggregate results on the admin detail page

---

## Run Tests

Run the full test suite from the repository root:

```bash
dotnet test
```

This executes unit tests, integration tests, and API tests across all test projects.

!!! example "Expected Output"
    You should see 200+ tests passing with over 80% code coverage. The integration tests require the Cosmos DB Emulator to be running.

---

!!! tip "Next Steps"
    - **[Prerequisites](prerequisites.md)** -- Detailed software requirements and verification commands
    - **[API Reference](../api/overview.md)** -- Full endpoint documentation
    - **[Security & Privacy](../security/anonymity.md)** -- How the anonymity architecture works
    - **[Deployment Guide](../deployment/guide.md)** -- Deploy Candour to Azure
    - **[Configuration](../configuration/functions.md)** -- Configure the Functions API and Blazor frontend
