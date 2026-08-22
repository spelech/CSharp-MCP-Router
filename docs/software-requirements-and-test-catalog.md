# Software Requirements Specification (SRS) & Test Verification Catalog

> **Automated Verification Document:** Generated via `dotnet run --project scripts/CatalogGenerator`
> **Catalog Statistics:** **94 Requirements Verified** across **234 Test Proofs** (71 Functional Capabilities, 23 Safety Guardrails).

---

## 1. System Taxonomy & Verification Summary

| Category | Domain | Total Requirements | Positive Features | Guardrails / Fail-Closed | Verification Proofs |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **`AUTH`** | Authentication, RBAC & Identity | **22** | 20 | 2 | 81 proofs |
| **`DB`** | Multi-Database Persistence & Migrations | **2** | 2 | 0 | 10 proofs |
| **`DOC`** | DOC | **4** | 4 | 0 | 4 proofs |
| **`GUARD`** | Universal Safety & Fail-Closed Guardrails | **16** | 0 | 16 | 34 proofs |
| **`MCP`** | Model Context Protocol Engine & Tool Routing | **31** | 31 | 0 | 32 proofs |
| **`SEC`** | Secrets Providers & Encryption | **9** | 6 | 3 | 18 proofs |
| **`TRANS`** | Transports (SSE, HTTP, STDIO, Proxy) | **3** | 3 | 0 | 9 proofs |
| **`UI`** | Dashboard, Test Bench & Settings UI | **7** | 5 | 2 | 46 proofs |

---

## 2. Functional Requirements ("What the Application DOES")

### `[AUTH-001]` Verify DatabaseUserSecretStore encrypts and decrypts secret correctly.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserSecretStoreTests.cs#L13`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserSecretStoreTests.cs#L13) (`DatabaseUserSecretStore_SavesAndRetrieves_Secret`)

### `[AUTH-002]` Verify UserCredentialsController returns configured server IDs.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserCredentialsControllerTests.cs#L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserCredentialsControllerTests.cs#L18) (`GetUserCredentials_ReturnsServerIds`)

