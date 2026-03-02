# Prerequisites

This page lists all software required to run Candour locally, along with verification commands and links to installation guides.

---

## Required Software

| Software | Minimum Version | Purpose | Install Link |
|----------|----------------|---------|-------------|
| **.NET 9 SDK** | 9.0 | Build and run the Functions API and Blazor WASM frontend | [Download .NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) |
| **Azure Functions Core Tools** | v4 | Run Azure Functions locally | [Install Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) |
| **Azure Cosmos DB Emulator** | Latest | Local document database for development | [Install Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator) |
| **Git** | 2.x+ | Cloning the repository | [Download Git](https://git-scm.com/) |

!!! note "Cosmos DB Emulator Alternatives"
    The native Windows emulator is the most straightforward option. On macOS or Linux, use the [Linux Docker image](https://learn.microsoft.com/en-us/azure/cosmos-db/docker-emulator-linux) instead. You can also connect to a live Cosmos DB account by updating the connection string in `local.settings.json`.

---

## Verify Your Installation

Run these commands to confirm each tool is installed and accessible:

```bash
# .NET SDK
dotnet --version
# Expected: 9.0.x

# Azure Functions Core Tools
func --version
# Expected: 4.x.x
```

For the Cosmos DB Emulator, verify it is running by opening `https://localhost:8081/_explorer/index.html` in your browser. You should see the emulator's data explorer interface.

---

## Optional Tooling

These tools are not required but can improve the development experience.

| Software | Purpose | Install Link |
|----------|---------|-------------|
| **Azure CLI** | Manage Azure resources, deploy infrastructure | [Install Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) |
| **Visual Studio Code** | Lightweight editor with C# and Azure extensions | [Download VS Code](https://code.visualstudio.com/) |
| **JetBrains Rider** | Full-featured .NET IDE | [Download Rider](https://www.jetbrains.com/rider/) |
| **Node.js 20+** | Required only for running end-to-end (E2E) tests | [Download Node.js](https://nodejs.org/) |
| **Docker** | Run the Cosmos DB Emulator on macOS/Linux | [Install Docker](https://docs.docker.com/get-docker/) |

!!! tip "Recommended VS Code Extensions"
    If using VS Code, install the following extensions for the best experience:

    - **C# Dev Kit** -- IntelliSense, debugging, and project management for .NET
    - **Azure Functions** -- Local Functions development and deployment
    - **REST Client** -- Test API endpoints directly from `.http` files

---

## Local Configuration

Candour ships with default local configuration files that work with the Cosmos DB Emulator out of the box. No manual configuration is needed for a basic local setup.

The key configuration files are:

- **`src/Candour.Functions/local.settings.json`** -- Functions API settings including the Cosmos DB connection string, auth mode, and CORS origins
- **`src/Candour.Web/wwwroot/appsettings.json`** -- Blazor WASM settings including the API base URL and Entra ID configuration

!!! info "Auth Mode"
    Local development uses API key authentication by default (`Candour__Auth__UseEntraId=false` with an empty API key). This allows full admin access without configuring Entra ID. See [Auth Modes](../configuration/auth-modes.md) for production authentication setup.

---

## Next Steps

Once your environment is set up, proceed to the [Quick Start](quickstart.md) guide to launch Candour locally.
