# Documentation, Code Coverage, and Class Architecture Design Spec

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to generate the implementation plan from this approved design spec.

**Goal:** Enhance repository documentation by adding a dedicated Code Coverage report to `README.md` and `docs/coverage-report.md`, enabling C# XML documentation comments, and adding comprehensive Mermaid class diagrams to `ARCHITECTURE.md`.

**Architecture:**
- **Code Coverage**: Detailed markdown breakdown at `docs/coverage-report.md` linked from `README.md`.
- **C# XML Docs**: Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in `mcp-router.csproj` with `<NoWarn>$(NoWarn);1591</NoWarn>` to suppress untargeted missing comment warnings. Add XML comments across key domain interfaces and services.
- **Visual Design**: Mermaid `classDiagram` visualizations in `ARCHITECTURE.md` covering Session & Modular Partial Classes, Strategy Interfaces, and Routing Managers.

---

## 1. Code Coverage & Quality Documentation

### `README.md` Section
Add a **🧪 Code Coverage & Quality Gate** section containing:
- High-level test summary: **277 / 277** unit and integration tests passing across 24 test suites.
- High-level module coverage summary table.
- Clickable relative link to [`docs/coverage-report.md`](file:///containers/dev/csharp-mcp-router/docs/coverage-report.md).

### `docs/coverage-report.md` File
Create a structured document containing:
- Executive Summary of overall repository test health.
- Detailed coverage table per module:
  - **Core Session & Management**: `ClientSession`, `BackendConnection`, `SessionManager`
  - **Routing Engine**: `ToolRoutingManager`, `ResourceRoutingManager`, `ResourceCatalogManager`, `ToolApprovalManager`, `ToolErrorFormatter`
  - **Security & Providers**: `CompositeIdentityProvider`, `CompositeSecretRetriever`, `VaultSecretRetriever`, `LdapActiveDirectoryService`, `AuditLogger`, `PiiSanitizer`
  - **Controllers & API**: `McpController`, `ProvidersController`, `AppKeysController`, `PermissionsController`
- Instructions for local coverage report generation:
  ```bash
  dotnet test McpRouter.slnx --collect:"XPlat Code Coverage"
  ```

---

## 2. C# XML Documentation & Intellisense

### Project Configuration (`mcp-router.csproj`)
Update `mcp-router.csproj` `<PropertyGroup>`:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

### Source Code XML Annotations
Add triple-slash (`/// <summary>`, `/// <param>`, `/// <returns>`) docstrings to:
- `Core/ClientSession.cs` & partial classes (`Authorization`, `BackendInitializer`, `ProxyForwarder`, `NotificationBroadcaster`, `JsonRpcRewriter`)
- `Core/Routing/ToolRoutingManager.cs`, `ResourceRoutingManager.cs`, `ResourceCatalogManager.cs`
- `Services/SemanticSearchService.cs`, `BackendHealthCheckService.cs`, `DatabaseSeederService.cs`
- `Core/Identity/IIdentityProvider.cs` & implementations
- `Core/Secrets/ISecretRetriever.cs` & implementations

---

## 3. Class Diagrams & Architecture Maps (`ARCHITECTURE.md`)

Update [`ARCHITECTURE.md`](file:///containers/dev/csharp-mcp-router/ARCHITECTURE.md) with 3 Mermaid `classDiagram` blocks:

1. **Client Session & Modular Partial Class Architecture**:
   - Visualizing `ClientSession` partial class breakdown (`Authorization`, `BackendInitializer`, `ProxyForwarder`, `NotificationBroadcaster`, `JsonRpcRewriter`), `SessionManager`, and `BackendConnection`.

2. **Pluggable Strategy Interfaces & Security Providers**:
   - Visualizing `IIdentityProvider`, `ISecretRetriever`, `ITransport`, and `IAuditLogger` implementations.

3. **Routing Engine Architecture & Helper Separation**:
   - Visualizing `ToolRoutingManager`, `ResourceRoutingManager`, `ResourceCatalogManager`, `ToolApprovalManager`, and `ToolErrorFormatter`.
