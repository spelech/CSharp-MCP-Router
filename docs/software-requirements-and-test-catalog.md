# Software Requirements Specification (SRS) & Test Verification Catalog

> **Automated Verification Document:** Generated via `dotnet run --project scripts/CatalogGenerator`
> **Catalog Statistics:** **134 Requirements Verified** across **295 Test Proofs** (111 Functional Capabilities, 23 Safety Guardrails).

---

## 1. System Taxonomy & Verification Summary

| Category | Domain | Total Requirements | Positive Features | Guardrails / Fail-Closed | Verification Proofs |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **`AUTH`** | Authentication, RBAC & Identity | **27** | 25 | 2 | 87 proofs |
| **`CORE`** | CORE | **1** | 1 | 0 | 9 proofs |
| **`DB`** | Multi-Database Persistence & Migrations | **2** | 2 | 0 | 10 proofs |
| **`DOC`** | DOC | **4** | 4 | 0 | 4 proofs |
| **`GUARD`** | Universal Safety & Fail-Closed Guardrails | **16** | 0 | 16 | 35 proofs |
| **`MCP`** | Model Context Protocol Engine & Tool Routing | **36** | 36 | 0 | 37 proofs |
| **`SEC`** | Secrets Providers & Encryption | **31** | 28 | 3 | 48 proofs |
| **`TRANS`** | Transports (SSE, HTTP, STDIO, Proxy) | **3** | 3 | 0 | 9 proofs |
| **`UI`** | Dashboard, Test Bench & Settings UI | **14** | 12 | 2 | 56 proofs |

---

## 2. Functional Requirements ("What the Application DOES")

### `[AUTH-001]` Verify DatabaseUserSecretStore encrypts and decrypts secret correctly.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserSecretStoreTests.cs#L8`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserSecretStoreTests.cs#L8) (`DatabaseUserSecretStore_SavesAndRetrieves_Secret`)

### `[AUTH-002]` Verify UserCredentialsController returns configured server IDs.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserCredentialsControllerTests.cs#L11`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserCredentialsControllerTests.cs#L11) (`GetUserCredentials_ReturnsServerIds`)

### `[AUTH-02]` AppKey scopes restrict access precisely across all MCP capabilities and backend targets
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (12):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L242`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L242) (`Pairwise_AppKeyScopes_RestrictsAccessPrecisely`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L146`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L146) (`AppKeyRepository_SaveAndGet_PersistsKeyTypeAndFilters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L194`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L194) (`AppKeysController_CreateAppKey_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L278`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L278) (`ClientsController_CreateClient_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L361`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L361) (`ClientSession_CategoryScope_AuthorizesMatchingServerTools_AndDeniesOthers`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L385`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L385) (`ClientSession_GroupAliasScope_AuthorizesIdenticallyToCategory`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L407`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L407) (`ClientSession_CategoryScope_IsCaseInsensitive`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L457`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L457) (`ClientSession_ResourcesAndTemplates_FilteredByCategoryScope`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L485`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L485) (`ClientSession_DynamicServerMembership_UpdatesAccessDynamically`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L521`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L521) (`ClientSession_MixedScopes_CombinesCategoryAndSpecificToolScopes`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L548`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L548) (`ClientSession_Complete_FiltersServerNamesByCategoryScope`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L70`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L70) (`AppKey Direct Context: connects with API key header identity`)

### `[AUTH-03]` SSO identity and group mappings resolve Windows SIDs and OIDC claims to internal access roles
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L319`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L319) (`Pairwise_SsoIdentityAndGroupMappings_EvaluateCorrectly`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L36`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L36) (`Operator Context: allows overview and testbench navigation with operator identity`)

### `[AUTH-04]` ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L12`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L12) (`ResolveIdentityAsync_ExtractsWindowsIdentitySids_ViaAccessor`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L50`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L50) (`ResolveIdentityAsync_AugmentsWithLdapSids_WhenLdapServiceProvided`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/ldap-identity-and-auth-flow.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/ldap-identity-and-auth-flow.spec.ts#L5) (`should configure LDAP identity provider, test connection, and save settings`)

### `[AUTH-05]` McpServer supports AllowPassThroughAuth flag
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/McpServerTests.cs#L5`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/McpServerTests.cs#L5) (`McpServer_Should_Have_AllowPassThroughAuth`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/my-mcp-servers.spec.ts#L7`](file:////containers/dev/csharp-mcp-router/frontend/e2e/my-mcp-servers.spec.ts#L7) (`should render user provided servers and allow editing credentials with SQLite schema`)

### `[AUTH-06]` Transports use passThroughToken when AllowPassThroughAuth is true
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L200`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L200) (`Transports_Use_PassThroughToken_If_Allowed`)

### `[AUTH-101]` HTTP transport injects X-Forwarded-User header based on connected user identity.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityHeaderTests.cs#L9`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityHeaderTests.cs#L9) (`HttpTransport_InjectsXForwardedUserHeader`)

### `[AUTH-105]` Dynamic Auth Target Pass-Through
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173) (`ExecuteTargetToolAsync_Catches401_AndReturnsAuthPrompt`)

### `[AUTH-110]` CreateAppKey allows creating unlimited AppKeys when UserMaxKeys is set to 0.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L339`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L339) (`CreateAppKey_AllowsUnlimited_WhenLimitsAreZero`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L129`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L129) (`ApplyConfigurationResponseContext_SetsRegistrationEndpoint`)

### `[AUTH-APPKEY-ADMIN-SCOPE-ALLOW]` AppKeys with admin scope grant Administrator role and pass AdminPolicy.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L79`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L79) (`AppKey_WithAdminScope_GrantsAdminAccess`)

### `[AUTH-APPKEY-ITEMS-SCOPE-ALLOW]` SecurityValidationHelper recognizes admin scopes in HttpContext.Items.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L255`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L255) (`IsAdmin_AppKeyScopes_InHttpContextItems_ReturnsTrue`)

### `[AUTH-APPKEY-WILDCARD-SCOPE-ALLOW]` AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L140`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L140) (`AppKey_WithWildcardScope_GrantsAdminAccess`)

### `[AUTH-COMPACT-APPKEY-TAXONOMY]` Generates compact ~32-character Base62 AppKeys with semantic prefixes.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L409`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L409) (`CreateCredentialAsync_GeneratesCompactKeysWithSemanticPrefixes`)

### `[AUTH-CUSTOM-ADMIN-KEY-SEEDING]` Seeds custom MCG_ADMIN_AUTH_KEY when provided in configuration.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L186`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L186) (`Startup_SeedsCustomAdminKey_WhenConfigured`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L238`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L238) (`Startup_SeedsCustomAdminKey_WhenMcgAdminKeyConfigured`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L289`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L289) (`Startup_UpdatesAdminKeyHash_WhenEnvironmentKeyChanges`)

### `[AUTH-PERSONAL-APPKEY-LIST]` Non-admin users can view their personal App Keys
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L125`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L125) (`GetAppKeys_NonAdmin_ReturnsOnlyPersonalKeys_ForCurrentUser`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L232`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L232) (`loads app keys and updates store`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L81`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L81) (`renders role-adaptive UI for non-admin user`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L34`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L34) (`renders role-adapted My App Keys view for non-admin user`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L79`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L79) (`renders keys list, copies config snippet, and revokes key`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L5) (`Non-Admin Context: displays My App Keys navigation and personal quota indicator`)

### `[AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE]` Custom user quotas override default limit
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L223`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L223) (`CreateAppKey_CustomQuotaOverride_AllowsHigherLimit`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L439`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L439) (`sets user quota override and refreshes quota list`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L6`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L6) (`renders GeneralTab with security default quota inputs and triggers save`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L71`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L71) (`updates form state when settings prop changes`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L222`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L222) (`manages custom user quotas in admin quotas tab`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L135`](file:////containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L135) (`Admin Context: configures custom user quota override`)

### `[AUTH-PREFIX-EXTRACTION]` ExtractKeyPrefix parses semantic prefixes, Base62 selectors, and legacy tokens accurately.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L443`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L443) (`ExtractKeyPrefix_ExtractsSemanticAndLegacyPrefixesAccurately`)

### `[AUTH-QUERY-TOKEN-EXTRACTION]` Query string token middleware extracts access_token or token query parameter to Authorization header.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L7`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L7) (`QueryStringTokenMiddleware_Extracts_AccessToken_To_AuthorizationHeader`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L45`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L45) (`QueryStringTokenMiddleware_Extracts_Token_To_AuthorizationHeader`)

### `[AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW]` AdminPolicy succeeds in standalone mode for unauthenticated loopback requests.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L176`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L176) (`AdminPolicy_StandaloneMode_LoopbackIp_PassesAdminPolicy`)

### `[AUTH-STANDALONE-CUSTOM-CIDR-ALLOW]` Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L35`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L35) (`IsAdmin_StandaloneMode_CustomCidr_ReturnsTrue`)

### `[AUTH-STANDALONE-LOOPBACK-ALLOW]` Standalone mode without external IDP grants admin access to loopback IP addresses.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L14`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L14) (`IsAdmin_StandaloneMode_LoopbackIp_ReturnsTrue`)

