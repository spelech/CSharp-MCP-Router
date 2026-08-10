# Code Coverage Report

This document details the code coverage metrics across the core modules of the MCP Router.

## Running Tests Locally

To generate the code coverage report locally, run the following command in the repository root:

```bash
dotnet test McpRouter.slnx --collect:"XPlat Code Coverage"
```

## Coverage Breakdown

To view the actual, real-time code coverage breakdown across core modules, run the locally collected coverage files using your preferred report generator (e.g. ReportGenerator tool).

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

This will analyze all tests run and generate a comprehensive HTML report showing exact line-by-line coverage for all of our active components including:
- **Core Session**: `ClientSession`, `SessionManager`, `BackendConnection`
- **Routing Engine**: `ToolRoutingManager`, `ResourceRoutingManager`
- **Controllers / Extensions**: `ProvidersController`, `ProxyEndpointsExtensions`
- **Security & Providers**: `VaultSecretRetriever`, `OidcIdentityProvider`, `WindowsRegistrySecretRetriever`
