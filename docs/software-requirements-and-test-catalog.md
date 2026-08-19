# Software Requirements Specification (SRS) & Test Verification Catalog

> **Automated Verification Document:** Generated via `dotnet run --project scripts/CatalogGenerator`
> **Catalog Statistics:** **59 Requirements Verified** across **138 Test Proofs** (45 Functional Capabilities, 14 Safety Guardrails).

---

## 1. System Taxonomy & Verification Summary

| Category | Domain | Total Requirements | Positive Features | Guardrails / Fail-Closed | Verification Proofs |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **`AUTH`** | Authentication, RBAC & Identity | **12** | 11 | 1 | 40 proofs |
| **`DB`** | Multi-Database Persistence & Migrations | **2** | 2 | 0 | 5 proofs |
| **`GUARD`** | Universal Safety & Fail-Closed Guardrails | **12** | 0 | 12 | 29 proofs |
| **`MCP`** | Model Context Protocol Engine & Tool Routing | **19** | 19 | 0 | 20 proofs |
| **`SEC`** | Secrets Providers & Encryption | **6** | 5 | 1 | 13 proofs |
| **`TRANS`** | Transports (SSE, HTTP, STDIO, Proxy) | **3** | 3 | 0 | 7 proofs |
| **`UI`** | Dashboard, Test Bench & Settings UI | **5** | 5 | 0 | 24 proofs |

---

## 2. Functional Requirements ("What the Application DOES")

### `[AUTH-02]` AppKey scopes restrict access precisely across all MCP capabilities and backend targets
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (13):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L254`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L254) (`Pairwise_AppKeyScopes_RestrictsAccessPrecisely`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L212`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L212) (`AppKeysController_CreateAppKey_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L296`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L296) (`ClientsController_CreateClient_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L379`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L379) (`ClientSession_CategoryScope_AuthorizesMatchingServerTools_AndDeniesOthers`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L403`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L403) (`ClientSession_GroupAliasScope_AuthorizesIdenticallyToCategory`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L425`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L425) (`ClientSession_CategoryScope_IsCaseInsensitive`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L475`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L475) (`ClientSession_ResourcesAndTemplates_FilteredByCategoryScope`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L503`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L503) (`ClientSession_DynamicServerMembership_UpdatesAccessDynamically`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L539`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L539) (`ClientSession_MixedScopes_CombinesCategoryAndSpecificToolScopes`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L566`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L566) (`ClientSession_Complete_FiltersServerNamesByCategoryScope`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L20`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L20) (`renders empty state when no keys exist`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L44`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L44) (`renders keys list, copies config snippet, and revokes key`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L68`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L68) (`AppKey Direct Context: connects with API key header identity`)

### `[AUTH-03]` SSO identity and group mappings resolve Windows SIDs and OIDC claims to internal access roles
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L331`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L331) (`Pairwise_SsoIdentityAndGroupMappings_EvaluateCorrectly`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/rbac-enforcement-flow.spec.ts#L4`](file:////containers/dev/csharp-mcp-router/frontend/e2e/rbac-enforcement-flow.spec.ts#L4) (`should create, verify, and delete RBAC policy and SID mapping`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L34`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L34) (`Operator Context: allows overview and testbench navigation with operator identity`)

### `[AUTH-04]` ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L17`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L17) (`ResolveIdentityAsync_ExtractsWindowsIdentitySids_ViaAccessor`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L55`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L55) (`ResolveIdentityAsync_AugmentsWithLdapSids_WhenLdapServiceProvided`)

### `[AUTH-05]` McpServer supports AllowPassThroughAuth flag
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/McpServerTests.cs#L9`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/McpServerTests.cs#L9) (`McpServer_Should_Have_AllowPassThroughAuth`)

### `[AUTH-06]` Transports use passThroughToken when AllowPassThroughAuth is true
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/TransportsAuthShapeTests.cs#L208`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/TransportsAuthShapeTests.cs#L208) (`Transports_Use_PassThroughToken_If_Allowed`)

### `[AUTH-APPKEY-ADMIN-SCOPE-ALLOW]` AppKeys with admin scope grant Administrator role and pass AdminPolicy.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L87`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L87) (`AppKey_WithAdminScope_GrantsAdminAccess`)

### `[AUTH-APPKEY-ITEMS-SCOPE-ALLOW]` SecurityValidationHelper recognizes admin scopes in HttpContext.Items.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L263`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L263) (`IsAdmin_AppKeyScopes_InHttpContextItems_ReturnsTrue`)

### `[AUTH-APPKEY-WILDCARD-SCOPE-ALLOW]` AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L148`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L148) (`AppKey_WithWildcardScope_GrantsAdminAccess`)

### `[AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW]` AdminPolicy succeeds in standalone mode for unauthenticated loopback requests.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L184`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L184) (`AdminPolicy_StandaloneMode_LoopbackIp_PassesAdminPolicy`)

### `[AUTH-STANDALONE-CUSTOM-CIDR-ALLOW]` Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L43`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L43) (`IsAdmin_StandaloneMode_CustomCidr_ReturnsTrue`)

### `[AUTH-STANDALONE-LOOPBACK-ALLOW]` Standalone mode without external IDP grants admin access to loopback IP addresses.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L22`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L22) (`IsAdmin_StandaloneMode_LoopbackIp_ReturnsTrue`)

### `[DB-01]` SQLite auto-migration seamlessly upgrades legacy schema, encrypts plaintext secrets, and preserves data
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L43`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L43) (`Sqlite_UpgradeMigration_FromLegacySchema_PreservesDataAndPassesValidation`)

### `[DB-02]` MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L198`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L198) (`Mssql_Scripts_DeclareAllProceduresAndExpectedParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L243`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L243) (`MySql_Scripts_DeclareAllProceduresWithP_PrefixParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L293`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L293) (`Repositories_MySQL_AppKeyOperations_UseP_PrefixParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/MySqlLiveIntegrationTests.cs#L36`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/MySqlLiveIntegrationTests.cs#L36) (`MySql_LiveRepository_AppKeyAndSecretProviderLifecycle_Succeeds`)

### `[MCP-01]` Meta-mode execute_tool strictly enforces target tool authorization policies
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L579`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L579) (`Pairwise_MetaMode_ExecuteTool_EnforcesTargetAuthorization`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L443`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L443) (`ClientSession_ExecuteTool_EnforcesCategoryScopeOnInnerTarget`)