### `[AUTH-SYSTEM-APPKEY-SEPARATION]` System keys are distinct and require admin permissions
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (10):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L151`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L151) (`SystemAppKeys_RequireAdmin_AndSeparateFromPersonalKeys`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L303`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L303) (`PersonalAppKey_WithAllScope_DoesNotGrantAdministratorRole`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L356`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L356) (`SystemAppKey_WithAdminScope_GrantsAdministratorRole`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L216`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L216) (`switches keyTypeTab between personal and system`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L250`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/useClientStore.test.ts#L250) (`fetches system-filtered app keys via query parameters`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L15`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L15) (`renders header, navigation tabs, and default overview dashboard for admin user`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L36`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/App.test.tsx#L36) (`switches between tabs on navigation click`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L31`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeyModal.test.tsx#L31) (`allows admin to select key type and create system app key`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L166`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/AppKeysCard.test.tsx#L166) (`handles admin tab switching and username filtering`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L87`](file:////containers/dev/csharp-mcp-router/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L87) (`Admin Context: manages segmented App-Level Keys and User Personal Keys`)

### `[UI-120]` RBAC and SID mapping administration UI allows configuring role policies and SID associations
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/rbac-enforcement-flow.spec.ts#L1`](file:////containers/dev/csharp-mcp-router/frontend/e2e/rbac-enforcement-flow.spec.ts#L1) (`should create, verify, and delete RBAC policy and SID mapping`)

### `[UI-125]` Admin role renders full administrative dashboard and server management controls
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L1`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L1) (`Admin Context: renders full administrator view and privileged controls`)

### `[CORE-101]` Auto-added requirement tracking
* **Category:** `CORE` (CORE)
* **Type:** Positive Feature Capability
* **Verification Proofs (9):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SessionManagerTests.cs#L9`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SessionManagerTests.cs#L9) (`PerformanceMetrics_And_TotalRequests_IncrementCorrectly`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SessionManagerTests.cs#L33`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SessionManagerTests.cs#L33) (`UpdateBackendStatus_TracksBackendHealth`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L34`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L34) (`ListToolsAsync_ReturnsMetaTools_InMetaMode`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L55`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L55) (`InvalidateCache_ClearsPopulatedState`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L64`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L64) (`CallToolAsync_SearchTools_ReturnsSemanticResults`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L93`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L93) (`CallToolAsync_ExecuteTool_ReturnsError_WhenNameMissing`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L120`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L120) (`CallToolAsync_ReturnsCancellationError_WhenCancelled`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L149`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L149) (`CallToolAsync_ThrowsKeyNotFound_WhenToolNotInRoutingTable`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173) (`ExecuteTargetToolAsync_Catches401_AndReturnsAuthPrompt`)

### `[DB-01]` SQLite auto-migration seamlessly upgrades legacy schema, encrypts plaintext secrets, and preserves data
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L29`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L29) (`Sqlite_UpgradeMigration_FromLegacySchema_PreservesDataAndPassesValidation`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L85`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L85) (`UserQuotaRepository_SetAndGet_ReturnsPersistedQuota`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L98`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L98) (`UserQuotaRepository_GetAll_ReturnsAllUserQuotas`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L117`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L117) (`UserQuotaRepository_Update_UpdatesExistingQuota`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L132`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L132) (`UserQuotaRepository_Delete_RemovesQuota`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L199`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L199) (`DependencyInjection_RegistersIUserQuotaRepository`)

### `[DB-02]` MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L311`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L311) (`Mssql_Scripts_DeclareAllProceduresAndExpectedParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L356`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L356) (`MySql_Scripts_DeclareAllProceduresWithP_PrefixParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L406`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L406) (`Repositories_MySQL_AppKeyOperations_UseP_PrefixParameters`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MySqlLiveIntegrationTests.cs#L25`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MySqlLiveIntegrationTests.cs#L25) (`MySql_LiveRepository_AppKeyAndSecretProviderLifecycle_Succeeds`)

### `[DOC-SETUP-SKILL-FRONTMATTER]` mcg-setup skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L18`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L18) (`Skill_Frontmatter_IsValidAndWithinCharacterLimit`)

### `[DOC-SETUP-SKILL-MIRROR]` The mcg-setup skill and templates are mirrored 1:1 in .agents/skills/mcg-setup/
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L152`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L152) (`Skill_MirroredInAgentsDirectory`)

### `[DOC-SETUP-SKILL-TEMPLATES]` All scaffold templates exist, are non-empty, and contain required directives such as responseBufferLimit, MCG_MASTER_KEY, and ghcr.io/spelech/model-context-gateway
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L98`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L98) (`Templates_AreValidAndContainRequiredDirectives`)

### `[DOC-SETUP-SKILL-WORKFLOW]` mcg-setup skill contains all 6 required setup phases including environment probing, hosting platforms, env vs UI trade-offs, identity/network topology, artifact generation, and health/client configuration
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L44`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L44) (`Skill_ContainsAllRequiredPhasesAndComparisons`)

### `[MCP-01]` Meta-mode execute_tool strictly enforces target tool authorization policies
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L567`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L567) (`Pairwise_MetaMode_ExecuteTool_EnforcesTargetAuthorization`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L425`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L425) (`ClientSession_ExecuteTool_EnforcesCategoryScopeOnInnerTarget`)

### `[MCP-02]` All MCP protocol capabilities enforce caller role authorizations consistently
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L385`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L385) (`Pairwise_AllCapabilities_UnderCallerRoles_EvaluateCorrectly`)

### `[MCP-ADMIN-ENDPOINT-CALL-TOOL]` Admin endpoint /admin/message executes tools/call for manage_system diagnostics.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L294`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L294) (`AdminEndpoint_SseSession_CallTool_ManageSystemDiagnostics`)

### `[MCP-ADMIN-ENDPOINT-HEAD-REQUEST]` Admin endpoint /admin handles HEAD request returning text/event-stream headers.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L212`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L212) (`AdminEndpoint_HeadRequest_ReturnsEventStreamHeaders`)

### `[MCP-ADMIN-ENDPOINT-LIST-TOOLS]` Admin endpoint /admin/message executes tools/list over active SSE session and returns 10 admin tools.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L224`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L224) (`AdminEndpoint_SseSession_ListTools`)

### `[MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET]` Target proxy endpoint /router-admin routes directly to the Admin MCP server.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L149`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L149) (`TargetProxy_RouterAdmin_RoutesToAdminServer`)

### `[MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE]` Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L62`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L62) (`AdminEndpoint_SseHandshake_NegotiatesProtocol`)

### `[MCP-ADMIN-INITIALIZE-HANDSHAKE]` AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L195`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L195) (`HandleInitializeAsync_NegotiatesProtocolVersion`)

### `[MCP-ADMIN-PARITY-APPKEYS]` manage_appkeys supports full parity for list, get_limits, create, and revoke actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L370`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L370) (`ManageAppKeys_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-CLIENTS]` manage_clients supports full parity for register, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L423`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L423) (`ManageClients_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-CUSTOM-FILES]` manage_custom_files supports full parity for list, get, save, and delete prompt and resource files.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L716`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L716) (`ManageCustomFiles_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-GROUP-MAPPINGS]` manage_group_mappings supports full parity for list, save, and delete external-to-internal group mappings.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L530`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L530) (`ManageGroupMappings_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-JSONRPC-DISPATCH]` AdminMcpServer processes standard JSON-RPC 2.0 requests (tools/list, tools/call, ping).
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L873`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L873) (`AdminTools_ProcessRequest_JsonRpcProtocol`)

### `[MCP-ADMIN-PARITY-POLICIES]` manage_policies supports full parity for list, save, and delete access control policies.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L464`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L464) (`ManagePolicies_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-PROVIDERS]` manage_providers supports full parity for list, save_secret, test_vault, save_auth, and test_ldap actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L577`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L577) (`ManageProviders_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SERVERS]` Validates that the manage_servers tool provides comprehensive administrative capabilities including listing, retrieving, creating, updating, toggling, deleting, and reconnecting servers.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L234`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L234) (`ManageServers_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SETTINGS]` manage_settings supports full parity for get and update global router configurations.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L667`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L667) (`ManageSettings_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SYSTEM]` manage_system supports full parity for diagnostics, get_logs, clear_logs, and query_audit actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L816`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L816) (`ManageSystem_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-TEST-TOOL-CALL]` test_tool_call executes test bench backend tool calls and formats responses.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L846`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L846) (`TestToolCall_Execution_Parity`)

### `[MCP-ADMIN-PARITY-TOOLS-COVERAGE]` Ensures every UI management workflow is backed by a verified, equivalent action within the consolidated Admin MCP tools.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L191`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L191) (`AdminTools_ExecuteSuccessfully`)

### `[MCP-ADMIN-SKILL-E2E-PROVISIONING]` Admin automation templates and JSON-RPC tool calls successfully provision a blank-slate gateway instance end-to-end via HTTP /admin/message.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L176`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L176) (`EndToEnd_BlankSlateProvisioning_ConfiguresAllEntitiesViaAdminTools`)

### `[MCP-ADMIN-SKILL-FRONTMATTER]` mcg-admin skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L21`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L21) (`Skill_Frontmatter_IsValidAndWithinCharacterLimit`)

### `[MCP-ADMIN-SKILL-MIRROR]` mcg-admin skill files and templates are identically mirrored between skills/ and .agents/skills/ directories
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L147`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L147) (`Skill_MirroredInAgentsDirectory`)

### `[MCP-ADMIN-SKILL-TEMPLATES]` All mcg-admin scaffold templates exist, are non-empty, and contain valid JSON or scripts for Authentik, Keycloak, Entra, ActiveDirectory, Cloudflare, Vault, Embeddings, Docker, and shell automation
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L103`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L103) (`Templates_AllExistAndAreValidJsonOrScripts`)

