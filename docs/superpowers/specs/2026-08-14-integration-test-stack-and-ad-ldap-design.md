# Integration Test Stack, AD/LDAP Testbed, and UI Completion Design

**Author:** Antigravity  
**Date:** 2026-08-14  
**Status:** Draft / Proposed  
**Related Issue/Task:** Expanding Test Coverage, Integration Compose Stack, AD/LDAP Tests & UI Flows  

---

## 1. Overview & Objectives

This design outlines the architecture and execution plan to significantly raise test coverage and production confidence across the **CSharp MCP Router** (`/containers/dev/csharp-mcp-router`).

### Primary Goals:
1. **All-in-One Developer Integration Compose Stack**:
   - Expand `docker-compose.test.yml` into a modular, self-seeding multi-service test environment with **HashiCorp Vault**, **OpenLDAP (LDAPS)**, **MySQL**, **MSSQL** (via profile), and **Mock MCP Servers** (HTTP/SSE/STDIO).
   - Provide automated seeding fixtures (LDIF users/SIDs, SQL schema/data) so any developer can stand up the entire ecosystem with a single command (`docker compose -f docker-compose.test.yml up -d`).
2. **Complete AD/LDAP & Vault UI Configuration Forms**:
   - Upgrade `frontend/src/components/settings/IdentityAuthTab.tsx` with comprehensive LDAP configuration inputs (Server, Port, Domain, BaseDN, BindDN, BindPassword, SSL toggle) and a "Test LDAP Connection" diagnostic action.
   - Upgrade `frontend/src/components/settings/SecretProvidersTab.tsx` with Vault AppRole authentication (`RoleId` / `SecretId`) and "Test Vault Connection" action.
   - Upgrade `frontend/src/components/servers/ServerModal.tsx` with dedicated, intuitive Mount/Path/Field inputs for Vault-backed MCP servers.
3. **Comprehensive Backend Integration Suites**:
   - Real LDAPS integration tests (`LdapActiveDirectoryServiceIntegrationTests.cs`) against containerized OpenLDAP with binary SID and `tokenGroups` parsing.
   - Multi-database engine verification (`MultiDatabaseProviderIntegrationTests.cs`) running schema migrations and Dapper query assertions across SQLite, MySQL, and MSSQL.
   - Vault AppRole & Token JIT TTL renewal test suites.
4. **Deep Playwright End-to-End Test Coverage**:
   - E2E tests for the new AD/LDAP settings UI and connection diagnostics.
   - Full lifecycle E2E tests for AppKey generation (one-time secret modal display & revocation) and OAuth/OpenIddict client registration.
   - Real RBAC policy enforcement E2E tests in the TestBench.

---

## 2. System Architecture & Compose Stack

```
                                  ┌────────────────────────────────────────┐
                                  │      docker-compose.test.yml           │
                                  └────────────────────────────────────────┘
                                                       │
        ┌──────────────────┬───────────────────────────┼──────────────────────────┬──────────────────┐
        │                  │                           │                          │                  │
        ▼                  ▼                           ▼                          ▼                  ▼
┌──────────────┐   ┌──────────────┐            ┌──────────────┐           ┌──────────────┐   ┌──────────────┐
│  OpenLDAP    │   │  HashiCorp   │            │   MySQL 8    │           │ MSSQL Server │   │   Mock MCP   │
│  (LDAPS 636) │   │    Vault     │            │  (Port 3306) │           │  (Port 1433) │   │ (HTTP & SSE) │
│  corp.local  │   │  (Port 8200) │            │  mcp_router_ │           │ mcp_router_  │   │  (Port 8090) │
│  users/SIDs  │   │  KV v2 &     │            │    test      │           │    test      │   │              │
│              │   │  AppRole     │            │              │           │ [profile:    │   │              │
│              │   │              │            │              │           │   mssql]     │   │              │
└──────────────┘   └──────────────┘            └──────────────┘           └──────────────┘   └──────────────┘
        ▲                  ▲                           ▲                          ▲                  ▲
        │                  │                           │                          │                  │
┌──────────────┐   ┌──────────────┐            ┌──────────────┐           ┌──────────────┐           │
│  Bootstrap   │   │  vault-init  │            │  mysql-init  │           │  mssql-init  │           │
│  LDIF Seed   │   │  Populate KV │            │  .sql mount  │           │  .sql mount  │           │
│  SIDs/Groups │   │  & AppRole   │            │              │           │              │           │
└──────────────┘   └──────────────┘            └──────────────┘           └──────────────┘           │
        ▲                  ▲                           ▲                          ▲                  │
        └──────────────────┴───────────────────────────┴──────────────────────────┴──────────────────┘
                                                       │
                                                       ▼
                                            ┌────────────────────┐
                                            │   mcp-router-e2e   │
                                            │   (Gateway under   │
                                            │    Test on 8088)   │
                                            └────────────────────┘
```

### 2.1 Services in `docker-compose.test.yml`