### `[MCP-02]` All MCP protocol capabilities enforce caller role authorizations consistently
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L397`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L397) (`Pairwise_AllCapabilities_UnderCallerRoles_EvaluateCorrectly`)

### `[MCP-ADMIN-ENDPOINT-CALL-TOOL]` Admin endpoint /admin/message executes tools/call for manage_system diagnostics.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L289`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L289) (`AdminEndpoint_SseSession_CallTool_ManageSystemDiagnostics`)

### `[MCP-ADMIN-ENDPOINT-HEAD-REQUEST]` Admin endpoint /admin handles HEAD request returning text/event-stream headers.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L215`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L215) (`AdminEndpoint_HeadRequest_ReturnsEventStreamHeaders`)

### `[MCP-ADMIN-ENDPOINT-LIST-TOOLS]` Admin endpoint /admin/message executes tools/list over active SSE session and returns 10 admin tools.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L227`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L227) (`AdminEndpoint_SseSession_ListTools`)

### `[MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET]` Target proxy endpoint /router-admin routes directly to the Admin MCP server.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L152`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L152) (`TargetProxy_RouterAdmin_RoutesToAdminServer`)

### `[MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE]` Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L73`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L73) (`AdminEndpoint_SseHandshake_NegotiatesProtocol`)

### `[MCP-ADMIN-INITIALIZE-HANDSHAKE]` AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L205`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L205) (`HandleInitializeAsync_NegotiatesProtocolVersion`)

### `[MCP-ADMIN-TOOL-AUDIT-LOG]` AdminMcpServer tool calls record audit log entries with caller and tool name.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L276`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L276) (`CallToolAsync_RecordsAuditLog`)

