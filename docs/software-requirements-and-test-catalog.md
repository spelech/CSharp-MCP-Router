# Software Requirements Specification (SRS) & Test Verification Catalog

> **Automated Verification Document:** Generated via `dotnet run --project scripts/CatalogGenerator`
> **Catalog Statistics:** **184 Requirements Verified** across **830 Test Proofs** (154 Functional Capabilities, 30 Safety Guardrails).

---

## 1. System Taxonomy & Verification Summary

| Category | Domain | Total Requirements | Positive Features | Guardrails / Fail-Closed | Verification Proofs |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **`AUTH`** | Authentication, RBAC & Identity | **41** | 38 | 3 | 210 proofs |
| **`CORE`** | CORE | **1** | 1 | 0 | 2 proofs |
| **`DB`** | Multi-Database Persistence & Migrations | **3** | 3 | 0 | 31 proofs |
| **`DOC`** | DOC | **4** | 4 | 0 | 4 proofs |
| **`GUARD`** | Universal Safety & Fail-Closed Guardrails | **16** | 0 | 16 | 134 proofs |
| **`MCP`** | Model Context Protocol Engine & Tool Routing | **52** | 51 | 1 | 163 proofs |
| **`SEC`** | Secrets Providers & Encryption | **39** | 32 | 7 | 128 proofs |
| **`TRANS`** | Transports (SSE, HTTP, STDIO, Proxy) | **3** | 3 | 0 | 28 proofs |
| **`UI`** | Dashboard, Test Bench & Settings UI | **25** | 22 | 3 | 130 proofs |

---

## 2. Functional Requirements ("What the Application DOES")

### `[AUTH-001]` Verify DatabaseUserSecretStore encrypts and decrypts secret correctly.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserSecretStoreTests.cs#L8`](file:////app/ModelContextGateway.Tests/UserSecretStoreTests.cs#L8) (`DatabaseUserSecretStore_SavesAndRetrieves_Secret`)

### `[AUTH-002]` Verify UserCredentialsController returns configured server IDs.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserCredentialsControllerTests.cs#L11`](file:////app/ModelContextGateway.Tests/UserCredentialsControllerTests.cs#L11) (`GetUserCredentials_ReturnsServerIds`)

### `[AUTH-02]` Allows token pass-through in query parameters for SSE stream initialization.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (48):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L48`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L48) (`Pipeline_QueryToken_MiddlewareBypass`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L319`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L319) (`Pipeline_AppKey_Create_And_Revoke`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L402`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L402) (`Pipeline_GET_AppKeys_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L411`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L411) (`Pipeline_GET_AppKeysLimits_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L242`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L242) (`Pairwise_AppKeyScopes_RestrictsAccessPrecisely`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L146`](file:////app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L146) (`AppKeyRepository_SaveAndGet_PersistsKeyTypeAndFilters`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L34`](file:////app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L34) (`AppKeyAuthenticationHandler_Emits_Sid_Claim_When_OwnerSid_Present`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L69`](file:////app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L69) (`AppKeyIdentityProvider_ResolvesOwnerAndSid_FromHttpContextItems`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L87`](file:////app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L87) (`AppKeyIdentityProvider_ReturnsAnonymous_WhenNoAppKey`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L251`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L251) (`AppKeyScopes_RestrictTargetAccessPrecisely`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L86`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L86) (`AppKeys_PrefixLookup_WorksCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L122`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L122) (`AppKeys_KeyExpiration_CheckedCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L151`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L151) (`AppKeys_Limits_CheckWorks`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L189`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L189) (`AppKeys_Sha256Hashing_VerificationWorks`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L278`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L278) (`CreateAppKey_CreatesNewKey_Successfully_WithDifferentScopeSlugs`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L391`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L391) (`GetAppKeysLimits_ReturnsLimitsAndCounts`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L197`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L197) (`AppKeysController_CreateAppKey_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L281`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L281) (`ClientsController_CreateClient_ValidCategory_Succeeds`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L364`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L364) (`ClientSession_CategoryScope_AuthorizesMatchingServerTools_AndDeniesOthers`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L388`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L388) (`ClientSession_GroupAliasScope_AuthorizesIdenticallyToCategory`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L410`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L410) (`ClientSession_CategoryScope_IsCaseInsensitive`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L460`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L460) (`ClientSession_ResourcesAndTemplates_FilteredByCategoryScope`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L488`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L488) (`ClientSession_DynamicServerMembership_UpdatesAccessDynamically`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L524`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L524) (`ClientSession_MixedScopes_CombinesCategoryAndSpecificToolScopes`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L551`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L551) (`ClientSession_Complete_FiltersServerNamesByCategoryScope`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientSetupGuide.test.tsx#L51`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L51) (`switches between format tabs (Standard, VS Code, Generic SSE)`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientSetupGuide.test.tsx#L78`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L78) (`switches server scope from all servers to individual server`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientSetupGuide.test.tsx#L97`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L97) (`updates domain when LAN or custom is chosen`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientSetupGuide.test.tsx#L122`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L122) (`toggles meta mode when server scope is all`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientSetupGuide.test.tsx#L143`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L143) (`populates app keys dropdown and injects selected key`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientSetupGuide.test.tsx#L166`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L166) (`copies configuration to clipboard and triggers success toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientModal.test.tsx#L15`](file:////app/frontend/src/test/components/ClientModal.test.tsx#L15) (`renders nothing when isAddClientOpen is false`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L22`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L22) (`initializes with default state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L57`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L57) (`handles fetch error gracefully without crashing`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L123`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L123) (`handles register error with toast and propagates error`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L195`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L195) (`handles delete failure with error toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L261`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L261) (`opens and closes add client modal and resets created result`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L316`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L316) (`initializes with default state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L388`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L388) (`loads app key limits`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L402`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L402) (`handles fetch error gracefully without crashing`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L466`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L466) (`handles create key error with toast and throws`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L541`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L541) (`handles revoke failure with error toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L559`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L559) (`loads user quotas and updates store`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L577`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L577) (`handles fetchUserQuotas error gracefully without crashing`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L616`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L616) (`handles setUserQuota error with toast and throws`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L687`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L687) (`handles deleteUserQuota failure with error toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L705`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L705) (`opens and closes create modal and clears result`)
  - [Playwright E2E] [`/app/frontend/e2e/multi-user-matrix.spec.ts#L70`](file:////app/frontend/e2e/multi-user-matrix.spec.ts#L70) (`AppKey Direct Context: connects with API key header identity`)

### `[AUTH-03]` GET /api/permissions/mappings returns group mappings with 200 OK.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (26):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L366`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L366) (`Pipeline_GET_Permissions_Mappings_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L384`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L384) (`Pipeline_GET_Providers_Auth_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/GroupMappingsAndSpecAuthTests.cs#L93`](file:////app/ModelContextGateway.Tests/GroupMappingsAndSpecAuthTests.cs#L93) (`GroupMapping_AllowsUser_WhenMappingResolvesToAllowedInternalGroup`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/GroupMappingsAndSpecAuthTests.cs#L111`](file:////app/ModelContextGateway.Tests/GroupMappingsAndSpecAuthTests.cs#L111) (`GroupMapping_AllowsUser_WhenOidcGroupMapsToAllowedInternalGroup`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L319`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L319) (`Pairwise_SsoIdentityAndGroupMappings_EvaluateCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L10`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L10) (`OidcIdentityProvider_Parses_Remote_User_And_Groups_Headers`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L31`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L31) (`CompositeIdentityProvider_Falls_Back_To_Oidc_When_AD_Not_Authenticated`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L74`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L74) (`HeaderAuth_AllowsHeaders_ForTrustedProxy`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L97`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L97) (`OidcIdentityProvider_DoesNotMapAdminSid_ForAdminGroups`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L603`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L603) (`AuthMiddleware_CaseInsensitivity_BypassAndHeader_Check`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L53`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L53) (`GetAllProviders_ReturnsOkWithSecretAndAuthProviders`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L161`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L161) (`GetAuthProviders_ReturnsOkWithList`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L185`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L185) (`SaveAuthProvider_SavesSuccessfully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L11`](file:////app/ModelContextGateway.Tests/AuditSidAttributionTests.cs#L11) (`HeaderIdentityProvider_Extracts_RemoteUserSid_And_Populates_Sid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L330`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L330) (`HeaderIdentityProvider_DynamicallyLoadsAndAppliesDbConfig`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L137`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L137) (`GetMappings_ReturnsOk`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L207`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L207) (`DeleteMapping_DeletesSuccessfully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L9`](file:////app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L9) (`ProviderName_ReturnsComposite`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L17`](file:////app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L17) (`ResolveIdentityAsync_ReturnsFirstNonAnonymousUser`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L38`](file:////app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L38) (`ResolveIdentityAsync_FallsBackToAnonymous_WhenNoUserResolved`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L55`](file:////app/ModelContextGateway.Tests/CompositeIdentityProviderTests.cs#L55) (`ResolveIdentityAsync_FallsBackToOidcProvider_WhenAnonymous`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L644`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L644) (`AuthMiddleware_Allows_SSO_Session_With_RemoteUser_Header`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L60`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L60) (`handles provider fetch warnings gracefully when endpoints are unavailable`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L80`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L80) (`saves auth provider config and refreshes providers`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L109`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L109) (`handles auth provider save error and displays toast`)
  - [Playwright E2E] [`/app/frontend/e2e/multi-user-matrix.spec.ts#L36`](file:////app/frontend/e2e/multi-user-matrix.spec.ts#L36) (`Operator Context: allows overview and testbench navigation with operator identity`)

### `[AUTH-04]` ProvidersController validates LDAP test connections and handles connection errors gracefully.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (14):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L298`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L298) (`TestLdapConnection_ValidatesInputAndHandlesFailureGracefully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L23`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L23) (`ConvertSidBytesToString_FormatsValidBinarySid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L34`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L34) (`ConvertSidBytesToString_ReturnsEmpty_OnInvalidBytes`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L42`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L42) (`ResolveUserSidsAsync_ReturnsEmpty_WhenUsernameEmpty`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L53`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L53) (`ResolveUserSidsAsync_ReturnsEmpty_WhenServerNotConfigured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L79`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L79) (`ActiveDirectoryIdentityProvider_ReturnsAnonymous_WhenUntrustedProxy`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L96`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L96) (`ActiveDirectoryIdentityProvider_ReturnsAnonymous_WhenNotWindowsAuth`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L112`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L112) (`ActiveDirectoryIdentityProvider_ResolvesLdapSids_WhenLdapServiceProvided`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L365`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L365) (`LdapActiveDirectoryService_RespectsDisabledStatusInDatabase`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L11`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L11) (`ResolveUserSidsAsync_ReturnsEmpty_WhenLdapProviderDisabledInDb`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L59`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L59) (`ResolveUserSidsAsync_UsesCache_WhenCachedSidsExist`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L12`](file:////app/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L12) (`ResolveIdentityAsync_ExtractsWindowsIdentitySids_ViaAccessor`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L50`](file:////app/ModelContextGateway.Tests/ActiveDirectoryWindowsIdentityTests.cs#L50) (`ResolveIdentityAsync_AugmentsWithLdapSids_WhenLdapServiceProvided`)
  - [Playwright E2E] [`/app/frontend/e2e/ldap-identity-and-auth-flow.spec.ts#L5`](file:////app/frontend/e2e/ldap-identity-and-auth-flow.spec.ts#L5) (`should configure LDAP identity provider, test connection, and save settings`)

### `[AUTH-05]` McpServer supports AllowPassThroughAuth flag
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpServerTests.cs#L5`](file:////app/ModelContextGateway.Tests/McpServerTests.cs#L5) (`McpServer_Should_Have_AllowPassThroughAuth`)
  - [Playwright E2E] [`/app/frontend/e2e/my-mcp-servers.spec.ts#L7`](file:////app/frontend/e2e/my-mcp-servers.spec.ts#L7) (`should render user provided servers and allow editing credentials with SQLite schema`)

### `[AUTH-06]` Transports use passThroughToken when AllowPassThroughAuth is true
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L208`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L208) (`Transports_Use_PassThroughToken_If_Allowed`)

### `[AUTH-101]` HTTP transport injects X-Forwarded-User header based on connected user identity.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityHeaderTests.cs#L9`](file:////app/ModelContextGateway.Tests/IdentityHeaderTests.cs#L9) (`HttpTransport_InjectsXForwardedUserHeader`)

### `[AUTH-110]` GetClients returns list of OAuthClient records without secret hashes
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L51`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L51) (`GetClients_ReturnsOk_WithClientsAndMappedProperties`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L238`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L238) (`GetClients_NeverLeaksRawBearerSecretOrHash`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L343`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L343) (`CreateAppKey_AllowsUnlimited_WhenLimitsAreZero`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L135`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L135) (`ApplyConfigurationResponseContext_SetsRegistrationEndpoint`)

### `[AUTH-14]` Tool execution catches 401 Unauthorized from downstream target servers and returns interactive auth remediation.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173) (`ExecuteTargetToolAsync_Catches401_AndReturnsAuthPrompt`)

### `[AUTH-15]` OpenIddict initializes ephemeral development signing certificates in Development environment.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OpenIddictProductionTests.cs#L30`](file:////app/ModelContextGateway.Tests/OpenIddictProductionTests.cs#L30) (`Development_WithNoCert_BootsOnDevCerts`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OpenIddictProductionTests.cs#L46`](file:////app/ModelContextGateway.Tests/OpenIddictProductionTests.cs#L46) (`Production_WithValidPfx_Boots`)

### `[AUTH-35]` Single-user homelab startup initializes SQLite, auto-generates Admin and Client AppKeys without PFX certificate requirements
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L30`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L30) (`Homelab_ZeroConfigStartup_SeedsAdminAndClientKeys_AndPersistsFiles`)

### `[AUTH-36]` Pre-configured MCG_CLIENT_APP_KEYS seeds functional individualized client keys with custom scopes
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L98`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L98) (`Homelab_PreConfiguredClientKeys_SeedsIndividualizedScopedKeys`)

### `[AUTH-37]` AppKeys with server and category scopes enforce precise tool execution boundaries
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L168`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L168) (`AppKey_ScopeExtraction_ExtractsSemanticPrefixes`)

### `[AUTH-38]` LAN CIDR network configuration allows standalone web dashboard access from local subnet
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L183`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L183) (`Standalone_LanCidr_GrantsAdminAccessToLocalSubnet`)

### `[AUTH-39]` Zero-config startup defaults enterprise auth providers and secret providers to disabled
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L211`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L211) (`ZeroConfig_Startup_DefaultsEnterpriseProviders_ToDisabled`)

### `[AUTH-APPKEY-ADMIN-SCOPE-ALLOW]` AppKeys with admin scope grant Administrator role and pass AdminPolicy.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L79`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L79) (`AppKey_WithAdminScope_GrantsAdminAccess`)

### `[AUTH-APPKEY-ITEMS-SCOPE-ALLOW]` SecurityValidationHelper recognizes admin scopes in HttpContext.Items.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L255`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L255) (`IsAdmin_AppKeyScopes_InHttpContextItems_ReturnsTrue`)

### `[AUTH-APPKEY-WILDCARD-SCOPE-ALLOW]` AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L140`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L140) (`AppKey_WithWildcardScope_GrantsAdminAccess`)

### `[AUTH-COMPACT-APPKEY-TAXONOMY]` Generates compact ~32-character Base62 AppKeys with semantic prefixes.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L417`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L417) (`CreateCredentialAsync_GeneratesCompactKeysWithSemanticPrefixes`)

### `[AUTH-CUSTOM-ADMIN-KEY-SEEDING]` Seeds custom MCG_ADMIN_AUTH_KEY when provided in configuration.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L189`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L189) (`Startup_SeedsCustomAdminKey_WhenConfigured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L241`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L241) (`Startup_SeedsCustomAdminKey_WhenMcgAdminKeyConfigured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L292`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L292) (`Startup_UpdatesAdminKeyHash_WhenEnvironmentKeyChanges`)