### `[MCP-ADMIN-SKILL-WORKFLOW]` mcg-admin skill contains all 7 administration phases including diagnostics, secrets, auth providers, RBAC/group mappings, settings/embeddings, servers/clients, and live tool verification
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L47`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L47) (`Skill_ContainsAllRequiredPhasesAndProviderCookbooks`)

### `[MCP-ADMIN-TOOL-AUDIT-LOG]` AdminMcpServer tool calls record audit log entries with caller and tool name.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L266`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L266) (`CallToolAsync_RecordsAuditLog`)

### `[MCP-ADMIN-TOOL-MANAGE-APPKEYS]` AdminMcpServer executes manage_appkeys create, list, limits, and revoke actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L283`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L283) (`CallToolAsync_ManageAppKeys_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-CLIENTS]` AdminMcpServer executes manage_clients register, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L330`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L330) (`CallToolAsync_ManageClients_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-CUSTOM-FILES]` AdminMcpServer executes manage_custom_files save, get, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L504`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L504) (`CallToolAsync_ManageCustomFiles_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-GROUP-MAPPINGS]` AdminMcpServer executes manage_group_mappings save, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L402`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L402) (`CallToolAsync_ManageGroupMappings_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-POLICIES]` AdminMcpServer executes manage_policies save, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L368`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L368) (`CallToolAsync_ManagePolicies_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-PROVIDERS]` AdminMcpServer executes manage_providers list, save_secret, and save_auth actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L435`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L435) (`CallToolAsync_ManageProviders_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-SERVERS]` AdminMcpServer executes manage_servers list, get, create, update, toggle, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L214`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L214) (`CallToolAsync_ManageServers_ListAndCreate`)

### `[MCP-ADMIN-TOOL-MANAGE-SETTINGS]` AdminMcpServer executes manage_settings get and update actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L478`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L478) (`CallToolAsync_ManageSettings_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-SYSTEM]` AdminMcpServer executes manage_system diagnostics, get_logs, clear_logs, and query_audit actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L558`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L558) (`CallToolAsync_ManageSystem_Lifecycle`)

### `[MCP-ADMIN-TOOLS-LIST-COUNT]` AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L147`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L147) (`ListToolsAsync_ReturnsTenConsolidatedTools`)

### `[AUTH-107]` RegisterClient successfully handles DCR requests when open DCR is enabled.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L33`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L33) (`RegisterClient_CreatesApplicationAndReturnsOk`)

### `[AUTH-109]` RegisterClient uses ICredentialService when IOpenIddictApplicationManager is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L88`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L88) (`RegisterClient_UsesCredentialService_WhenApplicationManagerNull`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/pages/ConsentView.test.tsx#L16`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/ConsentView.test.tsx#L16) (`renders client name from query string and sets form action`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/oauth-consent-flow.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/oauth-consent-flow.spec.ts#L5) (`should render interactive OAuth consent screen and display requesting client name`)

### `[AUTH-111]` Pipeline exposes RFC 9728 OAuth Protected Resource discovery endpoints with dynamic resource identifiers.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L61`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L61) (`Pipeline_WellKnown_Endpoints_ReturnSuccess`)

### `[SEC-01]` VaultSecretRetriever authenticates with HashiCorp Vault using AppRole RoleID and SecretID credentials
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L14`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L14) (`EnsureVaultClientAsync_CreatesClient_WithAppRoleCredentials`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L35`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L35) (`EnsureVaultClientAsync_LoadsFromSecretRepo_WhenConfigJsonHasAppRole`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L87`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L87) (`ReloadConfigAsync_ClearsClient_ForcesRecreation`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L19`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L19) (`renders provider inputs and submits updated configuration`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L90`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/SecretProvidersTab.test.tsx#L90) (`handles Test Vault connection button with success and failure responses`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-sse-vault.spec.ts#L8`](file:////containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-sse-vault.spec.ts#L8) (`should register SSE server with Vault provider (Mount/Path/Field), verify badge, and run semantic search`)

### `[SEC-02]` STDIO transport securely injects secret credentials via environment variables rather than command-line arguments
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L344`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L344) (`StdioTransport_ShouldPassSecretViaEnvironmentVariables_AndNotCommandLine`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L429`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L429) (`StdioTransport_ShouldSanitizeAndMaskSecretsInLogs`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/vault-approle-config-flow.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/vault-approle-config-flow.spec.ts#L5) (`should configure Vault AppRole credentials and test connection in settings`)

### `[SEC-03]` Ensure TrustedProxyHelper supports CIDR ranges in XFF validation
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L360`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L360) (`TrustedProxyHelper_AllowsXForwardedFor_WhenChainIsFullyTrusted`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L419`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L419) (`TrustedProxyHelper_ConfiguredProxyTrusted_CIDR`)

### `[SEC-04]` WindowsRegistrySecretRetriever securely decrypts DPAPI LocalMachine machine-level encrypted binary values and retrieves plaintext registry strings
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L12`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L12) (`GetSecretAsync_ReturnsPlainString_WhenRegistryValueIsString`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L27`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L27) (`GetSecretAsync_DecryptsDpapiBytes_WhenRegistryValueIsByteArray`)

### `[SEC-ADMIN-AUDIT-REDACTION]` AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L609`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L609) (`CallToolAsync_AuditLog_RedactsSensitivePayloadData`)

### `[SEC-GATEWAY-ZERO-CONFIG-BOOT]` Gateway boots from a blank slate with zero master key environment variables, auto-generates .master.key, and serves health and admin endpoints.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L352`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L352) (`Gateway_BlankSlate_WithoutMasterKeyEnv_AutoGeneratesKeyFileAndBootsSuccessfully`)

### `[SEC-KEY-PROVIDER-AUTOGEN]` EncryptionKeyProvider delegates to DbKeyHelper to auto-generate master key when unconfigured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L42`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L42) (`GetDbEncryptionKey_AutoGenerates_WhenUnconfigured`)

### `[SEC-KEY-PROVIDER-CONFIG]` EncryptionKeyProvider returns configured DB_ENCRYPTION_KEY or MCG_SECRET.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L28`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L28) (`GetDbEncryptionKey_UsesConfig_WhenProvided`)

### `[SEC-KEY-PROVIDER-FALLBACK]` EncryptionKeyProvider falls back to DB_ENCRYPTION_KEY when MCG_SECRET is unconfigured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L70`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L70) (`GetRouterSecret_FallsBackToDbEncryptionKey_WhenDbEncryptionKeyProvided`)

### `[SEC-KEY-PROVIDER-SECRET]` EncryptionKeyProvider returns configured MCG_SECRET.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L56`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L56) (`GetRouterSecret_UsesConfig_WhenProvided`)

### `[SEC-KEYFILE-AUTOGEN]` Blank-slate initialization auto-generates a 256-bit base64 master key and persists it to .master.key.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L63`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L63) (`ResolveDbEncryptionKey_AutoGeneratesAndPersistsKey_WhenBlankSlate`)

### `[SEC-KEYFILE-ENV-PRECEDENCE]` Explicit environment variables MCG_MASTER_KEY or MCG_SECRET take precedence over keyfiles.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L28`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L28) (`ResolveDbEncryptionKey_ReturnsConfiguredEnvKey_WhenPresent`)

### `[SEC-KEYFILE-FILE-OVER-KEYFILE]` Explicit file secrets take precedence over persistent .master.key files.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L123`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L123) (`ResolveDbEncryptionKey_FileSecretTakesPrecedenceOverKeyFile`)

### `[SEC-KEYFILE-FILE-SECRET]` File-based secrets configured via MCG_MASTER_KEY_FILE or standard Docker secrets paths are resolved.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L45`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L45) (`ResolveDbEncryptionKey_ReturnsFileSecret_WhenKeyFileSpecified`)

### `[SEC-KEYFILE-HIERARCHY-PRECEDENCE]` Explicit environment variables take precedence over file secrets and keyfiles.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L101`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L101) (`ResolveDbEncryptionKey_EnvVarTakesPrecedenceOverFileSecretAndKeyFile`)

### `[SEC-KEYFILE-RELOAD]` Existing .master.key file is loaded across gateway restarts without key mutation.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L83`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L83) (`ResolveDbEncryptionKey_LoadsExistingKeyFile_OnSubsequentBoot`)

### `[SEC-KEYSOURCE-DETECTION]` Correctly identifies KeySource origin for environment, file, and auto-generated keys.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L144`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L144) (`ResolveDbEncryptionKey_IdentifiesKeySourceAccurately`)

### `[SEC-KEYSOURCE-SETCACHEDKEY]` SetCachedKey sets in-memory encryption key and updates ActiveKeySource.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L314`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L314) (`SetCachedKey_UpdatesCachedKeyAndActiveKeySource`)

### `[SEC-MASTERKEY-ATOMIC-REENCRYPTION]` Atomically re-encrypts database credentials when setting a custom master key.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L142`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L142) (`SetMasterKey_AtomicallyReEncryptsDatabaseCredentials`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L241`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L241) (`SetMasterKey_RejectsWhenKeySourceIsExternalOrVault`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L259`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L259) (`AdminMcpServer_ManageSystem_SetMasterKey_ReencryptsCleanly`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L338`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L338) (`SetMasterKey_RejectsInvalidOrShortKeys`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L452`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L452) (`Pipeline_POST_MasterKey_RejectsWhenExternalKeySource`)