### `[MCP-ADMIN-TOOL-MANAGE-APPKEYS]` AdminMcpServer executes manage_appkeys create, list, limits, and revoke actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L293`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L293) (`CallToolAsync_ManageAppKeys_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-CLIENTS]` AdminMcpServer executes manage_clients register, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L340`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L340) (`CallToolAsync_ManageClients_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-CUSTOM-FILES]` AdminMcpServer executes manage_custom_files save, get, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L514`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L514) (`CallToolAsync_ManageCustomFiles_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-GROUP-MAPPINGS]` AdminMcpServer executes manage_group_mappings save, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L412`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L412) (`CallToolAsync_ManageGroupMappings_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-POLICIES]` AdminMcpServer executes manage_policies save, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L378`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L378) (`CallToolAsync_ManagePolicies_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-PROVIDERS]` AdminMcpServer executes manage_providers list, save_secret, and save_auth actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L445`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L445) (`CallToolAsync_ManageProviders_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-SERVERS]` AdminMcpServer executes manage_servers list, get, create, update, toggle, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L224`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L224) (`CallToolAsync_ManageServers_ListAndCreate`)

### `[MCP-ADMIN-TOOL-MANAGE-SETTINGS]` AdminMcpServer executes manage_settings get and update actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L488`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L488) (`CallToolAsync_ManageSettings_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-SYSTEM]` AdminMcpServer executes manage_system diagnostics, get_logs, clear_logs, and query_audit actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L568`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L568) (`CallToolAsync_ManageSystem_Lifecycle`)

### `[MCP-ADMIN-TOOLS-LIST-COUNT]` AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L157`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L157) (`ListToolsAsync_ReturnsTenConsolidatedTools`)

### `[SEC-01]` VaultSecretRetriever authenticates with HashiCorp Vault using AppRole RoleID and SecretID credentials
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L23`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L23) (`EnsureVaultClientAsync_CreatesClient_WithAppRoleCredentials`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L44`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L44) (`EnsureVaultClientAsync_LoadsFromSecretRepo_WhenConfigJsonHasAppRole`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L96`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L96) (`ReloadConfigAsync_ClearsClient_ForcesRecreation`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L17`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L17) (`renders provider inputs and submits updated configuration`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L60`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L60) (`handles Test Vault connection button with success and failure responses`)

### `[SEC-02]` STDIO transport securely injects secret credentials via environment variables rather than command-line arguments
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L354`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L354) (`StdioTransport_ShouldPassSecretViaEnvironmentVariables_AndNotCommandLine`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L439`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L439) (`StdioTransport_ShouldSanitizeAndMaskSecretsInLogs`)

### `[SEC-03]` Ensure TrustedProxyHelper supports CIDR ranges in XFF validation
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L366`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L366) (`TrustedProxyHelper_AllowsXForwardedFor_WhenChainIsFullyTrusted`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L425`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L425) (`TrustedProxyHelper_ConfiguredProxyTrusted_CIDR`)

### `[SEC-04]` WindowsRegistrySecretRetriever securely decrypts DPAPI LocalMachine machine-level encrypted binary values and retrieves plaintext registry strings
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/WindowsRegistrySecretRetrieverTests.cs#L16`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/WindowsRegistrySecretRetrieverTests.cs#L16) (`GetSecretAsync_ReturnsPlainString_WhenRegistryValueIsString`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/WindowsRegistrySecretRetrieverTests.cs#L31`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/WindowsRegistrySecretRetrieverTests.cs#L31) (`GetSecretAsync_DecryptsDpapiBytes_WhenRegistryValueIsByteArray`)

### `[SEC-ADMIN-AUDIT-REDACTION]` AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L619`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L619) (`CallToolAsync_AuditLog_RedactsSensitivePayloadData`)

### `[TRANS-01]` SSE transport resolves static plaintext API keys when provider is None
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L18) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)

### `[TRANS-02]` HTTP stateless transport resolves static API keys when secret provider is None
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L19`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L19) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)

### `[TRANS-03]` STDIO transport spawns subprocess, handles JSON-RPC initialization and executes tool calls
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L59`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L59) (`StdioTransport_ShouldInitializeAndCallToolSuccessfully`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L180`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L180) (`StdioTransport_ShouldRouteStderrToLogs`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L252`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L252) (`StdioTransport_ShouldSupportCancellationAndProcessTreeTermination`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L337`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L337) (`StdioTransport_ParseCommandLine_Handles_Quotes_And_Spaces`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L482`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L482) (`StdioTransport_ShouldDrainReaderStreamsToEOF_WhenProcessExitsImmediately`)