### `[AUTH-02]` AppKey scopes restrict access precisely across all MCP capabilities and backend targets
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (12):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L255`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L255) (`Pairwise_AppKeyScopes_RestrictsAccessPrecisely`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L151`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L151) (`AppKeyRepository_SaveAndGet_PersistsKeyTypeAndFilters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L213`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L213) (`AppKeysController_CreateAppKey_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L297`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L297) (`ClientsController_CreateClient_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L380`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L380) (`ClientSession_CategoryScope_AuthorizesMatchingServerTools_AndDeniesOthers`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L404`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L404) (`ClientSession_GroupAliasScope_AuthorizesIdenticallyToCategory`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L426`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L426) (`ClientSession_CategoryScope_IsCaseInsensitive`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L476`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L476) (`ClientSession_ResourcesAndTemplates_FilteredByCategoryScope`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L504`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L504) (`ClientSession_DynamicServerMembership_UpdatesAccessDynamically`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L540`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L540) (`ClientSession_MixedScopes_CombinesCategoryAndSpecificToolScopes`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L567`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L567) (`ClientSession_Complete_FiltersServerNamesByCategoryScope`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L68`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L68) (`AppKey Direct Context: connects with API key header identity`)

### `[AUTH-03]` SSO identity and group mappings resolve Windows SIDs and OIDC claims to internal access roles
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L332`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L332) (`Pairwise_SsoIdentityAndGroupMappings_EvaluateCorrectly`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/rbac-enforcement-flow.spec.ts#L4`](file:////containers/dev/csharp-mcp-router/frontend/e2e/rbac-enforcement-flow.spec.ts#L4) (`should create, verify, and delete RBAC policy and SID mapping`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L34`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L34) (`Operator Context: allows overview and testbench navigation with operator identity`)

### `[AUTH-04]` ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L17`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L17) (`ResolveIdentityAsync_ExtractsWindowsIdentitySids_ViaAccessor`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L55`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L55) (`ResolveIdentityAsync_AugmentsWithLdapSids_WhenLdapServiceProvided`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/ldap-identity-and-auth-flow.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/ldap-identity-and-auth-flow.spec.ts#L5) (`should configure LDAP identity provider, test connection, and save settings`)

### `[AUTH-05]` McpServer supports AllowPassThroughAuth flag
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/McpServerTests.cs#L9`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/McpServerTests.cs#L9) (`McpServer_Should_Have_AllowPassThroughAuth`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/my-mcp-servers.spec.ts#L7`](file:////containers/dev/csharp-mcp-router/frontend/e2e/my-mcp-servers.spec.ts#L7) (`should render user provided servers and allow editing credentials with SQLite schema`)

### `[AUTH-06]` Transports use passThroughToken when AllowPassThroughAuth is true
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/TransportsAuthShapeTests.cs#L208`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/TransportsAuthShapeTests.cs#L208) (`Transports_Use_PassThroughToken_If_Allowed`)

### `[AUTH-101]` HTTP transport injects X-Forwarded-User header based on connected user identity.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityHeaderTests.cs#L16`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityHeaderTests.cs#L16) (`HttpTransport_InjectsXForwardedUserHeader`)

### `[AUTH-105]` Dynamic Auth Target Pass-Through
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ToolRoutingManagerTests.cs#L180`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ToolRoutingManagerTests.cs#L180) (`ExecuteTargetToolAsync_Catches401_AndReturnsAuthPrompt`)

### `[AUTH-109]` ConsentView properly renders the client name from query string and builds correct form action.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/pages/ConsentView.test.tsx#L16`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/ConsentView.test.tsx#L16) (`renders client name from query string and sets form action`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/oauth-consent-flow.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/oauth-consent-flow.spec.ts#L5) (`should render interactive OAuth consent screen and display requesting client name`)

### `[AUTH-110]` CreateAppKey allows creating unlimited AppKeys when UserMaxKeys is set to 0.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L347`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L347) (`CreateAppKey_AllowsUnlimited_WhenLimitsAreZero`)

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

### `[REQ-AUTH-PERSONAL-APPKEY-LIST]` Non-admin users can view their personal App Keys
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L133`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L133) (`GetAppKeys_NonAdmin_ReturnsOnlyPersonalKeys_ForCurrentUser`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L232`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L232) (`loads app keys and updates store`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L79`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L79) (`renders role-adaptive UI for non-admin user`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L34`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L34) (`renders role-adapted My App Keys view for non-admin user`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L79`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L79) (`renders keys list, copies config snippet, and revokes key`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L5) (`Non-Admin Context: displays My App Keys navigation and personal quota indicator`)

### `[REQ-AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE]` Custom user quotas override default limit
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L231`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L231) (`CreateAppKey_CustomQuotaOverride_AllowsHigherLimit`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L439`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L439) (`sets user quota override and refreshes quota list`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L6`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L6) (`renders GeneralTab with security default quota inputs and triggers save`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L71`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L71) (`updates form state when settings prop changes`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L222`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L222) (`manages custom user quotas in admin quotas tab`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L135`](file:////containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L135) (`Admin Context: configures custom user quota override`)

### `[REQ-AUTH-SYSTEM-APPKEY-SEPARATION]` System keys are distinct and require admin permissions
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (10):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L159`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L159) (`SystemAppKeys_RequireAdmin_AndSeparateFromPersonalKeys`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeyAuthenticationTests.cs#L307`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeyAuthenticationTests.cs#L307) (`PersonalAppKey_WithAllScope_DoesNotGrantAdministratorRole`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeyAuthenticationTests.cs#L360`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeyAuthenticationTests.cs#L360) (`SystemAppKey_WithAdminScope_GrantsAdministratorRole`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L216`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L216) (`switches keyTypeTab between personal and system`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L250`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L250) (`fetches system-filtered app keys via query parameters`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L15`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L15) (`renders header, navigation tabs, and default overview dashboard for admin user`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L35`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L35) (`switches between tabs on navigation click`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L31`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L31) (`allows admin to select key type and create system app key`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L166`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L166) (`handles admin tab switching and username filtering`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L87`](file:////containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L87) (`Admin Context: manages segmented App-Level Keys and User Personal Keys`)

### `[DB-01]` SQLite auto-migration seamlessly upgrades legacy schema, encrypts plaintext secrets, and preserves data
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L43`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L43) (`Sqlite_UpgradeMigration_FromLegacySchema_PreservesDataAndPassesValidation`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L90`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L90) (`UserQuotaRepository_SetAndGet_ReturnsPersistedQuota`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L103`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L103) (`UserQuotaRepository_GetAll_ReturnsAllUserQuotas`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L122`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L122) (`UserQuotaRepository_Update_UpdatesExistingQuota`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L137`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L137) (`UserQuotaRepository_Delete_RemovesQuota`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L204`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L204) (`DependencyInjection_RegistersIUserQuotaRepository`)

### `[DB-02]` MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L325`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L325) (`Mssql_Scripts_DeclareAllProceduresAndExpectedParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L370`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L370) (`MySql_Scripts_DeclareAllProceduresWithP_PrefixParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L420`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L420) (`Repositories_MySQL_AppKeyOperations_UseP_PrefixParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/MySqlLiveIntegrationTests.cs#L36`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/MySqlLiveIntegrationTests.cs#L36) (`MySql_LiveRepository_AppKeyAndSecretProviderLifecycle_Succeeds`)

### `[DOC-SETUP-SKILL-FRONTMATTER]` mcp-router-setup skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L22`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L22) (`Skill_Frontmatter_IsValidAndWithinCharacterLimit`)

### `[DOC-SETUP-SKILL-MIRROR]` The mcp-router-setup skill and templates are mirrored 1:1 in .agents/skills/mcp-router-setup/
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L156`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L156) (`Skill_MirroredInAgentsDirectory`)

### `[DOC-SETUP-SKILL-TEMPLATES]` All scaffold templates exist, are non-empty, and contain required directives such as responseBufferLimit, ROUTER_MASTER_KEY, and ghcr.io/spelech/mcp-router
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L102`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L102) (`Templates_AreValidAndContainRequiredDirectives`)

### `[DOC-SETUP-SKILL-WORKFLOW]` mcp-router-setup skill contains all 6 required setup phases including environment probing, hosting platforms, env vs UI trade-offs, identity/network topology, artifact generation, and health/client configuration
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L48`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L48) (`Skill_ContainsAllRequiredPhasesAndComparisons`)

### `[MCP-01]` Meta-mode execute_tool strictly enforces target tool authorization policies
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L580`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L580) (`Pairwise_MetaMode_ExecuteTool_EnforcesTargetAuthorization`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L444`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L444) (`ClientSession_ExecuteTool_EnforcesCategoryScopeOnInnerTarget`)

### `[MCP-02]` All MCP protocol capabilities enforce caller role authorizations consistently
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L398`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L398) (`Pairwise_AllCapabilities_UnderCallerRoles_EvaluateCorrectly`)

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