### `[SEC-MASTERKEY-CONFIGURED-STATUS-BADGE]` Displays configured badge and rotate button when custom master key is configured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L192`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L192) (`renders configured badge and rotate key button when master key is Configured`)

### `[SEC-MASTERKEY-CUSTOM-MODAL-REENCRYPTION]` Validates master key inputs (length, match) and triggers atomic re-encryption.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/MasterKeyModal.test.tsx#L6`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/MasterKeyModal.test.tsx#L6) (`validates key inputs and submits custom master key to callback`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/MasterKeyModal.test.tsx#L54`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/MasterKeyModal.test.tsx#L54) (`generates a strong random master key when auto-generate button is clicked`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/MasterKeyModal.test.tsx#L85`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/MasterKeyModal.test.tsx#L85) (`displays validation error when onSetMasterKey returns failure`)

### `[SEC-MASTERKEY-EXTERNAL-LOCKED-BADGE]` Displays locked badge when master key is externally managed via Vault or Environment.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L149`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L149) (`renders locked badge when master key is managed externally`)

### `[SEC-MASTERKEY-UI-STATUS-BANNER]` Displays warning banner when keySource is AutoGenerated and opens custom master key modal.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L115`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L115) (`renders AutoGenerated warning banner and opens MasterKeyModal`)

### `[SEC-VAULT-BOOTSTRAPPING]` Bootstraps master encryption key directly from HashiCorp Vault when VAULT_ADDR is configured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L191`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L191) (`ResolveDbEncryptionKey_BootstrapsFromVault_WhenVaultConfigured`)

### `[SEC-VAULT-CUSTOM-PATH]` Bootstraps master key from Vault using custom mount path and secret key name.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L236`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L236) (`ResolveDbEncryptionKey_BootstrapsFromVault_WithCustomPathAndKeyName`)

### `[TRANS-01]` SSE transport resolves static plaintext API keys when provider is None
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L11`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L11) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-http-direct.spec.ts#L8`](file:////containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-http-direct.spec.ts#L8) (`should register HTTP server with Direct Key, verify status badge, and execute tool in Test Bench`)

### `[TRANS-02]` HTTP stateless transport resolves static API keys when secret provider is None
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/HttpTransportTests.cs#L11`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/HttpTransportTests.cs#L11) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-stdio-env.spec.ts#L8`](file:////containers/dev/csharp-mcp-router/frontend/e2e/full-ui-flow-stdio-env.spec.ts#L8) (`should register STDIO server, verify card, and execute echo tool via Test Bench`)

### `[TRANS-03]` STDIO transport spawns subprocess, handles JSON-RPC initialization and executes tool calls
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L49`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L49) (`StdioTransport_ShouldInitializeAndCallToolSuccessfully`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L170`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L170) (`StdioTransport_ShouldRouteStderrToLogs`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L242`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L242) (`StdioTransport_ShouldSupportCancellationAndProcessTreeTermination`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L327`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L327) (`StdioTransport_ParseCommandLine_Handles_Quotes_And_Spaces`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L472`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L472) (`StdioTransport_ShouldDrainReaderStreamsToEOF_WhenProcessExitsImmediately`)

### `[UI-01]` Dashboard shows empty filter state when no servers match search term
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (10):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L115`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L115) (`renders empty state when no servers match search`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L19`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L19) (`renders top navigation bar with centered alignment in layout.css and App`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L42`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L42) (`renders tester tabs with centered alignment in tester.css`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L58`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L58) (`renders SettingsView sub-navigation bar with centered alignment`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L74`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L74) (`renders AppKeysCard sub-navigation tabs with centered alignment for admin`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L90`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L90) (`uses body::before and body::after pseudo-elements for ambient gradients and removes background-decor DOM nodes`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L117`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/LayoutCentering.test.tsx#L117) (`defines focus-visible outline indicators for interactive focus styling`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L42`](file:////containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L42) (`should navigate to Custom Files and Prompts in Settings view`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L23`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L23) (`should display aggregate statistics cards`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L37`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L37) (`should filter servers using search input`)

### `[UI-02]` Inspect modal displays spinner loading state while querying server capabilities
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L61`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L61) (`renders loading state when inspectLoading is true`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L79`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L79) (`renders tools tab with schema and handles tab switching`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L116`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L116) (`renders resources tab items and handles search filtering`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L144`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L144) (`renders prompts tab with arguments and empty state when filtered out`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L166`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L166) (`renders empty states for tabs when data is empty`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L194`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L194) (`closes modal when close button is clicked`)

### `[UI-03]` Grouped server view renders category sections and supports collapsible groups
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L63`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L63) (`renders grouped server view by category and allows collapsing`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L90`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L90) (`renders grouped server view by status and type`)

### `[UI-04]` Tool selector filters available tools by selected backend server
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (7):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L77`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L77) (`filters tools by selected server and handles tool change`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L106`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L106) (`filters custom tools with no namespace prefix when selectedServer is custom`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L131`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L131) (`renders dynamic fields for boolean, number, string, array, and object types`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L178`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L178) (`renders empty state when selected tool takes no arguments`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L203`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L203) (`switches to raw JSON tab and handles raw JSON editing`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L242`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L242) (`handles form submission`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L5`](file:////containers/dev/csharp-mcp-router/frontend/e2e/prompts-resources-customfiles.spec.ts#L5) (`should interact with Prompt Tester and Resource Tester cards in Test Bench`)

### `[UI-05]` Router allows customized branding parameters (DashboardTitle, DashboardIcon) to be saved and retrieved via the API.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L240`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L240) (`Pipeline_Settings_Branding_ReadWrite`)

### `[UI-06]` Router supports uploading and retrieving custom branding logo images via dedicated endpoints.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L418`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L418) (`Branding_Logo_Upload_And_Retrieval_Works`)

### `[UI-07]` Audits desktop viewport layout for zero horizontal overflow and high UX score.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/layout-inspector.spec.ts#L38`](file:////containers/dev/csharp-mcp-router/frontend/e2e/layout-inspector.spec.ts#L38) (`should pass layout audit on desktop 1080p viewport`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/layout-inspector.spec.ts#L64`](file:////containers/dev/csharp-mcp-router/frontend/e2e/layout-inspector.spec.ts#L64) (`should pass layout audit on Samsung Galaxy S25+ mobile viewport`)

### `[UI-102]` Dashboard renders stats card, connected server list, and setup instructions
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L1`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L1) (`renders stats card, server list, and client setup guide`)

### `[UI-103]` Interactive tool tester renders server and tool selection dropdowns
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L1`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L1) (`renders initial server and tool selection options`)

### `[UI-109]` Renders ClientSetupGuide below the user credentials card.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L102`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L102) (`renders client setup guide below credentials card`)

### `[UI-116]` Modal remains hidden when isInspectOpen is false
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L1`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L1) (`renders nothing when isInspectOpen is false`)

### `[UI-124]` Renders main dashboard navigation tabs and layout headers
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L1`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L1) (`should render the dashboard layout and header components`)

---

## 3. Boundary & Guardrail Invariants ("What the Application DOES NOT DO")

> [!IMPORTANT]
> The following guardrails define strict security boundaries, fail-closed fault invariants, and forbidden application states.

### `[AUTH-01]` AdminPolicy allows principal with configured Admin Group Name (e.g., full_admin)
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (15):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L13`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L13) (`AdminPolicy_Allows_Principal_With_AdminGroupName`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L47`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L47) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L81`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L81) (`AdminPolicy_Allows_Principal_With_ConfiguredAdminGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L116`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L116) (`AdminPolicy_Denies_StandardRole_WithoutAdminSidOrGroup`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L16`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L16) (`AdminPolicy_Denies_StandardRole_Without_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L58`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L58) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L255`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L255) (`AppKeysController_CreateAppKey_UnknownCategory_Admin_Succeeds`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L220`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L220) (`SecurityValidationHelper_IsAdmin_RequiresAdminGroupSid`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L238`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L238) (`SecurityValidationHelper_IsAdmin_AllowsAdminGroupName`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L252`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L252) (`SecurityValidationHelper_IsAdmin_RejectsNonAdminGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L269`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L269) (`SecurityValidationHelper_IsAdmin_AllowsCustomAdminGroupsArray`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L284`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L284) (`SecurityValidationHelper_IsAdmin_AllowsMappedGroups`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L299`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L299) (`OidcIdentityProvider_DoesNotGrantAdminSid_FromGroupOrUserNames`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L12`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L12) (`renders Active Directory disabled initially, toggles on and exposes fields`)
  - [Frontend Vitest] [`/containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L46`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/IdentityAuthTab.test.tsx#L46) (`fills LDAP parameters and executes test connection`)

### `[AUTH-PERSONAL-APPKEY-CREATE]` Non-admin users can create personal App Keys up to quota
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (9):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L191`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L191) (`CreateAppKey_NonAdmin_CreatesPersonalKey_UpToDefaultQuota`)
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
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L224`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L224) (`AdminPolicy_ExternalIdpConfigured_LoopbackIp_RequiresCredentials`)

### `[AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY]` AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L200`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L200) (`AdminPolicy_StandaloneMode_ExternalUntrustedIp_FailsAdminPolicy`)

