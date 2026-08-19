# Admin MCP Server & Standalone Hybrid Authorization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a native in-process Admin MCP Server with 10 consolidated entity tools covering 100% of dashboard management flows, exposed at `/admin` and `/router-admin`, with hybrid standalone network authorization supporting loopback/custom CIDRs and Admin AppKeys.

**Architecture:** An in-process virtual MCP server (`AdminMcpServer`) is registered in ASP.NET Core DI to handle MCP `2026-07-28` and `2024-11-05` requests on `/admin` and `/{targetServerId}` (`/router-admin`). `SecurityValidationHelper` and `AppKeyAuthenticationHandler` are updated to enforce hybrid admin access (AD SIDs, OIDC groups, database group mappings, Admin AppKeys with `admin`/`all` scope, and standalone CIDR network matching).

**Tech Stack:** C# .NET 10, ASP.NET Core Minimal APIs, Dapper, System.Text.Json, xUnit.

## Global Constraints

- Mandatory Version Bump: Minor bump from `4.18.2` -> `4.19.0` across `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, `README.md`.
- Protocol Version: Default `2026-07-28` with backward compatibility for `2024-11-05`.
- Test Requirement Annotations: Every new C# test proof must include `[Requirement("REQ-ID", "Category", RequirementType.Positive, "Description")]`.
- Living Catalog: Must regenerate and verify zero drift with `dotnet run --project scripts/CatalogGenerator -- --verify-only`.
- Safe Serialization: Never register `JsonRpcMessageConverter` globally or invoke recursively. Use `JsonNode`/`JsonDocument` for payload inspection.

---

### Task 1: Standalone Network Authorization & Admin AppKey Privileges

**Files:**
- Modify: `Components/Authorization/SecurityValidationHelper.cs`
- Modify: `Middleware/AppKeyAuthenticationHandler.cs`
- Modify: `Extensions/OpenIddictExtensions.cs`
- Test: `McpRouter.Tests/StandaloneAdminAuthTests.cs`

**Interfaces:**
- Consumes: `UserIdentityContext`, `IConfiguration`, `IDbConnectionFactory`, `HttpContext`
- Produces: `SecurityValidationHelper.IsAdmin(UserIdentityContext?, IConfiguration?, HttpContext?, IEnumerable<string>?)`, `SecurityValidationHelper.IsStandaloneAdminNetwork(IPAddress, IConfiguration)`

- [ ] **Step 1: Write failing tests for Standalone Network and Admin AppKey Authorization**

```csharp
namespace McpRouter.Tests
{
    public class StandaloneAdminAuthTests
    {
        [Fact]
        [Requirement("AUTH-STANDALONE-LOOPBACK-ALLOW", "AUTH", RequirementType.Positive, "Standalone mode without external IDP grants admin access to loopback IP addresses.")]
        public void IsAdmin_StandaloneMode_LoopbackIp_ReturnsTrue()
        {
            // Assert loopback IP is recognized as admin in standalone mode
        }

        [Fact]
        [Requirement("AUTH-STANDALONE-CUSTOM-CIDR-ALLOW", "AUTH", RequirementType.Positive, "Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges.")]
        public void IsAdmin_StandaloneMode_CustomCidr_ReturnsTrue()
        {
            // Assert configured LAN IP (e.g. 10.0.1.50 matching 10.0.0.0/8) is recognized as admin
        }

        [Fact]
        [Requirement("AUTH-STANDALONE-EXTERNAL-DENY", "GUARD", RequirementType.FailClosedGuardrail, "Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey.")]
        public void IsAdmin_StandaloneMode_UntrustedIp_ReturnsFalse()
        {
            // Assert non-whitelisted IP (e.g. 203.0.113.10) without admin key returns false
        }