### `[MCP-ADMIN-PARITY-APPKEYS]` manage_appkeys supports full parity for list, get_limits, create, and revoke actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L381`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L381) (`ManageAppKeys_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-CLIENTS]` manage_clients supports full parity for register, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L434`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L434) (`ManageClients_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-CUSTOM-FILES]` manage_custom_files supports full parity for list, get, save, and delete prompt and resource files.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L727`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L727) (`ManageCustomFiles_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-GROUP-MAPPINGS]` manage_group_mappings supports full parity for list, save, and delete external-to-internal group mappings.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L541`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L541) (`ManageGroupMappings_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-JSONRPC-DISPATCH]` AdminMcpServer processes standard JSON-RPC 2.0 requests (tools/list, tools/call, ping).
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L884`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L884) (`AdminTools_ProcessRequest_JsonRpcProtocol`)

### `[MCP-ADMIN-PARITY-POLICIES]` manage_policies supports full parity for list, save, and delete access control policies.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L475`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L475) (`ManagePolicies_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-PROVIDERS]` manage_providers supports full parity for list, save_secret, test_vault, save_auth, and test_ldap actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L588`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L588) (`ManageProviders_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SERVERS]` manage_servers supports full parity for list, get, create, update, toggle, delete, reconnect, and reconnect_all actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L245`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L245) (`ManageServers_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SETTINGS]` manage_settings supports full parity for get and update global router configurations.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L678`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L678) (`ManageSettings_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SYSTEM]` manage_system supports full parity for diagnostics, get_logs, clear_logs, and query_audit actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L827`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L827) (`ManageSystem_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-TEST-TOOL-CALL]` test_tool_call executes test bench backend tool calls and formats responses.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L857`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L857) (`TestToolCall_Execution_Parity`)

### `[MCP-ADMIN-PARITY-TOOLS-COVERAGE]` Every UI management flow has a corresponding verified action in the consolidated Admin MCP tools.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L202`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L202) (`AdminTools_ExecuteSuccessfully`)

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

### `[AUTH-107]` RegisterClient successfully handles DCR requests when open DCR is enabled.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L41`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L41) (`RegisterClient_CreatesApplicationAndReturnsOk`)

### `[SEC-01]` VaultSecretRetriever authenticates with HashiCorp Vault using AppRole RoleID and SecretID credentials
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L23`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L23) (`EnsureVaultClientAsync_CreatesClient_WithAppRoleCredentials`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L44`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L44) (`EnsureVaultClientAsync_LoadsFromSecretRepo_WhenConfigJsonHasAppRole`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L96`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L96) (`ReloadConfigAsync_ClearsClient_ForcesRecreation`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L19`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L19) (`renders provider inputs and submits updated configuration`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L90`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L90) (`handles Test Vault connection button with success and failure responses`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-sse-vault.spec.ts#L8`](file:////containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-sse-vault.spec.ts#L8) (`should register SSE server with Vault provider (Mount/Path/Field), verify badge, and run semantic search`)

### `[SEC-02]` STDIO transport securely injects secret credentials via environment variables rather than command-line arguments
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L354`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L354) (`StdioTransport_ShouldPassSecretViaEnvironmentVariables_AndNotCommandLine`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L439`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L439) (`StdioTransport_ShouldSanitizeAndMaskSecretsInLogs`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/vault-approle-config-flow.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/vault-approle-config-flow.spec.ts#L5) (`should configure Vault AppRole credentials and test connection in settings`)

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
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L18) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-http-direct.spec.ts#L8`](file:////containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-http-direct.spec.ts#L8) (`should register HTTP server with Direct Key, verify status badge, and execute tool in Test Bench`)

### `[TRANS-02]` HTTP stateless transport resolves static API keys when secret provider is None
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L19`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L19) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-stdio-env.spec.ts#L8`](file:////containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-stdio-env.spec.ts#L8) (`should register STDIO server, verify card, and execute echo tool via Test Bench`)

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
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L38`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L38) (`renders stats card, server list, and client setup guide`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L113`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L113) (`renders empty state when no servers match search`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L42`](file:////containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L42) (`should navigate to Custom Files and Prompts in Settings view`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L5) (`should render the dashboard layout and header components`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L21`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L21) (`should display aggregate statistics cards`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L35`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L35) (`should filter servers using search input`)

### `[UI-02]` Modal remains hidden when isInspectOpen is false
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (7):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L47`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L47) (`renders nothing when isInspectOpen is false`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L59`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L59) (`renders loading state when inspectLoading is true`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L77`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L77) (`renders tools tab with schema and handles tab switching`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L114`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L114) (`renders resources tab items and handles search filtering`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L142`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L142) (`renders prompts tab with arguments and empty state when filtered out`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L164`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L164) (`renders empty states for tabs when data is empty`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L192`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L192) (`closes modal when close button is clicked`)