### `[AUTH-STANDALONE-EXTERNAL-DENY]` Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L57`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L57) (`IsAdmin_StandaloneMode_UntrustedIp_ReturnsFalse`)

### `[GUARD-01]` Null or empty capability targets must immediately fail closed and return unauthorized
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (5):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L468`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L468) (`Pairwise_NullOrEmptyTarget_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L490`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L490) (`Pairwise_CorruptedAppKeyScopesJson_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L217`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L217) (`AppKeysController_CreateAppKey_UnknownCategory_NonAdmin_FailsWithBadRequest`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L236`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L236) (`AppKeysController_CreateAppKey_EmptyCategory_FailsWithBadRequest`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L301`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L301) (`ClientsController_CreateClient_EmptyCategory_ReturnsBadRequest`)

### `[GUARD-02]` SSE transport fails closed with SecurityException when secret provider resolution fails
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (8):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L32`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L32) (`ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L53`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L53) (`ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L283`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L283) (`ResolveDbEncryptionKey_ThrowsInvalidOperationException_WhenVaultFails`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L394`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L394) (`StdioTransport_ShouldFailClosed_WhenSecretResolutionFails`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/HttpTransportTests.cs#L31`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/HttpTransportTests.cs#L31) (`ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/HttpTransportTests.cs#L54`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/HttpTransportTests.cs#L54) (`ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L61`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L61) (`EnsureVaultClientAsync_ReturnsNull_WhenVaultProviderDisabledInRepo`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L113`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L113) (`GetSecretAsync_ThrowsSecurityException_OnVaultException`)

### `[GUARD-03]` STDIO transport rejects commands with shell metacharacters or dangerous commands
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (6):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L104`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L104) (`StdioTransport_ShouldThrowSecurityExceptionForUnsafeExecutable`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L126`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L126) (`StdioTransport_ShouldThrowSecurityExceptionForShellExecutable`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L148`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L148) (`StdioTransport_ShouldThrowOnInvalidExecutable`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L210`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L210) (`StdioTransport_ShouldTimeoutOnSlowRequests`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L289`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L289) (`StdioTransport_ShouldHandleUnexpectedExit`)
  - [Playwright E2E] [`/containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L55`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L55) (`Guest / Denied Context: restricted user session renders safely`)

### `[GUARD-04]` Malformed completion payloads or unmapped backends must fail closed safely
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (4):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L508`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L508) (`Pairwise_CompleteAsync_MalformedOrMissingBackends_ThrowsOrFailsClosed`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L531`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L531) (`Pairwise_DatabaseDisconnection_FailsClosedSafely`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L163`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L163) (`SchemaValidation_FailsClosed_WhenRequiredColumnOrTableMissing`)
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L189`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L189) (`SchemaValidation_FailsClosed_WhenUserQuotasOrKeyTypeMissing`)

### `[GUARD-05]` Batch save of authentication providers must fail closed if all providers are disabled
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ProvidersControllerTests.cs#L315`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ProvidersControllerTests.cs#L315) (`SaveAuthProvidersBatch_ReturnsBadRequest_WhenAllProvidersDisabled`)

### `[GUARD-06]` Global deny policies with TargetId '*' and IsAllowed false must fail closed
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PermissionsControllerTests.cs#L227`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PermissionsControllerTests.cs#L227) (`SavePolicy_ReturnsBadRequest_WhenWildcardDenyPolicy`)

### `[GUARD-ADMIN-CUSTOM-FILES-VALIDATION]` manage_custom_files rejects invalid prompt JSON syntax and unsupported file categories.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L779`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L779) (`ManageCustomFiles_ValidationGuardrails`)

### `[GUARD-ADMIN-ENDPOINT-UNAUTHORIZED]` Unauthenticated / non-admin client request to /admin receives 403 Forbidden.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L193`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L193) (`AdminEndpoint_UnauthorizedCaller_Returns403`)

### `[GUARD-ADMIN-POLICIES-WILDCARD-DENY]` manage_policies rejects wildcard deny policies to prevent global lockout.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L513`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L513) (`ManagePolicies_WildcardDenyGuardrail`)

### `[GUARD-ADMIN-PROVIDERS-LDAP-PLAINTEXT]` manage_providers rejects unencrypted LDAP connections on port 389.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L650`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L650) (`ManageProviders_LdapPlaintextGuardrail`)

### `[GUARD-ADMIN-SERVERS-VALIDATION]` Verifies that the manage_servers tool accurately enforces validation by rejecting malformed transport types, missing required parameters, and requests for non-existent servers.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L324`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L324) (`ManageServers_ValidationGuardrails`)

### `[GUARD-ADMIN-UNKNOWN-TOOL]` AdminMcpServer returns an error response for unknown tool or action invocations.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L592`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L592) (`CallToolAsync_UnknownToolOrAction_ReturnsErrorResponse`)

### `[MCP-ADMIN-TOOL-TEST-CALL-ERROR]` AdminMcpServer test_tool_call propagates downstream backend errors with visibility.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L633`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L633) (`CallToolAsync_TestToolCall_MissingServer_ReturnsError`)

### `[AUTH-106]` Exchange throws InvalidOperationException when request is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L15`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L15) (`Exchange_ThrowsInvalidOperationException_WhenRequestNull`)

### `[AUTH-108]` Authorize throws InvalidOperationException when OIDC request is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L71`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L71) (`Authorize_ThrowsInvalidOperationException_WhenRequestNull`)

### `[SEC-05]` Router must not overwrite corrupt encrypted database fields if an update occurs without user reset.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L379`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L379) (`SaveSecretProvider_WhenDecryptionFailed_DoesNotOverwriteCorruptPayload`)

### `[UI-CONFIRM-MODAL]` Deletes an access policy when confirmed via confirm modal.
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