        [Fact]
        [Requirement("AUTH-APPKEY-ADMIN-SCOPE-ALLOW", "AUTH", RequirementType.Positive, "AppKeys with admin scope grant Administrator role and pass AdminPolicy.")]
        public async Task AppKey_WithAdminScope_GrantsAdminAccess()
        {
            // Assert AppKey with scopes ["admin"] or ["all"] assigns Administrator role
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~StandaloneAdminAuthTests"`
Expected: Compilation failure or FAIL.

- [ ] **Step 3: Implement Standalone Network Evaluation & AppKey Admin Role Assignment**

1. In `Components/Authorization/SecurityValidationHelper.cs`:
   - Add `IsStandaloneAdminNetwork(IPAddress? clientIp, IConfiguration? config)` using `IsInSubnet` against `Admin:StandaloneAllowedNetworks` (defaulting to `127.0.0.1` and `::1`).
   - In `IsAdmin(UserIdentityContext? identity, IConfiguration? config, HttpContext? httpContext = null, IEnumerable<string>? mappedGroups = null)`:
     - If AD/OIDC identity has admin SID or admin group, return `true`.
     - If `httpContext != null` and no external IDP configured, check `IsStandaloneAdminNetwork(httpContext.Connection.RemoteIpAddress, config)`.
     - Check if AppKey has `admin` or `all` scope in `Context.Items["AppKeyScopes"]`.
2. In `Middleware/AppKeyAuthenticationHandler.cs`:
   - When validating `appKey.ScopesJson`, if scopes contain `"admin"`, `"*"` or `"all"`, add `Claim(ClaimTypes.Role, "Administrator")` and `Claim("Scope", "admin")`.
3. In `Extensions/OpenIddictExtensions.cs`:
   - Ensure `AdminPolicy` assertion evaluates `ctx.User.IsInRole("Administrator")`, `ctx.User.HasClaim("Scope", "admin")`, standalone network check on `httpContext`, and SID/Group checks.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~StandaloneAdminAuthTests"`
Expected: PASS (4/4 passed).

- [ ] **Step 5: Commit**

```bash
git add Components/Authorization/SecurityValidationHelper.cs Middleware/AppKeyAuthenticationHandler.cs Extensions/OpenIddictExtensions.cs McpRouter.Tests/StandaloneAdminAuthTests.cs
git commit -m "feat(auth): add standalone network authorization and admin appkey role assignment"
```

---

### Task 2: Implement In-Process Virtual Admin MCP Server (`AdminMcpServer`)

**Files:**
- Create: `Core/Routing/AdminMcpServer.cs`
- Modify: `Extensions/ServiceCollectionExtensions.cs`
- Test: `McpRouter.Tests/AdminMcpServerTests.cs`

**Interfaces:**
- Consumes: `IServerRepository`, `IAppKeyRepository`, `ISecretProviderRepository`, `IAuthProviderRepository`, `ISettingRepository`, `IDbConnectionFactory`, `IAuditLogger`, `ICredentialService`, `BackendHealthCheckService`, `DynamicEmbeddingService`, `SessionManager`, `ILdapService`
- Produces: `AdminMcpServer.HandleInitializeAsync`, `AdminMcpServer.ListToolsAsync`, `AdminMcpServer.CallToolAsync`

- [ ] **Step 1: Write failing tests for AdminMcpServer**

```csharp
namespace McpRouter.Tests
{
    public class AdminMcpServerTests
    {
        [Fact]
        [Requirement("MCP-ADMIN-TOOLS-LIST-COUNT", "MCP", RequirementType.Positive, "AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas.")]
        public async Task ListToolsAsync_ReturnsTenConsolidatedTools()
        {
            // Assert tools/list returns 10 tools: manage_servers, manage_appkeys, manage_clients, manage_policies, manage_group_mappings, manage_providers, manage_settings, manage_custom_files, manage_system, test_tool_call
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-SERVERS", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_servers list and create actions.")]
        public async Task CallToolAsync_ManageServers_ListAndCreate()
        {
            // Assert create server and list server actions succeed
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-AUDIT-LOG", "MCP", RequirementType.Positive, "AdminMcpServer tool calls record audit log entries with caller and tool name.")]
        public async Task CallToolAsync_RecordsAuditLog()
        {
            // Assert execution writes to AuditLogger
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~AdminMcpServerTests"`
Expected: FAIL (`AdminMcpServer` not found).

- [ ] **Step 3: Implement `AdminMcpServer` with 10 consolidated tools and audit logging**

Implement `Core/Routing/AdminMcpServer.cs`:
1. `GetToolDefinitions()` returning JSON schemas for:
   - `manage_servers`
   - `manage_appkeys`
   - `manage_clients`
   - `manage_policies`
   - `manage_group_mappings`
   - `manage_providers`
   - `manage_settings`
   - `manage_custom_files`
   - `manage_system`
   - `test_tool_call`
2. `CallToolAsync(string toolName, JsonElement arguments, string callerUsername)`:
   - Dispatches `action` parameter to appropriate repository/service method.
   - Catches errors and formats clean MCP responses `{ isError = false/true, content = [ { type = "text", text = ... } ] }`.
   - Records audit log via `IAuditLogger.LogAdminActionAsync`.
3. Register `AdminMcpServer` as singleton in `Extensions/ServiceCollectionExtensions.cs`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~AdminMcpServerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Core/Routing/AdminMcpServer.cs Extensions/ServiceCollectionExtensions.cs McpRouter.Tests/AdminMcpServerTests.cs
git commit -m "feat(mcp): implement in-process virtual AdminMcpServer with 10 consolidated entity tools"
```

---

### Task 3: Admin MCP Endpoints & Proxy Route Integration

**Files:**
- Create: `Components/Capabilities/AdminEndpoints.cs`
- Modify: `Components/Capabilities/ProxyEndpoints.cs`
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Test: `McpRouter.Tests/AdminEndpointsTests.cs`

**Interfaces:**
- Consumes: `AdminMcpServer`, `AdminPolicy`, `SessionManager`
- Produces: `GET/POST /admin`, `GET/POST /admin/sse`, `POST /admin/message`, `/router-admin` proxy target

- [ ] **Step 1: Write failing tests for Admin Endpoints**

```csharp
namespace McpRouter.Tests
{
    public class AdminEndpointsTests
    {
        [Fact]
        [Requirement("MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE", "MCP", RequirementType.Positive, "Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version.")]
        public async Task AdminEndpoint_SseHandshake_NegotiatesProtocol()
        {
            // Assert GET /admin/sse sets text/event-stream and POST initialize returns serverInfo
        }

        [Fact]
        [Requirement("MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET", "MCP", RequirementType.Positive, "Target proxy endpoint /router-admin routes directly to the Admin MCP server.")]
        public async Task TargetProxy_RouterAdmin_RoutesToAdminServer()
        {
            // Assert POST /router-admin with tools/list returns admin tools
        }

        [Fact]
        [Requirement("GUARD-ADMIN-ENDPOINT-UNAUTHORIZED", "GUARD", RequirementType.FailClosedGuardrail, "Unauthenticated / non-admin client request to /admin receives 403 Forbidden.")]
        public async Task AdminEndpoint_UnauthorizedCaller_Returns403()
        {
            // Assert request from external non-whitelisted IP without key is rejected
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~AdminEndpointsTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `AdminEndpoints.cs` and wire in `ApplicationBuilderExtensions.cs` & `ProxyEndpoints.cs`**

1. Create `Components/Capabilities/AdminEndpoints.cs`:
   - Map `/admin` and `/admin/sse` (GET/POST/HEAD) with `RequireAuthorization("AdminPolicy")`.
   - Handle SSE stream connection, session creation, and JSON-RPC dispatch to `AdminMcpServer`.
   - Map `/admin/message` for subsequent messages.
2. In `Components/Capabilities/ProxyEndpoints.cs`:
   - In `/{targetServerId}` handler, if `targetServerId == "router-admin"` or `"admin"`, route directly to `AdminMcpServer`.
3. In `Extensions/ApplicationBuilderExtensions.cs`:
   - Call `app.MapAdminMcpEndpoints()`.
   - Add startup information log for standalone mode and allowed network CIDRs.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~AdminEndpointsTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Components/Capabilities/AdminEndpoints.cs Components/Capabilities/ProxyEndpoints.cs Extensions/ApplicationBuilderExtensions.cs McpRouter.Tests/AdminEndpointsTests.cs
git commit -m "feat(api): expose /admin and /router-admin MCP endpoints with AdminPolicy enforcement"
```

---

### Task 4: Comprehensive Test Suite for All 10 Admin Tools & Edge Cases

**Files:**
- Create: `McpRouter.Tests/AdminToolsParityTests.cs`

**Interfaces:**
- Consumes: `AdminMcpServer`, `IDbConnectionFactory`
- Produces: Test proof for all 10 tools and their individual actions

- [ ] **Step 1: Write parity tests covering all 10 consolidated tools**

```csharp
namespace McpRouter.Tests
{
    public class AdminToolsParityTests
    {
        [Theory]
        [InlineData("manage_servers")]
        [InlineData("manage_appkeys")]
        [InlineData("manage_clients")]
        [InlineData("manage_policies")]
        [InlineData("manage_group_mappings")]
        [InlineData("manage_providers")]
        [InlineData("manage_settings")]
        [InlineData("manage_custom_files")]
        [InlineData("manage_system")]
        [InlineData("test_tool_call")]
        [Requirement("MCP-ADMIN-PARITY-TOOLS-COVERAGE", "MCP", RequirementType.Positive, "Every UI management flow has a corresponding verified action in the consolidated Admin MCP tools.")]
        public async Task AdminTools_ExecuteSuccessfully(string toolName)
        {
            // Assert tool executes cleanly for primary action
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~AdminToolsParityTests"`
Expected: PASS.

- [ ] **Step 3: Run entire test suite**

Run: `dotnet test McpRouter.slnx`
Expected: 560+ tests passing, 0 failing.

- [ ] **Step 4: Commit**

```bash
git add McpRouter.Tests/AdminToolsParityTests.cs
git commit -m "test(mcp): add comprehensive parity test proofs for all 10 consolidated admin tools"
```

---

### Task 5: Living Requirements Catalog Regeneration, Documentation & Version Bump

**Files:**
- Modify: `mcp-router.csproj` (bump version to `4.19.0`)
- Modify: `frontend/src/stores/useUserStore.ts` (bump fallback version to `4.19.0`)
- Modify: `CHANGELOG.md` (add `v4.19.0` release entry)
- Modify: `README.md` (update preview table and add Admin MCP Server docs)
- Modify: `docs/features-guide.md` & `ARCHITECTURE.md` (document `/admin` MCP server & standalone auth)
- Regenerate: `docs/software-requirements-and-test-catalog.md` & `docs/requirements-catalog.json`

- [ ] **Step 1: Update version numbers across the 4 mandatory files**

Update `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md` to `4.19.0`.

- [ ] **Step 2: Update documentation**

Add documentation for the Admin MCP Server in `README.md`, `ARCHITECTURE.md`, and `docs/features-guide.md` (connecting via Claude Desktop, Cursor, Cline using `/admin` or `/router-admin`, tool reference, and standalone configuration).

- [ ] **Step 3: Regenerate and verify Living Requirements Catalog**

Run:
```bash
dotnet run --project scripts/CatalogGenerator
dotnet run --project scripts/CatalogGenerator -- --verify-only
```
Expected: Verification passes with 0 drift.

- [ ] **Step 4: Run full solution tests**

Run: `dotnet test McpRouter.slnx`
Expected: 100% tests passing.

- [ ] **Step 5: Commit**

```bash
git add mcp-router.csproj frontend/src/stores/useUserStore.ts CHANGELOG.md README.md docs/ ARCHITECTURE.md
git commit -m "chore(release): bump version to 4.19.0, update docs, and regenerate requirements catalog"
```