### `[UI-03]` Grouped server view renders category sections and supports collapsible groups
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L61`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L61) (`renders grouped server view by category and allows collapsing`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L88`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L88) (`renders grouped server view by status and type`)

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
* **Verification Proofs (16):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L18) (`AdminPolicy_Allows_Principal_With_AdminGroupName`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L52`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L52) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L86`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L86) (`AdminPolicy_Allows_Principal_With_ConfiguredAdminGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L121`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L121) (`AdminPolicy_Denies_StandardRole_WithoutAdminSidOrGroup`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L21`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L21) (`AdminPolicy_Denies_StandardRole_Without_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L63`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs#L63) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L274`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L274) (`AppKeysController_CreateAppKey_UnknownCategory_Admin_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L226`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L226) (`SecurityValidationHelper_IsAdmin_RequiresAdminGroupSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L244`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L244) (`SecurityValidationHelper_IsAdmin_AllowsAdminGroupName`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L258`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L258) (`SecurityValidationHelper_IsAdmin_RejectsNonAdminGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L275`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L275) (`SecurityValidationHelper_IsAdmin_AllowsCustomAdminGroupsArray`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L290`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L290) (`SecurityValidationHelper_IsAdmin_AllowsMappedGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L305`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L305) (`OidcIdentityProvider_DoesNotGrantAdminSid_FromGroupOrUserNames`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L12`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L12) (`renders Active Directory disabled initially, toggles on and exposes fields`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L46`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L46) (`fills LDAP parameters and executes test connection`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L5) (`Admin Context: renders full administrator view and privileged controls`)

### `[REQ-AUTH-PERSONAL-APPKEY-CREATE]` Non-admin users can create personal App Keys up to quota
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (9):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L199`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L199) (`CreateAppKey_NonAdmin_CreatesPersonalKey_UpToDefaultQuota`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L290`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L290) (`creates category-scoped key, captures one-time plaintext key, and refreshes`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L19`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L19) (`renders nothing when isCreateModalOpen is false`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L61`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L61) (`locks key type to personal key for non-admin and shows quota feedback`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L115`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L115) (`handles scope serialization for server scope and target username for admin`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L154`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L154) (`handles scope serialization for category scope and expiration days`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L192`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L192) (`disables submit button when quota limit is reached`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L217`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L217) (`displays one-time secret result and copies plaintext key to clipboard`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L33`](file:////containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L33) (`Non-Admin Context: mints personal key, views snippet, and revokes key`)

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
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L481`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L481) (`Pairwise_NullOrEmptyTarget_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L503`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L503) (`Pairwise_CorruptedAppKeyScopesJson_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L236`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L236) (`AppKeysController_CreateAppKey_UnknownCategory_NonAdmin_FailsWithBadRequest`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L255`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L255) (`AppKeysController_CreateAppKey_EmptyCategory_FailsWithBadRequest`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L320`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/CategoryScopedAppKeysTests.cs#L320) (`ClientsController_CreateClient_EmptyCategory_ReturnsBadRequest`)

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
* **Verification Proofs (4):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L521`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L521) (`Pairwise_CompleteAsync_MalformedOrMissingBackends_ThrowsOrFailsClosed`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L544`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L544) (`Pairwise_DatabaseDisconnection_FailsClosedSafely`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L177`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L177) (`SchemaValidation_FailsClosed_WhenRequiredColumnOrTableMissing`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L203`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L203) (`SchemaValidation_FailsClosed_WhenUserQuotasOrKeyTypeMissing`)

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

### `[GUARD-ADMIN-CUSTOM-FILES-VALIDATION]` manage_custom_files rejects invalid prompt JSON syntax and unsupported file categories.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L790`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L790) (`ManageCustomFiles_ValidationGuardrails`)

### `[GUARD-ADMIN-ENDPOINT-UNAUTHORIZED]` Unauthenticated / non-admin client request to /admin receives 403 Forbidden.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L196`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L196) (`AdminEndpoint_UnauthorizedCaller_Returns403`)

### `[GUARD-ADMIN-POLICIES-WILDCARD-DENY]` manage_policies rejects wildcard deny policies to prevent global lockout.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L524`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L524) (`ManagePolicies_WildcardDenyGuardrail`)

### `[GUARD-ADMIN-PROVIDERS-LDAP-PLAINTEXT]` manage_providers rejects unencrypted LDAP connections on port 389.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L661`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L661) (`ManageProviders_LdapPlaintextGuardrail`)

### `[GUARD-ADMIN-SERVERS-VALIDATION]` manage_servers rejects invalid transport types, non-existent servers, and missing required parameters.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L335`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L335) (`ManageServers_ValidationGuardrails`)

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

### `[AUTH-106]` Exchange throws InvalidOperationException when request is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L23`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L23) (`Exchange_ThrowsInvalidOperationException_WhenRequestNull`)

### `[AUTH-108]` Authorize throws InvalidOperationException when OIDC request is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L79`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L79) (`Authorize_ThrowsInvalidOperationException_WhenRequestNull`)

### `[SEC-05]` Router must not overwrite corrupt encrypted database fields if an update occurs without user reset.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/McpRouter.Tests/ProviderSettingsEncryptionTests.cs#L395`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ProviderSettingsEncryptionTests.cs#L395) (`SaveSecretProvider_WhenDecryptionFailed_DoesNotOverwriteCorruptPayload`)