### `[UI-01]` Dashboard renders stats card, connected server list, and setup instructions
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L36`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L36) (`renders stats card, server list, and client setup guide`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L111`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L111) (`renders empty state when no servers match search`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L42`](file:////containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L42) (`should navigate to Custom Files and Backups in Settings view`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L5) (`should render the dashboard layout and header components`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L21`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L21) (`should display aggregate statistics cards`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L35`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L35) (`should filter servers using search input`)

### `[UI-02]` Modal remains hidden when isInspectOpen is false
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (7):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L46`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L46) (`renders nothing when isInspectOpen is false`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L58`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L58) (`renders loading state when inspectLoading is true`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L76`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L76) (`renders tools tab with schema and handles tab switching`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L113`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L113) (`renders resources tab items and handles search filtering`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L141`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L141) (`renders prompts tab with arguments and empty state when filtered out`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L163`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L163) (`renders empty states for tabs when data is empty`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L191`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L191) (`closes modal when close button is clicked`)

### `[UI-03]` Grouped server view renders category sections and supports collapsible groups
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L59`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L59) (`renders grouped server view by category and allows collapsing`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L86`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L86) (`renders grouped server view by status and type`)

### `[UI-04]` Interactive tool tester renders server and tool selection dropdowns
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (8):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L41`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L41) (`renders initial server and tool selection options`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L75`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L75) (`filters tools by selected server and handles tool change`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L104`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L104) (`filters custom tools with no namespace prefix when selectedServer is custom`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L129`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L129) (`renders dynamic fields for boolean, number, string, array, and object types`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L176`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L176) (`renders empty state when selected tool takes no arguments`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L201`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L201) (`switches to raw JSON tab and handles raw JSON editing`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L240`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L240) (`handles form submission`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L5) (`should interact with Prompt Tester and Resource Tester cards in Test Bench`)

### `[UI-05]` Router allows customized branding parameters (DashboardTitle, DashboardIcon) to be saved and retrieved via the API.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PipelineIntegrationTests.cs#L242`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PipelineIntegrationTests.cs#L242) (`Pipeline_Settings_Branding_ReadWrite`)

---

## 3. Boundary & Guardrail Invariants ("What the Application DOES NOT DO")

> [!IMPORTANT]
> The following guardrails define strict security boundaries, fail-closed fault invariants, and forbidden application states.

### `[AUTH-01]` AdminPolicy allows principal with configured Admin Group Name (e.g., full_admin)
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (14):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L18) (`AdminPolicy_Allows_Principal_With_AdminGroupName`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L52`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L52) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L86`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L86) (`AdminPolicy_Allows_Principal_With_ConfiguredAdminGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L121`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L121) (`AdminPolicy_Denies_StandardRole_WithoutAdminSidOrGroup`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L21`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L21) (`AdminPolicy_Denies_StandardRole_Without_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L63`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L63) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L273`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L273) (`AppKeysController_CreateAppKey_UnknownCategory_Admin_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L226`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L226) (`SecurityValidationHelper_IsAdmin_RequiresAdminGroupSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L244`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L244) (`SecurityValidationHelper_IsAdmin_AllowsAdminGroupName`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L258`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L258) (`SecurityValidationHelper_IsAdmin_RejectsNonAdminGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L275`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L275) (`SecurityValidationHelper_IsAdmin_AllowsCustomAdminGroupsArray`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L290`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L290) (`SecurityValidationHelper_IsAdmin_AllowsMappedGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L305`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L305) (`OidcIdentityProvider_DoesNotGrantAdminSid_FromGroupOrUserNames`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L5) (`Admin Context: renders full administrator view and privileged controls`)

### `[AUTH-EXTERNAL-IDP-DENIES-ANONYMOUS-LOOPBACK]` When an external IDP is configured, anonymous loopback requests do not bypass authentication.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L232`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L232) (`AdminPolicy_ExternalIdpConfigured_LoopbackIp_RequiresCredentials`)

