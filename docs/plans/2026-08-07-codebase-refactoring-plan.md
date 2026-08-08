# Refactoring Unwieldy Core Classes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Break up `ClientSession.cs`, `ApplicationBuilderExtensions.cs`, `DatabaseSeederService.cs`, and `ResourceRoutingManager.cs` into modular, focused files with single responsibilities while preserving 100% backward compatibility and test passing rate.

**Architecture:** 
1. Use C# `partial class` files for `ClientSession` (`ClientSession.JsonRpcRewriter.cs`, `ClientSession.NotificationBroadcaster.cs`).
2. Extract endpoint mappers for `ApplicationBuilderExtensions` into `Extensions/Endpoints/` (`ServerEndpointsExtensions.cs`, `AdminEndpointsExtensions.cs`, `ProxyEndpointsExtensions.cs`).
3. Extract database seeding logic from `DatabaseSeederService` into `Services/DatabaseSeeders/` (`CatalogDatabaseSeeder.cs`, `ClientAppKeySeeder.cs`).
4. Extract resource search & template logic from `ResourceRoutingManager` into `Core/Routing/ResourceCatalogManager.cs`.

**Tech Stack:** C# ASP.NET Core (.NET 10), EF Core, Dapper, System.Text.Json, xUnit.

## Global Constraints

- Preserve all existing public method signatures and API endpoints.
- Ensure all 277 unit tests continue to pass after each step.
- Update version in `mcp-router.csproj`, `README.md`, and `frontend/src/stores/useUserStore.ts` upon completion.

---

### Task 1: Refactor `ClientSession.cs` into Partial Class Modules

**Files:**
- Create: `Core/ClientSession/ClientSession.JsonRpcRewriter.cs`
- Create: `Core/ClientSession/ClientSession.NotificationBroadcaster.cs`
- Modify: `Core/ClientSession.cs`
- Test: `McpRouter.Tests/ClientSessionTests.cs`

**Interfaces:**
- Consumes: JSON-RPC request/response string payloads
- Produces: Sanitized/namespaced `JsonNode` payloads and SSE notifications

- [ ] **Step 1: Create `Core/ClientSession/ClientSession.JsonRpcRewriter.cs`**

Extract `RewriteRequestJson`, `RewriteResponseJson`, `RewriteNotificationJson`, and `RewriteResultJson` into partial class `ClientSession`.

- [ ] **Step 2: Create `Core/ClientSession/ClientSession.NotificationBroadcaster.cs`**

Extract `SendNotificationToClientAsync`, `ForwardNotificationAsync`, and `NotifySessionStateChangedAsync` into partial class `ClientSession`.

- [ ] **Step 3: Remove extracted methods from main `Core/ClientSession.cs`**

Clean up `Core/ClientSession.cs` to contain only constructor, initialization locks, and lifecycle orchestration.

- [ ] **Step 4: Run test suite to verify refactor**

Run: `dotnet test McpRouter.slnx --filter "ClientSessionTests"`
Expected: PASS (All tests pass)

- [ ] **Step 5: Commit changes**

```bash
./commit.sh "refactor(session): extract ClientSession rewriter and notification broadcaster partials"
```

---

### Task 2: Refactor `ApplicationBuilderExtensions.cs` into Endpoint Mappers

**Files:**
- Create: `Extensions/Endpoints/ServerEndpointsExtensions.cs`
- Create: `Extensions/Endpoints/AdminEndpointsExtensions.cs`
- Create: `Extensions/Endpoints/ProxyEndpointsExtensions.cs`
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Test: `McpRouter.Tests/ClientSessionTests.cs`

**Interfaces:**
- Consumes: `WebApplication` HTTP routing pipeline
- Produces: Mapped API endpoints (`/api/servers`, `/api/admin`, `/sse`)

- [ ] **Step 1: Create `Extensions/Endpoints/ServerEndpointsExtensions.cs`**

Extract server management endpoints (`/api/servers`, `/api/servers/{id}`).

- [ ] **Step 2: Create `Extensions/Endpoints/AdminEndpointsExtensions.cs`**

Extract administrative endpoints (`/api/admin/...`, `/api/settings`, `/api/logs`).

- [ ] **Step 3: Create `Extensions/Endpoints/ProxyEndpointsExtensions.cs`**

Extract gateway proxy endpoints (`/sse`, `/{targetServerId}/sse`).

- [ ] **Step 4: Refactor `Extensions/ApplicationBuilderExtensions.cs`**

Update `ConfigureMcpRouterPipeline` to delegate endpoint mapping to `MapServerEndpoints()`, `MapAdminEndpoints()`, and `MapProxyEndpoints()`.

- [ ] **Step 5: Run full test suite**

Run: `dotnet test McpRouter.slnx`
Expected: PASS

- [ ] **Step 6: Commit changes**

```bash
./commit.sh "refactor(endpoints): extract modular endpoint extension classes from ApplicationBuilderExtensions"
```

---

### Task 3: Refactor `DatabaseSeederService.cs` into Seeder Modules

**Files:**
- Create: `Services/DatabaseSeeders/CatalogDatabaseSeeder.cs`
- Create: `Services/DatabaseSeeders/ClientAppKeySeeder.cs`
- Modify: `Services/DatabaseSeederService.cs`
- Test: `McpRouter.Tests/DatabaseSeederServiceTests.cs`

**Interfaces:**
- Consumes: `RouterDbContext`, `IConfiguration`
- Produces: Populated initial settings, default clients, app keys, and servers catalog

- [ ] **Step 1: Create `Services/DatabaseSeeders/CatalogDatabaseSeeder.cs`**

Extract catalog server seeding (`SeedCatalogServers`).

- [ ] **Step 2: Create `Services/DatabaseSeeders/ClientAppKeySeeder.cs`**

Extract default client app key seeding (`SeedDefaultClientsAndKeys`).

- [ ] **Step 3: Refactor `Services/DatabaseSeederService.cs`**

Update `SeedDatabase` extension entry point to delegate to `CatalogDatabaseSeeder` and `ClientAppKeySeeder`.

- [ ] **Step 4: Run test suite**

Run: `dotnet test McpRouter.slnx --filter "DatabaseSeederServiceTests"`
Expected: PASS

- [ ] **Step 5: Commit changes**

```bash
./commit.sh "refactor(seeder): extract CatalogDatabaseSeeder and ClientAppKeySeeder helpers"
```

---

### Task 4: Refactor `ResourceRoutingManager.cs`

**Files:**
- Create: `Core/Routing/ResourceCatalogManager.cs`
- Modify: `Core/Routing/ResourceRoutingManager.cs`
- Test: `McpRouter.Tests/ResourceRoutingManagerTests.cs`

**Interfaces:**
- Consumes: JSON-RPC resource arrays and queries
- Produces: Filtered resource lists and resource templates

- [ ] **Step 1: Create `Core/Routing/ResourceCatalogManager.cs`**

Extract `SearchResourcesAsync` and resource template resolution into `ResourceCatalogManager`.

- [ ] **Step 2: Refactor `Core/Routing/ResourceRoutingManager.cs`**

Delegate resource searching to `ResourceCatalogManager` while keeping public signatures intact.

- [ ] **Step 3: Run full test suite with coverage**

Run: `dotnet test McpRouter.slnx --collect:"XPlat Code Coverage"`
Expected: PASS

- [ ] **Step 4: Commit changes & bump version**

```bash
./commit.sh "refactor(resources): extract ResourceCatalogManager and modularize ResourceRoutingManager"
```