1. **`vault-test` & `vault-init`**:
   - Image: `hashicorp/vault:latest`
   - Dev mode with root token `root-test-token`.
   - `vault-init` configures KV v2 secret `secret/services/vault-test` (`token=test-vault-token-123`) and sets up AppRole auth (`test-role-id` / `test-secret-id`).
2. **`ldap-test`**:
   - Image: `osixia/openldap:latest`
   - Ports: `6636:636` (LDAPS), `3389:389` (LDAP).
   - Domain: `corp.local` (`dc=corp,dc=local`).
   - Admin DN: `cn=admin,dc=corp,dc=local`, Password: `adminpassword`.
   - Bootstrapped via custom LDIF:
     - `cn=Administrator,ou=users,dc=corp,dc=local` (sAMAccountName: `admin`, objectSid: `S-1-5-32-544`)
     - `cn=Alice Developer,ou=users,dc=corp,dc=local` (sAMAccountName: `alice`, objectSid: `S-1-5-21-1001`, tokenGroups: `S-1-5-21-2001`)
     - `cn=Bob Operator,ou=users,dc=corp,dc=local` (sAMAccountName: `bob`, objectSid: `S-1-5-21-1002`, tokenGroups: `S-1-5-21-2002`)
3. **`mysql-test`**:
   - Image: `mysql:8.0`
   - Port: `33066:3306`
   - Database: `mcp_router_test`, User: `mcp_user`, Password: `mcp_password`, Root Password: `root_password`.
   - Mounted `tests/fixtures/sql/mysql-init.sql` to `/docker-entrypoint-initdb.d/`.
4. **`mssql-test` (Profile: `mssql`)**:
   - Image: `mcr.microsoft.com/mssql/server:2022-latest`
   - Port: `14333:1433`
   - Environment: `ACCEPT_EULA=Y`, `SA_PASSWORD=McpRouterMSSQL2026!`.
   - Profile `mssql` allows developers on resource-constrained systems to start the core stack without MSSQL, while CI or full runs use `--profile mssql`.
5. **`mock-mcp-server`**:
   - Image: `node:20-alpine`
   - Port: `8090:8090`
   - Implements JSON-RPC 2.0 `initialize`, `tools/list`, and `tools/call` for HTTP (`/mcp`) and SSE (`/sse`, `/message`).
6. **`mcp-router-e2e`**:
   - Built from local repository `Dockerfile`.
   - Port: `8088:8080`.
   - Configured with environment variables linking to Vault, OpenLDAP, MySQL, and Mock MCP.

---

## 3. Frontend UI Upgrades

### 3.1 `IdentityAuthTab.tsx` (AD / LDAP Settings)
Upgrade `frontend/src/components/settings/IdentityAuthTab.tsx` to render complete Active Directory configuration inputs when enabled:
- **Server Address**: text input (e.g. `ldap-test` or `10.0.0.2`)
- **Port**: number input (default: `636`)
- **Use LDAPS (SSL)**: checkbox toggle (default: `true`)
- **Domain**: text input (e.g. `corp.local`)
- **Base DN**: text input (e.g. `DC=corp,DC=local`)
- **Bind DN / Service Account**: text input (e.g. `CN=admin,DC=corp,DC=local`)
- **Bind Password**: password input
- **Test Connection Button**: Triggers `POST /api/settings/auth/test-ad` and displays real-time connection status (bind success, user query check, SSL handshake verification).

### 3.2 `SecretProvidersTab.tsx` (Vault AppRole & Diagnostics)
Upgrade `frontend/src/components/settings/SecretProvidersTab.tsx`:
- **Auth Method Selector**: Radio toggle between `Token` and `AppRole`.
- **Token Input**: Password field when `Token` is selected.
- **AppRole Inputs**: `RoleId` (text) and `SecretId` (password) fields when `AppRole` is selected.
- **Test Connection Button**: Triggers `POST /api/settings/secrets/test-vault` to verify secret retrieval against `mountPath`.

### 3.3 `ServerModal.tsx` (Vault Mount/Path/Field Inputs)
When `secretProvider === 'Vault'`, replace the single ambiguous text input with 3 structured fields:
- **Mount Point**: text input (default: `secret`)
- **Secret Path**: text input (e.g. `services/my-service`)
- **Secret Field / Key**: text input (e.g. `api_key` or `token`)
- Encodes into payload cleanly while maintaining backward compatibility with the legacy `mount:path:field` format.

---

## 4. Backend Enhancements & Test Diagnostics

### 4.1 Diagnostic Endpoints (`Components/Providers/ProvidersController.cs`)
1. **`POST /api/settings/auth/test-ad`**:
   - Accepts LDAP connection parameters.
   - Attempts LDAPS connection and bind using `LdapConnection`.
   - Returns JSON `{ success: true, message: "LDAPS bind successful to corp.local on port 636." }` or `{ success: false, error: "..." }`.
2. **`POST /api/settings/secrets/test-vault`**:
   - Accepts Vault address, auth method, token / approle, and mount path.
   - Instantiates a temporary `VaultClient`, checks authentication and mount accessibility.
   - Returns JSON `{ success: true, message: "Vault connection authenticated successfully." }` or error.