### `[UI-TOAST-TRANSITION]` Displays error toast notification when saving invalid JSON credentials for user-provided server.
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
| `AUTH-001` | Positive | `AUTH` | Verify DatabaseUserSecretStore encrypts and decrypts secret correctly. | [`UserSecretStoreTests.cs:L8`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserSecretStoreTests.cs#L8) | Backend xUnit |
| `AUTH-002` | Positive | `AUTH` | Verify UserCredentialsController returns configured server IDs. | [`UserCredentialsControllerTests.cs:L11`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/UserCredentialsControllerTests.cs#L11) | Backend xUnit |
| `AUTH-01` | **Guardrail** | `AUTH` | AdminPolicy allows principal with configured Admin Group Name (e.g., full_admin) | [`AdminPolicyHybridAuthTests.cs:L13`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L13) | Backend xUnit |
| `AUTH-02` | Positive | `AUTH` | AppKey scopes restrict access precisely across all MCP capabilities and backend targets | [`PairwiseIntegrationMatrixTests.cs:L242`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L242) | Backend xUnit |
| `AUTH-03` | Positive | `AUTH` | SSO identity and group mappings resolve Windows SIDs and OIDC claims to internal access roles | [`PairwiseIntegrationMatrixTests.cs:L319`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L319) | Backend xUnit |
| `AUTH-04` | Positive | `AUTH` | ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP | [`ActiveDirectoryWindowsIdentityTests.cs:L12`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L12) | Backend xUnit |
| `AUTH-05` | Positive | `AUTH` | McpServer supports AllowPassThroughAuth flag | [`McpServerTests.cs:L5`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/McpServerTests.cs#L5) | Backend xUnit |
| `AUTH-06` | Positive | `AUTH` | Transports use passThroughToken when AllowPassThroughAuth is true | [`TransportsAuthShapeTests.cs:L200`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L200) | Backend xUnit |
| `AUTH-101` | Positive | `AUTH` | HTTP transport injects X-Forwarded-User header based on connected user identity. | [`IdentityHeaderTests.cs:L9`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityHeaderTests.cs#L9) | Backend xUnit |
| `AUTH-105` | Positive | `AUTH` | Dynamic Auth Target Pass-Through | [`ToolRoutingManagerTests.cs:L173`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173) | Backend xUnit |
| `AUTH-110` | Positive | `AUTH` | CreateAppKey allows creating unlimited AppKeys when UserMaxKeys is set to 0. | [`AppKeysControllerTests.cs:L339`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L339) | Backend xUnit |
| `AUTH-APPKEY-ADMIN-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with admin scope grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L79`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L79) | Backend xUnit |
| `AUTH-APPKEY-ITEMS-SCOPE-ALLOW` | Positive | `AUTH` | SecurityValidationHelper recognizes admin scopes in HttpContext.Items. | [`StandaloneAdminAuthTests.cs:L255`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L255) | Backend xUnit |
| `AUTH-APPKEY-WILDCARD-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L140`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L140) | Backend xUnit |
| `AUTH-COMPACT-APPKEY-TAXONOMY` | Positive | `AUTH` | Generates compact ~32-character Base62 AppKeys with semantic prefixes. | [`AppKeyAuthenticationTests.cs:L409`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L409) | Backend xUnit |
| `AUTH-CUSTOM-ADMIN-KEY-SEEDING` | Positive | `AUTH` | Seeds custom MCG_ADMIN_AUTH_KEY when provided in configuration. | [`DatabaseSeederServiceTests.cs:L186`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L186) | Backend xUnit |
| `AUTH-PERSONAL-APPKEY-CREATE` | **Guardrail** | `AUTH` | Non-admin users can create personal App Keys up to quota | [`AppKeysControllerTests.cs:L191`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L191) | Backend xUnit |
| `AUTH-PERSONAL-APPKEY-LIST` | Positive | `AUTH` | Non-admin users can view their personal App Keys | [`AppKeysControllerTests.cs:L125`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L125) | Backend xUnit |
| `AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE` | Positive | `AUTH` | Custom user quotas override default limit | [`AppKeysControllerTests.cs:L223`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L223) | Backend xUnit |
| `AUTH-PREFIX-EXTRACTION` | Positive | `AUTH` | ExtractKeyPrefix parses semantic prefixes, Base62 selectors, and legacy tokens accurately. | [`AppKeyAuthenticationTests.cs:L443`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L443) | Backend xUnit |
| `AUTH-QUERY-TOKEN-EXTRACTION` | Positive | `AUTH` | Query string token middleware extracts access_token or token query parameter to Authorization header. | [`EndpointAuthorizationTests.cs:L7`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L7) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW` | Positive | `AUTH` | AdminPolicy succeeds in standalone mode for unauthenticated loopback requests. | [`StandaloneAdminAuthTests.cs:L176`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L176) | Backend xUnit |
| `AUTH-STANDALONE-CUSTOM-CIDR-ALLOW` | Positive | `AUTH` | Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges. | [`StandaloneAdminAuthTests.cs:L35`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L35) | Backend xUnit |
| `AUTH-STANDALONE-LOOPBACK-ALLOW` | Positive | `AUTH` | Standalone mode without external IDP grants admin access to loopback IP addresses. | [`StandaloneAdminAuthTests.cs:L14`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L14) | Backend xUnit |
| `AUTH-SYSTEM-APPKEY-SEPARATION` | Positive | `AUTH` | System keys are distinct and require admin permissions | [`AppKeysControllerTests.cs:L151`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AppKeysControllerTests.cs#L151) | Backend xUnit |
| `UI-120` | Positive | `AUTH` | RBAC and SID mapping administration UI allows configuring role policies and SID associations | [`rbac-enforcement-flow.spec.ts:L1`](file:////containers/dev/csharp-mcp-router/frontend/e2e/rbac-enforcement-flow.spec.ts#L1) | Playwright E2E |
| `UI-125` | Positive | `AUTH` | Admin role renders full administrative dashboard and server management controls | [`multi-user-matrix.spec.ts:L1`](file:////containers/dev/csharp-mcp-router/frontend/e2e/multi-user-matrix.spec.ts#L1) | Playwright E2E |
| `CORE-101` | Positive | `CORE` | Auto-added requirement tracking | [`SessionManagerTests.cs:L9`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SessionManagerTests.cs#L9) | Backend xUnit |
| `DB-01` | Positive | `DB` | SQLite auto-migration seamlessly upgrades legacy schema, encrypts plaintext secrets, and preserves data | [`DatabaseSchemaUpgradeAndContractTests.cs:L29`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L29) | Backend xUnit |
| `DB-02` | Positive | `DB` | MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly | [`DatabaseSchemaUpgradeAndContractTests.cs:L311`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L311) | Backend xUnit |
| `DOC-SETUP-SKILL-FRONTMATTER` | Positive | `DOC` | mcg-setup skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters | [`SetupSkillTests.cs:L18`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L18) | Backend xUnit |
| `DOC-SETUP-SKILL-MIRROR` | Positive | `DOC` | The mcg-setup skill and templates are mirrored 1:1 in .agents/skills/mcg-setup/ | [`SetupSkillTests.cs:L152`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L152) | Backend xUnit |
| `DOC-SETUP-SKILL-TEMPLATES` | Positive | `DOC` | All scaffold templates exist, are non-empty, and contain required directives such as responseBufferLimit, MCG_MASTER_KEY, and ghcr.io/spelech/model-context-gateway | [`SetupSkillTests.cs:L98`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L98) | Backend xUnit |
| `DOC-SETUP-SKILL-WORKFLOW` | Positive | `DOC` | mcg-setup skill contains all 6 required setup phases including environment probing, hosting platforms, env vs UI trade-offs, identity/network topology, artifact generation, and health/client configuration | [`SetupSkillTests.cs:L44`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SetupSkillTests.cs#L44) | Backend xUnit |
| `AUTH-EXTERNAL-IDP-DENIES-ANONYMOUS-LOOPBACK` | **Guardrail** | `GUARD` | When an external IDP is configured, anonymous loopback requests do not bypass authentication. | [`StandaloneAdminAuthTests.cs:L224`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L224) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY` | **Guardrail** | `GUARD` | AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode. | [`StandaloneAdminAuthTests.cs:L200`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L200) | Backend xUnit |
| `AUTH-STANDALONE-EXTERNAL-DENY` | **Guardrail** | `GUARD` | Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey. | [`StandaloneAdminAuthTests.cs:L57`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L57) | Backend xUnit |
| `GUARD-01` | **Guardrail** | `GUARD` | Null or empty capability targets must immediately fail closed and return unauthorized | [`PairwiseIntegrationMatrixTests.cs:L468`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L468) | Backend xUnit |
| `GUARD-02` | **Guardrail** | `GUARD` | SSE transport fails closed with SecurityException when secret provider resolution fails | [`SseTransportTests.cs:L32`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L32) | Backend xUnit |
| `GUARD-03` | **Guardrail** | `GUARD` | STDIO transport rejects commands with shell metacharacters or dangerous commands | [`StdioTransportTests.cs:L104`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L104) | Backend xUnit |
| `GUARD-04` | **Guardrail** | `GUARD` | Malformed completion payloads or unmapped backends must fail closed safely | [`PairwiseIntegrationMatrixTests.cs:L508`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L508) | Backend xUnit |
| `GUARD-05` | **Guardrail** | `GUARD` | Batch save of authentication providers must fail closed if all providers are disabled | [`ProvidersControllerTests.cs:L315`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ProvidersControllerTests.cs#L315) | Backend xUnit |
| `GUARD-06` | **Guardrail** | `GUARD` | Global deny policies with TargetId '*' and IsAllowed false must fail closed | [`PermissionsControllerTests.cs:L227`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PermissionsControllerTests.cs#L227) | Backend xUnit |
| `GUARD-ADMIN-CUSTOM-FILES-VALIDATION` | **Guardrail** | `GUARD` | manage_custom_files rejects invalid prompt JSON syntax and unsupported file categories. | [`AdminToolsParityTests.cs:L779`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L779) | Backend xUnit |
| `GUARD-ADMIN-ENDPOINT-UNAUTHORIZED` | **Guardrail** | `GUARD` | Unauthenticated / non-admin client request to /admin receives 403 Forbidden. | [`AdminEndpointsTests.cs:L193`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L193) | Backend xUnit |
| `GUARD-ADMIN-POLICIES-WILDCARD-DENY` | **Guardrail** | `GUARD` | manage_policies rejects wildcard deny policies to prevent global lockout. | [`AdminToolsParityTests.cs:L513`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L513) | Backend xUnit |
| `GUARD-ADMIN-PROVIDERS-LDAP-PLAINTEXT` | **Guardrail** | `GUARD` | manage_providers rejects unencrypted LDAP connections on port 389. | [`AdminToolsParityTests.cs:L650`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L650) | Backend xUnit |
| `GUARD-ADMIN-SERVERS-VALIDATION` | **Guardrail** | `GUARD` | Verifies that the manage_servers tool accurately enforces validation by rejecting malformed transport types, missing required parameters, and requests for non-existent servers. | [`AdminToolsParityTests.cs:L324`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L324) | Backend xUnit |
| `GUARD-ADMIN-UNKNOWN-TOOL` | **Guardrail** | `GUARD` | AdminMcpServer returns an error response for unknown tool or action invocations. | [`AdminMcpServerTests.cs:L592`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L592) | Backend xUnit |
| `MCP-ADMIN-TOOL-TEST-CALL-ERROR` | **Guardrail** | `GUARD` | AdminMcpServer test_tool_call propagates downstream backend errors with visibility. | [`AdminMcpServerTests.cs:L633`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L633) | Backend xUnit |
| `MCP-01` | Positive | `MCP` | Meta-mode execute_tool strictly enforces target tool authorization policies | [`PairwiseIntegrationMatrixTests.cs:L567`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L567) | Backend xUnit |
| `MCP-02` | Positive | `MCP` | All MCP protocol capabilities enforce caller role authorizations consistently | [`PairwiseIntegrationMatrixTests.cs:L385`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L385) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-CALL-TOOL` | Positive | `MCP` | Admin endpoint /admin/message executes tools/call for manage_system diagnostics. | [`AdminEndpointsTests.cs:L294`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L294) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-HEAD-REQUEST` | Positive | `MCP` | Admin endpoint /admin handles HEAD request returning text/event-stream headers. | [`AdminEndpointsTests.cs:L212`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L212) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-LIST-TOOLS` | Positive | `MCP` | Admin endpoint /admin/message executes tools/list over active SSE session and returns 10 admin tools. | [`AdminEndpointsTests.cs:L224`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L224) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET` | Positive | `MCP` | Target proxy endpoint /router-admin routes directly to the Admin MCP server. | [`AdminEndpointsTests.cs:L149`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L149) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE` | Positive | `MCP` | Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version. | [`AdminEndpointsTests.cs:L62`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminEndpointsTests.cs#L62) | Backend xUnit |
| `MCP-ADMIN-INITIALIZE-HANDSHAKE` | Positive | `MCP` | AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05. | [`AdminMcpServerTests.cs:L195`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L195) | Backend xUnit |
| `MCP-ADMIN-PARITY-APPKEYS` | Positive | `MCP` | manage_appkeys supports full parity for list, get_limits, create, and revoke actions. | [`AdminToolsParityTests.cs:L370`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L370) | Backend xUnit |
| `MCP-ADMIN-PARITY-CLIENTS` | Positive | `MCP` | manage_clients supports full parity for register, list, and delete actions. | [`AdminToolsParityTests.cs:L423`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L423) | Backend xUnit |
| `MCP-ADMIN-PARITY-CUSTOM-FILES` | Positive | `MCP` | manage_custom_files supports full parity for list, get, save, and delete prompt and resource files. | [`AdminToolsParityTests.cs:L716`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L716) | Backend xUnit |
| `MCP-ADMIN-PARITY-GROUP-MAPPINGS` | Positive | `MCP` | manage_group_mappings supports full parity for list, save, and delete external-to-internal group mappings. | [`AdminToolsParityTests.cs:L530`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L530) | Backend xUnit |
| `MCP-ADMIN-PARITY-JSONRPC-DISPATCH` | Positive | `MCP` | AdminMcpServer processes standard JSON-RPC 2.0 requests (tools/list, tools/call, ping). | [`AdminToolsParityTests.cs:L873`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L873) | Backend xUnit |
| `MCP-ADMIN-PARITY-POLICIES` | Positive | `MCP` | manage_policies supports full parity for list, save, and delete access control policies. | [`AdminToolsParityTests.cs:L464`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L464) | Backend xUnit |
| `MCP-ADMIN-PARITY-PROVIDERS` | Positive | `MCP` | manage_providers supports full parity for list, save_secret, test_vault, save_auth, and test_ldap actions. | [`AdminToolsParityTests.cs:L577`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L577) | Backend xUnit |
| `MCP-ADMIN-PARITY-SERVERS` | Positive | `MCP` | Validates that the manage_servers tool provides comprehensive administrative capabilities including listing, retrieving, creating, updating, toggling, deleting, and reconnecting servers. | [`AdminToolsParityTests.cs:L234`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L234) | Backend xUnit |
| `MCP-ADMIN-PARITY-SETTINGS` | Positive | `MCP` | manage_settings supports full parity for get and update global router configurations. | [`AdminToolsParityTests.cs:L667`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L667) | Backend xUnit |
| `MCP-ADMIN-PARITY-SYSTEM` | Positive | `MCP` | manage_system supports full parity for diagnostics, get_logs, clear_logs, and query_audit actions. | [`AdminToolsParityTests.cs:L816`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L816) | Backend xUnit |
| `MCP-ADMIN-PARITY-TEST-TOOL-CALL` | Positive | `MCP` | test_tool_call executes test bench backend tool calls and formats responses. | [`AdminToolsParityTests.cs:L846`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L846) | Backend xUnit |
| `MCP-ADMIN-PARITY-TOOLS-COVERAGE` | Positive | `MCP` | Ensures every UI management workflow is backed by a verified, equivalent action within the consolidated Admin MCP tools. | [`AdminToolsParityTests.cs:L191`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminToolsParityTests.cs#L191) | Backend xUnit |
| `MCP-ADMIN-SKILL-E2E-PROVISIONING` | Positive | `MCP` | Admin automation templates and JSON-RPC tool calls successfully provision a blank-slate gateway instance end-to-end via HTTP /admin/message. | [`AdminAutomationSkillTests.cs:L176`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L176) | Backend xUnit |
| `MCP-ADMIN-SKILL-FRONTMATTER` | Positive | `MCP` | mcg-admin skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters | [`AdminAutomationSkillTests.cs:L21`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L21) | Backend xUnit |
| `MCP-ADMIN-SKILL-MIRROR` | Positive | `MCP` | mcg-admin skill files and templates are identically mirrored between skills/ and .agents/skills/ directories | [`AdminAutomationSkillTests.cs:L147`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L147) | Backend xUnit |
| `MCP-ADMIN-SKILL-TEMPLATES` | Positive | `MCP` | All mcg-admin scaffold templates exist, are non-empty, and contain valid JSON or scripts for Authentik, Keycloak, Entra, ActiveDirectory, Cloudflare, Vault, Embeddings, Docker, and shell automation | [`AdminAutomationSkillTests.cs:L103`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L103) | Backend xUnit |
| `MCP-ADMIN-SKILL-WORKFLOW` | Positive | `MCP` | mcg-admin skill contains all 7 administration phases including diagnostics, secrets, auth providers, RBAC/group mappings, settings/embeddings, servers/clients, and live tool verification | [`AdminAutomationSkillTests.cs:L47`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L47) | Backend xUnit |
| `MCP-ADMIN-TOOL-AUDIT-LOG` | Positive | `MCP` | AdminMcpServer tool calls record audit log entries with caller and tool name. | [`AdminMcpServerTests.cs:L266`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L266) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-APPKEYS` | Positive | `MCP` | AdminMcpServer executes manage_appkeys create, list, limits, and revoke actions. | [`AdminMcpServerTests.cs:L283`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L283) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-CLIENTS` | Positive | `MCP` | AdminMcpServer executes manage_clients register, list, and delete actions. | [`AdminMcpServerTests.cs:L330`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L330) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-CUSTOM-FILES` | Positive | `MCP` | AdminMcpServer executes manage_custom_files save, get, list, and delete actions. | [`AdminMcpServerTests.cs:L504`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L504) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-GROUP-MAPPINGS` | Positive | `MCP` | AdminMcpServer executes manage_group_mappings save, list, and delete actions. | [`AdminMcpServerTests.cs:L402`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L402) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-POLICIES` | Positive | `MCP` | AdminMcpServer executes manage_policies save, list, and delete actions. | [`AdminMcpServerTests.cs:L368`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L368) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-PROVIDERS` | Positive | `MCP` | AdminMcpServer executes manage_providers list, save_secret, and save_auth actions. | [`AdminMcpServerTests.cs:L435`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L435) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SERVERS` | Positive | `MCP` | AdminMcpServer executes manage_servers list, get, create, update, toggle, and delete actions. | [`AdminMcpServerTests.cs:L214`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L214) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SETTINGS` | Positive | `MCP` | AdminMcpServer executes manage_settings get and update actions. | [`AdminMcpServerTests.cs:L478`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L478) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SYSTEM` | Positive | `MCP` | AdminMcpServer executes manage_system diagnostics, get_logs, clear_logs, and query_audit actions. | [`AdminMcpServerTests.cs:L558`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L558) | Backend xUnit |
| `MCP-ADMIN-TOOLS-LIST-COUNT` | Positive | `MCP` | AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas. | [`AdminMcpServerTests.cs:L147`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L147) | Backend xUnit |
| `AUTH-106` | **Guardrail** | `SEC` | Exchange throws InvalidOperationException when request is null. | [`AuthorizationControllerTests.cs:L15`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L15) | Backend xUnit |
| `AUTH-107` | Positive | `SEC` | RegisterClient successfully handles DCR requests when open DCR is enabled. | [`AuthorizationControllerTests.cs:L33`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L33) | Backend xUnit |
| `AUTH-108` | **Guardrail** | `SEC` | Authorize throws InvalidOperationException when OIDC request is null. | [`AuthorizationControllerTests.cs:L71`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L71) | Backend xUnit |
| `AUTH-109` | Positive | `SEC` | RegisterClient uses ICredentialService when IOpenIddictApplicationManager is null. | [`AuthorizationControllerTests.cs:L88`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L88) | Backend xUnit |
| `AUTH-111` | Positive | `SEC` | Pipeline exposes RFC 9728 OAuth Protected Resource discovery endpoints with dynamic resource identifiers. | [`PipelineIntegrationTests.cs:L61`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L61) | Backend xUnit |
| `SEC-01` | Positive | `SEC` | VaultSecretRetriever authenticates with HashiCorp Vault using AppRole RoleID and SecretID credentials | [`VaultAppRoleAndRenewalTests.cs:L14`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L14) | Backend xUnit |
| `SEC-02` | Positive | `SEC` | STDIO transport securely injects secret credentials via environment variables rather than command-line arguments | [`StdioTransportTests.cs:L344`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L344) | Backend xUnit |
| `SEC-03` | Positive | `SEC` | Ensure TrustedProxyHelper supports CIDR ranges in XFF validation | [`IdentityProviderTests.cs:L360`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/IdentityProviderTests.cs#L360) | Backend xUnit |
| `SEC-04` | Positive | `SEC` | WindowsRegistrySecretRetriever securely decrypts DPAPI LocalMachine machine-level encrypted binary values and retrieves plaintext registry strings | [`WindowsRegistrySecretRetrieverTests.cs:L12`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L12) | Backend xUnit |
| `SEC-05` | **Guardrail** | `SEC` | Router must not overwrite corrupt encrypted database fields if an update occurs without user reset. | [`ProviderSettingsEncryptionTests.cs:L379`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L379) | Backend xUnit |
| `SEC-ADMIN-AUDIT-REDACTION` | Positive | `SEC` | AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs. | [`AdminMcpServerTests.cs:L609`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminMcpServerTests.cs#L609) | Backend xUnit |
| `SEC-GATEWAY-ZERO-CONFIG-BOOT` | Positive | `SEC` | Gateway boots from a blank slate with zero master key environment variables, auto-generates .master.key, and serves health and admin endpoints. | [`AdminAutomationSkillTests.cs:L352`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L352) | Backend xUnit |
| `SEC-KEY-PROVIDER-AUTOGEN` | Positive | `SEC` | EncryptionKeyProvider delegates to DbKeyHelper to auto-generate master key when unconfigured. | [`EncryptionKeyProviderTests.cs:L42`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L42) | Backend xUnit |
| `SEC-KEY-PROVIDER-CONFIG` | Positive | `SEC` | EncryptionKeyProvider returns configured DB_ENCRYPTION_KEY or MCG_SECRET. | [`EncryptionKeyProviderTests.cs:L28`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L28) | Backend xUnit |
| `SEC-KEY-PROVIDER-FALLBACK` | Positive | `SEC` | EncryptionKeyProvider falls back to DB_ENCRYPTION_KEY when MCG_SECRET is unconfigured. | [`EncryptionKeyProviderTests.cs:L70`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L70) | Backend xUnit |
| `SEC-KEY-PROVIDER-SECRET` | Positive | `SEC` | EncryptionKeyProvider returns configured MCG_SECRET. | [`EncryptionKeyProviderTests.cs:L56`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L56) | Backend xUnit |
| `SEC-KEYFILE-AUTOGEN` | Positive | `SEC` | Blank-slate initialization auto-generates a 256-bit base64 master key and persists it to .master.key. | [`DbKeyHelperTests.cs:L63`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L63) | Backend xUnit |
| `SEC-KEYFILE-ENV-PRECEDENCE` | Positive | `SEC` | Explicit environment variables MCG_MASTER_KEY or MCG_SECRET take precedence over keyfiles. | [`DbKeyHelperTests.cs:L28`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L28) | Backend xUnit |
| `SEC-KEYFILE-FILE-OVER-KEYFILE` | Positive | `SEC` | Explicit file secrets take precedence over persistent .master.key files. | [`DbKeyHelperTests.cs:L123`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L123) | Backend xUnit |
| `SEC-KEYFILE-FILE-SECRET` | Positive | `SEC` | File-based secrets configured via MCG_MASTER_KEY_FILE or standard Docker secrets paths are resolved. | [`DbKeyHelperTests.cs:L45`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L45) | Backend xUnit |
| `SEC-KEYFILE-HIERARCHY-PRECEDENCE` | Positive | `SEC` | Explicit environment variables take precedence over file secrets and keyfiles. | [`DbKeyHelperTests.cs:L101`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L101) | Backend xUnit |
| `SEC-KEYFILE-RELOAD` | Positive | `SEC` | Existing .master.key file is loaded across gateway restarts without key mutation. | [`DbKeyHelperTests.cs:L83`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L83) | Backend xUnit |
| `SEC-KEYSOURCE-DETECTION` | Positive | `SEC` | Correctly identifies KeySource origin for environment, file, and auto-generated keys. | [`DbKeyHelperTests.cs:L144`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L144) | Backend xUnit |
| `SEC-KEYSOURCE-SETCACHEDKEY` | Positive | `SEC` | SetCachedKey sets in-memory encryption key and updates ActiveKeySource. | [`DbKeyHelperTests.cs:L314`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L314) | Backend xUnit |
| `SEC-MASTERKEY-ATOMIC-REENCRYPTION` | Positive | `SEC` | Atomically re-encrypts database credentials when setting a custom master key. | [`MasterKeyReEncryptionTests.cs:L142`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L142) | Backend xUnit |
| `SEC-MASTERKEY-CONFIGURED-STATUS-BADGE` | Positive | `SEC` | Displays configured badge and rotate button when custom master key is configured. | [`GeneralTab.test.tsx:L192`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L192) | Frontend Vitest |
| `SEC-MASTERKEY-CUSTOM-MODAL-REENCRYPTION` | Positive | `SEC` | Validates master key inputs (length, match) and triggers atomic re-encryption. | [`MasterKeyModal.test.tsx:L6`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/MasterKeyModal.test.tsx#L6) | Frontend Vitest |
| `SEC-MASTERKEY-EXTERNAL-LOCKED-BADGE` | Positive | `SEC` | Displays locked badge when master key is externally managed via Vault or Environment. | [`GeneralTab.test.tsx:L149`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L149) | Frontend Vitest |
| `SEC-MASTERKEY-UI-STATUS-BANNER` | Positive | `SEC` | Displays warning banner when keySource is AutoGenerated and opens custom master key modal. | [`GeneralTab.test.tsx:L115`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/GeneralTab.test.tsx#L115) | Frontend Vitest |
| `SEC-VAULT-BOOTSTRAPPING` | Positive | `SEC` | Bootstraps master encryption key directly from HashiCorp Vault when VAULT_ADDR is configured. | [`DbKeyHelperTests.cs:L191`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L191) | Backend xUnit |
| `SEC-VAULT-CUSTOM-PATH` | Positive | `SEC` | Bootstraps master key from Vault using custom mount path and secret key name. | [`DbKeyHelperTests.cs:L236`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/DbKeyHelperTests.cs#L236) | Backend xUnit |
| `TRANS-01` | Positive | `TRANS` | SSE transport resolves static plaintext API keys when provider is None | [`SseTransportTests.cs:L11`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/SseTransportTests.cs#L11) | Backend xUnit |
| `TRANS-02` | Positive | `TRANS` | HTTP stateless transport resolves static API keys when secret provider is None | [`HttpTransportTests.cs:L11`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/HttpTransportTests.cs#L11) | Backend xUnit |
| `TRANS-03` | Positive | `TRANS` | STDIO transport spawns subprocess, handles JSON-RPC initialization and executes tool calls | [`StdioTransportTests.cs:L49`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/StdioTransportTests.cs#L49) | Backend xUnit |
| `UI-01` | Positive | `UI` | Dashboard shows empty filter state when no servers match search term | [`DashboardView.test.tsx:L115`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L115) | Frontend Vitest |
| `UI-02` | Positive | `UI` | Inspect modal displays spinner loading state while querying server capabilities | [`ServerInspectModal.test.tsx:L61`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L61) | Frontend Vitest |
| `UI-03` | Positive | `UI` | Grouped server view renders category sections and supports collapsible groups | [`DashboardView.test.tsx:L63`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L63) | Frontend Vitest |
| `UI-04` | Positive | `UI` | Tool selector filters available tools by selected backend server | [`ToolTesterCard.test.tsx:L77`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L77) | Frontend Vitest |
| `UI-05` | Positive | `UI` | Router allows customized branding parameters (DashboardTitle, DashboardIcon) to be saved and retrieved via the API. | [`PipelineIntegrationTests.cs:L240`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L240) | Backend xUnit |
| `UI-06` | Positive | `UI` | Router supports uploading and retrieving custom branding logo images via dedicated endpoints. | [`PipelineIntegrationTests.cs:L418`](file:////containers/dev/csharp-mcp-router/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L418) | Backend xUnit |
| `UI-07` | Positive | `UI` | Audits desktop viewport layout for zero horizontal overflow and high UX score. | [`layout-inspector.spec.ts:L38`](file:////containers/dev/csharp-mcp-router/frontend/e2e/layout-inspector.spec.ts#L38) | Playwright E2E |
| `UI-102` | Positive | `UI` | Dashboard renders stats card, connected server list, and setup instructions | [`DashboardView.test.tsx:L1`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/DashboardView.test.tsx#L1) | Frontend Vitest |
| `UI-103` | Positive | `UI` | Interactive tool tester renders server and tool selection dropdowns | [`ToolTesterCard.test.tsx:L1`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ToolTesterCard.test.tsx#L1) | Frontend Vitest |
| `UI-109` | Positive | `UI` | Renders ClientSetupGuide below the user credentials card. | [`MyMcpServers.test.tsx:L102`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L102) | Frontend Vitest |
| `UI-116` | Positive | `UI` | Modal remains hidden when isInspectOpen is false | [`ServerInspectModal.test.tsx:L1`](file:////containers/dev/csharp-mcp-router/frontend/src/test/components/ServerInspectModal.test.tsx#L1) | Frontend Vitest |
| `UI-124` | Positive | `UI` | Renders main dashboard navigation tabs and layout headers | [`dashboard.spec.ts:L1`](file:////containers/dev/csharp-mcp-router/frontend/e2e/dashboard.spec.ts#L1) | Playwright E2E |
| `UI-CONFIRM-MODAL` | **Guardrail** | `UI` | Deletes an access policy when confirmed via confirm modal. | [`usePolicyStore.test.ts:L76`](file:////containers/dev/csharp-mcp-router/frontend/src/test/stores/usePolicyStore.test.ts#L76) | Frontend Vitest |
| `UI-TOAST-TRANSITION` | **Guardrail** | `UI` | Displays error toast notification when saving invalid JSON credentials for user-provided server. | [`MyMcpServers.test.tsx:L23`](file:////containers/dev/csharp-mcp-router/frontend/src/test/pages/MyMcpServers.test.tsx#L23) | Frontend Vitest |