### `[REQ-UI-CONFIRM-MODAL]` Deletes an access policy when confirmed via confirm modal.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (14):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L76`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L76) (`deletes a policy when confirmed`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L105`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L105) (`does not delete policy when confirm is cancelled`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L166`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L166) (`deletes a group mapping when confirmed`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L195`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L195) (`does not delete group mapping when confirm is cancelled`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L222`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L222) (`deletes a custom file when confirmed`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L251`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L251) (`does not delete custom file when confirm is cancelled`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L88`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L88) (`prompts confirmation and deletes client when confirmed`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L117`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L117) (`cancels deletion when user denies confirmation`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L350`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L350) (`confirms and revokes AppKey and refreshes list`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L379`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L379) (`cancels revocation when confirm is rejected`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L471`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L471) (`prompts confirmation modal and resets user quota when confirmed`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L501`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L501) (`cancels quota reset when user denies confirmation`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useServerStore.test.ts#L203`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useServerStore.test.ts#L203) (`prompts window.confirm and deletes server when confirmed`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useServerStore.test.ts#L232`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useServerStore.test.ts#L232) (`does not send delete request when confirm is cancelled`)

### `[REQ-UI-TOAST-TRANSITION]` Displays error toast notification when saving invalid JSON credentials for user-provided server.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (8):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L23`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L23) (`shows error toast when saving invalid JSON credentials`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L64`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L64) (`saves valid credentials successfully and closes modal`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/CustomFileModal.test.tsx#L88`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/CustomFileModal.test.tsx#L88) (`shows error toast when switching from invalid JSON to Visual Prompt Builder`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/CustomFileModal.test.tsx#L109`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/CustomFileModal.test.tsx#L109) (`shows error toast when saving without a file name`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/CustomFileModal.test.tsx#L129`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/CustomFileModal.test.tsx#L129) (`shows error toast when saving prompt with invalid JSON content`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L99`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L99) (`saves updated Active Directory configuration JSON`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L138`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L138) (`displays error toast when saving auth providers fails`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L63`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L63) (`displays error toast when saving secret providers fails`)

---

## 4. Complete Verification Traceability Matrix