### `[AUTH-PERSONAL-APPKEY-LIST]` Non-admin users can view their personal App Keys
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L125`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L125) (`GetAppKeys_NonAdmin_ReturnsOnlyPersonalKeys_ForCurrentUser`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeysCard.test.tsx#L34`](file:////app/frontend/src/test/components/AppKeysCard.test.tsx#L34) (`renders role-adapted My App Keys view for non-admin user`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeysCard.test.tsx#L79`](file:////app/frontend/src/test/components/AppKeysCard.test.tsx#L79) (`renders keys list, copies config snippet, and revokes key`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/App.test.tsx#L81`](file:////app/frontend/src/test/components/App.test.tsx#L81) (`renders role-adaptive UI for non-admin user`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L351`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L351) (`loads app keys and updates store`)
  - [Playwright E2E] [`/app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L5`](file:////app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L5) (`Non-Admin Context: displays My App Keys navigation and personal quota indicator`)

### `[AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE]` Custom user quotas override default limit
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L223`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L223) (`CreateAppKey_CustomQuotaOverride_AllowsHigherLimit`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeysCard.test.tsx#L222`](file:////app/frontend/src/test/components/AppKeysCard.test.tsx#L222) (`manages custom user quotas in admin quotas tab`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTab.test.tsx#L6`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L6) (`renders GeneralTab with security default quota inputs and triggers save`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTab.test.tsx#L71`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L71) (`updates form state when settings prop changes`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L594`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L594) (`sets user quota override and refreshes quota list`)
  - [Playwright E2E] [`/app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L135`](file:////app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L135) (`Admin Context: configures custom user quota override`)

### `[AUTH-PREFIX-EXTRACTION]` ExtractKeyPrefix parses semantic prefixes, Base62 selectors, and legacy tokens accurately.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L451`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L451) (`ExtractKeyPrefix_ExtractsSemanticAndLegacyPrefixesAccurately`)

### `[AUTH-QUERY-TOKEN-EXTRACTION]` Query string token middleware extracts access_token or token query parameter to Authorization header.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L7`](file:////app/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L7) (`QueryStringTokenMiddleware_Extracts_AccessToken_To_AuthorizationHeader`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L45`](file:////app/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L45) (`QueryStringTokenMiddleware_Extracts_Token_To_AuthorizationHeader`)

### `[AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW]` AdminPolicy succeeds in standalone mode for unauthenticated loopback requests.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L176`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L176) (`AdminPolicy_StandaloneMode_LoopbackIp_PassesAdminPolicy`)

### `[AUTH-STANDALONE-CUSTOM-CIDR-ALLOW]` Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L35`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L35) (`IsAdmin_StandaloneMode_CustomCidr_ReturnsTrue`)

### `[AUTH-STANDALONE-LOOPBACK-ALLOW]` Standalone mode without external IDP grants admin access to loopback IP addresses.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L14`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L14) (`IsAdmin_StandaloneMode_LoopbackIp_ReturnsTrue`)

### `[AUTH-SYSTEM-APPKEY-SEPARATION]` Personal AppKey with 'all' scope does not grant Administrator role
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (10):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L311`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L311) (`PersonalAppKey_WithAllScope_DoesNotGrantAdministratorRole`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L364`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L364) (`SystemAppKey_WithAdminScope_GrantsAdministratorRole`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L151`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L151) (`SystemAppKeys_RequireAdmin_AndSeparateFromPersonalKeys`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeysCard.test.tsx#L166`](file:////app/frontend/src/test/components/AppKeysCard.test.tsx#L166) (`handles admin tab switching and username filtering`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/App.test.tsx#L15`](file:////app/frontend/src/test/components/App.test.tsx#L15) (`renders header, navigation tabs, and default overview dashboard for admin user`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/App.test.tsx#L36`](file:////app/frontend/src/test/components/App.test.tsx#L36) (`switches between tabs on navigation click`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeyModal.test.tsx#L31`](file:////app/frontend/src/test/components/AppKeyModal.test.tsx#L31) (`allows admin to select key type and create system app key`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L335`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L335) (`switches keyTypeTab between personal and system`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L369`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L369) (`fetches system-filtered app keys via query parameters`)
  - [Playwright E2E] [`/app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L87`](file:////app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L87) (`Admin Context: manages segmented App-Level Keys and User Personal Keys`)

### `[UI-100]` initializes with empty providers
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L1`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L1) (`initializes with empty providers`)

### `[UI-101]` should initialize with default values
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useUserStore.test.ts#L1`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L1) (`should initialize with default values`)

### `[UI-109]` renders default standard mcpServers configuration with meta mode
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientSetupGuide.test.tsx#L1`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L1) (`renders default standard mcpServers configuration with meta mode`)
  - [Frontend Vitest] [`/app/frontend/src/test/pages/MyMcpServers.test.tsx#L102`](file:////app/frontend/src/test/pages/MyMcpServers.test.tsx#L102) (`renders client setup guide below credentials card`)

### `[UI-114]` renders nothing when isPolicyModalOpen is false
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/PolicyModal.test.tsx#L1`](file:////app/frontend/src/test/components/PolicyModal.test.tsx#L1) (`renders nothing when isPolicyModalOpen is false`)

### `[UI-120]` RBAC and SID mapping administration UI allows configuring role policies and SID associations
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/rbac-enforcement-flow.spec.ts#L1`](file:////app/frontend/e2e/rbac-enforcement-flow.spec.ts#L1) (`should create, verify, and delete RBAC policy and SID mapping`)

### `[UI-123]` should open App Keys & Security view and display client setup controls
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/client-setup-and-appkeys.spec.ts#L1`](file:////app/frontend/e2e/client-setup-and-appkeys.spec.ts#L1) (`should open App Keys & Security view and display client setup controls`)

### `[UI-125]` Admin role renders full administrative dashboard and server management controls
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/multi-user-matrix.spec.ts#L1`](file:////app/frontend/e2e/multi-user-matrix.spec.ts#L1) (`Admin Context: renders full administrator view and privileged controls`)

### `[UI-127]` should navigate to settings permissions tab and open policy configuration modal
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/rbac-and-permissions.spec.ts#L1`](file:////app/frontend/e2e/rbac-and-permissions.spec.ts#L1) (`should navigate to settings permissions tab and open policy configuration modal`)

### `[UI-129]` should create client application and generate AppKey with scope constraints
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/appkey-and-client-lifecycle.spec.ts#L1`](file:////app/frontend/e2e/appkey-and-client-lifecycle.spec.ts#L1) (`should create client application and generate AppKey with scope constraints`)

### `[CORE-101]` Auto-added requirement tracking
* **Category:** `CORE` (CORE)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SessionManagerTests.cs#L9`](file:////app/ModelContextGateway.Tests/SessionManagerTests.cs#L9) (`PerformanceMetrics_And_TotalRequests_IncrementCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SessionManagerTests.cs#L33`](file:////app/ModelContextGateway.Tests/SessionManagerTests.cs#L33) (`UpdateBackendStatus_TracksBackendHealth`)

### `[DB-01]` DatabaseSeeder initializes default router tables, settings, and seed servers.
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (17):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SeederAndDiscoveryTests.cs#L53`](file:////app/ModelContextGateway.Tests/SeederAndDiscoveryTests.cs#L53) (`DatabaseSeeder_SeedsDefaultData_Successfully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L85`](file:////app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L85) (`UserQuotaRepository_SetAndGet_ReturnsPersistedQuota`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L98`](file:////app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L98) (`UserQuotaRepository_GetAll_ReturnsAllUserQuotas`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L117`](file:////app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L117) (`UserQuotaRepository_Update_UpdatesExistingQuota`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L132`](file:////app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L132) (`UserQuotaRepository_Delete_RemovesQuota`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L199`](file:////app/ModelContextGateway.Tests/UserQuotaAndAppKeyRepositoryTests.cs#L199) (`DependencyInjection_RegistersIUserQuotaRepository`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L29`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L29) (`Sqlite_UpgradeMigration_FromLegacySchema_PreservesDataAndPassesValidation`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L80`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L80) (`SavePolicy_SavesSuccessfully_OnSqlite`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L91`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L91) (`SavePolicy_SavesSuccessfully_OnMySql`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L182`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L182) (`SaveMapping_SavesSuccessfully_OnSqlite`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbConnectionFactoryTests.cs#L10`](file:////app/ModelContextGateway.Tests/DbConnectionFactoryTests.cs#L10) (`Factory_Creates_Sqlite_Connection_By_Default`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbConnectionFactoryTests.cs#L28`](file:////app/ModelContextGateway.Tests/DbConnectionFactoryTests.cs#L28) (`Factory_Creates_MySql_Connection_When_Configured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbConnectionFactoryTests.cs#L46`](file:////app/ModelContextGateway.Tests/DbConnectionFactoryTests.cs#L46) (`Factory_Creates_MsSql_Connection_When_Configured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MinimalApiEndpointsTests.cs#L42`](file:////app/ModelContextGateway.Tests/MinimalApiEndpointsTests.cs#L42) (`GetServers_Returns_Server_List`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MultiDatabaseProviderIntegrationTests.cs#L9`](file:////app/ModelContextGateway.Tests/MultiDatabaseProviderIntegrationTests.cs#L9) (`DbConnectionFactory_Instantiates_SupportedProviders`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MultiDatabaseProviderIntegrationTests.cs#L42`](file:////app/ModelContextGateway.Tests/MultiDatabaseProviderIntegrationTests.cs#L42) (`JsonListTypeHandler_SerializesAndDeserializes_StringLists`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L30`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L30) (`Seeder_Initializes_Default_Settings_And_Providers`)

### `[DB-02]` MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L311`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L311) (`Mssql_Scripts_DeclareAllProceduresAndExpectedParameters`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L369`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L369) (`MySql_Scripts_DeclareAllProceduresWithP_PrefixParameters`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L708`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L708) (`Repositories_MySQL_AppKeyOperations_UseP_PrefixParameters`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MySqlLiveIntegrationTests.cs#L25`](file:////app/ModelContextGateway.Tests/MySqlLiveIntegrationTests.cs#L25) (`MySql_LiveRepository_AppKeyAndSecretProviderLifecycle_Succeeds`)

### `[DB-07]` SQLite upgrade migration automatically provisions OAuthClients table on legacy database
* **Category:** `DB` (Multi-Database Persistence & Migrations)
* **Type:** Positive Feature Capability
* **Verification Proofs (10):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L433`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L433) (`Sqlite_UpgradeMigration_ProvisionsOAuthClientsTable`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L543`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L543) (`Mssql_Migration004_DeclaresOAuthClientsTableAndProcedures`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L565`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L565) (`MySql_Migration004_DeclaresOAuthClientsTableAndProcedures`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L71`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L71) (`SaveAndGetOAuthClientById_Success`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L108`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L108) (`SaveOAuthClient_UpdateExisting_Success`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L150`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L150) (`GetOAuthClients_ReturnsAllClientsOrderedByCreatedAt`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L177`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L177) (`DeleteOAuthClient_ExistingClient_ReturnsTrueAndRemovesClient`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L198`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L198) (`DeleteOAuthClient_NonExistentClient_ReturnsFalse`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L206`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L206) (`GetOAuthClientById_NonExistentClient_ReturnsNull`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L335`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L335) (`Seeder_Initializes_OAuthClients_Table`)

### `[DOC-SETUP-SKILL-FRONTMATTER]` mcg-setup skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SetupSkillTests.cs#L18`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L18) (`Skill_Frontmatter_IsValidAndWithinCharacterLimit`)

### `[DOC-SETUP-SKILL-MIRROR]` The mcg-setup skill and templates are mirrored 1:1 in .agents/skills/mcg-setup/
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SetupSkillTests.cs#L152`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L152) (`Skill_MirroredInAgentsDirectory`)

### `[DOC-SETUP-SKILL-TEMPLATES]` All scaffold templates exist, are non-empty, and contain required directives such as responseBufferLimit, MCG_MASTER_KEY, and ghcr.io/spelech/model-context-gateway
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SetupSkillTests.cs#L98`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L98) (`Templates_AreValidAndContainRequiredDirectives`)

### `[DOC-SETUP-SKILL-WORKFLOW]` mcg-setup skill contains all 6 required setup phases including environment probing, hosting platforms, env vs UI trade-offs, identity/network topology, artifact generation, and health/client configuration
* **Category:** `DOC` (DOC)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SetupSkillTests.cs#L44`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L44) (`Skill_ContainsAllRequiredPhasesAndComparisons`)

### `[MCP-01]` Full end-to-end JSON-RPC protocol suite executes across SSE pipeline.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (60):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L85`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L85) (`Pipeline_POST_Sse_JSONRPC_Full_Protocol_Suite`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L142`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L142) (`Pipeline_POST_Message_FullProtocolSession_Suite`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L276`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L276) (`Pipeline_Server_CRUD_Endpoints`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L330`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L330) (`Pipeline_GET_Version_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L339`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L339) (`Pipeline_GET_Servers_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L429`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L429) (`Pipeline_GET_Stats_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L438`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L438) (`Pipeline_GET_Health_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L567`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L567) (`Pairwise_MetaMode_ExecuteTool_EnforcesTargetAuthorization`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L191`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L191) (`JsonNode_Rewrite_HandlesBatchCommentsAndCommas`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L336`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L336) (`SendRequestAsync_Succeeds_When_Response_Has_Method_Property`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L572`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L572) (`JsonNode_Rewrite_HandlesAdversarialEdgeCases`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L684`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L684) (`PlainJsonRpcMessages_DoNotCauseStackOverflow_PolymorphicVariants`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L57`](file:////app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L57) (`ProbeServerAsync_Sets_Connected_When_Endpoint_Responds_200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L97`](file:////app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L97) (`ProbeServerAsync_Sets_Failed_When_Endpoint_Throws_Exception`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L132`](file:////app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L132) (`ProbeServerAsync_Sets_Disabled_When_Server_Not_Enabled`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L161`](file:////app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L161) (`ProbeAllServersAsync_Probes_All_Enabled_Servers`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L190`](file:////app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L190) (`ProbeServerAsync_Sets_Connected_For_Valid_Stdio_Server_Without_Http_Probe`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L264`](file:////app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L264) (`ProbeServerAsync_Sets_Connected_For_Custom_Server`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolErrorFormatterTests.cs#L7`](file:////app/ModelContextGateway.Tests/ToolErrorFormatterTests.cs#L7) (`TransformError_FormatsJsonRpcErrorWithRemediation`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolErrorFormatterTests.cs#L27`](file:////app/ModelContextGateway.Tests/ToolErrorFormatterTests.cs#L27) (`TransformException_FormatsExceptionWithRemediation`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolErrorFormatterTests.cs#L42`](file:////app/ModelContextGateway.Tests/ToolErrorFormatterTests.cs#L42) (`GetActionableSuggestion_ReturnsExpectedCategory`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MinimalApiEndpointsTests.cs#L55`](file:////app/ModelContextGateway.Tests/MinimalApiEndpointsTests.cs#L55) (`Post_Put_Delete_Server_Lifecycle_Works`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L428`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L428) (`ClientSession_ExecuteTool_EnforcesCategoryScopeOnInnerTarget`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientSessionTests.cs#L59`](file:////app/ModelContextGateway.Tests/ClientSessionTests.cs#L59) (`ClientSession_InitializationAndLifecycle_ExecutesSuccessfully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L274`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L274) (`PolymorphicDeserialization_Correctly_Deserializes_JsonRpcMessage_Subclasses`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L302`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L302) (`Deserializing_Plain_JsonRpcMessage_Does_Not_Cause_StackOverflow`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L318`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L318) (`Serializing_Plain_JsonRpcMessage_Does_Not_Cause_StackOverflow`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L332`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L332) (`TestInitializationDiagnostics`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L895`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L895) (`ErrorTransformation_Cancellation_And_Sampling_Works_Correctly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1084`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1084) (`CustomFilesDirectoryHelper_CreatesDirectoriesCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1099`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1099) (`SessionManager_PerServerCache_WorksCorrectly`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerModal.test.tsx#L41`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L41) (`renders Add MCP Server form with default values when in add mode`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerModal.test.tsx#L62`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L62) (`renders Edit MCP Server form populated with server details when editing`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerModal.test.tsx#L83`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L83) (`switches to connection command when STDIO transport type is selected`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerModal.test.tsx#L104`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L104) (`shows custom header input when auth shape is custom-header or query`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerModal.test.tsx#L127`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L127) (`closes modal when cancel button or close X is clicked`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerModal.test.tsx#L147`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L147) (`submits form with correctly formatted payload including trimmed categories`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerCard.test.tsx#L66`](file:////app/frontend/src/test/components/ServerCard.test.tsx#L66) (`renders connecting/retrying state`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerCard.test.tsx#L83`](file:////app/frontend/src/test/components/ServerCard.test.tsx#L83) (`renders failed state with retry button`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerCard.test.tsx#L106`](file:////app/frontend/src/test/components/ServerCard.test.tsx#L106) (`renders disconnected state with connect button and hidden badge`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerCard.test.tsx#L129`](file:////app/frontend/src/test/components/ServerCard.test.tsx#L129) (`renders disabled state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L24`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L24) (`initializes with default state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L46`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L46) (`successfully loads servers and updates state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L65`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L65) (`triggers batch reconnect when refreshAll is true`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L85`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L85) (`handles server fetch errors gracefully and shows error toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L105`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L105) (`creates a new server via POST when no id is present`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L142`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L142) (`updates an existing server via PUT when id is present`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L176`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L176) (`shows error toast when save fails`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L194`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L194) (`sends PUT request to update server enabled state and refreshes`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L216`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L216) (`handles toggle failure with error toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L232`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L232) (`sends reconnect POST request and shows info toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L252`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L252) (`handles reconnect failure with error toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L325`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L325) (`updates search query and resets page to 1`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L338`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L338) (`updates sortBy and groupBy`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L352`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L352) (`updates page and pageSize`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L367`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L367) (`toggles group collapse state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L381`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L381) (`manages modal open/close actions`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L403`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L403) (`opens inspect modal and loads server inspection data`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L428`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L428) (`handles inspect failure with error toast`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L445`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L445) (`sets inspect active tab and search query`)

### `[MCP-02]` All MCP protocol capabilities enforce caller role authorizations consistently
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L385`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L385) (`Pairwise_AllCapabilities_UnderCallerRoles_EvaluateCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L34`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L34) (`ListToolsAsync_ReturnsMetaTools_InMetaMode`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L55`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L55) (`InvalidateCache_ClearsPopulatedState`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L363`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L363) (`ToolListing_And_Remapping_Works_Correctly`)

### `[MCP-05]` ResourceRoutingManager filters and matches MCP resources using semantic and keyword matching.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (10):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ResourceRoutingTests.cs#L5`](file:////app/ModelContextGateway.Tests/ResourceRoutingTests.cs#L5) (`SearchResourcesAsync_FiltersResourcesCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L61`](file:////app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L61) (`ValidateResourceUri_ValidatesUris`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L8`](file:////app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L8) (`SearchResourcesAsync_ReturnsAll_WhenQueryIsEmpty`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L23`](file:////app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L23) (`SearchResourcesAsync_FiltersByQuery_MatchingNameOrDescription`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L41`](file:////app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L41) (`ReadResourceAsync_LocalBuiltInResources_ReturnCorrectJson`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L82`](file:////app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L82) (`ListResourceTemplatesAsync_ReturnsBuiltInTemplates`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L426`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L426) (`ResourceRouting_And_UriTranslation_Works_Correctly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L782`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L782) (`BuiltInResources_Templates_And_Autocompletion_Works_Correctly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L992`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L992) (`CustomUserPrompts_And_Resources_Work_Correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ResourceTesterCard.test.tsx#L47`](file:////app/frontend/src/test/components/ResourceTesterCard.test.tsx#L47) (`handles custom URI input and submit`)

### `[MCP-06]` prompts/list aggregates, namespaces, and routes prompts to target backends.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L514`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L514) (`PromptListAggregation_And_Routing_Works_Correctly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L868`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L868) (`MetaPrompts_Works_Correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/PromptTesterCard.test.tsx#L53`](file:////app/frontend/src/test/components/PromptTesterCard.test.tsx#L53) (`triggers arg change and form submit`)

### `[MCP-08]` completion/complete forwards prompt completions to backend when caller is authorized.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L439`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L439) (`CompleteAsync_ForPrompt_ForwardsToBackend_WhenAuthorized`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L497`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L497) (`CompleteAsync_ForResourceTemplate_ForwardsToBackend_WhenAuthorized`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L555`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L555) (`CompleteAsync_LogsTemplate_ReturnsOnlyAuthorizedServers`)

### `[MCP-10]` DockerAutoDiscoveryService initializes with valid container service dependencies.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L45`](file:////app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L45) (`Service_Initializes_With_Valid_Dependencies`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L79`](file:////app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L79) (`ExecuteAsync_SkipsScan_WhenDockerSocketDoesNotExist`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L103`](file:////app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L103) (`ParseDiscoveredServers_ParsesValidDockerContainerLabels`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L137`](file:////app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L137) (`UpsertDiscoveredServers_AddsNewServers_AndDisablesStoppedServers`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SeederAndDiscoveryTests.cs#L83`](file:////app/ModelContextGateway.Tests/SeederAndDiscoveryTests.cs#L83) (`DockerAutoDiscovery_ScanContainers_HandlesMissingSocketGracefully`)

### `[MCP-12]` ApiEmbeddingService parses OpenAI-compatible vector responses and extracts float embeddings.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (19):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EmbeddingServiceTests.cs#L61`](file:////app/ModelContextGateway.Tests/EmbeddingServiceTests.cs#L61) (`ApiEmbeddingService_GetEmbeddingAsync_Returns_Vector_From_OpenAI_Response`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ApiEmbeddingServiceTests.cs#L5`](file:////app/ModelContextGateway.Tests/ApiEmbeddingServiceTests.cs#L5) (`CalculateCosineSimilarity_ComputesSimilarity`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ApiEmbeddingServiceTests.cs#L17`](file:////app/ModelContextGateway.Tests/ApiEmbeddingServiceTests.cs#L17) (`ReloadSettings_UpdatesSettings`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SeederAndDiscoveryTests.cs#L100`](file:////app/ModelContextGateway.Tests/SeederAndDiscoveryTests.cs#L100) (`SemanticSearchService_Fallback_With_DummyEmbeddings`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SemanticSearchServiceTests.cs#L42`](file:////app/ModelContextGateway.Tests/SemanticSearchServiceTests.cs#L42) (`SearchToolsSemanticAsync_ScoresAndRanksTools`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SemanticSearchServiceTests.cs#L61`](file:////app/ModelContextGateway.Tests/SemanticSearchServiceTests.cs#L61) (`SearchTools_KeywordMatching_WorksCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SemanticSearchServiceTests.cs#L78`](file:////app/ModelContextGateway.Tests/SemanticSearchServiceTests.cs#L78) (`SearchToolsSemanticAsync_FallsBackToKeyword_WhenEmbeddingServiceThrows`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L64`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L64) (`CallToolAsync_SearchTools_ReturnsSemanticResults`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L62`](file:////app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L62) (`DynamicEmbeddingService_Gets_And_Saves_Settings`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L108`](file:////app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L108) (`ReloadSettings_UpdatesSettingsAndActiveService`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L125`](file:////app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L125) (`DynamicEmbeddingService_GetEmbeddingAsync_Uses_ApiProvider_When_Configured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L149`](file:////app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L149) (`CosineSimilarity_Calculates_Correct_Vector_Distance`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L173`](file:////app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L173) (`PreWarmAsync_Executes_Without_Throwing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L193`](file:////app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L193) (`GenerateEmbeddingAsync_Uses_UnderlyingProvider`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L683`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L683) (`SemanticToolSearchRanking_Sorts_By_Score`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L7`](file:////app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L7) (`Service_InitializesAndSetsUpPaths`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L20`](file:////app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L20) (`ReloadSettings_ClearsSessionAndTokenizerState`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L33`](file:////app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L33) (`CosineSimilarity_CalculatesOrthogonalAndIdenticalVectors`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L51`](file:////app/ModelContextGateway.Tests/OnnxEmbeddingServiceTests.cs#L51) (`GetEmbeddingAsync_ReturnsEmpty384Vector_ForEmptyString`)

### `[MCP-15]` All JSON-RPC results return a resultType discriminator (complete or input_required) per MCP 2026-07-28 spec.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L7`](file:////app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L7) (`EnsureResultType_AttachesComplete_WhenMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L23`](file:////app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L23) (`EnsureResultType_PreservesExistingResultType`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L39`](file:////app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L39) (`EnsureResultType_HandlesNullResult`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L53`](file:////app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L53) (`EnsureResultType_HandlesJsonElement`)

### `[MCP-21]` Admin endpoint handles direct Streamable HTTP POST tools/list request returning JSON even with Accept text/event-stream header.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (8):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L370`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L370) (`AdminEndpoint_DirectPost_ToolsList_ReturnsJson_EvenWithSseAcceptHeader`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L401`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L401) (`AdminEndpoint_DirectPost_Notification_ReturnsAccepted`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L421`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L421) (`TargetAdminEndpoint_DirectPost_ToolsList_ReturnsJson`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L8`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L8) (`Middleware_Parses_2026_Spec_Headers`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L36`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L36) (`Middleware_Falls_Back_To_Json_Body_When_Headers_Missing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L64`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L64) (`Middleware_Matches_Admin_And_Target_Proxy_Paths`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L86`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L86) (`Middleware_Detects_Notifications_Correctly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L103`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L103) (`Middleware_Skips_Non_Mcp_Paths`)

### `[MCP-23]` AdminMcpServer HandleInitializeAsync includes subscriptions capability in capabilities object.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L651`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L651) (`HandleInitializeAsync_Advertises_Subscriptions_Capability`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L663`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L663) (`ProcessRequestAsync_Handles_Subscriptions_Listen`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L194`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L194) (`Middleware_Parses_Subscriptions_Listen_Request`)

### `[MCP-24]` McpSpecMiddleware extracts OpenTelemetry W3C traceparent, tracestate, and baggage from headers and _meta.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L219`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L219) (`Middleware_Extracts_Trace_Context_From_Headers_And_Meta`)

### `[MCP-ADMIN-ENDPOINT-CALL-TOOL]` Admin endpoint /admin/message executes tools/call for manage_system diagnostics.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L294`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L294) (`AdminEndpoint_SseSession_CallTool_ManageSystemDiagnostics`)

### `[MCP-ADMIN-ENDPOINT-HEAD-REQUEST]` Admin endpoint /admin handles HEAD request returning text/event-stream headers.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L212`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L212) (`AdminEndpoint_HeadRequest_ReturnsEventStreamHeaders`)

### `[MCP-ADMIN-ENDPOINT-LIST-TOOLS]` Admin endpoint /admin/message executes tools/list over active SSE session and returns 10 admin tools.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L224`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L224) (`AdminEndpoint_SseSession_ListTools`)

### `[MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET]` Target proxy endpoint /router-admin routes directly to the Admin MCP server.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L149`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L149) (`TargetProxy_RouterAdmin_RoutesToAdminServer`)

### `[MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE]` Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L62`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L62) (`AdminEndpoint_SseHandshake_NegotiatesProtocol`)

### `[MCP-ADMIN-INITIALIZE-HANDSHAKE]` AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L195`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L195) (`HandleInitializeAsync_NegotiatesProtocolVersion`)

### `[MCP-ADMIN-PARITY-APPKEYS]` manage_appkeys supports full parity for list, get_limits, create, and revoke actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L370`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L370) (`ManageAppKeys_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-CLIENTS]` manage_clients supports full parity for register, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L423`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L423) (`ManageClients_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-CUSTOM-FILES]` manage_custom_files supports full parity for list, get, save, and delete prompt and resource files.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L716`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L716) (`ManageCustomFiles_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-GROUP-MAPPINGS]` manage_group_mappings supports full parity for list, save, and delete external-to-internal group mappings.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L530`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L530) (`ManageGroupMappings_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-JSONRPC-DISPATCH]` AdminMcpServer processes standard JSON-RPC 2.0 requests (tools/list, tools/call, ping).
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L873`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L873) (`AdminTools_ProcessRequest_JsonRpcProtocol`)

### `[MCP-ADMIN-PARITY-POLICIES]` manage_policies supports full parity for list, save, and delete access control policies.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L464`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L464) (`ManagePolicies_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-PROVIDERS]` manage_providers supports full parity for list, save_secret, test_vault, save_auth, and test_ldap actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L577`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L577) (`ManageProviders_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SERVERS]` Validates that the manage_servers tool provides comprehensive administrative capabilities including listing, retrieving, creating, updating, toggling, deleting, and reconnecting servers.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L234`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L234) (`ManageServers_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SETTINGS]` manage_settings supports full parity for get and update global router configurations.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L667`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L667) (`ManageSettings_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-SYSTEM]` manage_system supports full parity for diagnostics, get_logs, clear_logs, and query_audit actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L816`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L816) (`ManageSystem_Parity_AllActions`)