### 4.2 Backend Unit & Integration Tests

1. **`LdapActiveDirectoryServiceIntegrationTests.cs`**:
   - Connects to containerized OpenLDAP on `:6636`.
   - Tests `ResolveUserSidsAsync("admin")` -> verifies `S-1-5-32-544` is returned.
   - Tests `ResolveUserSidsAsync("alice")` -> verifies user SID and group SID `S-1-5-21-2001`.
   - Tests fail-closed behavior on invalid bind credentials.
2. **`MultiDatabaseProviderIntegrationTests.cs`**:
   - Parametric tests running across SQLite, MySQL, and MSSQL:
     - Table initialization and schema migration idempotency.
     - Dapper CRUD operations on `McpServer`, `AppKey`, `AuthProviderConfig`, `SecretProviderConfig`.
     - JSON list serialization/deserialization with `JsonListTypeHandler`.
3. **`VaultAppRoleAndRenewalTests.cs`**:
   - Unit tests with mock `IVaultClient`:
     - Verifies `AppRoleAuthMethodInfo` initialization.
     - Simulates `LookupSelfAsync()` returning `TimeToLive < 300` -> asserts client re-creation / re-login.

---

## 5. Playwright End-to-End (E2E) Test Suite

| Test File | Description & Verification Flow |
| :--- | :--- |
| `frontend/e2e/ldap-identity-and-auth-flow.spec.ts` | 1. Navigate to Settings -> Identity & Auth.<br>2. Fill in LDAP server (`ldap-test`), port (636), domain, baseDn, bindDn, bindPassword.<br>3. Click "Test Connection" and verify success badge.<br>4. Save settings and verify persistence on page reload. |
| `frontend/e2e/appkey-and-client-lifecycle.spec.ts` | 1. Open App Keys & Security view.<br>2. Click "Create App Key", fill name, categories, TTL.<br>3. Submit and verify one-time secret key modal displays with copy button.<br>4. Revoke key and verify status changes to Revoked.<br>5. Open "Register Client", fill OAuth client details, save, and verify in Registered Clients table. |
| `frontend/e2e/rbac-enforcement-flow.spec.ts` | 1. Open Access Control settings.<br>2. Add policy restricting `server:mock` to group `AdminOnly`.<br>3. In Guest user context, navigate to TestBench.<br>4. Attempt tool execution -> assert 403 Forbidden / policy denial in output console. |
| `frontend/e2e/vault-approle-config-flow.spec.ts` | 1. Open Secret Providers settings.<br>2. Switch Vault auth to AppRole, fill `RoleId` and `SecretId`.<br>3. Click "Test Vault Connection" -> verify success.<br>4. Save and verify provider state. |

---

## 6. Directory & File Manifest

### New Files:
- `tests/fixtures/ldap/01-users-and-sids.ldif`
- `tests/fixtures/sql/mysql-init.sql`
- `tests/fixtures/sql/mssql-init.sql`
- `scripts/test-stack.sh`
- `McpRouter.Tests/LdapActiveDirectoryServiceIntegrationTests.cs`
- `McpRouter.Tests/MultiDatabaseProviderIntegrationTests.cs`
- `McpRouter.Tests/VaultAppRoleAndRenewalTests.cs`
- `frontend/src/test/components/IdentityAuthTab.test.tsx`
- `frontend/src/test/components/PromptTesterCard.test.tsx`
- `frontend/src/test/components/ResourceTesterCard.test.tsx`
- `frontend/e2e/ldap-identity-and-auth-flow.spec.ts`
- `frontend/e2e/appkey-and-client-lifecycle.spec.ts`
- `frontend/e2e/rbac-enforcement-flow.spec.ts`
- `frontend/e2e/vault-approle-config-flow.spec.ts`

### Modified Files:
- `docker-compose.test.yml`
- `frontend/src/components/settings/IdentityAuthTab.tsx`
- `frontend/src/components/settings/SecretProvidersTab.tsx`
- `frontend/src/components/servers/ServerModal.tsx`
- `frontend/src/shared/types/settings.ts`
- `Components/Providers/ProvidersController.cs`
- `Infrastructure/Secrets/VaultSecretRetriever.cs`
- `Infrastructure/Identity/LdapActiveDirectoryService.cs`

---

## 7. Verification & Release Gates

1. **Testbed Health**: Run `scripts/test-stack.sh up` -> verify all containers healthy (`vault-test`, `ldap-test`, `mysql-test`, `mock-mcp-server`, `mcp-router-e2e`).
2. **Backend Unit & Integration**: Run `dotnet test McpRouter.slnx` -> all 520+ tests pass.
3. **Frontend Quality & Vitest**: Run `npm run lint`, `npm run build`, and `npm test` in `frontend/` -> 0 errors, 100% tests pass.
4. **Playwright E2E**: Run `npm run test:e2e` in `frontend/` -> all E2E specs pass against live testbed.
5. **Release Verification Gate**: Run `python3 scripts/verify_release.py` -> 100% release checks pass.