### `[AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY]` AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L208`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L208) (`AdminPolicy_StandaloneMode_ExternalUntrustedIp_FailsAdminPolicy`)

### `[AUTH-STANDALONE-EXTERNAL-DENY]` Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L65`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L65) (`IsAdmin_StandaloneMode_UntrustedIp_ReturnsFalse`)

### `[GUARD-01]` Null or empty capability targets must immediately fail closed and return unauthorized
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (5):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L480`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L480) (`Pairwise_NullOrEmptyTarget_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L502`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L502) (`Pairwise_CorruptedAppKeyScopesJson_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L235`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L235) (`AppKeysController_CreateAppKey_UnknownCategory_NonAdmin_FailsWithBadRequest`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L254`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L254) (`AppKeysController_CreateAppKey_EmptyCategory_FailsWithBadRequest`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L319`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L319) (`ClientsController_CreateClient_EmptyCategory_ReturnsBadRequest`)

### `[GUARD-02]` SSE transport fails closed with SecurityException when secret provider resolution fails
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (7):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L39`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L39) (`ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L60`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L60) (`ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L404`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L404) (`StdioTransport_ShouldFailClosed_WhenSecretResolutionFails`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L39`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L39) (`ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L62`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L62) (`ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L70`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L70) (`EnsureVaultClientAsync_ReturnsNull_WhenVaultProviderDisabledInRepo`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L122`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L122) (`GetSecretAsync_ThrowsSecurityException_OnVaultException`)

### `[GUARD-03]` STDIO transport rejects commands with shell metacharacters or dangerous commands
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L114`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L114) (`StdioTransport_ShouldThrowSecurityExceptionForUnsafeExecutable`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L136`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L136) (`StdioTransport_ShouldThrowSecurityExceptionForShellExecutable`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L158`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L158) (`StdioTransport_ShouldThrowOnInvalidExecutable`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L220`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L220) (`StdioTransport_ShouldTimeoutOnSlowRequests`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L299`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L299) (`StdioTransport_ShouldHandleUnexpectedExit`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L53`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L53) (`Guest / Denied Context: restricted user session renders safely`)