### `[MCP-ADMIN-PARITY-TEST-TOOL-CALL]` test_tool_call executes test bench backend tool calls and formats responses.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L846`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L846) (`TestToolCall_Execution_Parity`)

### `[MCP-ADMIN-PARITY-TOOLS-COVERAGE]` Ensures every UI management workflow is backed by a verified, equivalent action within the consolidated Admin MCP tools.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L191`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L191) (`AdminTools_ExecuteSuccessfully`)

### `[MCP-ADMIN-SKILL-E2E-PROVISIONING]` Admin automation templates and JSON-RPC tool calls successfully provision a blank-slate gateway instance end-to-end via HTTP /admin/message.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L176`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L176) (`EndToEnd_BlankSlateProvisioning_ConfiguresAllEntitiesViaAdminTools`)

### `[MCP-ADMIN-SKILL-FRONTMATTER]` mcg-admin skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L21`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L21) (`Skill_Frontmatter_IsValidAndWithinCharacterLimit`)

### `[MCP-ADMIN-SKILL-MIRROR]` mcg-admin skill files and templates are identically mirrored between skills/ and .agents/skills/ directories
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L147`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L147) (`Skill_MirroredInAgentsDirectory`)

### `[MCP-ADMIN-SKILL-TEMPLATES]` All mcg-admin scaffold templates exist, are non-empty, and contain valid JSON or scripts for Authentik, Keycloak, Entra, ActiveDirectory, Cloudflare, Vault, Embeddings, Docker, and shell automation
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L103`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L103) (`Templates_AllExistAndAreValidJsonOrScripts`)

### `[MCP-ADMIN-SKILL-WORKFLOW]` mcg-admin skill contains all 7 administration phases including diagnostics, secrets, auth providers, RBAC/group mappings, settings/embeddings, servers/clients, and live tool verification
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L47`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L47) (`Skill_ContainsAllRequiredPhasesAndProviderCookbooks`)

### `[MCP-ADMIN-TOOL-AUDIT-LOG]` AdminMcpServer tool calls record audit log entries with caller and tool name.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L266`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L266) (`CallToolAsync_RecordsAuditLog`)

### `[MCP-ADMIN-TOOL-MANAGE-APPKEYS]` AdminMcpServer executes manage_appkeys create, list, limits, and revoke actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L283`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L283) (`CallToolAsync_ManageAppKeys_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-CLIENTS]` AdminMcpServer executes manage_clients register, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L330`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L330) (`CallToolAsync_ManageClients_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-CUSTOM-FILES]` AdminMcpServer executes manage_custom_files save, get, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L504`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L504) (`CallToolAsync_ManageCustomFiles_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-GROUP-MAPPINGS]` AdminMcpServer executes manage_group_mappings save, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L402`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L402) (`CallToolAsync_ManageGroupMappings_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-POLICIES]` AdminMcpServer executes manage_policies save, list, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L368`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L368) (`CallToolAsync_ManagePolicies_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-PROVIDERS]` AdminMcpServer executes manage_providers list, save_secret, and save_auth actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L435`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L435) (`CallToolAsync_ManageProviders_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-SERVERS]` AdminMcpServer executes manage_servers list, get, create, update, toggle, and delete actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L214`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L214) (`CallToolAsync_ManageServers_ListAndCreate`)

### `[MCP-ADMIN-TOOL-MANAGE-SETTINGS]` AdminMcpServer executes manage_settings get and update actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L478`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L478) (`CallToolAsync_ManageSettings_Lifecycle`)

### `[MCP-ADMIN-TOOL-MANAGE-SYSTEM]` AdminMcpServer executes manage_system diagnostics, get_logs, clear_logs, and query_audit actions.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L558`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L558) (`CallToolAsync_ManageSystem_Lifecycle`)

### `[MCP-ADMIN-TOOLS-LIST-COUNT]` AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L147`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L147) (`ListToolsAsync_ReturnsTenConsolidatedTools`)

### `[UI-104]` renders resource tester with servers and resources
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ResourceTesterCard.test.tsx#L1`](file:////app/frontend/src/test/components/ResourceTesterCard.test.tsx#L1) (`renders resource tester with servers and resources`)

### `[UI-106]` renders connected server details with badges and triggers actions
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerCard.test.tsx#L1`](file:////app/frontend/src/test/components/ServerCard.test.tsx#L1) (`renders connected server details with badges and triggers actions`)

### `[UI-107]` renders prompt dropdown and filters by selected server
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/PromptTesterCard.test.tsx#L1`](file:////app/frontend/src/test/components/PromptTesterCard.test.tsx#L1) (`renders prompt dropdown and filters by selected server`)

### `[UI-112]` renders nothing when isAddEditOpen is false
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerModal.test.tsx#L1`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L1) (`renders nothing when isAddEditOpen is false`)

### `[UI-121]` should open Add Server modal and switch secret provider types
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/server-management.spec.ts#L1`](file:////app/frontend/e2e/server-management.spec.ts#L1) (`should open Add Server modal and switch secret provider types`)

### `[UI-126]` should open Server Inspect Modal if servers are present on dashboard
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/server-inspector.spec.ts#L1`](file:////app/frontend/e2e/server-inspector.spec.ts#L1) (`should open Server Inspect Modal if servers are present on dashboard`)

### `[AUTH-107]` RegisterClient successfully handles DCR requests when open DCR is enabled.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L36`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L36) (`RegisterClient_CreatesApplicationAndReturnsOk`)