| Requirement ID | Type | Category | Description | Primary Proof | Suite |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `AUTH-001` | Positive | `AUTH` | Verify DatabaseUserSecretStore encrypts and decrypts secret correctly. | [`UserSecretStoreTests.cs:L13`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserSecretStoreTests.cs#L13) | Backend xUnit |
| `AUTH-002` | Positive | `AUTH` | Verify UserCredentialsController returns configured server IDs. | [`UserCredentialsControllerTests.cs:L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/UserCredentialsControllerTests.cs#L18) | Backend xUnit |
| `AUTH-01` | **Guardrail** | `AUTH` | AdminPolicy allows principal with configured Admin Group Name (e.g., full_admin) | [`AdminPolicyHybridAuthTests.cs:L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs#L18) | Backend xUnit |
| `AUTH-02` | Positive | `AUTH` | AppKey scopes restrict access precisely across all MCP capabilities and backend targets | [`PairwiseIntegrationMatrixTests.cs:L255`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L255) | Backend xUnit |
| `AUTH-03` | Positive | `AUTH` | SSO identity and group mappings resolve Windows SIDs and OIDC claims to internal access roles | [`PairwiseIntegrationMatrixTests.cs:L332`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L332) | Backend xUnit |
| `AUTH-04` | Positive | `AUTH` | ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP | [`ActiveDirectoryWindowsIdentityTests.cs:L17`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ActiveDirectoryWindowsIdentityTests.cs#L17) | Backend xUnit |
| `AUTH-05` | Positive | `AUTH` | McpServer supports AllowPassThroughAuth flag | [`McpServerTests.cs:L9`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/McpServerTests.cs#L9) | Backend xUnit |
| `AUTH-06` | Positive | `AUTH` | Transports use passThroughToken when AllowPassThroughAuth is true | [`TransportsAuthShapeTests.cs:L208`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/TransportsAuthShapeTests.cs#L208) | Backend xUnit |
| `AUTH-101` | Positive | `AUTH` | HTTP transport injects X-Forwarded-User header based on connected user identity. | [`IdentityHeaderTests.cs:L16`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityHeaderTests.cs#L16) | Backend xUnit |
| `AUTH-105` | Positive | `AUTH` | Dynamic Auth Target Pass-Through | [`ToolRoutingManagerTests.cs:L180`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ToolRoutingManagerTests.cs#L180) | Backend xUnit |
| `AUTH-109` | Positive | `AUTH` | ConsentView properly renders the client name from query string and builds correct form action. | [`ConsentView.test.tsx:L16`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/ConsentView.test.tsx#L16) | Frontend Vitest |
| `AUTH-110` | Positive | `AUTH` | CreateAppKey allows creating unlimited AppKeys when UserMaxKeys is set to 0. | [`AppKeysControllerTests.cs:L347`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L347) | Backend xUnit |
| `AUTH-APPKEY-ADMIN-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with admin scope grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L87`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L87) | Backend xUnit |
| `AUTH-APPKEY-ITEMS-SCOPE-ALLOW` | Positive | `AUTH` | SecurityValidationHelper recognizes admin scopes in HttpContext.Items. | [`StandaloneAdminAuthTests.cs:L263`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L263) | Backend xUnit |
| `AUTH-APPKEY-WILDCARD-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L148`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L148) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW` | Positive | `AUTH` | AdminPolicy succeeds in standalone mode for unauthenticated loopback requests. | [`StandaloneAdminAuthTests.cs:L184`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L184) | Backend xUnit |
| `AUTH-STANDALONE-CUSTOM-CIDR-ALLOW` | Positive | `AUTH` | Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges. | [`StandaloneAdminAuthTests.cs:L43`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L43) | Backend xUnit |
| `AUTH-STANDALONE-LOOPBACK-ALLOW` | Positive | `AUTH` | Standalone mode without external IDP grants admin access to loopback IP addresses. | [`StandaloneAdminAuthTests.cs:L22`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L22) | Backend xUnit |
| `REQ-AUTH-PERSONAL-APPKEY-CREATE` | **Guardrail** | `AUTH` | Non-admin users can create personal App Keys up to quota | [`AppKeysControllerTests.cs:L199`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L199) | Backend xUnit |
| `REQ-AUTH-PERSONAL-APPKEY-LIST` | Positive | `AUTH` | Non-admin users can view their personal App Keys | [`AppKeysControllerTests.cs:L133`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L133) | Backend xUnit |
| `REQ-AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE` | Positive | `AUTH` | Custom user quotas override default limit | [`AppKeysControllerTests.cs:L231`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L231) | Backend xUnit |
| `REQ-AUTH-SYSTEM-APPKEY-SEPARATION` | Positive | `AUTH` | System keys are distinct and require admin permissions | [`AppKeysControllerTests.cs:L159`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AppKeysControllerTests.cs#L159) | Backend xUnit |
| `DB-01` | Positive | `DB` | SQLite auto-migration seamlessly upgrades legacy schema, encrypts plaintext secrets, and preserves data | [`DatabaseSchemaUpgradeAndContractTests.cs:L43`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L43) | Backend xUnit |
| `DB-02` | Positive | `DB` | MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly | [`DatabaseSchemaUpgradeAndContractTests.cs:L325`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L325) | Backend xUnit |
| `DOC-SETUP-SKILL-FRONTMATTER` | Positive | `DOC` | mcp-router-setup skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters | [`SetupSkillTests.cs:L22`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L22) | Backend xUnit |
| `DOC-SETUP-SKILL-MIRROR` | Positive | `DOC` | The mcp-router-setup skill and templates are mirrored 1:1 in .agents/skills/mcp-router-setup/ | [`SetupSkillTests.cs:L156`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L156) | Backend xUnit |
| `DOC-SETUP-SKILL-TEMPLATES` | Positive | `DOC` | All scaffold templates exist, are non-empty, and contain required directives such as responseBufferLimit, ROUTER_MASTER_KEY, and ghcr.io/spelech/mcp-router | [`SetupSkillTests.cs:L102`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L102) | Backend xUnit |
| `DOC-SETUP-SKILL-WORKFLOW` | Positive | `DOC` | mcp-router-setup skill contains all 6 required setup phases including environment probing, hosting platforms, env vs UI trade-offs, identity/network topology, artifact generation, and health/client configuration | [`SetupSkillTests.cs:L48`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SetupSkillTests.cs#L48) | Backend xUnit |
| `AUTH-EXTERNAL-IDP-DENIES-ANONYMOUS-LOOPBACK` | **Guardrail** | `GUARD` | When an external IDP is configured, anonymous loopback requests do not bypass authentication. | [`StandaloneAdminAuthTests.cs:L232`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L232) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY` | **Guardrail** | `GUARD` | AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode. | [`StandaloneAdminAuthTests.cs:L208`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L208) | Backend xUnit |
| `AUTH-STANDALONE-EXTERNAL-DENY` | **Guardrail** | `GUARD` | Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey. | [`StandaloneAdminAuthTests.cs:L65`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StandaloneAdminAuthTests.cs#L65) | Backend xUnit |
| `GUARD-01` | **Guardrail** | `GUARD` | Null or empty capability targets must immediately fail closed and return unauthorized | [`PairwiseIntegrationMatrixTests.cs:L481`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L481) | Backend xUnit |
| `GUARD-02` | **Guardrail** | `GUARD` | SSE transport fails closed with SecurityException when secret provider resolution fails | [`SseTransportTests.cs:L39`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L39) | Backend xUnit |
| `GUARD-03` | **Guardrail** | `GUARD` | STDIO transport rejects commands with shell metacharacters or dangerous commands | [`StdioTransportTests.cs:L114`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L114) | Backend xUnit |
| `GUARD-04` | **Guardrail** | `GUARD` | Malformed completion payloads or unmapped backends must fail closed safely | [`PairwiseIntegrationMatrixTests.cs:L521`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L521) | Backend xUnit |
| `GUARD-05` | **Guardrail** | `GUARD` | Batch save of authentication providers must fail closed if all providers are disabled | [`ProvidersControllerTests.cs:L324`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ProvidersControllerTests.cs#L324) | Backend xUnit |
| `GUARD-06` | **Guardrail** | `GUARD` | Global deny policies with TargetId '*' and IsAllowed false must fail closed | [`PermissionsControllerTests.cs:L236`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PermissionsControllerTests.cs#L236) | Backend xUnit |
| `GUARD-ADMIN-CUSTOM-FILES-VALIDATION` | **Guardrail** | `GUARD` | manage_custom_files rejects invalid prompt JSON syntax and unsupported file categories. | [`AdminToolsParityTests.cs:L790`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L790) | Backend xUnit |
| `GUARD-ADMIN-ENDPOINT-UNAUTHORIZED` | **Guardrail** | `GUARD` | Unauthenticated / non-admin client request to /admin receives 403 Forbidden. | [`AdminEndpointsTests.cs:L196`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L196) | Backend xUnit |
| `GUARD-ADMIN-POLICIES-WILDCARD-DENY` | **Guardrail** | `GUARD` | manage_policies rejects wildcard deny policies to prevent global lockout. | [`AdminToolsParityTests.cs:L524`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L524) | Backend xUnit |
| `GUARD-ADMIN-PROVIDERS-LDAP-PLAINTEXT` | **Guardrail** | `GUARD` | manage_providers rejects unencrypted LDAP connections on port 389. | [`AdminToolsParityTests.cs:L661`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L661) | Backend xUnit |
| `GUARD-ADMIN-SERVERS-VALIDATION` | **Guardrail** | `GUARD` | manage_servers rejects invalid transport types, non-existent servers, and missing required parameters. | [`AdminToolsParityTests.cs:L335`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L335) | Backend xUnit |
| `GUARD-ADMIN-UNKNOWN-TOOL` | **Guardrail** | `GUARD` | AdminMcpServer returns an error response for unknown tool or action invocations. | [`AdminMcpServerTests.cs:L602`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L602) | Backend xUnit |
| `MCP-ADMIN-TOOL-TEST-CALL-ERROR` | **Guardrail** | `GUARD` | AdminMcpServer test_tool_call propagates downstream backend errors with visibility. | [`AdminMcpServerTests.cs:L643`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L643) | Backend xUnit |
| `MCP-01` | Positive | `MCP` | Meta-mode execute_tool strictly enforces target tool authorization policies | [`PairwiseIntegrationMatrixTests.cs:L580`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L580) | Backend xUnit |
| `MCP-02` | Positive | `MCP` | All MCP protocol capabilities enforce caller role authorizations consistently | [`PairwiseIntegrationMatrixTests.cs:L398`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PairwiseIntegrationMatrixTests.cs#L398) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-CALL-TOOL` | Positive | `MCP` | Admin endpoint /admin/message executes tools/call for manage_system diagnostics. | [`AdminEndpointsTests.cs:L289`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L289) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-HEAD-REQUEST` | Positive | `MCP` | Admin endpoint /admin handles HEAD request returning text/event-stream headers. | [`AdminEndpointsTests.cs:L215`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L215) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-LIST-TOOLS` | Positive | `MCP` | Admin endpoint /admin/message executes tools/list over active SSE session and returns 10 admin tools. | [`AdminEndpointsTests.cs:L227`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L227) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET` | Positive | `MCP` | Target proxy endpoint /router-admin routes directly to the Admin MCP server. | [`AdminEndpointsTests.cs:L152`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L152) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE` | Positive | `MCP` | Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version. | [`AdminEndpointsTests.cs:L73`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminEndpointsTests.cs#L73) | Backend xUnit |
| `MCP-ADMIN-INITIALIZE-HANDSHAKE` | Positive | `MCP` | AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05. | [`AdminMcpServerTests.cs:L205`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L205) | Backend xUnit |
| `MCP-ADMIN-PARITY-APPKEYS` | Positive | `MCP` | manage_appkeys supports full parity for list, get_limits, create, and revoke actions. | [`AdminToolsParityTests.cs:L381`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L381) | Backend xUnit |
| `MCP-ADMIN-PARITY-CLIENTS` | Positive | `MCP` | manage_clients supports full parity for register, list, and delete actions. | [`AdminToolsParityTests.cs:L434`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L434) | Backend xUnit |
| `MCP-ADMIN-PARITY-CUSTOM-FILES` | Positive | `MCP` | manage_custom_files supports full parity for list, get, save, and delete prompt and resource files. | [`AdminToolsParityTests.cs:L727`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L727) | Backend xUnit |
| `MCP-ADMIN-PARITY-GROUP-MAPPINGS` | Positive | `MCP` | manage_group_mappings supports full parity for list, save, and delete external-to-internal group mappings. | [`AdminToolsParityTests.cs:L541`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L541) | Backend xUnit |
| `MCP-ADMIN-PARITY-JSONRPC-DISPATCH` | Positive | `MCP` | AdminMcpServer processes standard JSON-RPC 2.0 requests (tools/list, tools/call, ping). | [`AdminToolsParityTests.cs:L884`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L884) | Backend xUnit |
| `MCP-ADMIN-PARITY-POLICIES` | Positive | `MCP` | manage_policies supports full parity for list, save, and delete access control policies. | [`AdminToolsParityTests.cs:L475`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L475) | Backend xUnit |
| `MCP-ADMIN-PARITY-PROVIDERS` | Positive | `MCP` | manage_providers supports full parity for list, save_secret, test_vault, save_auth, and test_ldap actions. | [`AdminToolsParityTests.cs:L588`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L588) | Backend xUnit |
| `MCP-ADMIN-PARITY-SERVERS` | Positive | `MCP` | manage_servers supports full parity for list, get, create, update, toggle, delete, reconnect, and reconnect_all actions. | [`AdminToolsParityTests.cs:L245`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L245) | Backend xUnit |
| `MCP-ADMIN-PARITY-SETTINGS` | Positive | `MCP` | manage_settings supports full parity for get and update global router configurations. | [`AdminToolsParityTests.cs:L678`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L678) | Backend xUnit |
| `MCP-ADMIN-PARITY-SYSTEM` | Positive | `MCP` | manage_system supports full parity for diagnostics, get_logs, clear_logs, and query_audit actions. | [`AdminToolsParityTests.cs:L827`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L827) | Backend xUnit |
| `MCP-ADMIN-PARITY-TEST-TOOL-CALL` | Positive | `MCP` | test_tool_call executes test bench backend tool calls and formats responses. | [`AdminToolsParityTests.cs:L857`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L857) | Backend xUnit |
| `MCP-ADMIN-PARITY-TOOLS-COVERAGE` | Positive | `MCP` | Every UI management flow has a corresponding verified action in the consolidated Admin MCP tools. | [`AdminToolsParityTests.cs:L202`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminToolsParityTests.cs#L202) | Backend xUnit |
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
| `AUTH-106` | **Guardrail** | `SEC` | Exchange throws InvalidOperationException when request is null. | [`AuthorizationControllerTests.cs:L23`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L23) | Backend xUnit |
| `AUTH-107` | Positive | `SEC` | RegisterClient successfully handles DCR requests when open DCR is enabled. | [`AuthorizationControllerTests.cs:L41`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L41) | Backend xUnit |
| `AUTH-108` | **Guardrail** | `SEC` | Authorize throws InvalidOperationException when OIDC request is null. | [`AuthorizationControllerTests.cs:L79`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AuthorizationControllerTests.cs#L79) | Backend xUnit |
| `SEC-01` | Positive | `SEC` | VaultSecretRetriever authenticates with HashiCorp Vault using AppRole RoleID and SecretID credentials | [`VaultAppRoleAndRenewalTests.cs:L23`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/VaultAppRoleAndRenewalTests.cs#L23) | Backend xUnit |
| `SEC-02` | Positive | `SEC` | STDIO transport securely injects secret credentials via environment variables rather than command-line arguments | [`StdioTransportTests.cs:L354`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L354) | Backend xUnit |
| `SEC-03` | Positive | `SEC` | Ensure TrustedProxyHelper supports CIDR ranges in XFF validation | [`IdentityProviderTests.cs:L366`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs#L366) | Backend xUnit |
| `SEC-04` | Positive | `SEC` | WindowsRegistrySecretRetriever securely decrypts DPAPI LocalMachine machine-level encrypted binary values and retrieves plaintext registry strings | [`WindowsRegistrySecretRetrieverTests.cs:L16`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/WindowsRegistrySecretRetrieverTests.cs#L16) | Backend xUnit |
| `SEC-05` | **Guardrail** | `SEC` | Router must not overwrite corrupt encrypted database fields if an update occurs without user reset. | [`ProviderSettingsEncryptionTests.cs:L395`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/ProviderSettingsEncryptionTests.cs#L395) | Backend xUnit |
| `SEC-ADMIN-AUDIT-REDACTION` | Positive | `SEC` | AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs. | [`AdminMcpServerTests.cs:L619`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/AdminMcpServerTests.cs#L619) | Backend xUnit |
| `TRANS-01` | Positive | `TRANS` | SSE transport resolves static plaintext API keys when provider is None | [`SseTransportTests.cs:L18`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/SseTransportTests.cs#L18) | Backend xUnit |
| `TRANS-02` | Positive | `TRANS` | HTTP stateless transport resolves static API keys when secret provider is None | [`HttpTransportTests.cs:L19`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/HttpTransportTests.cs#L19) | Backend xUnit |
| `TRANS-03` | Positive | `TRANS` | STDIO transport spawns subprocess, handles JSON-RPC initialization and executes tool calls | [`StdioTransportTests.cs:L59`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/StdioTransportTests.cs#L59) | Backend xUnit |
| `REQ-UI-CONFIRM-MODAL` | **Guardrail** | `UI` | Deletes an access policy when confirmed via confirm modal. | [`usePolicyStore.test.ts:L76`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L76) | Frontend Vitest |
| `REQ-UI-TOAST-TRANSITION` | **Guardrail** | `UI` | Displays error toast notification when saving invalid JSON credentials for user-provided server. | [`MyMcpServers.test.tsx:L23`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L23) | Frontend Vitest |
| `UI-01` | Positive | `UI` | Dashboard renders stats card, connected server list, and setup instructions | [`DashboardView.test.tsx:L38`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L38) | Frontend Vitest |
| `UI-02` | Positive | `UI` | Modal remains hidden when isInspectOpen is false | [`ServerInspectModal.test.tsx:L47`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L47) | Frontend Vitest |
| `UI-03` | Positive | `UI` | Grouped server view renders category sections and supports collapsible groups | [`DashboardView.test.tsx:L61`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L61) | Frontend Vitest |
| `UI-04` | Positive | `UI` | Interactive tool tester renders server and tool selection dropdowns | [`ToolTesterCard.test.tsx:L41`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L41) | Frontend Vitest |
| `UI-05` | Positive | `UI` | Router allows customized branding parameters (DashboardTitle, DashboardIcon) to be saved and retrieved via the API. | [`PipelineIntegrationTests.cs:L242`](file:////containers/dev/csharp-mcp-router/McpRouter.Tests/PipelineIntegrationTests.cs#L242) | Backend xUnit |