### `[GUARD-04]` Malformed completion payloads or unmapped backends must fail closed safely
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L520`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L520) (`Pairwise_CompleteAsync_MalformedOrMissingBackends_ThrowsOrFailsClosed`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L543`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L543) (`Pairwise_DatabaseDisconnection_FailsClosedSafely`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L172`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L172) (`SchemaValidation_FailsClosed_WhenRequiredColumnOrTableMissing`)

### `[GUARD-05]` Batch save of authentication providers must fail closed if all providers are disabled
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ProvidersControllerTests.cs#L324`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ProvidersControllerTests.cs#L324) (`SaveAuthProvidersBatch_ReturnsBadRequest_WhenAllProvidersDisabled`)

### `[GUARD-06]` Global deny policies with TargetId '*' and IsAllowed false must fail closed
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PermissionsControllerTests.cs#L236`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PermissionsControllerTests.cs#L236) (`SavePolicy_ReturnsBadRequest_WhenWildcardDenyPolicy`)

### `[GUARD-ADMIN-ENDPOINT-UNAUTHORIZED]` Unauthenticated / non-admin client request to /admin receives 403 Forbidden.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L196`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L196) (`AdminEndpoint_UnauthorizedCaller_Returns403`)

### `[GUARD-ADMIN-UNKNOWN-TOOL]` AdminMcpServer returns an error response for unknown tool or action invocations.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L602`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L602) (`CallToolAsync_UnknownToolOrAction_ReturnsErrorResponse`)

### `[MCP-ADMIN-TOOL-TEST-CALL-ERROR]` AdminMcpServer test_tool_call propagates downstream backend errors with visibility.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L643`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L643) (`CallToolAsync_TestToolCall_MissingServer_ReturnsError`)

### `[SEC-05]` Router must not overwrite corrupt encrypted database fields if an update occurs without user reset.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ProviderSettingsEncryptionTests.cs#L395`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ProviderSettingsEncryptionTests.cs#L395) (`SaveSecretProvider_WhenDecryptionFailed_DoesNotOverwriteCorruptPayload`)

---

## 4. Complete Verification Traceability Matrix

| Requirement ID | Type | Category | Description | Primary Proof | Suite |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `AUTH-01` | **Guardrail** | `AUTH` | AdminPolicy allows principal with configured Admin Group Name (e.g., full_admin) | [`AdminPolicyHybridAuthTests.cs:L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L18) | Backend xUnit |
| `AUTH-02` | Positive | `AUTH` | AppKey scopes restrict access precisely across all MCP capabilities and backend targets | [`PairwiseIntegrationMatrixTests.cs:L254`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L254) | Backend xUnit |
| `AUTH-03` | Positive | `AUTH` | SSO identity and group mappings resolve Windows SIDs and OIDC claims to internal access roles | [`PairwiseIntegrationMatrixTests.cs:L331`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L331) | Backend xUnit |
| `AUTH-04` | Positive | `AUTH` | ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP | [`ActiveDirectoryWindowsIdentityTests.cs:L17`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L17) | Backend xUnit |
| `AUTH-05` | Positive | `AUTH` | McpServer supports AllowPassThroughAuth flag | [`McpServerTests.cs:L9`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/McpServerTests.cs#L9) | Backend xUnit |
| `AUTH-06` | Positive | `AUTH` | Transports use passThroughToken when AllowPassThroughAuth is true | [`TransportsAuthShapeTests.cs:L208`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/TransportsAuthShapeTests.cs#L208) | Backend xUnit |
| `AUTH-APPKEY-ADMIN-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with admin scope grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L87`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L87) | Backend xUnit |
| `AUTH-APPKEY-ITEMS-SCOPE-ALLOW` | Positive | `AUTH` | SecurityValidationHelper recognizes admin scopes in HttpContext.Items. | [`StandaloneAdminAuthTests.cs:L263`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L263) | Backend xUnit |
| `AUTH-APPKEY-WILDCARD-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L148`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L148) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW` | Positive | `AUTH` | AdminPolicy succeeds in standalone mode for unauthenticated loopback requests. | [`StandaloneAdminAuthTests.cs:L184`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L184) | Backend xUnit |
| `AUTH-STANDALONE-CUSTOM-CIDR-ALLOW` | Positive | `AUTH` | Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges. | [`StandaloneAdminAuthTests.cs:L43`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L43) | Backend xUnit |
| `AUTH-STANDALONE-LOOPBACK-ALLOW` | Positive | `AUTH` | Standalone mode without external IDP grants admin access to loopback IP addresses. | [`StandaloneAdminAuthTests.cs:L22`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L22) | Backend xUnit |
| `DB-01` | Positive | `DB` | SQLite auto-migration seamlessly upgrades legacy schema, encrypts plaintext secrets, and preserves data | [`DatabaseSchemaUpgradeAndContractTests.cs:L43`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L43) | Backend xUnit |
| `DB-02` | Positive | `DB` | MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly | [`DatabaseSchemaUpgradeAndContractTests.cs:L198`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L198) | Backend xUnit |
| `AUTH-EXTERNAL-IDP-DENIES-ANONYMOUS-LOOPBACK` | **Guardrail** | `GUARD` | When an external IDP is configured, anonymous loopback requests do not bypass authentication. | [`StandaloneAdminAuthTests.cs:L232`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L232) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY` | **Guardrail** | `GUARD` | AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode. | [`StandaloneAdminAuthTests.cs:L208`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L208) | Backend xUnit |
| `AUTH-STANDALONE-EXTERNAL-DENY` | **Guardrail** | `GUARD` | Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey. | [`StandaloneAdminAuthTests.cs:L65`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L65) | Backend xUnit |
| `GUARD-01` | **Guardrail** | `GUARD` | Null or empty capability targets must immediately fail closed and return unauthorized | [`PairwiseIntegrationMatrixTests.cs:L480`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L480) | Backend xUnit |
| `GUARD-02` | **Guardrail** | `GUARD` | SSE transport fails closed with SecurityException when secret provider resolution fails | [`SseTransportTests.cs:L39`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L39) | Backend xUnit |
| `GUARD-03` | **Guardrail** | `GUARD` | STDIO transport rejects commands with shell metacharacters or dangerous commands | [`StdioTransportTests.cs:L114`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L114) | Backend xUnit |
| `GUARD-04` | **Guardrail** | `GUARD` | Malformed completion payloads or unmapped backends must fail closed safely | [`PairwiseIntegrationMatrixTests.cs:L520`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L520) | Backend xUnit |
| `GUARD-05` | **Guardrail** | `GUARD` | Batch save of authentication providers must fail closed if all providers are disabled | [`ProvidersControllerTests.cs:L324`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ProvidersControllerTests.cs#L324) | Backend xUnit |
| `GUARD-06` | **Guardrail** | `GUARD` | Global deny policies with TargetId '*' and IsAllowed false must fail closed | [`PermissionsControllerTests.cs:L236`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PermissionsControllerTests.cs#L236) | Backend xUnit |
| `GUARD-ADMIN-ENDPOINT-UNAUTHORIZED` | **Guardrail** | `GUARD` | Unauthenticated / non-admin client request to /admin receives 403 Forbidden. | [`AdminEndpointsTests.cs:L196`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L196) | Backend xUnit |
| `GUARD-ADMIN-UNKNOWN-TOOL` | **Guardrail** | `GUARD` | AdminMcpServer returns an error response for unknown tool or action invocations. | [`AdminMcpServerTests.cs:L602`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L602) | Backend xUnit |
| `MCP-ADMIN-TOOL-TEST-CALL-ERROR` | **Guardrail** | `GUARD` | AdminMcpServer test_tool_call propagates downstream backend errors with visibility. | [`AdminMcpServerTests.cs:L643`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L643) | Backend xUnit |
| `MCP-01` | Positive | `MCP` | Meta-mode execute_tool strictly enforces target tool authorization policies | [`PairwiseIntegrationMatrixTests.cs:L579`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L579) | Backend xUnit |
| `MCP-02` | Positive | `MCP` | All MCP protocol capabilities enforce caller role authorizations consistently | [`PairwiseIntegrationMatrixTests.cs:L397`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L397) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-CALL-TOOL` | Positive | `MCP` | Admin endpoint /admin/message executes tools/call for manage_system diagnostics. | [`AdminEndpointsTests.cs:L289`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L289) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-HEAD-REQUEST` | Positive | `MCP` | Admin endpoint /admin handles HEAD request returning text/event-stream headers. | [`AdminEndpointsTests.cs:L215`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L215) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-LIST-TOOLS` | Positive | `MCP` | Admin endpoint /admin/message executes tools/list over active SSE session and returns 10 admin tools. | [`AdminEndpointsTests.cs:L227`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L227) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET` | Positive | `MCP` | Target proxy endpoint /router-admin routes directly to the Admin MCP server. | [`AdminEndpointsTests.cs:L152`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L152) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE` | Positive | `MCP` | Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version. | [`AdminEndpointsTests.cs:L73`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L73) | Backend xUnit |
| `MCP-ADMIN-INITIALIZE-HANDSHAKE` | Positive | `MCP` | AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05. | [`AdminMcpServerTests.cs:L205`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L205) | Backend xUnit |
| `MCP-ADMIN-TOOL-AUDIT-LOG` | Positive | `MCP` | AdminMcpServer tool calls record audit log entries with caller and tool name. | [`AdminMcpServerTests.cs:L276`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L276) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-APPKEYS` | Positive | `MCP` | AdminMcpServer executes manage_appkeys create, list, limits, and revoke actions. | [`AdminMcpServerTests.cs:L293`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L293) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-CLIENTS` | Positive | `MCP` | AdminMcpServer executes manage_clients register, list, and delete actions. | [`AdminMcpServerTests.cs:L340`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L340) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-CUSTOM-FILES` | Positive | `MCP` | AdminMcpServer executes manage_custom_files save, get, list, and delete actions. | [`AdminMcpServerTests.cs:L514`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L514) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-GROUP-MAPPINGS` | Positive | `MCP` | AdminMcpServer executes manage_group_mappings save, list, and delete actions. | [`AdminMcpServerTests.cs:L412`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L412) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-POLICIES` | Positive | `MCP` | AdminMcpServer executes manage_policies save, list, and delete actions. | [`AdminMcpServerTests.cs:L378`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L378) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-PROVIDERS` | Positive | `MCP` | AdminMcpServer executes manage_providers list, save_secret, and save_auth actions. | [`AdminMcpServerTests.cs:L445`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L445) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SERVERS` | Positive | `MCP` | AdminMcpServer executes manage_servers list, get, create, update, toggle, and delete actions. | [`AdminMcpServerTests.cs:L224`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L224) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SETTINGS` | Positive | `MCP` | AdminMcpServer executes manage_settings get and update actions. | [`AdminMcpServerTests.cs:L488`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L488) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SYSTEM` | Positive | `MCP` | AdminMcpServer executes manage_system diagnostics, get_logs, clear_logs, and query_audit actions. | [`AdminMcpServerTests.cs:L568`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L568) | Backend xUnit |
| `MCP-ADMIN-TOOLS-LIST-COUNT` | Positive | `MCP` | AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas. | [`AdminMcpServerTests.cs:L157`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L157) | Backend xUnit |
| `SEC-01` | Positive | `SEC` | VaultSecretRetriever authenticates with HashiCorp Vault using AppRole RoleID and SecretID credentials | [`VaultAppRoleAndRenewalTests.cs:L23`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L23) | Backend xUnit |
| `SEC-02` | Positive | `SEC` | STDIO transport securely injects secret credentials via environment variables rather than command-line arguments | [`StdioTransportTests.cs:L354`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L354) | Backend xUnit |
| `SEC-03` | Positive | `SEC` | Ensure TrustedProxyHelper supports CIDR ranges in XFF validation | [`IdentityProviderTests.cs:L366`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L366) | Backend xUnit |
| `SEC-04` | Positive | `SEC` | WindowsRegistrySecretRetriever securely decrypts DPAPI LocalMachine machine-level encrypted binary values and retrieves plaintext registry strings | [`WindowsRegistrySecretRetrieverTests.cs:L16`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/WindowsRegistrySecretRetrieverTests.cs#L16) | Backend xUnit |
| `SEC-05` | **Guardrail** | `SEC` | Router must not overwrite corrupt encrypted database fields if an update occurs without user reset. | [`ProviderSettingsEncryptionTests.cs:L395`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ProviderSettingsEncryptionTests.cs#L395) | Backend xUnit |
| `SEC-ADMIN-AUDIT-REDACTION` | Positive | `SEC` | AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs. | [`AdminMcpServerTests.cs:L619`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L619) | Backend xUnit |
| `TRANS-01` | Positive | `TRANS` | SSE transport resolves static plaintext API keys when provider is None | [`SseTransportTests.cs:L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L18) | Backend xUnit |
| `TRANS-02` | Positive | `TRANS` | HTTP stateless transport resolves static API keys when secret provider is None | [`HttpTransportTests.cs:L19`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L19) | Backend xUnit |
| `TRANS-03` | Positive | `TRANS` | STDIO transport spawns subprocess, handles JSON-RPC initialization and executes tool calls | [`StdioTransportTests.cs:L59`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L59) | Backend xUnit |
| `UI-01` | Positive | `UI` | Dashboard renders stats card, connected server list, and setup instructions | [`DashboardView.test.tsx:L36`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L36) | Frontend Vitest |
| `UI-02` | Positive | `UI` | Modal remains hidden when isInspectOpen is false | [`ServerInspectModal.test.tsx:L46`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L46) | Frontend Vitest |
| `UI-03` | Positive | `UI` | Grouped server view renders category sections and supports collapsible groups | [`DashboardView.test.tsx:L59`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L59) | Frontend Vitest |
| `UI-04` | Positive | `UI` | Interactive tool tester renders server and tool selection dropdowns | [`ToolTesterCard.test.tsx:L41`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L41) | Frontend Vitest |
| `UI-05` | Positive | `UI` | Router allows customized branding parameters (DashboardTitle, DashboardIcon) to be saved and retrieved via the API. | [`PipelineIntegrationTests.cs:L242`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PipelineIntegrationTests.cs#L242) | Backend xUnit |