### `[AUTH-109]` RegisterClient uses IOAuthClientRepository when IOpenIddictApplicationManager is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L94`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L94) (`RegisterClient_UsesOAuthClientRepository_WhenApplicationManagerNull`)
  - [Frontend Vitest] [`/app/frontend/src/test/pages/ConsentView.test.tsx#L16`](file:////app/frontend/src/test/pages/ConsentView.test.tsx#L16) (`renders client name from query string and sets form action`)
  - [Playwright E2E] [`/app/frontend/e2e/oauth-consent-flow.spec.ts#L5`](file:////app/frontend/e2e/oauth-consent-flow.spec.ts#L5) (`should render interactive OAuth consent screen and display requesting client name`)

### `[AUTH-113]` RegisterClient supports public clients with PKCE (token_endpoint_auth_method: none) and omits client secret.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L351`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L351) (`RegisterClient_PublicClient_SucceedsWithoutSecret`)

### `[AUTH-115]` RegisterClient dynamically binds requested scopes to OpenIddict application descriptor permissions.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L435`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L435) (`RegisterClient_DynamicScopes_AddedToPermissions`)

### `[AUTH-118]` RegisterClient deduplicates and reuses existing dynamic client registrations for matching client name and type without accumulating redundant records.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L556`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L556) (`RegisterClient_DuplicateDcrRequest_ReusesExistingClientIdAndUpdatesRecord`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L214`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L214) (`FindDcrClient_ReturnsMatchingClient`)

### `[AUTH-119]` CleanupClients endpoint triggers DCR pruning and returns total cleaned client count.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L333`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L333) (`CleanupClients_CallsRepoAndReturnsCleanedCount`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L237`](file:////app/ModelContextGateway.Tests/OAuthClientRepositoryTests.cs#L237) (`CleanupDcrClients_PrunesDuplicateRegistrations_AndExpiredClients`)

### `[SEC-01]` Encrypts SecretProvider configuration JSON at rest in database using Master Key.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (13):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L70`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L70) (`SaveSecretProvider_EncryptsConfigJson_AtRestInDatabase`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L99`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L99) (`SaveAuthProvider_EncryptsConfigJson_AtRestInDatabase`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L218`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L218) (`ProvidersController_MaskPreserving_PreservesExistingDecryptedSecret_WhenMaskSubmitted`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L72`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L72) (`SymmetricEncryptionHelper_EncryptsAndDecryptsCorrectly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseEncryptionTests.cs#L8`](file:////app/ModelContextGateway.Tests/DatabaseEncryptionTests.cs#L8) (`SqliteDatabase_IsEncrypted_WithSQLCipher`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L14`](file:////app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L14) (`EnsureVaultClientAsync_CreatesClient_WithAppRoleCredentials`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L35`](file:////app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L35) (`EnsureVaultClientAsync_LoadsFromSecretRepo_WhenConfigJsonHasAppRole`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L87`](file:////app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L87) (`ReloadConfigAsync_ClearsClient_ForcesRecreation`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L52`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L52) (`DbEncryptionKey_Warning_Detection_Works_Correctly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L70`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L70) (`Startup_MigratesLegacyKeysToHashedKeys`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SecretProvidersTab.test.tsx#L19`](file:////app/frontend/src/test/components/SecretProvidersTab.test.tsx#L19) (`renders provider inputs and submits updated configuration`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SecretProvidersTab.test.tsx#L90`](file:////app/frontend/src/test/components/SecretProvidersTab.test.tsx#L90) (`handles Test Vault connection button with success and failure responses`)
  - [Playwright E2E] [`/app/frontend/e2e/full-ui-flow-sse-vault.spec.ts#L8`](file:////app/frontend/e2e/full-ui-flow-sse-vault.spec.ts#L8) (`should register SSE server with Vault provider (Mount/Path/Field), verify badge, and run semantic search`)

### `[SEC-02]` GET /api/providers/secrets returns secret providers with 200 OK.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (27):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L375`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L375) (`Pipeline_GET_Providers_Secret_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L482`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L482) (`ConnectAndInitializeBackendAsync_WithVaultServer_ResolvesRetrieverFromRootServices_WhenHttpContextIsNull`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L79`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L79) (`GetSecretProviders_ReturnsOkWithList`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L121`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L121) (`SaveSecretProvider_SavesSuccessfully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L257`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L257) (`TestVaultConnection_ValidatesInputAndHandlesFailureGracefully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L349`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L349) (`SaveSecretProvider_HttpUrl_AllowedForLocalhost`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L373`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L373) (`SaveSecretProvider_HttpUrl_AllowedForSimpleHost`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L37`](file:////app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L37) (`GetSecretAsync_MintsTokenViaTokenExchange_AndCachesResponse`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L127`](file:////app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L127) (`CompositeSecretRetriever_RoutesOboAndPocketIdAliases_ToTokenExchangeRetriever`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L134`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L134) (`SseTransport_ResolveTokenAsync_Uses_Custom_Path_Field_And_Mount`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L160`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L160) (`HttpTransport_ResolveTokenAsync_Defaults_To_Url_And_ApiKey_When_Not_Configured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L294`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L294) (`VaultSecretRetriever_DynamicallyLoadsAndAppliesDbConfig_WithReload`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L344`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L344) (`StdioTransport_ShouldPassSecretViaEnvironmentVariables_AndNotCommandLine`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L429`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L429) (`StdioTransport_ShouldSanitizeAndMaskSecretsInLogs`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L8`](file:////app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L8) (`GetSecretForProviderAsync_ReturnsNull_WhenProviderIsNone`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L26`](file:////app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L26) (`GetSecretForProviderAsync_RoutesToTargetProvider_AndCachesValue`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L48`](file:////app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L48) (`GetSecretForProviderAsync_MatchesVaultAliasNames`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L11`](file:////app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L11) (`ProviderName_ReturnsHashiCorpVault`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L36`](file:////app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L36) (`EnsureVaultClientAsync_ReturnsNull_WhenCredentialsMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L56`](file:////app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L56) (`EnsureVaultClientAsync_CreatesClient_WhenValidConfig`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L78`](file:////app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L78) (`GetSecretAsync_ReturnsCachedValue_WhenPresent`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L92`](file:////app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L92) (`GetSecretAsync_ReturnsNull_WhenClientIsNull`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L41`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L41) (`successfully loads auth and secret providers`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L131`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L131) (`saves secret provider preserving Vault token and mount path`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L170`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L170) (`saves Windows Registry and Environment secret providers correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useProviderStore.test.ts#L205`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L205) (`handles secret provider save error with toast and throws`)
  - [Playwright E2E] [`/app/frontend/e2e/vault-approle-config-flow.spec.ts#L5`](file:////app/frontend/e2e/vault-approle-config-flow.spec.ts#L5) (`should configure Vault AppRole credentials and test connection in settings`)

### `[SEC-03]` Ensure TrustedProxyHelper supports CIDR ranges in XFF validation
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L370`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L370) (`TrustedProxyHelper_AllowsXForwardedFor_WhenChainIsFullyTrusted`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L431`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L431) (`TrustedProxyHelper_ConfiguredProxyTrusted_CIDR`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EnvironmentSecretRetrieverTests.cs#L5`](file:////app/ModelContextGateway.Tests/EnvironmentSecretRetrieverTests.cs#L5) (`EnvironmentSecretRetriever_RetrievesSecret_FromEnvironmentVariables`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SecretRetrieverTests.cs#L5`](file:////app/ModelContextGateway.Tests/SecretRetrieverTests.cs#L5) (`EnvironmentSecretRetriever_ReturnsEnvVariable_WhenExists`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SecretRetrieverTests.cs#L25`](file:////app/ModelContextGateway.Tests/SecretRetrieverTests.cs#L25) (`EnvironmentSecretRetriever_ReturnsNull_WhenVariableDoesNotExist`)

### `[SEC-04]` WindowsRegistrySecretRetriever handles non-Windows platforms gracefully and returns null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (4):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SecretRetrieverTests.cs#L34`](file:////app/ModelContextGateway.Tests/SecretRetrieverTests.cs#L34) (`WindowsRegistrySecretRetriever_HandlesNonWindowsGracefully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L12`](file:////app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L12) (`GetSecretAsync_ReturnsPlainString_WhenRegistryValueIsString`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L27`](file:////app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L27) (`GetSecretAsync_DecryptsDpapiBytes_WhenRegistryValueIsByteArray`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L51`](file:////app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L51) (`GetSecretAsync_ReturnsNull_WhenKeyNotFoundOrNull`)

### `[SEC-ADMIN-AUDIT-REDACTION]` AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L609`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L609) (`CallToolAsync_AuditLog_RedactsSensitivePayloadData`)

### `[SEC-GATEWAY-ZERO-CONFIG-BOOT]` Gateway boots from a blank slate with zero master key environment variables, auto-generates .master.key, and serves health and admin endpoints.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L352`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L352) (`Gateway_BlankSlate_WithoutMasterKeyEnv_AutoGeneratesKeyFileAndBootsSuccessfully`)

### `[SEC-KEY-PROVIDER-AUTOGEN]` EncryptionKeyProvider delegates to DbKeyHelper to auto-generate master key when unconfigured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L42`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L42) (`GetDbEncryptionKey_AutoGenerates_WhenUnconfigured`)

### `[SEC-KEY-PROVIDER-CONFIG]` EncryptionKeyProvider returns configured DB_ENCRYPTION_KEY or MCG_SECRET.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L28`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L28) (`GetDbEncryptionKey_UsesConfig_WhenProvided`)

### `[SEC-KEY-PROVIDER-FALLBACK]` EncryptionKeyProvider falls back to DB_ENCRYPTION_KEY when MCG_SECRET is unconfigured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L70`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L70) (`GetRouterSecret_FallsBackToDbEncryptionKey_WhenDbEncryptionKeyProvided`)

### `[SEC-KEY-PROVIDER-SECRET]` EncryptionKeyProvider returns configured MCG_SECRET.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L56`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L56) (`GetRouterSecret_UsesConfig_WhenProvided`)

### `[SEC-KEYFILE-AUTOGEN]` Blank-slate initialization auto-generates a 256-bit base64 master key and persists it to .master.key.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L63`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L63) (`ResolveDbEncryptionKey_AutoGeneratesAndPersistsKey_WhenBlankSlate`)

### `[SEC-KEYFILE-ENV-PRECEDENCE]` Explicit environment variables MCG_MASTER_KEY or MCG_SECRET take precedence over keyfiles.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L28`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L28) (`ResolveDbEncryptionKey_ReturnsConfiguredEnvKey_WhenPresent`)

### `[SEC-KEYFILE-FILE-OVER-KEYFILE]` Explicit file secrets take precedence over persistent .master.key files.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L123`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L123) (`ResolveDbEncryptionKey_FileSecretTakesPrecedenceOverKeyFile`)

### `[SEC-KEYFILE-FILE-SECRET]` File-based secrets configured via MCG_MASTER_KEY_FILE or standard Docker secrets paths are resolved.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L45`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L45) (`ResolveDbEncryptionKey_ReturnsFileSecret_WhenKeyFileSpecified`)

### `[SEC-KEYFILE-HIERARCHY-PRECEDENCE]` Explicit environment variables take precedence over file secrets and keyfiles.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L101`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L101) (`ResolveDbEncryptionKey_EnvVarTakesPrecedenceOverFileSecretAndKeyFile`)

### `[SEC-KEYFILE-RELOAD]` Existing .master.key file is loaded across gateway restarts without key mutation.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L83`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L83) (`ResolveDbEncryptionKey_LoadsExistingKeyFile_OnSubsequentBoot`)

### `[SEC-KEYSOURCE-DETECTION]` Correctly identifies KeySource origin for environment, file, and auto-generated keys.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L144`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L144) (`ResolveDbEncryptionKey_IdentifiesKeySourceAccurately`)

### `[SEC-KEYSOURCE-SETCACHEDKEY]` SetCachedKey sets in-memory encryption key and updates ActiveKeySource.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L314`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L314) (`SetCachedKey_UpdatesCachedKeyAndActiveKeySource`)

### `[SEC-MASTERKEY-ATOMIC-REENCRYPTION]` Rejects POST /api/config/master-key when key source is external.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L481`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L481) (`Pipeline_POST_MasterKey_RejectsWhenExternalKeySource`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L142`](file:////app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L142) (`SetMasterKey_AtomicallyReEncryptsDatabaseCredentials`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L241`](file:////app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L241) (`SetMasterKey_RejectsWhenKeySourceIsExternalOrVault`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L259`](file:////app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L259) (`AdminMcpServer_ManageSystem_SetMasterKey_ReencryptsCleanly`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L338`](file:////app/ModelContextGateway.Tests/MasterKeyReEncryptionTests.cs#L338) (`SetMasterKey_RejectsInvalidOrShortKeys`)

### `[SEC-MASTERKEY-CONFIGURED-STATUS-BADGE]` Displays configured badge and rotate button when custom master key is configured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTab.test.tsx#L192`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L192) (`renders configured badge and rotate key button when master key is Configured`)

### `[SEC-MASTERKEY-CUSTOM-MODAL-REENCRYPTION]` Validates master key inputs (length, match) and triggers atomic re-encryption.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/MasterKeyModal.test.tsx#L6`](file:////app/frontend/src/test/components/MasterKeyModal.test.tsx#L6) (`validates key inputs and submits custom master key to callback`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/MasterKeyModal.test.tsx#L54`](file:////app/frontend/src/test/components/MasterKeyModal.test.tsx#L54) (`generates a strong random master key when auto-generate button is clicked`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/MasterKeyModal.test.tsx#L85`](file:////app/frontend/src/test/components/MasterKeyModal.test.tsx#L85) (`displays validation error when onSetMasterKey returns failure`)

### `[SEC-MASTERKEY-EXTERNAL-LOCKED-BADGE]` Displays locked badge when master key is externally managed via Vault or Environment.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTab.test.tsx#L149`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L149) (`renders locked badge when master key is managed externally`)

### `[SEC-MASTERKEY-UI-STATUS-BANNER]` Displays warning banner when keySource is AutoGenerated and opens custom master key modal.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTab.test.tsx#L115`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L115) (`renders AutoGenerated warning banner and opens MasterKeyModal`)

### `[SEC-VAULT-BOOTSTRAPPING]` Bootstraps master encryption key directly from HashiCorp Vault when VAULT_ADDR is configured.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L191`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L191) (`ResolveDbEncryptionKey_BootstrapsFromVault_WhenVaultConfigured`)

### `[SEC-VAULT-CUSTOM-PATH]` Bootstraps master key from Vault using custom mount path and secret key name.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L236`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L236) (`ResolveDbEncryptionKey_BootstrapsFromVault_WithCustomPathAndKeyName`)

### `[UI-105]` renders system logs and handles level filter
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/LogsTerminalCard.test.tsx#L1`](file:////app/frontend/src/test/components/LogsTerminalCard.test.tsx#L1) (`renders system logs and handles level filter`)

### `[TRANS-01]` SendRequestAsync times out cleanly and removes pending completion handlers without leaking memory.
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (11):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L276`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L276) (`SendRequestAsync_TimesOutCleanly_AndDoesNotLeak`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L419`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L419) (`SseBackend_Notification_IsForwardedToClient_WithAllFieldsIntact`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SseTransportTests.cs#L11`](file:////app/ModelContextGateway.Tests/SseTransportTests.cs#L11) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L7`](file:////app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L7) (`IsValidStdioCommand_ValidatesExecutableAndDisallowsUnsafeCommands`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L38`](file:////app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L38) (`IsValidServerUrl_Accepts_Valid_Http_Urls`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L17`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L17) (`SseTransport_ApplyAuthAndCustomHeaders_Formats_Standard_Headers`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L45`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L45) (`SseTransport_ApplyAuthAndCustomHeaders_Formats_CustomHeader`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L67`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L67) (`SseTransport_ApplyAuthAndCustomHeaders_Appends_QueryParameter`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L90`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L90) (`HttpTransport_ApplyAuthAndCustomHeaders_Formats_CustomHeader`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L112`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L112) (`SseTransport_ApplyAuthAndCustomHeaders_Parses_HeadersJson`)
  - [Playwright E2E] [`/app/frontend/e2e/full-ui-flow-http-direct.spec.ts#L8`](file:////app/frontend/e2e/full-ui-flow-http-direct.spec.ts#L8) (`should register HTTP server with Direct Key, verify status badge, and execute tool in Test Bench`)

### `[TRANS-02]` Multiplexes concurrent client calls sharing identical JSON-RPC IDs and routes reversed responses correctly.
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (12):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L12`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L12) (`ConcurrentResponseIsolation_TwoCallersSameId_SucceedsWithReversedResponseOrder`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L114`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L114) (`HighConcurrencyResponseIsolation_RepeatedIdsAcrossCallers`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L219`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L219) (`TimeoutAndCancellationCleanup_DoesNotLeavePendingRequests`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L281`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L281) (`BackendDisconnectCleanup_ClearsPendingRequests`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L352`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L352) (`ConcurrentResponseIsolation_ExplicitNullId_Succeeds`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L427`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L427) (`ConcurrentResponseIsolation_Notification_DoesNotExpectResponse`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L492`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L492) (`ClientSession_ConcurrentStatelessRequestIsolateCancellation`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L552`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L552) (`ClientSession_TargetedCancellation_DoesNotCancelOtherClientsReusingId`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L621`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L621) (`ConcurrentResponseIsolation_MixedNumericStringNullIds`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L494`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L494) (`AsynchronousRouting_HighVolumeAndPolymorphic_DoesNotHang`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/HttpTransportTests.cs#L11`](file:////app/ModelContextGateway.Tests/HttpTransportTests.cs#L11) (`ResolveTokenAsync_ReturnsApiKey_WhenProviderNone`)
  - [Playwright E2E] [`/app/frontend/e2e/full-ui-flow-stdio-env.spec.ts#L8`](file:////app/frontend/e2e/full-ui-flow-stdio-env.spec.ts#L8) (`should register STDIO server, verify card, and execute echo tool via Test Bench`)

### `[TRANS-03]` STDIO transport spawns subprocess, handles JSON-RPC initialization and executes tool calls
* **Category:** `TRANS` (Transports (SSE, HTTP, STDIO, Proxy))
* **Type:** Positive Feature Capability
* **Verification Proofs (5):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L49`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L49) (`StdioTransport_ShouldInitializeAndCallToolSuccessfully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L170`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L170) (`StdioTransport_ShouldRouteStderrToLogs`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L242`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L242) (`StdioTransport_ShouldSupportCancellationAndProcessTreeTermination`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L327`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L327) (`StdioTransport_ParseCommandLine_Handles_Quotes_And_Spaces`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L472`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L472) (`StdioTransport_ShouldDrainReaderStreamsToEOF_WhenProcessExitsImmediately`)

### `[UI-01]` renders CustomFileModal in create mode and displays visual builder tabs
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (61):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L26`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L26) (`renders CustomFileModal in create mode and displays visual builder tabs`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L41`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L41) (`allows adding and removing arguments in visual builder`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L68`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L68) (`allows adding and removing messages in visual builder`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L95`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L95) (`switches between Raw JSON Editor and Visual Prompt Builder with synchronization`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L177`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L177) (`changes file type to resources and adjusts extension`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L194`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L194) (`submits form and calls saveCustomFile`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L222`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L222) (`renders in edit mode when editingFileMeta is set`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L244`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L244) (`allows adding assistant messages, modifying argument required checkbox, and rendering empty arguments state`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LayoutCentering.test.tsx#L19`](file:////app/frontend/src/test/components/LayoutCentering.test.tsx#L19) (`renders top navigation bar with centered alignment in layout.css and App`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LayoutCentering.test.tsx#L42`](file:////app/frontend/src/test/components/LayoutCentering.test.tsx#L42) (`renders tester tabs with centered alignment in tester.css`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LayoutCentering.test.tsx#L58`](file:////app/frontend/src/test/components/LayoutCentering.test.tsx#L58) (`renders SettingsView sub-navigation bar with centered alignment`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LayoutCentering.test.tsx#L74`](file:////app/frontend/src/test/components/LayoutCentering.test.tsx#L74) (`renders AppKeysCard sub-navigation tabs with centered alignment for admin`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LayoutCentering.test.tsx#L90`](file:////app/frontend/src/test/components/LayoutCentering.test.tsx#L90) (`uses body::before and body::after pseudo-elements for ambient gradients and removes background-decor DOM nodes`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LayoutCentering.test.tsx#L117`](file:////app/frontend/src/test/components/LayoutCentering.test.tsx#L117) (`defines focus-visible outline indicators for interactive focus styling`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/MappingModal.test.tsx#L27`](file:////app/frontend/src/test/components/MappingModal.test.tsx#L27) (`renders create mapping form with empty inputs`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/MappingModal.test.tsx#L42`](file:////app/frontend/src/test/components/MappingModal.test.tsx#L42) (`renders edit mapping form pre-filled with mapping data`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/MappingModal.test.tsx#L57`](file:////app/frontend/src/test/components/MappingModal.test.tsx#L57) (`submits form with externalId and internalGroup`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/MappingModal.test.tsx#L82`](file:////app/frontend/src/test/components/MappingModal.test.tsx#L82) (`closes modal on cancel click`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ConfirmModal.test.tsx#L32`](file:////app/frontend/src/test/components/ConfirmModal.test.tsx#L32) (`renders title, message, and action buttons when open`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ConfirmModal.test.tsx#L58`](file:////app/frontend/src/test/components/ConfirmModal.test.tsx#L58) (`calls handleConfirm when confirm button clicked`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ConfirmModal.test.tsx#L82`](file:////app/frontend/src/test/components/ConfirmModal.test.tsx#L82) (`calls handleCancel when cancel button clicked`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsTabs.test.tsx#L49`](file:////app/frontend/src/test/components/SettingsTabs.test.tsx#L49) (`renders IdentityAuthTab and SecretProvidersTab inside ProvidersTab`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsTabs.test.tsx#L77`](file:////app/frontend/src/test/components/SettingsTabs.test.tsx#L77) (`renders CustomFilesTab and triggers modal open and delete`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsTabs.test.tsx#L110`](file:////app/frontend/src/test/components/SettingsTabs.test.tsx#L110) (`renders AccessControlTab with policies and mappings`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsTabs.test.tsx#L139`](file:////app/frontend/src/test/components/SettingsTabs.test.tsx#L139) (`renders BackupsTab`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/DashboardView.test.tsx#L115`](file:////app/frontend/src/test/components/DashboardView.test.tsx#L115) (`renders empty state when no servers match search`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsView.test.tsx#L144`](file:////app/frontend/src/test/components/SettingsView.test.tsx#L144) (`saves embedding settings and displays success feedback`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsView.test.tsx#L183`](file:////app/frontend/src/test/components/SettingsView.test.tsx#L183) (`saves Auth Provider configurations including Active Directory and OIDC header mappings`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsView.test.tsx#L240`](file:////app/frontend/src/test/components/SettingsView.test.tsx#L240) (`saves secret providers while preserving Vault config and secrets`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsView.test.tsx#L311`](file:////app/frontend/src/test/components/SettingsView.test.tsx#L311) (`renders custom files table with edit and delete actions`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsView.test.tsx#L354`](file:////app/frontend/src/test/components/SettingsView.test.tsx#L354) (`renders access policies and group mappings with CRUD actions`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/Header.test.tsx#L38`](file:////app/frontend/src/test/components/Header.test.tsx#L38) (`renders admin badge and shield icon for full_admin users`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/Header.test.tsx#L64`](file:////app/frontend/src/test/components/Header.test.tsx#L64) (`renders standard user badge for non-admin users`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/Header.test.tsx#L90`](file:////app/frontend/src/test/components/Header.test.tsx#L90) (`does not render user status item when unauthenticated`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/Header.test.tsx#L110`](file:////app/frontend/src/test/components/Header.test.tsx#L110) (`displays gateway status and SSE endpoint`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/Header.test.tsx#L126`](file:////app/frontend/src/test/components/Header.test.tsx#L126) (`toggles light and dark theme on button click and updates document attribute`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SharedComponents.test.tsx#L26`](file:////app/frontend/src/test/components/SharedComponents.test.tsx#L26) (`renders title, children, and handles close button click`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SharedComponents.test.tsx#L50`](file:////app/frontend/src/test/components/SharedComponents.test.tsx#L50) (`renders various statuses correctly with indicators`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SharedComponents.test.tsx#L72`](file:////app/frontend/src/test/components/SharedComponents.test.tsx#L72) (`returns null when totalItems is 0`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SharedComponents.test.tsx#L91`](file:////app/frontend/src/test/components/SharedComponents.test.tsx#L91) (`renders page info and navigation controls`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SharedComponents.test.tsx#L130`](file:////app/frontend/src/test/components/SharedComponents.test.tsx#L130) (`handles pageSize all`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/TestBenchView.test.tsx#L71`](file:////app/frontend/src/test/components/TestBenchView.test.tsx#L71) (`handles semantic search queries in SemanticRouterCard`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/TestBenchView.test.tsx#L99`](file:////app/frontend/src/test/components/TestBenchView.test.tsx#L99) (`executes tool and updates console`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/TestBenchView.test.tsx#L130`](file:////app/frontend/src/test/components/TestBenchView.test.tsx#L130) (`executes prompt get in prompt tester tab`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/TestBenchView.test.tsx#L164`](file:////app/frontend/src/test/components/TestBenchView.test.tsx#L164) (`executes resource read in resource inspector tab`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/HeaderBranding.test.tsx#L37`](file:////app/frontend/src/test/components/HeaderBranding.test.tsx#L37) (`identifies FontAwesome class names and invalid inputs as non-image URLs`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/HeaderBranding.test.tsx#L54`](file:////app/frontend/src/test/components/HeaderBranding.test.tsx#L54) (`updates document.title and sets custom image favicon when icon is an image URL`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/HeaderBranding.test.tsx#L69`](file:////app/frontend/src/test/components/HeaderBranding.test.tsx#L69) (`sets default title and generated SVG favicon when branding is null or uses FontAwesome icon`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/HeaderBranding.test.tsx#L87`](file:////app/frontend/src/test/components/HeaderBranding.test.tsx#L87) (`renders img element with logo-icon logo-img class when branding.icon is an image endpoint`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/HeaderBranding.test.tsx#L115`](file:////app/frontend/src/test/components/HeaderBranding.test.tsx#L115) (`renders FontAwesome i element when branding.icon is a FontAwesome class`)
  - [Frontend Vitest] [`/app/frontend/src/test/api/typedApi.test.ts#L74`](file:////app/frontend/src/test/api/typedApi.test.ts#L74) (`calls client and appkey endpoints correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/api/typedApi.test.ts#L125`](file:////app/frontend/src/test/api/typedApi.test.ts#L125) (`calls user quota endpoints correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/api/typedApi.test.ts#L154`](file:////app/frontend/src/test/api/typedApi.test.ts#L154) (`calls policies and mappings endpoints correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/api/typedApi.test.ts#L181`](file:////app/frontend/src/test/api/typedApi.test.ts#L181) (`calls settings, providers, custom files, approvals endpoints correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/api/typedApi.test.ts#L237`](file:////app/frontend/src/test/api/typedApi.test.ts#L237) (`calls testbench tool, prompt, resource, log endpoints correctly`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useConfirmStore.test.ts#L31`](file:////app/frontend/src/test/stores/useConfirmStore.test.ts#L31) (`opens confirmation modal and resolves true when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useConfirmStore.test.ts#L58`](file:////app/frontend/src/test/stores/useConfirmStore.test.ts#L58) (`resolves false when cancelled`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useConfirmStore.test.ts#L76`](file:////app/frontend/src/test/stores/useConfirmStore.test.ts#L76) (`settles existing pending promise with false when a new confirmation is opened`)
  - [Playwright E2E] [`/app/frontend/e2e/prompts-resources-customfiles.spec.ts#L42`](file:////app/frontend/e2e/prompts-resources-customfiles.spec.ts#L42) (`should navigate to Custom Files and Prompts in Settings view`)
  - [Playwright E2E] [`/app/frontend/e2e/dashboard.spec.ts#L23`](file:////app/frontend/e2e/dashboard.spec.ts#L23) (`should display aggregate statistics cards`)
  - [Playwright E2E] [`/app/frontend/e2e/dashboard.spec.ts#L37`](file:////app/frontend/e2e/dashboard.spec.ts#L37) (`should filter servers using search input`)

### `[UI-02]` Inspect modal displays spinner loading state while querying server capabilities
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (6):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerInspectModal.test.tsx#L61`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L61) (`renders loading state when inspectLoading is true`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerInspectModal.test.tsx#L79`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L79) (`renders tools tab with schema and handles tab switching`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerInspectModal.test.tsx#L116`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L116) (`renders resources tab items and handles search filtering`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerInspectModal.test.tsx#L144`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L144) (`renders prompts tab with arguments and empty state when filtered out`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerInspectModal.test.tsx#L166`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L166) (`renders empty states for tabs when data is empty`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerInspectModal.test.tsx#L194`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L194) (`closes modal when close button is clicked`)

### `[UI-03]` Grouped server view renders category sections and supports collapsible groups
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/DashboardView.test.tsx#L63`](file:////app/frontend/src/test/components/DashboardView.test.tsx#L63) (`renders grouped server view by category and allows collapsing`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/DashboardView.test.tsx#L90`](file:////app/frontend/src/test/components/DashboardView.test.tsx#L90) (`renders grouped server view by status and type`)

### `[UI-04]` Tool selector filters available tools by selected backend server
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (7):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ToolTesterCard.test.tsx#L77`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L77) (`filters tools by selected server and handles tool change`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ToolTesterCard.test.tsx#L106`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L106) (`filters custom tools with no namespace prefix when selectedServer is custom`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ToolTesterCard.test.tsx#L131`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L131) (`renders dynamic fields for boolean, number, string, array, and object types`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ToolTesterCard.test.tsx#L178`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L178) (`renders empty state when selected tool takes no arguments`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ToolTesterCard.test.tsx#L203`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L203) (`switches to raw JSON tab and handles raw JSON editing`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ToolTesterCard.test.tsx#L242`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L242) (`handles form submission`)
  - [Playwright E2E] [`/app/frontend/e2e/prompts-resources-customfiles.spec.ts#L5`](file:////app/frontend/e2e/prompts-resources-customfiles.spec.ts#L5) (`should interact with Prompt Tester and Resource Tester cards in Test Bench`)

### `[UI-05]` Router allows customized branding parameters (DashboardTitle, DashboardIcon) to be saved and retrieved via the API.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L253`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L253) (`Pipeline_Settings_Branding_ReadWrite`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L6`](file:////app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L6) (`renders branding label and FontAwesome icon preview when icon is a CSS class`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/HeaderBranding.test.tsx#L7`](file:////app/frontend/src/test/components/HeaderBranding.test.tsx#L7) (`identifies image URLs and paths accurately`)

### `[UI-06]` Router supports uploading and retrieving custom branding logo images via dedicated endpoints.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L447`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L447) (`Branding_Logo_Upload_And_Retrieval_Works`)

### `[UI-07]` Audits desktop viewport layout for zero horizontal overflow and high UX score.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (2):**
  - [Playwright E2E] [`/app/frontend/e2e/layout-inspector.spec.ts#L38`](file:////app/frontend/e2e/layout-inspector.spec.ts#L38) (`should pass layout audit on desktop 1080p viewport`)
  - [Playwright E2E] [`/app/frontend/e2e/layout-inspector.spec.ts#L64`](file:////app/frontend/e2e/layout-inspector.spec.ts#L64) (`should pass layout audit on Samsung Galaxy S25+ mobile viewport`)

### `[UI-102]` Dashboard renders stats card, connected server list, and setup instructions
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/DashboardView.test.tsx#L1`](file:////app/frontend/src/test/components/DashboardView.test.tsx#L1) (`renders stats card, server list, and client setup guide`)

### `[UI-103]` Interactive tool tester renders server and tool selection dropdowns
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ToolTesterCard.test.tsx#L1`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L1) (`renders initial server and tool selection options`)

### `[UI-108]` renders nothing when isMappingModalOpen is false
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/MappingModal.test.tsx#L1`](file:////app/frontend/src/test/components/MappingModal.test.tsx#L1) (`renders nothing when isMappingModalOpen is false`)

### `[UI-110]` renders title, MCG badge, subtitle, and version badge
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/Header.test.tsx#L1`](file:////app/frontend/src/test/components/Header.test.tsx#L1) (`renders title, MCG badge, subtitle, and version badge`)

### `[UI-111]` renders GeneralTab and triggers save
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsTabs.test.tsx#L1`](file:////app/frontend/src/test/components/SettingsTabs.test.tsx#L1) (`renders GeneralTab and triggers save`)

### `[UI-113]` renders tab navigation and switches active subviews
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/SettingsView.test.tsx#L1`](file:////app/frontend/src/test/components/SettingsView.test.tsx#L1) (`renders tab navigation and switches active subviews`)

### `[UI-115]` renders test bench cards and switches tabs
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/TestBenchView.test.tsx#L1`](file:////app/frontend/src/test/components/TestBenchView.test.tsx#L1) (`renders test bench cards and switches tabs`)

### `[UI-116]` Modal remains hidden when isInspectOpen is false
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ServerInspectModal.test.tsx#L1`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L1) (`renders nothing when isInspectOpen is false`)

### `[UI-117]` returns null when isOpen is false
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/SharedComponents.test.tsx#L1`](file:////app/frontend/src/test/components/SharedComponents.test.tsx#L1) (`returns null when isOpen is false`)

### `[UI-119]` calls server endpoints correctly
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/api/typedApi.test.ts#L1`](file:////app/frontend/src/test/api/typedApi.test.ts#L1) (`calls server endpoints correctly`)

### `[UI-122]` should navigate to Settings view and configure vector embedding options
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/settings.spec.ts#L1`](file:////app/frontend/e2e/settings.spec.ts#L1) (`should navigate to Settings view and configure vector embedding options`)

### `[UI-124]` Renders main dashboard navigation tabs and layout headers
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/dashboard.spec.ts#L1`](file:////app/frontend/e2e/dashboard.spec.ts#L1) (`should render the dashboard layout and header components`)

### `[UI-128]` should navigate to Test Bench view and render tester cards
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Playwright E2E] [`/app/frontend/e2e/testbench.spec.ts#L1`](file:////app/frontend/e2e/testbench.spec.ts#L1) (`should navigate to Test Bench view and render tester cards`)

### `[UI-30]` Renders client registration form with inputs for name, client type, redirect URIs, grant types, scopes, and expiration.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (3):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientModal.test.tsx#L27`](file:////app/frontend/src/test/components/ClientModal.test.tsx#L27) (`renders client registration form with rich OAuth fields and cancel button`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientModal.test.tsx#L55`](file:////app/frontend/src/test/components/ClientModal.test.tsx#L55) (`submits registration form with parsed scopes array and OAuth metadata`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/ClientModal.test.tsx#L94`](file:////app/frontend/src/test/components/ClientModal.test.tsx#L94) (`renders one-time secret display result card with copy buttons when createdClientResult is populated`)

### `[UI-32]` Registers OAuth client with extended metadata (redirect URIs, grant types, client type, expiration) and captures one-time credentials.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Positive Feature Capability
* **Verification Proofs (1):**
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L76`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L76) (`creates client with one-time secret result and refreshes list`)

---

## 3. Boundary & Guardrail Invariants ("What the Application DOES NOT DO")

> [!IMPORTANT]
> The following guardrails define strict security boundaries, fail-closed fault invariants, and forbidden application states.

### `[AUTH-01]` Dashboard management API suite executes for authorized administrators.
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (47):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L170`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L170) (`Pipeline_Dashboard_Management_Suite`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L304`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L304) (`Pipeline_Permissions_Policy_And_Mapping_CRUD`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L348`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L348) (`Pipeline_GET_Clients_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L357`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L357) (`Pipeline_GET_Permissions_Policies_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L121`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L121) (`SSE_ValidatesIdentityPerMessage`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L228`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L228) (`SecurityValidationHelper_IsAdmin_RequiresAdminGroupSid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L246`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L246) (`SecurityValidationHelper_IsAdmin_AllowsAdminGroupName`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L260`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L260) (`SecurityValidationHelper_IsAdmin_RejectsNonAdminGroups`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L277`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L277) (`SecurityValidationHelper_IsAdmin_AllowsCustomAdminGroupsArray`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L292`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L292) (`SecurityValidationHelper_IsAdmin_AllowsMappedGroups`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L307`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L307) (`OidcIdentityProvider_DoesNotGrantAdminSid_FromGroupOrUserNames`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L13`](file:////app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L13) (`AdminPolicy_Allows_Principal_With_AdminGroupName`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L47`](file:////app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L47) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L81`](file:////app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L81) (`AdminPolicy_Allows_Principal_With_ConfiguredAdminGroups`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L116`](file:////app/ModelContextGateway.Tests/AdminPolicyHybridAuthTests.cs#L116) (`AdminPolicy_Denies_StandardRole_WithoutAdminSidOrGroup`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L48`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L48) (`GetPolicies_ReturnsOk`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L114`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L114) (`DeletePolicy_DeletesSuccessfully`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L16`](file:////app/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L16) (`AdminPolicy_Denies_StandardRole_Without_AdminSid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L58`](file:////app/ModelContextGateway.Tests/AdminPolicySidOnlyTests.cs#L58) (`AdminPolicy_Allows_Principal_With_AdminSid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L153`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L153) (`AdminBypass_AllowsAllCapabilities_EvenWithoutDbPolicies`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L211`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L211) (`ServerLevelPolicy_AuthorizesAllCapabilitiesUnderServer`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L283`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L283) (`ListToolsAsync_FiltersUnauthorizedTools`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L328`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L328) (`ListPromptsAsync_FiltersUnauthorizedPrompts`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L371`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L371) (`ListResourcesAsync_FiltersUnauthorizedResources`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L414`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L414) (`ListResourceTemplatesAsync_FiltersUnauthorizedTemplates`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L94`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L94) (`RBAC_AllowsUser_WhenPolicyMatchesRequiredGroup`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L200`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L200) (`ToolsList_FiltersByAuthorization`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L403`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L403) (`QuotaEndpoints_Admin_CanManageCustomUserQuotas`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L258`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L258) (`AppKeysController_CreateAppKey_UnknownCategory_Admin_Succeeds`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/IdentityAuthTab.test.tsx#L12`](file:////app/frontend/src/test/components/IdentityAuthTab.test.tsx#L12) (`renders Active Directory disabled initially, toggles on and exposes fields`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/IdentityAuthTab.test.tsx#L46`](file:////app/frontend/src/test/components/IdentityAuthTab.test.tsx#L46) (`fills LDAP parameters and executes test connection`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/PolicyModal.test.tsx#L28`](file:////app/frontend/src/test/components/PolicyModal.test.tsx#L28) (`renders create policy form with default inputs`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/PolicyModal.test.tsx#L44`](file:////app/frontend/src/test/components/PolicyModal.test.tsx#L44) (`renders edit policy form pre-filled with policy data`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/PolicyModal.test.tsx#L87`](file:////app/frontend/src/test/components/PolicyModal.test.tsx#L87) (`closes modal on cancel click`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useUserStore.test.ts#L23`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L23) (`successfully loads user profile from /api/me`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useUserStore.test.ts#L50`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L50) (`handles error response gracefully and sets unauthenticated user state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useUserStore.test.ts#L69`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L69) (`handles network failure gracefully`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useUserStore.test.ts#L89`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L89) (`correctly handles non-admin user role extraction`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useUserStore.test.ts#L113`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L113) (`successfully updates version and service from /health endpoint`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useUserStore.test.ts#L128`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L128) (`keeps existing fallback version on error`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L21`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L21) (`initializes with empty policies and mappings`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L38`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L38) (`fetches access policies and updates store`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L53`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L53) (`creates/saves a policy (ALLOW rule) and closes modal`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L156`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L156) (`fetches group mappings and updates store`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L171`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L171) (`saves a group mapping and closes mapping modal`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L314`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L314) (`handles policy modal open and close`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L330`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L330) (`handles mapping modal open and close`)

### `[AUTH-112]` DeleteClient removes OAuthClient via repository
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L120`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L120) (`DeleteClient_ReturnsNoContent_WhenAppExists`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L141`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L141) (`DeleteClient_ReturnsNotFound_WhenAppDoesNotExist`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L297`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L297) (`Authorize_ResolvesClientAndRedirectsToConsent`)

### `[AUTH-PERSONAL-APPKEY-CREATE]` Non-admin users can create personal App Keys up to quota
* **Category:** `AUTH` (Authentication, RBAC & Identity)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (9):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L191`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L191) (`CreateAppKey_NonAdmin_CreatesPersonalKey_UpToDefaultQuota`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeyModal.test.tsx#L19`](file:////app/frontend/src/test/components/AppKeyModal.test.tsx#L19) (`renders nothing when isCreateModalOpen is false`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeyModal.test.tsx#L61`](file:////app/frontend/src/test/components/AppKeyModal.test.tsx#L61) (`locks key type to personal key for non-admin and shows quota feedback`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeyModal.test.tsx#L115`](file:////app/frontend/src/test/components/AppKeyModal.test.tsx#L115) (`handles scope serialization for server scope and target username for admin`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeyModal.test.tsx#L154`](file:////app/frontend/src/test/components/AppKeyModal.test.tsx#L154) (`handles scope serialization for category scope and expiration days`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeyModal.test.tsx#L192`](file:////app/frontend/src/test/components/AppKeyModal.test.tsx#L192) (`disables submit button when quota limit is reached`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/AppKeyModal.test.tsx#L217`](file:////app/frontend/src/test/components/AppKeyModal.test.tsx#L217) (`displays one-time secret result and copies plaintext key to clipboard`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L421`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L421) (`creates category-scoped key, captures one-time plaintext key, and refreshes`)
  - [Playwright E2E] [`/app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L33`](file:////app/frontend/e2e/personal-appkeys-and-quotas.spec.ts#L33) (`Non-Admin Context: mints personal key, views snippet, and revokes key`)

### `[AUTH-EXTERNAL-IDP-DENIES-ANONYMOUS-LOOPBACK]` When an external IDP is configured, anonymous loopback requests do not bypass authentication.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L224`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L224) (`AdminPolicy_ExternalIdpConfigured_LoopbackIp_RequiresCredentials`)

### `[AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY]` AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L200`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L200) (`AdminPolicy_StandaloneMode_ExternalUntrustedIp_FailsAdminPolicy`)

### `[AUTH-STANDALONE-EXTERNAL-DENY]` Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L57`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L57) (`IsAdmin_StandaloneMode_UntrustedIp_ReturnsFalse`)

### `[GUARD-01]` JsonRpcStateManager rejects registration and cancels pending completions upon disconnect.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (43):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L585`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L585) (`JsonRpcStateManager_Disconnect_PreventsRegistrationAndCancelsPending`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/GroupMappingsAndSpecAuthTests.cs#L125`](file:////app/ModelContextGateway.Tests/GroupMappingsAndSpecAuthTests.cs#L125) (`GroupMapping_RejectsUser_WhenNoMappingExistsForRestrictedTarget`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L468`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L468) (`Pairwise_NullOrEmptyTarget_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L490`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L490) (`Pairwise_CorruptedAppKeyScopesJson_FailsClosed_ReturnsFalse`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L90`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L90) (`SaveSecretProvider_ReturnsBadRequest_WhenProviderNameMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L172`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L172) (`SaveAuthProvider_ReturnsBadRequest_WhenProviderNameMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L58`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L58) (`SavePolicy_ReturnsBadRequest_WhenTargetIdMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L69`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L69) (`SavePolicy_ReturnsBadRequest_WhenRequiredGroupMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L160`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L160) (`SaveMapping_ReturnsBadRequest_WhenExternalIdMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L171`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L171) (`SaveMapping_ReturnsBadRequest_WhenInternalGroupMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L270`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L270) (`CreateClient_ReturnsBadRequest_WhenDisplayNameMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L283`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L283) (`CreateClient_ReturnsBadRequest_WhenCategoryScopeEmpty`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L300`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L300) (`CreateClient_Returns500_WhenOAuthClientRepositoryThrows`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L317`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L317) (`DeleteClient_Returns500_WhenOAuthClientRepositoryThrows`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L352`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L352) (`CleanupClients_Returns500_WhenOAuthClientRepositoryThrows`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L176`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L176) (`NonAdmin_DefaultsToDeny_WhenNoMatchingPoliciesConfigured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L197`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L197) (`IsUserAuthorizedAsync_FailsClosed_OnNullOrWhitespaceTarget`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L234`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L234) (`ExplicitDeny_OverridesGroupAllow`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L579`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L579) (`CompleteAsync_ForPrompt_ThrowsUnauthorized_WhenCallerDenied`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L611`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L611) (`CompleteAsync_ForResourceTemplate_ThrowsUnauthorized_WhenCallerDenied`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L643`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L643) (`CompleteAsync_FailsClosed_OnUnknownOrUnresolvedTargets`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L84`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L84) (`RBAC_DefaultsToDenied_WhenNoPoliciesConfigured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L106`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L106) (`RBAC_RejectsUser_WhenPolicyRequiresDifferentGroup`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L118`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L118) (`RBAC_RejectsUser_OnExplicitDeny`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L130`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L130) (`CallToolAsync_ReturnsError_WhenUnauthorized`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L145`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L145) (`GetPromptAsync_ThrowsUnauthorized_WhenUnauthorized`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L159`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L159) (`ReadResourceAsync_ThrowsUnauthorized_WhenUnauthorized`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L93`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L93) (`CallToolAsync_ExecuteTool_ReturnsError_WhenNameMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L120`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L120) (`CallToolAsync_ReturnsCancellationError_WhenCancelled`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L149`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L149) (`CallToolAsync_ThrowsKeyNotFound_WhenToolNotInRoutingTable`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L312`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L312) (`CreateAppKey_ReturnsBadRequest_WhenNameMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L324`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L324) (`CreateAppKey_EnforcesUserLimit_ForNonAdmin`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L369`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L369) (`RevokeAppKey_ReturnsNotFound_WhenIdDoesNotExist`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L378`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L378) (`RevokeAppKey_ReturnsForbid_WhenUserNotOwnerOrAdmin`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L433`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L433) (`QuotaEndpoints_Validation_ReturnsBadRequest_OnInvalidInputs`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L220`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L220) (`AppKeysController_CreateAppKey_UnknownCategory_NonAdmin_FailsWithBadRequest`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L239`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L239) (`AppKeysController_CreateAppKey_EmptyCategory_FailsWithBadRequest`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L304`](file:////app/ModelContextGateway.Tests/CategoryScopedAppKeysTests.cs#L304) (`ClientsController_CreateClient_EmptyCategory_ReturnsBadRequest`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L44`](file:////app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L44) (`ValidateToolOrPromptName_ValidatesNames`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L67`](file:////app/ModelContextGateway.Tests/ResourceRoutingManagerTests.cs#L67) (`ReadResourceAsync_ThrowsKeyNotFound_WhenResourceNotRegistered`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L606`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L606) (`AuthMiddleware_Blocks_Unauthorized_Request`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/PolicyModal.test.tsx#L60`](file:////app/frontend/src/test/components/PolicyModal.test.tsx#L60) (`submits form with constructed payload for DENY policy`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L86`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L86) (`handles policy save failure with error toast`)

### `[GUARD-02]` LdapService rejects unencrypted LDAP with InvalidOperationException.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (15):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L188`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L188) (`LdapService_ThrowsInvalidOperation_WhenUseSslFalse`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L208`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L208) (`LdapService_ThrowsSecurityException_OnBindFailure`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L283`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L283) (`ResolveDbEncryptionKey_ThrowsInvalidOperationException_WhenVaultFails`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SseTransportTests.cs#L32`](file:////app/ModelContextGateway.Tests/SseTransportTests.cs#L32) (`ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SseTransportTests.cs#L53`](file:////app/ModelContextGateway.Tests/SseTransportTests.cs#L53) (`ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/HttpTransportTests.cs#L31`](file:////app/ModelContextGateway.Tests/HttpTransportTests.cs#L31) (`ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/HttpTransportTests.cs#L54`](file:////app/ModelContextGateway.Tests/HttpTransportTests.cs#L54) (`ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L218`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L218) (`HttpTransport_SendRequestAsync_Throws_When_Impersonation_Missing_WindowsIdentity`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L10`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L10) (`EscapeLdapFilter_EscapesSpecialCharacters`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L64`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceTests.cs#L64) (`ResolveUserSidsAsync_ThrowsInvalidOperation_WhenPlaintextLdapConfigured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L394`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L394) (`StdioTransport_ShouldFailClosed_WhenSecretResolutionFails`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L37`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L37) (`ResolveUserSidsAsync_ThrowsInvalidOperation_WhenDbConfigSpecifiesPlaintextLdap`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L78`](file:////app/ModelContextGateway.Tests/LdapActiveDirectoryServiceIntegrationTests.cs#L78) (`ResolveUserSidsAsync_FailsClosedWithSecurityException_OnUnreachableServer`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L61`](file:////app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L61) (`EnsureVaultClientAsync_ReturnsNull_WhenVaultProviderDisabledInRepo`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L113`](file:////app/ModelContextGateway.Tests/VaultAppRoleAndRenewalTests.cs#L113) (`GetSecretAsync_ThrowsSecurityException_OnVaultException`)

### `[GUARD-03]` TokenExchangeSecretRetriever fails closed with InvalidOperationException when token endpoint is not configured.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (13):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L94`](file:////app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L94) (`GetSecretAsync_ThrowsInvalidOperationException_WhenTokenEndpointMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L102`](file:////app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L102) (`GetSecretAsync_ThrowsSecurityException_WhenHttpResponseIsNotSuccess`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L184`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L184) (`SseTransport_ResolveTokenAsync_FailsClosed_WhenVaultResolvesNull`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L104`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L104) (`StdioTransport_ShouldThrowSecurityExceptionForUnsafeExecutable`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L126`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L126) (`StdioTransport_ShouldThrowSecurityExceptionForShellExecutable`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L148`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L148) (`StdioTransport_ShouldThrowOnInvalidExecutable`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L210`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L210) (`StdioTransport_ShouldTimeoutOnSlowRequests`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/StdioTransportTests.cs#L289`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L289) (`StdioTransport_ShouldHandleUnexpectedExit`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L17`](file:////app/ModelContextGateway.Tests/CompositeSecretRetrieverTests.cs#L17) (`GetSecretForProviderAsync_ThrowsInvalidOperationException_WhenProviderNotRegistered`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L19`](file:////app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L19) (`EnsureVaultClientAsync_ThrowsArgumentException_WhenAddressInvalidScheme`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L104`](file:////app/ModelContextGateway.Tests/VaultSecretRetrieverTests.cs#L104) (`GetSecretAsync_UsesCustomVaultClientFactory`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L66`](file:////app/ModelContextGateway.Tests/WindowsRegistrySecretRetrieverTests.cs#L66) (`GetSecretAsync_HandlesExceptionGracefully_ReturnsNull`)
  - [Playwright E2E] [`/app/frontend/e2e/multi-user-matrix.spec.ts#L55`](file:////app/frontend/e2e/multi-user-matrix.spec.ts#L55) (`Guest / Denied Context: restricted user session renders safely`)

### `[GUARD-04]` Malformed completion payloads or unmapped backends must fail closed safely
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (24):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L508`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L508) (`Pairwise_CompleteAsync_MalformedOrMissingBackends_ThrowsOrFailsClosed`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L531`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L531) (`Pairwise_DatabaseDisconnection_FailsClosedSafely`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L64`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L64) (`GetAllProviders_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L140`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L140) (`SaveSecretProvider_Returns500_WhenRepositoryThrows`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L206`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L206) (`SaveAuthProvider_Returns500_WhenRepositoryThrows`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L227`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L227) (`GetSecretProviders_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L242`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L242) (`GetAuthProviders_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L163`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L163) (`SchemaValidation_FailsClosed_WhenRequiredColumnOrTableMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L189`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L189) (`SchemaValidation_FailsClosed_WhenUserQuotasOrKeyTypeMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L587`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L587) (`SchemaValidation_FailsClosed_WhenOAuthClientsTableMissing`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L124`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L124) (`DeletePolicy_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L147`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L147) (`GetMappings_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L193`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L193) (`SaveMapping_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L217`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L217) (`DeleteMapping_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L230`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L230) (`GetPolicies_Returns500_OnDbException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L173`](file:////app/ModelContextGateway.Tests/FineGrainedRbacTests.cs#L173) (`RBAC_DefaultsToDenied_WhenDbExceptionThrown`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L216`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L216) (`AuditLogger_ThrowsException_OnDatabaseError`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L240`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L240) (`CallTool_FailsClosed_WhenAuditLogFails`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L279`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L279) (`CallTool_FailsClosed_WhenAuditLoggerUnresolved`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L449`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L449) (`Controllers_HandleDbFailures_Returning500`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditLoggerTests.cs#L91`](file:////app/ModelContextGateway.Tests/AuditLoggerTests.cs#L91) (`LogInvocationAsync_ThrowsInvalidOperationException_OnConnectionFailure`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditLoggerTests.cs#L104`](file:////app/ModelContextGateway.Tests/AuditLoggerTests.cs#L104) (`LogAdminActionAsync_ThrowsInvalidOperationException_OnConnectionFailure`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/MultiDatabaseProviderIntegrationTests.cs#L29`](file:////app/ModelContextGateway.Tests/MultiDatabaseProviderIntegrationTests.cs#L29) (`DbConnectionFactory_Throws_OnUnsupportedProvider`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L104`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L104) (`AuditLogger_AuditFailClosed_RefusesInvocation_OnAuditWriteError`)

### `[GUARD-05]` Docker auto-discovery skips containers resolving to blocked private IP ranges (SSRF protection).
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (16):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L58`](file:////app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L58) (`DockerDiscovery_SkipsContainer_ResolvingToPrivateIp`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/EmbeddingServiceTests.cs#L83`](file:////app/ModelContextGateway.Tests/EmbeddingServiceTests.cs#L83) (`ApiEmbeddingService_GetEmbeddingAsync_Throws_On_Http_Error`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L712`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L712) (`Connect_BlocksPrivateOrLoopbackIPs_AtSocketLevel`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L744`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L744) (`SecurityValidationHelper_IsBlockedIp_ValidatesAllBlockedAndAllowedRanges`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L103`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L103) (`SaveSecretProvider_ReturnsBadRequest_WhenHttpUrlPassedInConfig`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L330`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L330) (`SaveAuthProvidersBatch_ReturnsBadRequest_WhenAllProvidersDisabled`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L397`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L397) (`SaveSecretProvider_HttpUrl_RejectedForExternal`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L232`](file:////app/ModelContextGateway.Tests/BackendHealthCheckServiceTests.cs#L232) (`ProbeServerAsync_Sets_Failed_For_Invalid_Stdio_Server_Command`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L28`](file:////app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L28) (`IsValidServerUrl_Rejects_Invalid_Http_Urls`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L53`](file:////app/ModelContextGateway.Tests/ServerEndpointsValidationTests.cs#L53) (`Validation_Rejects_TypeOnly_Update_Leaving_Incompatible_Url`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L256`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L256) (`FailClosedValidation_RejectsInvalidJson_AndInsecureUrls`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L7`](file:////app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L7) (`IsBlockedIp_ValidatesSpecialIpRanges`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L31`](file:////app/ModelContextGateway.Tests/SecurityValidationHelperTests.cs#L31) (`IsInSubnet_HandlesSpecialCases`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L84`](file:////app/ModelContextGateway.Tests/DynamicEmbeddingServiceTests.cs#L84) (`PrivateOrLoopback_Blocked_When_AllowPrivateIps_False`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L73`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L73) (`McpClient_NamedHttpClient_Applies_SsrfConnectCallback_AndBlocksPrivateIps`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1069`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1069) (`CustomFilesSanitization_PreventsDirectoryTraversal`)

### `[GUARD-06]` Header authentication strips remote identity headers when request is sent through untrusted proxy.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (13):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L52`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L52) (`HeaderAuth_StripsHeaders_ForUntrustedProxy`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L330`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L330) (`TrustedProxyHelper_DeniesLoopback_WhenNotExplicitlyAllowlisted`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L349`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L349) (`TrustedProxyHelper_DeniesXForwardedFor_WhenChainHasUntrustedHop`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L390`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L390) (`TrustedProxyHelper_Unconfigured_LoopbackTrusted_LANNotTrusted`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L412`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L412) (`TrustedProxyHelper_ConfiguredProxyTrusted`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/IdentityProviderTests.cs#L458`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L458) (`TrustedProxyHelper_ForgedHeaderFromLanHost_DegradesToGuest`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ChallengerTests.cs#L216`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L216) (`AuthMiddleware_CaseInsensitivity_Bypass_Check`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/OpenIddictProductionTests.cs#L12`](file:////app/ModelContextGateway.Tests/OpenIddictProductionTests.cs#L12) (`Production_WithNoCert_Throws_InvalidOperationException`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L243`](file:////app/ModelContextGateway.Tests/PermissionsControllerTests.cs#L243) (`SavePolicy_ReturnsBadRequest_WhenWildcardDenyPolicy`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CorsTests.cs#L20`](file:////app/ModelContextGateway.Tests/CorsTests.cs#L20) (`Cors_DefaultFallback_Allows_LocalhostOrigins`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CorsTests.cs#L50`](file:////app/ModelContextGateway.Tests/CorsTests.cs#L50) (`Cors_DefaultFallback_Denies_In_Production`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CorsTests.cs#L92`](file:////app/ModelContextGateway.Tests/CorsTests.cs#L92) (`Cors_WithConfiguredOrigins_RestrictsToConfigured`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/CorsTests.cs#L121`](file:////app/ModelContextGateway.Tests/CorsTests.cs#L121) (`Cors_WithAllowedOriginsKeyFallback_RestrictsToConfigured`)

### `[GUARD-ADMIN-CUSTOM-FILES-VALIDATION]` manage_custom_files rejects invalid prompt JSON syntax and unsupported file categories.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L779`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L779) (`ManageCustomFiles_ValidationGuardrails`)

### `[GUARD-ADMIN-ENDPOINT-UNAUTHORIZED]` Unauthenticated / non-admin client request to /admin receives 403 Forbidden.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L193`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L193) (`AdminEndpoint_UnauthorizedCaller_Returns403`)

### `[GUARD-ADMIN-POLICIES-WILDCARD-DENY]` manage_policies rejects wildcard deny policies to prevent global lockout.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L513`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L513) (`ManagePolicies_WildcardDenyGuardrail`)

### `[GUARD-ADMIN-PROVIDERS-LDAP-PLAINTEXT]` manage_providers rejects unencrypted LDAP connections on port 389.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L650`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L650) (`ManageProviders_LdapPlaintextGuardrail`)

### `[GUARD-ADMIN-SERVERS-VALIDATION]` Verifies that the manage_servers tool accurately enforces validation by rejecting malformed transport types, missing required parameters, and requests for non-existent servers.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L324`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L324) (`ManageServers_ValidationGuardrails`)

### `[GUARD-ADMIN-UNKNOWN-TOOL]` AdminMcpServer returns an error response for unknown tool or action invocations.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L592`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L592) (`CallToolAsync_UnknownToolOrAction_ReturnsErrorResponse`)

### `[MCP-ADMIN-TOOL-TEST-CALL-ERROR]` AdminMcpServer test_tool_call propagates downstream backend errors with visibility.
* **Category:** `GUARD` (Universal Safety & Fail-Closed Guardrails)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L633`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L633) (`CallToolAsync_TestToolCall_MissingServer_ReturnsError`)

### `[MCP-22]` AdminMcpServer ProcessRequestAsync handles server/discover request returning supported versions and subscriptions capability.
* **Category:** `MCP` (Model Context Protocol Engine & Tool Routing)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (3):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L682`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L682) (`ProcessRequestAsync_Handles_Server_Discover`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L124`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L124) (`Middleware_Extracts_Stateless_Capabilities_And_ClientInfo_In_Meta`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L162`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L162) (`Middleware_Rejects_Unsupported_Protocol_Version_With_32021_Error`)

### `[AUTH-106]` Exchange throws InvalidOperationException when request is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L19`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L19) (`Exchange_ThrowsInvalidOperationException_WhenRequestNull`)

### `[AUTH-108]` Authorize throws InvalidOperationException when OIDC request is null.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L77`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L77) (`Authorize_ThrowsInvalidOperationException_WhenRequestNull`)

### `[AUTH-111]` Pipeline exposes RFC 9728 OAuth Protected Resource discovery endpoints with dynamic resource identifiers.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (8):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L62`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L62) (`Pipeline_WellKnown_Endpoints_ReturnSuccess`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L84`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L84) (`CreateClient_ReturnsOk_WithGeneratedCredentials`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L153`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L153) (`DatabaseAssertion_PlaintextNotPersisted`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L179`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L179) (`CreateClient_AdminCreator_DoesNotInheritAdminSid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ClientsControllerTests.cs#L215`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L215) (`CreateClient_WithExpiresInDays_SetsExpiration`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L159`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L159) (`Exchange_ClientCredentials_ValidSecret_ReturnsSignInResult`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L206`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L206) (`Exchange_ClientCredentials_InvalidSecret_ReturnsForbid`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L251`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L251) (`Exchange_ClientCredentials_ExpiredClient_ReturnsForbid`)

### `[AUTH-114]` RegisterClient rejects invalid or non-absolute redirect URIs with standard RFC 7591 invalid_redirect_uri error.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L400`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L400) (`RegisterClient_InvalidRedirectUri_ReturnsBadRequest`)

### `[AUTH-116]` Exchange rejects client_credentials grant attempts by public clients with UnauthorizedClient error.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L475`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L475) (`Exchange_PublicClient_ClientCredentials_ReturnsForbid`)

### `[AUTH-117]` RegisterClient returns 403 Forbidden with access_denied when open client registration is disabled and caller is unauthorized.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (1):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L516`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L516) (`RegisterClient_WhenClosedRegistration_UnauthorizedUser_ReturnsForbidden`)

### `[SEC-05]` GET /api/audit returns audit log records with 200 OK.
* **Category:** `SEC` (Secrets Providers & Encryption)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (28):**
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L393`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L393) (`Pipeline_GET_Audit_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L420`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L420) (`Pipeline_GET_Logs_Returns200`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SanitizingLoggerProviderTests.cs#L8`](file:////app/ModelContextGateway.Tests/SanitizingLoggerProviderTests.cs#L8) (`SanitizingLoggerProvider_RedactsBearerTokensAndKeys`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SanitizingLoggerProviderTests.cs#L43`](file:////app/ModelContextGateway.Tests/SanitizingLoggerProviderTests.cs#L43) (`SanitizingLoggerProvider_LeavesPlainMessagesUnchanged`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/SanitizingLoggerProviderTests.cs#L76`](file:////app/ModelContextGateway.Tests/SanitizingLoggerProviderTests.cs#L76) (`SanitizingLoggerProvider_RedactsSecretsInExceptionMessageAndToString`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L130`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L130) (`ProvidersController_GetEndpoints_RedactSensitiveSecrets`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L185`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L185) (`ProvidersController_SaveEndpoints_RedactAuditLogPayloads`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L388`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L388) (`SaveSecretProvider_WhenDecryptionFailed_DoesNotOverwriteCorruptPayload`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L259`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L259) (`GetAppKeys_ReturnsSanitizedKeys_ForAdminAndFiltered`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L5`](file:////app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L5) (`SanitizePayload_Redacts_Bearer_Tokens`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L16`](file:////app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L16) (`SanitizePayload_Redacts_Api_Keys_And_Passwords`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L29`](file:////app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L29) (`SanitizePayload_Redacts_ConnectionString_Passwords`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L40`](file:////app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L40) (`LogBuffer_Add_Sanitizes_PII_Payloads`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L53`](file:////app/ModelContextGateway.Tests/PiiSanitizerTests.cs#L53) (`PiiSanitizer_Redacts_Basic_ApiKey_Cookie_QueryToken_UrlUserInfo`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditLoggerTests.cs#L60`](file:////app/ModelContextGateway.Tests/AuditLoggerTests.cs#L60) (`LogInvocationAsync_WritesEntryToDatabase`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditLoggerTests.cs#L75`](file:////app/ModelContextGateway.Tests/AuditLoggerTests.cs#L75) (`LogAdminActionAsync_WritesEntryToDatabase`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L60`](file:////app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L60) (`AuditQuery_ReturnsFilteredRows_AndLogsAuditAction`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L83`](file:////app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L83) (`SavePolicy_WritesAuditAction_OnSuccess`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L117`](file:////app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L117) (`SaveMapping_WritesAuditAction_OnSuccess`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L150`](file:////app/ModelContextGateway.Tests/AuditQueryApiTests.cs#L150) (`LogAdminActionAsync_WritesRowToAdminAuditLogs`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L143`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L143) (`AuditLogger_RecordsPerRequestActor_NotHandshakeActor`)
  - [Backend xUnit] [`/app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1134`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L1134) (`Mcp_SessionId_IsOpaque_NotBearerToken`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L56`](file:////app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L56) (`renders img live preview when dashboardIcon is an image URL`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L88`](file:////app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L88) (`updates dashboardIcon and live preview when a logo image file is uploaded`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L141`](file:////app/frontend/src/test/components/GeneralTabLogoUpload.test.tsx#L141) (`saves settings with the updated logo URL when form is submitted after upload`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LogsTerminalCard.test.tsx#L61`](file:////app/frontend/src/test/components/LogsTerminalCard.test.tsx#L61) (`renders RPC message stream with formatted JSON`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LogsTerminalCard.test.tsx#L81`](file:////app/frontend/src/test/components/LogsTerminalCard.test.tsx#L81) (`toggles autoscroll and handles clear logs`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/LogsTerminalCard.test.tsx#L108`](file:////app/frontend/src/test/components/LogsTerminalCard.test.tsx#L108) (`shows empty state when no logs match filter`)

### `[UI-31]` Fetches clients on mount and renders table headers and action buttons.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (7):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L42`](file:////app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L42) (`renders header, register button, and calls fetchClients on mount`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L73`](file:////app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L73) (`renders empty state when no registered clients exist`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L90`](file:////app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L90) (`renders rich client columns and handles client ID copy`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L136`](file:////app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L136) (`triggers deleteClient when Delete button is clicked`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L37`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L37) (`fetches registered clients and updates state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L213`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L213) (`prompts confirmation and calls cleanupClientsApi when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L239`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L239) (`cancels DCR cleanup when user cancels confirmation modal`)

### `[UI-CONFIRM-MODAL]` Renders confirmation dialog with title, message, and trigger buttons for confirm and cancel.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (16):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/ConfirmModal.test.tsx#L6`](file:////app/frontend/src/test/components/ConfirmModal.test.tsx#L6) (`renders nothing when closed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L269`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L269) (`prompts window.confirm and deletes server when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useServerStore.test.ts#L298`](file:////app/frontend/src/test/stores/useServerStore.test.ts#L298) (`does not send delete request when confirm is cancelled`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useConfirmStore.test.ts#L4`](file:////app/frontend/src/test/stores/useConfirmStore.test.ts#L4) (`initializes in closed state`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L100`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L100) (`deletes a policy when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L129`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L129) (`does not delete policy when confirm is cancelled`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L202`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L202) (`deletes a group mapping when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L231`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L231) (`does not delete group mapping when confirm is cancelled`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L258`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L258) (`deletes a custom file when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/usePolicyStore.test.ts#L287`](file:////app/frontend/src/test/stores/usePolicyStore.test.ts#L287) (`does not delete custom file when confirm is cancelled`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L141`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L141) (`prompts confirmation and deletes client when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L170`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L170) (`cancels deletion when user denies confirmation`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L487`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L487) (`confirms and revokes AppKey and refreshes list`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L516`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L516) (`cancels revocation when confirm is rejected`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L632`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L632) (`prompts confirmation modal and resets user quota when confirmed`)
  - [Frontend Vitest] [`/app/frontend/src/test/stores/useClientStore.test.ts#L662`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L662) (`cancels quota reset when user denies confirmation`)

### `[UI-TOAST-TRANSITION]` Displays error toast notification when switching from invalid JSON to Visual Prompt Builder.
* **Category:** `UI` (Dashboard, Test Bench & Settings UI)
* **Type:** Negative / Safety Guardrail (Fail-Closed)
* **Verification Proofs (8):**
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L112`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L112) (`shows error toast when switching from invalid JSON to Visual Prompt Builder`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L133`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L133) (`shows error toast when saving without a file name`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/CustomFileModal.test.tsx#L153`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L153) (`shows error toast when saving prompt with invalid JSON content`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/SecretProvidersTab.test.tsx#L63`](file:////app/frontend/src/test/components/SecretProvidersTab.test.tsx#L63) (`displays error toast when saving secret providers fails`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/IdentityAuthTab.test.tsx#L99`](file:////app/frontend/src/test/components/IdentityAuthTab.test.tsx#L99) (`saves updated Active Directory configuration JSON`)
  - [Frontend Vitest] [`/app/frontend/src/test/components/IdentityAuthTab.test.tsx#L138`](file:////app/frontend/src/test/components/IdentityAuthTab.test.tsx#L138) (`displays error toast when saving auth providers fails`)
  - [Frontend Vitest] [`/app/frontend/src/test/pages/MyMcpServers.test.tsx#L23`](file:////app/frontend/src/test/pages/MyMcpServers.test.tsx#L23) (`shows error toast when saving invalid JSON credentials`)
  - [Frontend Vitest] [`/app/frontend/src/test/pages/MyMcpServers.test.tsx#L64`](file:////app/frontend/src/test/pages/MyMcpServers.test.tsx#L64) (`saves valid credentials successfully and closes modal`)

---

## 4. Complete Verification Traceability Matrix

| Requirement ID | Type | Category | Description | Primary Proof | Suite |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `AUTH-001` | Positive | `AUTH` | Verify DatabaseUserSecretStore encrypts and decrypts secret correctly. | [`UserSecretStoreTests.cs:L8`](file:////app/ModelContextGateway.Tests/UserSecretStoreTests.cs#L8) | Backend xUnit |
| `AUTH-002` | Positive | `AUTH` | Verify UserCredentialsController returns configured server IDs. | [`UserCredentialsControllerTests.cs:L11`](file:////app/ModelContextGateway.Tests/UserCredentialsControllerTests.cs#L11) | Backend xUnit |
| `AUTH-01` | **Guardrail** | `AUTH` | Dashboard management API suite executes for authorized administrators. | [`PipelineIntegrationTests.cs:L170`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L170) | Backend xUnit |
| `AUTH-02` | Positive | `AUTH` | Allows token pass-through in query parameters for SSE stream initialization. | [`PipelineIntegrationTests.cs:L48`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L48) | Backend xUnit |
| `AUTH-03` | Positive | `AUTH` | GET /api/permissions/mappings returns group mappings with 200 OK. | [`PipelineIntegrationTests.cs:L366`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L366) | Backend xUnit |
| `AUTH-04` | Positive | `AUTH` | ProvidersController validates LDAP test connections and handles connection errors gracefully. | [`ProvidersControllerTests.cs:L298`](file:////app/ModelContextGateway.Tests/ProvidersControllerTests.cs#L298) | Backend xUnit |
| `AUTH-05` | Positive | `AUTH` | McpServer supports AllowPassThroughAuth flag | [`McpServerTests.cs:L5`](file:////app/ModelContextGateway.Tests/McpServerTests.cs#L5) | Backend xUnit |
| `AUTH-06` | Positive | `AUTH` | Transports use passThroughToken when AllowPassThroughAuth is true | [`TransportsAuthShapeTests.cs:L208`](file:////app/ModelContextGateway.Tests/TransportsAuthShapeTests.cs#L208) | Backend xUnit |
| `AUTH-101` | Positive | `AUTH` | HTTP transport injects X-Forwarded-User header based on connected user identity. | [`IdentityHeaderTests.cs:L9`](file:////app/ModelContextGateway.Tests/IdentityHeaderTests.cs#L9) | Backend xUnit |
| `AUTH-110` | Positive | `AUTH` | GetClients returns list of OAuthClient records without secret hashes | [`ClientsControllerTests.cs:L51`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L51) | Backend xUnit |
| `AUTH-112` | **Guardrail** | `AUTH` | DeleteClient removes OAuthClient via repository | [`ClientsControllerTests.cs:L120`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L120) | Backend xUnit |
| `AUTH-14` | Positive | `AUTH` | Tool execution catches 401 Unauthorized from downstream target servers and returns interactive auth remediation. | [`ToolRoutingManagerTests.cs:L173`](file:////app/ModelContextGateway.Tests/ToolRoutingManagerTests.cs#L173) | Backend xUnit |
| `AUTH-15` | Positive | `AUTH` | OpenIddict initializes ephemeral development signing certificates in Development environment. | [`OpenIddictProductionTests.cs:L30`](file:////app/ModelContextGateway.Tests/OpenIddictProductionTests.cs#L30) | Backend xUnit |
| `AUTH-35` | Positive | `AUTH` | Single-user homelab startup initializes SQLite, auto-generates Admin and Client AppKeys without PFX certificate requirements | [`SingleUserHomelabTests.cs:L30`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L30) | Backend xUnit |
| `AUTH-36` | Positive | `AUTH` | Pre-configured MCG_CLIENT_APP_KEYS seeds functional individualized client keys with custom scopes | [`SingleUserHomelabTests.cs:L98`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L98) | Backend xUnit |
| `AUTH-37` | Positive | `AUTH` | AppKeys with server and category scopes enforce precise tool execution boundaries | [`SingleUserHomelabTests.cs:L168`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L168) | Backend xUnit |
| `AUTH-38` | Positive | `AUTH` | LAN CIDR network configuration allows standalone web dashboard access from local subnet | [`SingleUserHomelabTests.cs:L183`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L183) | Backend xUnit |
| `AUTH-39` | Positive | `AUTH` | Zero-config startup defaults enterprise auth providers and secret providers to disabled | [`SingleUserHomelabTests.cs:L211`](file:////app/ModelContextGateway.Tests/SingleUserHomelabTests.cs#L211) | Backend xUnit |
| `AUTH-APPKEY-ADMIN-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with admin scope grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L79`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L79) | Backend xUnit |
| `AUTH-APPKEY-ITEMS-SCOPE-ALLOW` | Positive | `AUTH` | SecurityValidationHelper recognizes admin scopes in HttpContext.Items. | [`StandaloneAdminAuthTests.cs:L255`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L255) | Backend xUnit |
| `AUTH-APPKEY-WILDCARD-SCOPE-ALLOW` | Positive | `AUTH` | AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy. | [`StandaloneAdminAuthTests.cs:L140`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L140) | Backend xUnit |
| `AUTH-COMPACT-APPKEY-TAXONOMY` | Positive | `AUTH` | Generates compact ~32-character Base62 AppKeys with semantic prefixes. | [`AppKeyAuthenticationTests.cs:L417`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L417) | Backend xUnit |
| `AUTH-CUSTOM-ADMIN-KEY-SEEDING` | Positive | `AUTH` | Seeds custom MCG_ADMIN_AUTH_KEY when provided in configuration. | [`DatabaseSeederServiceTests.cs:L189`](file:////app/ModelContextGateway.Tests/DatabaseSeederServiceTests.cs#L189) | Backend xUnit |
| `AUTH-PERSONAL-APPKEY-CREATE` | **Guardrail** | `AUTH` | Non-admin users can create personal App Keys up to quota | [`AppKeysControllerTests.cs:L191`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L191) | Backend xUnit |
| `AUTH-PERSONAL-APPKEY-LIST` | Positive | `AUTH` | Non-admin users can view their personal App Keys | [`AppKeysControllerTests.cs:L125`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L125) | Backend xUnit |
| `AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE` | Positive | `AUTH` | Custom user quotas override default limit | [`AppKeysControllerTests.cs:L223`](file:////app/ModelContextGateway.Tests/AppKeysControllerTests.cs#L223) | Backend xUnit |
| `AUTH-PREFIX-EXTRACTION` | Positive | `AUTH` | ExtractKeyPrefix parses semantic prefixes, Base62 selectors, and legacy tokens accurately. | [`AppKeyAuthenticationTests.cs:L451`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L451) | Backend xUnit |
| `AUTH-QUERY-TOKEN-EXTRACTION` | Positive | `AUTH` | Query string token middleware extracts access_token or token query parameter to Authorization header. | [`EndpointAuthorizationTests.cs:L7`](file:////app/ModelContextGateway.Tests/EndpointAuthorizationTests.cs#L7) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW` | Positive | `AUTH` | AdminPolicy succeeds in standalone mode for unauthenticated loopback requests. | [`StandaloneAdminAuthTests.cs:L176`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L176) | Backend xUnit |
| `AUTH-STANDALONE-CUSTOM-CIDR-ALLOW` | Positive | `AUTH` | Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges. | [`StandaloneAdminAuthTests.cs:L35`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L35) | Backend xUnit |
| `AUTH-STANDALONE-LOOPBACK-ALLOW` | Positive | `AUTH` | Standalone mode without external IDP grants admin access to loopback IP addresses. | [`StandaloneAdminAuthTests.cs:L14`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L14) | Backend xUnit |
| `AUTH-SYSTEM-APPKEY-SEPARATION` | Positive | `AUTH` | Personal AppKey with 'all' scope does not grant Administrator role | [`AppKeyAuthenticationTests.cs:L311`](file:////app/ModelContextGateway.Tests/AppKeyAuthenticationTests.cs#L311) | Backend xUnit |
| `UI-100` | Positive | `AUTH` | initializes with empty providers | [`useProviderStore.test.ts:L1`](file:////app/frontend/src/test/stores/useProviderStore.test.ts#L1) | Frontend Vitest |
| `UI-101` | Positive | `AUTH` | should initialize with default values | [`useUserStore.test.ts:L1`](file:////app/frontend/src/test/stores/useUserStore.test.ts#L1) | Frontend Vitest |
| `UI-109` | Positive | `AUTH` | renders default standard mcpServers configuration with meta mode | [`ClientSetupGuide.test.tsx:L1`](file:////app/frontend/src/test/components/ClientSetupGuide.test.tsx#L1) | Frontend Vitest |
| `UI-114` | Positive | `AUTH` | renders nothing when isPolicyModalOpen is false | [`PolicyModal.test.tsx:L1`](file:////app/frontend/src/test/components/PolicyModal.test.tsx#L1) | Frontend Vitest |
| `UI-120` | Positive | `AUTH` | RBAC and SID mapping administration UI allows configuring role policies and SID associations | [`rbac-enforcement-flow.spec.ts:L1`](file:////app/frontend/e2e/rbac-enforcement-flow.spec.ts#L1) | Playwright E2E |
| `UI-123` | Positive | `AUTH` | should open App Keys & Security view and display client setup controls | [`client-setup-and-appkeys.spec.ts:L1`](file:////app/frontend/e2e/client-setup-and-appkeys.spec.ts#L1) | Playwright E2E |
| `UI-125` | Positive | `AUTH` | Admin role renders full administrative dashboard and server management controls | [`multi-user-matrix.spec.ts:L1`](file:////app/frontend/e2e/multi-user-matrix.spec.ts#L1) | Playwright E2E |
| `UI-127` | Positive | `AUTH` | should navigate to settings permissions tab and open policy configuration modal | [`rbac-and-permissions.spec.ts:L1`](file:////app/frontend/e2e/rbac-and-permissions.spec.ts#L1) | Playwright E2E |
| `UI-129` | Positive | `AUTH` | should create client application and generate AppKey with scope constraints | [`appkey-and-client-lifecycle.spec.ts:L1`](file:////app/frontend/e2e/appkey-and-client-lifecycle.spec.ts#L1) | Playwright E2E |
| `CORE-101` | Positive | `CORE` | Auto-added requirement tracking | [`SessionManagerTests.cs:L9`](file:////app/ModelContextGateway.Tests/SessionManagerTests.cs#L9) | Backend xUnit |
| `DB-01` | Positive | `DB` | DatabaseSeeder initializes default router tables, settings, and seed servers. | [`SeederAndDiscoveryTests.cs:L53`](file:////app/ModelContextGateway.Tests/SeederAndDiscoveryTests.cs#L53) | Backend xUnit |
| `DB-02` | Positive | `DB` | MSSQL stored procedure scripts declare all required procedures and parameter contracts correctly | [`DatabaseSchemaUpgradeAndContractTests.cs:L311`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L311) | Backend xUnit |
| `DB-07` | Positive | `DB` | SQLite upgrade migration automatically provisions OAuthClients table on legacy database | [`DatabaseSchemaUpgradeAndContractTests.cs:L433`](file:////app/ModelContextGateway.Tests/DatabaseSchemaUpgradeAndContractTests.cs#L433) | Backend xUnit |
| `DOC-SETUP-SKILL-FRONTMATTER` | Positive | `DOC` | mcg-setup skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters | [`SetupSkillTests.cs:L18`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L18) | Backend xUnit |
| `DOC-SETUP-SKILL-MIRROR` | Positive | `DOC` | The mcg-setup skill and templates are mirrored 1:1 in .agents/skills/mcg-setup/ | [`SetupSkillTests.cs:L152`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L152) | Backend xUnit |
| `DOC-SETUP-SKILL-TEMPLATES` | Positive | `DOC` | All scaffold templates exist, are non-empty, and contain required directives such as responseBufferLimit, MCG_MASTER_KEY, and ghcr.io/spelech/model-context-gateway | [`SetupSkillTests.cs:L98`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L98) | Backend xUnit |
| `DOC-SETUP-SKILL-WORKFLOW` | Positive | `DOC` | mcg-setup skill contains all 6 required setup phases including environment probing, hosting platforms, env vs UI trade-offs, identity/network topology, artifact generation, and health/client configuration | [`SetupSkillTests.cs:L44`](file:////app/ModelContextGateway.Tests/SetupSkillTests.cs#L44) | Backend xUnit |
| `AUTH-EXTERNAL-IDP-DENIES-ANONYMOUS-LOOPBACK` | **Guardrail** | `GUARD` | When an external IDP is configured, anonymous loopback requests do not bypass authentication. | [`StandaloneAdminAuthTests.cs:L224`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L224) | Backend xUnit |
| `AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY` | **Guardrail** | `GUARD` | AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode. | [`StandaloneAdminAuthTests.cs:L200`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L200) | Backend xUnit |
| `AUTH-STANDALONE-EXTERNAL-DENY` | **Guardrail** | `GUARD` | Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey. | [`StandaloneAdminAuthTests.cs:L57`](file:////app/ModelContextGateway.Tests/StandaloneAdminAuthTests.cs#L57) | Backend xUnit |
| `GUARD-01` | **Guardrail** | `GUARD` | JsonRpcStateManager rejects registration and cancels pending completions upon disconnect. | [`ConcurrentResponseIsolationTests.cs:L585`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L585) | Backend xUnit |
| `GUARD-02` | **Guardrail** | `GUARD` | LdapService rejects unencrypted LDAP with InvalidOperationException. | [`IdentityProviderTests.cs:L188`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L188) | Backend xUnit |
| `GUARD-03` | **Guardrail** | `GUARD` | TokenExchangeSecretRetriever fails closed with InvalidOperationException when token endpoint is not configured. | [`TokenExchangeSecretRetrieverTests.cs:L94`](file:////app/ModelContextGateway.Tests/TokenExchangeSecretRetrieverTests.cs#L94) | Backend xUnit |
| `GUARD-04` | **Guardrail** | `GUARD` | Malformed completion payloads or unmapped backends must fail closed safely | [`PairwiseIntegrationMatrixTests.cs:L508`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L508) | Backend xUnit |
| `GUARD-05` | **Guardrail** | `GUARD` | Docker auto-discovery skips containers resolving to blocked private IP ranges (SSRF protection). | [`DockerAutoDiscoveryServiceTests.cs:L58`](file:////app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L58) | Backend xUnit |
| `GUARD-06` | **Guardrail** | `GUARD` | Header authentication strips remote identity headers when request is sent through untrusted proxy. | [`IdentityProviderTests.cs:L52`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L52) | Backend xUnit |
| `GUARD-ADMIN-CUSTOM-FILES-VALIDATION` | **Guardrail** | `GUARD` | manage_custom_files rejects invalid prompt JSON syntax and unsupported file categories. | [`AdminToolsParityTests.cs:L779`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L779) | Backend xUnit |
| `GUARD-ADMIN-ENDPOINT-UNAUTHORIZED` | **Guardrail** | `GUARD` | Unauthenticated / non-admin client request to /admin receives 403 Forbidden. | [`AdminEndpointsTests.cs:L193`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L193) | Backend xUnit |
| `GUARD-ADMIN-POLICIES-WILDCARD-DENY` | **Guardrail** | `GUARD` | manage_policies rejects wildcard deny policies to prevent global lockout. | [`AdminToolsParityTests.cs:L513`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L513) | Backend xUnit |
| `GUARD-ADMIN-PROVIDERS-LDAP-PLAINTEXT` | **Guardrail** | `GUARD` | manage_providers rejects unencrypted LDAP connections on port 389. | [`AdminToolsParityTests.cs:L650`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L650) | Backend xUnit |
| `GUARD-ADMIN-SERVERS-VALIDATION` | **Guardrail** | `GUARD` | Verifies that the manage_servers tool accurately enforces validation by rejecting malformed transport types, missing required parameters, and requests for non-existent servers. | [`AdminToolsParityTests.cs:L324`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L324) | Backend xUnit |
| `GUARD-ADMIN-UNKNOWN-TOOL` | **Guardrail** | `GUARD` | AdminMcpServer returns an error response for unknown tool or action invocations. | [`AdminMcpServerTests.cs:L592`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L592) | Backend xUnit |
| `MCP-ADMIN-TOOL-TEST-CALL-ERROR` | **Guardrail** | `GUARD` | AdminMcpServer test_tool_call propagates downstream backend errors with visibility. | [`AdminMcpServerTests.cs:L633`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L633) | Backend xUnit |
| `MCP-01` | Positive | `MCP` | Full end-to-end JSON-RPC protocol suite executes across SSE pipeline. | [`PipelineIntegrationTests.cs:L85`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L85) | Backend xUnit |
| `MCP-02` | Positive | `MCP` | All MCP protocol capabilities enforce caller role authorizations consistently | [`PairwiseIntegrationMatrixTests.cs:L385`](file:////app/ModelContextGateway.Tests/PairwiseIntegrationMatrixTests.cs#L385) | Backend xUnit |
| `MCP-05` | Positive | `MCP` | ResourceRoutingManager filters and matches MCP resources using semantic and keyword matching. | [`ResourceRoutingTests.cs:L5`](file:////app/ModelContextGateway.Tests/ResourceRoutingTests.cs#L5) | Backend xUnit |
| `MCP-06` | Positive | `MCP` | prompts/list aggregates, namespaces, and routes prompts to target backends. | [`McpIntegrationTests.cs:L514`](file:////app/ModelContextGateway.Tests/McpIntegrationTests.cs#L514) | Backend xUnit |
| `MCP-08` | Positive | `MCP` | completion/complete forwards prompt completions to backend when caller is authorized. | [`UnifiedMcpAuthorizationTests.cs:L439`](file:////app/ModelContextGateway.Tests/UnifiedMcpAuthorizationTests.cs#L439) | Backend xUnit |
| `MCP-10` | Positive | `MCP` | DockerAutoDiscoveryService initializes with valid container service dependencies. | [`DockerAutoDiscoveryServiceTests.cs:L45`](file:////app/ModelContextGateway.Tests/DockerAutoDiscoveryServiceTests.cs#L45) | Backend xUnit |
| `MCP-12` | Positive | `MCP` | ApiEmbeddingService parses OpenAI-compatible vector responses and extracts float embeddings. | [`EmbeddingServiceTests.cs:L61`](file:////app/ModelContextGateway.Tests/EmbeddingServiceTests.cs#L61) | Backend xUnit |
| `MCP-15` | Positive | `MCP` | All JSON-RPC results return a resultType discriminator (complete or input_required) per MCP 2026-07-28 spec. | [`ProtocolResultTypeTests.cs:L7`](file:////app/ModelContextGateway.Tests/ProtocolResultTypeTests.cs#L7) | Backend xUnit |
| `MCP-21` | Positive | `MCP` | Admin endpoint handles direct Streamable HTTP POST tools/list request returning JSON even with Accept text/event-stream header. | [`AdminEndpointsTests.cs:L370`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L370) | Backend xUnit |
| `MCP-22` | **Guardrail** | `MCP` | AdminMcpServer ProcessRequestAsync handles server/discover request returning supported versions and subscriptions capability. | [`AdminMcpServerTests.cs:L682`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L682) | Backend xUnit |
| `MCP-23` | Positive | `MCP` | AdminMcpServer HandleInitializeAsync includes subscriptions capability in capabilities object. | [`AdminMcpServerTests.cs:L651`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L651) | Backend xUnit |
| `MCP-24` | Positive | `MCP` | McpSpecMiddleware extracts OpenTelemetry W3C traceparent, tracestate, and baggage from headers and _meta. | [`McpSpecMiddlewareTests.cs:L219`](file:////app/ModelContextGateway.Tests/McpSpecMiddlewareTests.cs#L219) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-CALL-TOOL` | Positive | `MCP` | Admin endpoint /admin/message executes tools/call for manage_system diagnostics. | [`AdminEndpointsTests.cs:L294`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L294) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-HEAD-REQUEST` | Positive | `MCP` | Admin endpoint /admin handles HEAD request returning text/event-stream headers. | [`AdminEndpointsTests.cs:L212`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L212) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-LIST-TOOLS` | Positive | `MCP` | Admin endpoint /admin/message executes tools/list over active SSE session and returns 10 admin tools. | [`AdminEndpointsTests.cs:L224`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L224) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-ROUTER-ADMIN-TARGET` | Positive | `MCP` | Target proxy endpoint /router-admin routes directly to the Admin MCP server. | [`AdminEndpointsTests.cs:L149`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L149) | Backend xUnit |
| `MCP-ADMIN-ENDPOINT-SSE-HANDSHAKE` | Positive | `MCP` | Admin endpoint /admin/sse performs initialize handshake with 2026-07-28 protocol version. | [`AdminEndpointsTests.cs:L62`](file:////app/ModelContextGateway.Tests/AdminEndpointsTests.cs#L62) | Backend xUnit |
| `MCP-ADMIN-INITIALIZE-HANDSHAKE` | Positive | `MCP` | AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05. | [`AdminMcpServerTests.cs:L195`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L195) | Backend xUnit |
| `MCP-ADMIN-PARITY-APPKEYS` | Positive | `MCP` | manage_appkeys supports full parity for list, get_limits, create, and revoke actions. | [`AdminToolsParityTests.cs:L370`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L370) | Backend xUnit |
| `MCP-ADMIN-PARITY-CLIENTS` | Positive | `MCP` | manage_clients supports full parity for register, list, and delete actions. | [`AdminToolsParityTests.cs:L423`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L423) | Backend xUnit |
| `MCP-ADMIN-PARITY-CUSTOM-FILES` | Positive | `MCP` | manage_custom_files supports full parity for list, get, save, and delete prompt and resource files. | [`AdminToolsParityTests.cs:L716`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L716) | Backend xUnit |
| `MCP-ADMIN-PARITY-GROUP-MAPPINGS` | Positive | `MCP` | manage_group_mappings supports full parity for list, save, and delete external-to-internal group mappings. | [`AdminToolsParityTests.cs:L530`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L530) | Backend xUnit |
| `MCP-ADMIN-PARITY-JSONRPC-DISPATCH` | Positive | `MCP` | AdminMcpServer processes standard JSON-RPC 2.0 requests (tools/list, tools/call, ping). | [`AdminToolsParityTests.cs:L873`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L873) | Backend xUnit |
| `MCP-ADMIN-PARITY-POLICIES` | Positive | `MCP` | manage_policies supports full parity for list, save, and delete access control policies. | [`AdminToolsParityTests.cs:L464`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L464) | Backend xUnit |
| `MCP-ADMIN-PARITY-PROVIDERS` | Positive | `MCP` | manage_providers supports full parity for list, save_secret, test_vault, save_auth, and test_ldap actions. | [`AdminToolsParityTests.cs:L577`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L577) | Backend xUnit |
| `MCP-ADMIN-PARITY-SERVERS` | Positive | `MCP` | Validates that the manage_servers tool provides comprehensive administrative capabilities including listing, retrieving, creating, updating, toggling, deleting, and reconnecting servers. | [`AdminToolsParityTests.cs:L234`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L234) | Backend xUnit |
| `MCP-ADMIN-PARITY-SETTINGS` | Positive | `MCP` | manage_settings supports full parity for get and update global router configurations. | [`AdminToolsParityTests.cs:L667`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L667) | Backend xUnit |
| `MCP-ADMIN-PARITY-SYSTEM` | Positive | `MCP` | manage_system supports full parity for diagnostics, get_logs, clear_logs, and query_audit actions. | [`AdminToolsParityTests.cs:L816`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L816) | Backend xUnit |
| `MCP-ADMIN-PARITY-TEST-TOOL-CALL` | Positive | `MCP` | test_tool_call executes test bench backend tool calls and formats responses. | [`AdminToolsParityTests.cs:L846`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L846) | Backend xUnit |
| `MCP-ADMIN-PARITY-TOOLS-COVERAGE` | Positive | `MCP` | Ensures every UI management workflow is backed by a verified, equivalent action within the consolidated Admin MCP tools. | [`AdminToolsParityTests.cs:L191`](file:////app/ModelContextGateway.Tests/AdminToolsParityTests.cs#L191) | Backend xUnit |
| `MCP-ADMIN-SKILL-E2E-PROVISIONING` | Positive | `MCP` | Admin automation templates and JSON-RPC tool calls successfully provision a blank-slate gateway instance end-to-end via HTTP /admin/message. | [`AdminAutomationSkillTests.cs:L176`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L176) | Backend xUnit |
| `MCP-ADMIN-SKILL-FRONTMATTER` | Positive | `MCP` | mcg-admin skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters | [`AdminAutomationSkillTests.cs:L21`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L21) | Backend xUnit |
| `MCP-ADMIN-SKILL-MIRROR` | Positive | `MCP` | mcg-admin skill files and templates are identically mirrored between skills/ and .agents/skills/ directories | [`AdminAutomationSkillTests.cs:L147`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L147) | Backend xUnit |
| `MCP-ADMIN-SKILL-TEMPLATES` | Positive | `MCP` | All mcg-admin scaffold templates exist, are non-empty, and contain valid JSON or scripts for Authentik, Keycloak, Entra, ActiveDirectory, Cloudflare, Vault, Embeddings, Docker, and shell automation | [`AdminAutomationSkillTests.cs:L103`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L103) | Backend xUnit |
| `MCP-ADMIN-SKILL-WORKFLOW` | Positive | `MCP` | mcg-admin skill contains all 7 administration phases including diagnostics, secrets, auth providers, RBAC/group mappings, settings/embeddings, servers/clients, and live tool verification | [`AdminAutomationSkillTests.cs:L47`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L47) | Backend xUnit |
| `MCP-ADMIN-TOOL-AUDIT-LOG` | Positive | `MCP` | AdminMcpServer tool calls record audit log entries with caller and tool name. | [`AdminMcpServerTests.cs:L266`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L266) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-APPKEYS` | Positive | `MCP` | AdminMcpServer executes manage_appkeys create, list, limits, and revoke actions. | [`AdminMcpServerTests.cs:L283`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L283) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-CLIENTS` | Positive | `MCP` | AdminMcpServer executes manage_clients register, list, and delete actions. | [`AdminMcpServerTests.cs:L330`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L330) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-CUSTOM-FILES` | Positive | `MCP` | AdminMcpServer executes manage_custom_files save, get, list, and delete actions. | [`AdminMcpServerTests.cs:L504`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L504) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-GROUP-MAPPINGS` | Positive | `MCP` | AdminMcpServer executes manage_group_mappings save, list, and delete actions. | [`AdminMcpServerTests.cs:L402`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L402) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-POLICIES` | Positive | `MCP` | AdminMcpServer executes manage_policies save, list, and delete actions. | [`AdminMcpServerTests.cs:L368`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L368) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-PROVIDERS` | Positive | `MCP` | AdminMcpServer executes manage_providers list, save_secret, and save_auth actions. | [`AdminMcpServerTests.cs:L435`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L435) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SERVERS` | Positive | `MCP` | AdminMcpServer executes manage_servers list, get, create, update, toggle, and delete actions. | [`AdminMcpServerTests.cs:L214`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L214) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SETTINGS` | Positive | `MCP` | AdminMcpServer executes manage_settings get and update actions. | [`AdminMcpServerTests.cs:L478`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L478) | Backend xUnit |
| `MCP-ADMIN-TOOL-MANAGE-SYSTEM` | Positive | `MCP` | AdminMcpServer executes manage_system diagnostics, get_logs, clear_logs, and query_audit actions. | [`AdminMcpServerTests.cs:L558`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L558) | Backend xUnit |
| `MCP-ADMIN-TOOLS-LIST-COUNT` | Positive | `MCP` | AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas. | [`AdminMcpServerTests.cs:L147`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L147) | Backend xUnit |
| `UI-104` | Positive | `MCP` | renders resource tester with servers and resources | [`ResourceTesterCard.test.tsx:L1`](file:////app/frontend/src/test/components/ResourceTesterCard.test.tsx#L1) | Frontend Vitest |
| `UI-106` | Positive | `MCP` | renders connected server details with badges and triggers actions | [`ServerCard.test.tsx:L1`](file:////app/frontend/src/test/components/ServerCard.test.tsx#L1) | Frontend Vitest |
| `UI-107` | Positive | `MCP` | renders prompt dropdown and filters by selected server | [`PromptTesterCard.test.tsx:L1`](file:////app/frontend/src/test/components/PromptTesterCard.test.tsx#L1) | Frontend Vitest |
| `UI-112` | Positive | `MCP` | renders nothing when isAddEditOpen is false | [`ServerModal.test.tsx:L1`](file:////app/frontend/src/test/components/ServerModal.test.tsx#L1) | Frontend Vitest |
| `UI-121` | Positive | `MCP` | should open Add Server modal and switch secret provider types | [`server-management.spec.ts:L1`](file:////app/frontend/e2e/server-management.spec.ts#L1) | Playwright E2E |
| `UI-126` | Positive | `MCP` | should open Server Inspect Modal if servers are present on dashboard | [`server-inspector.spec.ts:L1`](file:////app/frontend/e2e/server-inspector.spec.ts#L1) | Playwright E2E |
| `AUTH-106` | **Guardrail** | `SEC` | Exchange throws InvalidOperationException when request is null. | [`AuthorizationControllerTests.cs:L19`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L19) | Backend xUnit |
| `AUTH-107` | Positive | `SEC` | RegisterClient successfully handles DCR requests when open DCR is enabled. | [`AuthorizationControllerTests.cs:L36`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L36) | Backend xUnit |
| `AUTH-108` | **Guardrail** | `SEC` | Authorize throws InvalidOperationException when OIDC request is null. | [`AuthorizationControllerTests.cs:L77`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L77) | Backend xUnit |
| `AUTH-109` | Positive | `SEC` | RegisterClient uses IOAuthClientRepository when IOpenIddictApplicationManager is null. | [`AuthorizationControllerTests.cs:L94`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L94) | Backend xUnit |
| `AUTH-111` | **Guardrail** | `SEC` | Pipeline exposes RFC 9728 OAuth Protected Resource discovery endpoints with dynamic resource identifiers. | [`PipelineIntegrationTests.cs:L62`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L62) | Backend xUnit |
| `AUTH-113` | Positive | `SEC` | RegisterClient supports public clients with PKCE (token_endpoint_auth_method: none) and omits client secret. | [`AuthorizationControllerTests.cs:L351`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L351) | Backend xUnit |
| `AUTH-114` | **Guardrail** | `SEC` | RegisterClient rejects invalid or non-absolute redirect URIs with standard RFC 7591 invalid_redirect_uri error. | [`AuthorizationControllerTests.cs:L400`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L400) | Backend xUnit |
| `AUTH-115` | Positive | `SEC` | RegisterClient dynamically binds requested scopes to OpenIddict application descriptor permissions. | [`AuthorizationControllerTests.cs:L435`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L435) | Backend xUnit |
| `AUTH-116` | **Guardrail** | `SEC` | Exchange rejects client_credentials grant attempts by public clients with UnauthorizedClient error. | [`AuthorizationControllerTests.cs:L475`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L475) | Backend xUnit |
| `AUTH-117` | **Guardrail** | `SEC` | RegisterClient returns 403 Forbidden with access_denied when open client registration is disabled and caller is unauthorized. | [`AuthorizationControllerTests.cs:L516`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L516) | Backend xUnit |
| `AUTH-118` | Positive | `SEC` | RegisterClient deduplicates and reuses existing dynamic client registrations for matching client name and type without accumulating redundant records. | [`AuthorizationControllerTests.cs:L556`](file:////app/ModelContextGateway.Tests/AuthorizationControllerTests.cs#L556) | Backend xUnit |
| `AUTH-119` | Positive | `SEC` | CleanupClients endpoint triggers DCR pruning and returns total cleaned client count. | [`ClientsControllerTests.cs:L333`](file:////app/ModelContextGateway.Tests/ClientsControllerTests.cs#L333) | Backend xUnit |
| `SEC-01` | Positive | `SEC` | Encrypts SecretProvider configuration JSON at rest in database using Master Key. | [`ProviderSettingsEncryptionTests.cs:L70`](file:////app/ModelContextGateway.Tests/ProviderSettingsEncryptionTests.cs#L70) | Backend xUnit |
| `SEC-02` | Positive | `SEC` | GET /api/providers/secrets returns secret providers with 200 OK. | [`PipelineIntegrationTests.cs:L375`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L375) | Backend xUnit |
| `SEC-03` | Positive | `SEC` | Ensure TrustedProxyHelper supports CIDR ranges in XFF validation | [`IdentityProviderTests.cs:L370`](file:////app/ModelContextGateway.Tests/IdentityProviderTests.cs#L370) | Backend xUnit |
| `SEC-04` | Positive | `SEC` | WindowsRegistrySecretRetriever handles non-Windows platforms gracefully and returns null. | [`SecretRetrieverTests.cs:L34`](file:////app/ModelContextGateway.Tests/SecretRetrieverTests.cs#L34) | Backend xUnit |
| `SEC-05` | **Guardrail** | `SEC` | GET /api/audit returns audit log records with 200 OK. | [`PipelineIntegrationTests.cs:L393`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L393) | Backend xUnit |
| `SEC-ADMIN-AUDIT-REDACTION` | Positive | `SEC` | AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs. | [`AdminMcpServerTests.cs:L609`](file:////app/ModelContextGateway.Tests/AdminMcpServerTests.cs#L609) | Backend xUnit |
| `SEC-GATEWAY-ZERO-CONFIG-BOOT` | Positive | `SEC` | Gateway boots from a blank slate with zero master key environment variables, auto-generates .master.key, and serves health and admin endpoints. | [`AdminAutomationSkillTests.cs:L352`](file:////app/ModelContextGateway.Tests/AdminAutomationSkillTests.cs#L352) | Backend xUnit |
| `SEC-KEY-PROVIDER-AUTOGEN` | Positive | `SEC` | EncryptionKeyProvider delegates to DbKeyHelper to auto-generate master key when unconfigured. | [`EncryptionKeyProviderTests.cs:L42`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L42) | Backend xUnit |
| `SEC-KEY-PROVIDER-CONFIG` | Positive | `SEC` | EncryptionKeyProvider returns configured DB_ENCRYPTION_KEY or MCG_SECRET. | [`EncryptionKeyProviderTests.cs:L28`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L28) | Backend xUnit |
| `SEC-KEY-PROVIDER-FALLBACK` | Positive | `SEC` | EncryptionKeyProvider falls back to DB_ENCRYPTION_KEY when MCG_SECRET is unconfigured. | [`EncryptionKeyProviderTests.cs:L70`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L70) | Backend xUnit |
| `SEC-KEY-PROVIDER-SECRET` | Positive | `SEC` | EncryptionKeyProvider returns configured MCG_SECRET. | [`EncryptionKeyProviderTests.cs:L56`](file:////app/ModelContextGateway.Tests/EncryptionKeyProviderTests.cs#L56) | Backend xUnit |
| `SEC-KEYFILE-AUTOGEN` | Positive | `SEC` | Blank-slate initialization auto-generates a 256-bit base64 master key and persists it to .master.key. | [`DbKeyHelperTests.cs:L63`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L63) | Backend xUnit |
| `SEC-KEYFILE-ENV-PRECEDENCE` | Positive | `SEC` | Explicit environment variables MCG_MASTER_KEY or MCG_SECRET take precedence over keyfiles. | [`DbKeyHelperTests.cs:L28`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L28) | Backend xUnit |
| `SEC-KEYFILE-FILE-OVER-KEYFILE` | Positive | `SEC` | Explicit file secrets take precedence over persistent .master.key files. | [`DbKeyHelperTests.cs:L123`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L123) | Backend xUnit |
| `SEC-KEYFILE-FILE-SECRET` | Positive | `SEC` | File-based secrets configured via MCG_MASTER_KEY_FILE or standard Docker secrets paths are resolved. | [`DbKeyHelperTests.cs:L45`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L45) | Backend xUnit |
| `SEC-KEYFILE-HIERARCHY-PRECEDENCE` | Positive | `SEC` | Explicit environment variables take precedence over file secrets and keyfiles. | [`DbKeyHelperTests.cs:L101`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L101) | Backend xUnit |
| `SEC-KEYFILE-RELOAD` | Positive | `SEC` | Existing .master.key file is loaded across gateway restarts without key mutation. | [`DbKeyHelperTests.cs:L83`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L83) | Backend xUnit |
| `SEC-KEYSOURCE-DETECTION` | Positive | `SEC` | Correctly identifies KeySource origin for environment, file, and auto-generated keys. | [`DbKeyHelperTests.cs:L144`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L144) | Backend xUnit |
| `SEC-KEYSOURCE-SETCACHEDKEY` | Positive | `SEC` | SetCachedKey sets in-memory encryption key and updates ActiveKeySource. | [`DbKeyHelperTests.cs:L314`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L314) | Backend xUnit |
| `SEC-MASTERKEY-ATOMIC-REENCRYPTION` | Positive | `SEC` | Rejects POST /api/config/master-key when key source is external. | [`PipelineIntegrationTests.cs:L481`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L481) | Backend xUnit |
| `SEC-MASTERKEY-CONFIGURED-STATUS-BADGE` | Positive | `SEC` | Displays configured badge and rotate button when custom master key is configured. | [`GeneralTab.test.tsx:L192`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L192) | Frontend Vitest |
| `SEC-MASTERKEY-CUSTOM-MODAL-REENCRYPTION` | Positive | `SEC` | Validates master key inputs (length, match) and triggers atomic re-encryption. | [`MasterKeyModal.test.tsx:L6`](file:////app/frontend/src/test/components/MasterKeyModal.test.tsx#L6) | Frontend Vitest |
| `SEC-MASTERKEY-EXTERNAL-LOCKED-BADGE` | Positive | `SEC` | Displays locked badge when master key is externally managed via Vault or Environment. | [`GeneralTab.test.tsx:L149`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L149) | Frontend Vitest |
| `SEC-MASTERKEY-UI-STATUS-BANNER` | Positive | `SEC` | Displays warning banner when keySource is AutoGenerated and opens custom master key modal. | [`GeneralTab.test.tsx:L115`](file:////app/frontend/src/test/components/GeneralTab.test.tsx#L115) | Frontend Vitest |
| `SEC-VAULT-BOOTSTRAPPING` | Positive | `SEC` | Bootstraps master encryption key directly from HashiCorp Vault when VAULT_ADDR is configured. | [`DbKeyHelperTests.cs:L191`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L191) | Backend xUnit |
| `SEC-VAULT-CUSTOM-PATH` | Positive | `SEC` | Bootstraps master key from Vault using custom mount path and secret key name. | [`DbKeyHelperTests.cs:L236`](file:////app/ModelContextGateway.Tests/DbKeyHelperTests.cs#L236) | Backend xUnit |
| `UI-105` | Positive | `SEC` | renders system logs and handles level filter | [`LogsTerminalCard.test.tsx:L1`](file:////app/frontend/src/test/components/LogsTerminalCard.test.tsx#L1) | Frontend Vitest |
| `TRANS-01` | Positive | `TRANS` | SendRequestAsync times out cleanly and removes pending completion handlers without leaking memory. | [`ChallengerTests.cs:L276`](file:////app/ModelContextGateway.Tests/ChallengerTests.cs#L276) | Backend xUnit |
| `TRANS-02` | Positive | `TRANS` | Multiplexes concurrent client calls sharing identical JSON-RPC IDs and routes reversed responses correctly. | [`ConcurrentResponseIsolationTests.cs:L12`](file:////app/ModelContextGateway.Tests/ConcurrentResponseIsolationTests.cs#L12) | Backend xUnit |
| `TRANS-03` | Positive | `TRANS` | STDIO transport spawns subprocess, handles JSON-RPC initialization and executes tool calls | [`StdioTransportTests.cs:L49`](file:////app/ModelContextGateway.Tests/StdioTransportTests.cs#L49) | Backend xUnit |
| `UI-01` | Positive | `UI` | renders CustomFileModal in create mode and displays visual builder tabs | [`CustomFileModal.test.tsx:L26`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L26) | Frontend Vitest |
| `UI-02` | Positive | `UI` | Inspect modal displays spinner loading state while querying server capabilities | [`ServerInspectModal.test.tsx:L61`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L61) | Frontend Vitest |
| `UI-03` | Positive | `UI` | Grouped server view renders category sections and supports collapsible groups | [`DashboardView.test.tsx:L63`](file:////app/frontend/src/test/components/DashboardView.test.tsx#L63) | Frontend Vitest |
| `UI-04` | Positive | `UI` | Tool selector filters available tools by selected backend server | [`ToolTesterCard.test.tsx:L77`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L77) | Frontend Vitest |
| `UI-05` | Positive | `UI` | Router allows customized branding parameters (DashboardTitle, DashboardIcon) to be saved and retrieved via the API. | [`PipelineIntegrationTests.cs:L253`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L253) | Backend xUnit |
| `UI-06` | Positive | `UI` | Router supports uploading and retrieving custom branding logo images via dedicated endpoints. | [`PipelineIntegrationTests.cs:L447`](file:////app/ModelContextGateway.Tests/PipelineIntegrationTests.cs#L447) | Backend xUnit |
| `UI-07` | Positive | `UI` | Audits desktop viewport layout for zero horizontal overflow and high UX score. | [`layout-inspector.spec.ts:L38`](file:////app/frontend/e2e/layout-inspector.spec.ts#L38) | Playwright E2E |
| `UI-102` | Positive | `UI` | Dashboard renders stats card, connected server list, and setup instructions | [`DashboardView.test.tsx:L1`](file:////app/frontend/src/test/components/DashboardView.test.tsx#L1) | Frontend Vitest |
| `UI-103` | Positive | `UI` | Interactive tool tester renders server and tool selection dropdowns | [`ToolTesterCard.test.tsx:L1`](file:////app/frontend/src/test/components/ToolTesterCard.test.tsx#L1) | Frontend Vitest |
| `UI-108` | Positive | `UI` | renders nothing when isMappingModalOpen is false | [`MappingModal.test.tsx:L1`](file:////app/frontend/src/test/components/MappingModal.test.tsx#L1) | Frontend Vitest |
| `UI-110` | Positive | `UI` | renders title, MCG badge, subtitle, and version badge | [`Header.test.tsx:L1`](file:////app/frontend/src/test/components/Header.test.tsx#L1) | Frontend Vitest |
| `UI-111` | Positive | `UI` | renders GeneralTab and triggers save | [`SettingsTabs.test.tsx:L1`](file:////app/frontend/src/test/components/SettingsTabs.test.tsx#L1) | Frontend Vitest |
| `UI-113` | Positive | `UI` | renders tab navigation and switches active subviews | [`SettingsView.test.tsx:L1`](file:////app/frontend/src/test/components/SettingsView.test.tsx#L1) | Frontend Vitest |
| `UI-115` | Positive | `UI` | renders test bench cards and switches tabs | [`TestBenchView.test.tsx:L1`](file:////app/frontend/src/test/components/TestBenchView.test.tsx#L1) | Frontend Vitest |
| `UI-116` | Positive | `UI` | Modal remains hidden when isInspectOpen is false | [`ServerInspectModal.test.tsx:L1`](file:////app/frontend/src/test/components/ServerInspectModal.test.tsx#L1) | Frontend Vitest |
| `UI-117` | Positive | `UI` | returns null when isOpen is false | [`SharedComponents.test.tsx:L1`](file:////app/frontend/src/test/components/SharedComponents.test.tsx#L1) | Frontend Vitest |
| `UI-119` | Positive | `UI` | calls server endpoints correctly | [`typedApi.test.ts:L1`](file:////app/frontend/src/test/api/typedApi.test.ts#L1) | Frontend Vitest |
| `UI-122` | Positive | `UI` | should navigate to Settings view and configure vector embedding options | [`settings.spec.ts:L1`](file:////app/frontend/e2e/settings.spec.ts#L1) | Playwright E2E |
| `UI-124` | Positive | `UI` | Renders main dashboard navigation tabs and layout headers | [`dashboard.spec.ts:L1`](file:////app/frontend/e2e/dashboard.spec.ts#L1) | Playwright E2E |
| `UI-128` | Positive | `UI` | should navigate to Test Bench view and render tester cards | [`testbench.spec.ts:L1`](file:////app/frontend/e2e/testbench.spec.ts#L1) | Playwright E2E |
| `UI-30` | Positive | `UI` | Renders client registration form with inputs for name, client type, redirect URIs, grant types, scopes, and expiration. | [`ClientModal.test.tsx:L27`](file:////app/frontend/src/test/components/ClientModal.test.tsx#L27) | Frontend Vitest |
| `UI-31` | **Guardrail** | `UI` | Fetches clients on mount and renders table headers and action buttons. | [`RegisteredClientsCard.test.tsx:L42`](file:////app/frontend/src/test/components/RegisteredClientsCard.test.tsx#L42) | Frontend Vitest |
| `UI-32` | Positive | `UI` | Registers OAuth client with extended metadata (redirect URIs, grant types, client type, expiration) and captures one-time credentials. | [`useClientStore.test.ts:L76`](file:////app/frontend/src/test/stores/useClientStore.test.ts#L76) | Frontend Vitest |
| `UI-CONFIRM-MODAL` | **Guardrail** | `UI` | Renders confirmation dialog with title, message, and trigger buttons for confirm and cancel. | [`ConfirmModal.test.tsx:L6`](file:////app/frontend/src/test/components/ConfirmModal.test.tsx#L6) | Frontend Vitest |
| `UI-TOAST-TRANSITION` | **Guardrail** | `UI` | Displays error toast notification when switching from invalid JSON to Visual Prompt Builder. | [`CustomFileModal.test.tsx:L112`](file:////app/frontend/src/test/components/CustomFileModal.test.tsx#L112) | Frontend Vitest |
