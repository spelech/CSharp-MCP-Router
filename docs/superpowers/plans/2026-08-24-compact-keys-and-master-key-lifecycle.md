# Compact AppKey Taxonomy & Master Key Lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement compact ~32-character Base62 AppKeys with semantic prefixes (`mcp-adm-`, `mcp-glb-`, `mcp-{domain}-`, `mcp-usr-`, `mcp-srv-`), custom `ROUTER_ADMIN_KEY` seeding, Master Key `KeySource` detection, Vault bootstrapping, safe atomic database re-encryption in Settings, and updated setup skills.

**Architecture:** Extend `CredentialService` with Base62 compact key generation and prefix mapping; update `AppKeyAuthenticationHandler` and `ClientAppKeySeeder` for custom admin keys; update `DbKeyHelper` for `KeySource` tracking and Vault bootstrapping; add master key re-encryption endpoint/tool; update Web UI Settings and setup skills; verify test catalog and bump version to v4.35.0.

**Tech Stack:** .NET 10 / C# 13, React/TypeScript, SQLite, MySQL, MSSQL, VaultSharp, xUnit, Playwright.

## Global Constraints
- Target Framework: net10.0
- Mandatory curly braces for all control flow statements (`csharp_prefer_braces = true:warning`)
- Preserve all MCP dual-spec and protocol version compatibility logic
- Preserve all database schema migration logic in `Infrastructure/Persistence/`
- Every test requirement must remain fully annotated with `[Requirement]` attributes and cataloged with zero drift (`scripts/CatalogGenerator`)
- Release version bump from 4.34.4 to 4.35.0 across `mcp-router.csproj`, `frontend/src/shared/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`

---

### Task 1: Compact Base62 AppKey Generator & Semantic Prefix Scheme in `CredentialService`

**Files:**
- Modify: `Components/Clients/CredentialService.cs`
- Modify: `McpRouter.Tests/AppKeyAuthenticationTests.cs`

**Interfaces:**
- Consumes: Scope list, key type, domain/server identifier
- Produces: Compact keys (~32–34 chars) formatted as `{prefix}{selector_8chars}-{secret_16chars}`

- [ ] **Step 1: Write failing unit test for compact key generation & prefix taxonomy**

Add tests to `McpRouter.Tests/AppKeyAuthenticationTests.cs`:
```csharp
[Fact]
[Requirement("AUTH-COMPACT-APPKEY-TAXONOMY", "AUTH", RequirementType.Positive, "Generates compact ~32-character Base62 AppKeys with semantic prefixes.")]
public async Task CreateCredentialAsync_GeneratesCompactKeysWithSemanticPrefixes()
{
    var credService = new CredentialService(_dbFactory);

    // 1. Admin key
    var (adminKey, adminPlain) = await credService.CreateCredentialAsync("Admin", "admin", "SID", new List<string> { "all", "admin" }, null, "system");
    Assert.StartsWith("mcp-adm-", adminPlain);
    Assert.InRange(adminPlain.Length, 32, 38);

    // 2. Global key
    var (glbKey, glbPlain) = await credService.CreateCredentialAsync("Global", "user1", "SID", new List<string> { "all" }, null, "personal");
    Assert.StartsWith("mcp-glb-", glbPlain);

    // 3. User / personal key
    var (usrKey, usrPlain) = await credService.CreateCredentialAsync("User", "user1", "SID", new List<string> { "mcp:read" }, null, "personal");
    Assert.StartsWith("mcp-usr-", usrPlain);

    // 4. Server-scoped key
    var (srvKey, srvPlain) = await credService.CreateCredentialAsync("Server", "user1", "SID", new List<string> { "server:docker" }, null, "personal");
    Assert.StartsWith("mcp-srv-", srvPlain);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~CreateCredentialAsync_GeneratesCompactKeysWithSemanticPrefixes`
Expected: FAIL (length is 108 chars instead of ~32-38 chars).

- [ ] **Step 3: Implement Base62 compact key generation in `CredentialService.cs`**

Update `CredentialService.cs`:
- Determine prefix slug:
  - If `keyType == "system"` or scopes contain `"admin"` -> `mcp-adm-`
  - Else if scopes contain `"all"` -> `mcp-glb-`
  - Else if server scope `server:{target}` -> `mcp-srv-` (or `mcp-srv-{target}-`)
  - Else if group/domain scope `group:{domain}` -> `mcp-{domain}-`
  - Else -> `mcp-usr-`
- Generate 6 Base62 bytes (8 chars) for selector and 12 Base62 bytes (16 chars) for secret.
- Store `KeyPrefix = $"{prefix}{selector}"`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~CreateCredentialAsync_GeneratesCompactKeysWithSemanticPrefixes`
Expected: PASS.

- [ ] **Step 5: Commit changes**

```bash
git add Components/Clients/CredentialService.cs McpRouter.Tests/AppKeyAuthenticationTests.cs
git commit -m "feat(auth): implement compact Base62 AppKeys with semantic prefix taxonomy"
```

---

### Task 2: Custom `ROUTER_ADMIN_KEY` Environment Seeding & Authentication Handling

**Files:**
- Modify: `Infrastructure/Persistence/DatabaseSeeders/ClientAppKeySeeder.cs`
- Modify: `Middleware/AppKeyAuthenticationHandler.cs`
- Modify: `McpRouter.Tests/DatabaseSeederServiceTests.cs`

**Interfaces:**
- Consumes: `IConfiguration["ROUTER_ADMIN_KEY"]` / `IConfiguration["MCP_ADMIN_KEY"]`
- Produces: Dynamic admin key seeding on startup and robust prefix-matching in auth middleware

- [ ] **Step 1: Write failing unit test for `ROUTER_ADMIN_KEY` environment seeding**

Add test in `McpRouter.Tests/DatabaseSeederServiceTests.cs`:
```csharp
[Fact]
[Requirement("AUTH-CUSTOM-ADMIN-KEY-SEEDING", "AUTH", RequirementType.Positive, "Seeds custom ROUTER_ADMIN_KEY when provided in configuration.")]
public async Task Startup_SeedsCustomAdminKey_WhenConfigured()
{
    var customKey = "mcp-adm-CustomKey123-Secret999";
    var inMemoryConfig = new Dictionary<string, string?>
    {
        { "ROUTER_ADMIN_KEY", customKey }
    };
    var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
    // Run seeder and verify key is stored and authenticates cleanly
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~Startup_SeedsCustomAdminKey_WhenConfigured`
Expected: FAIL.

- [ ] **Step 3: Implement custom key seeding in `ClientAppKeySeeder.cs` and update `AppKeyAuthenticationHandler.cs`**

- In `ClientAppKeySeeder.cs`:
  - Check `configuration["ROUTER_ADMIN_KEY"] ?? configuration["MCP_ADMIN_KEY"]`.
  - If set, parse selector prefix (or use first 16 chars) and seed with SHA-256 hash.
  - If unset, generate a compact `mcp-adm-XXXXXXXX-XXXXXXXXXXXXXXXX` key.
- In `AppKeyAuthenticationHandler.cs`:
  - Enhance prefix extraction to handle `mcp-adm-`, `mcp-glb-`, `mcp-usr-`, `mcp-srv-`, `mcp-{domain}-` with 8-char selectors cleanly.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~AppKey`
Expected: PASS.

- [ ] **Step 5: Commit changes**

```bash
git add Infrastructure/Persistence/DatabaseSeeders/ClientAppKeySeeder.cs Middleware/AppKeyAuthenticationHandler.cs McpRouter.Tests/DatabaseSeederServiceTests.cs
git commit -m "feat(auth): support ROUTER_ADMIN_KEY environment seeding and prefix authentication"
```

---

### Task 3: Master Key `KeySource` Tracking & Direct Vault Bootstrapping

**Files:**
- Modify: `Infrastructure/Secrets/DbKeyHelper.cs`
- Modify: `McpRouter.Tests/DbKeyHelperTests.cs`

**Interfaces:**
- Consumes: Vault configuration (`VAULT_ADDR`, `VAULT_TOKEN`, `VAULT_ROLE_ID`, `VAULT_SECRET_ID`, `VAULT_MASTER_KEY_PATH`), env vars, secret files
- Produces: `ResolveDbEncryptionKey()` with `KeySource` (`External`, `AutoGenerated`, `Configured`, `Vault`)

- [ ] **Step 1: Write failing unit test for `KeySource` detection**

Add tests to `McpRouter.Tests/DbKeyHelperTests.cs`:
```csharp
[Fact]
[Requirement("SEC-KEYSOURCE-DETECTION", "SEC", RequirementType.Positive, "Correctly identifies KeySource origin for environment, file, and auto-generated keys.")]
public void ResolveDbEncryptionKey_IdentifiesKeySourceAccurately()
{
    DbKeyHelper.ResetCache();
    var envConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "ROUTER_MASTER_KEY", "TestEnvKey1234567890123456789012==" } }).Build();
    DbKeyHelper.ResolveDbEncryptionKey(envConfig);
    Assert.Equal(MasterKeySource.External, DbKeyHelper.ActiveKeySource);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~ResolveDbEncryptionKey_IdentifiesKeySourceAccurately`
Expected: FAIL (property `ActiveKeySource` not yet defined).

- [ ] **Step 3: Implement `MasterKeySource` enum and Vault bootstrapping in `DbKeyHelper.cs`**

- Add `MasterKeySource` (`External`, `Vault`, `SecretFile`, `Configured`, `AutoGenerated`).
- Add Vault retrieval logic if `VAULT_ADDR` is configured.
- Track and expose `ActiveKeySource` property.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~DbKeyHelperTests`
Expected: PASS.

- [ ] **Step 5: Commit changes**

```bash
git add Infrastructure/Secrets/DbKeyHelper.cs McpRouter.Tests/DbKeyHelperTests.cs
git commit -m "feat(secrets): implement MasterKeySource tracking and Vault bootstrapping in DbKeyHelper"
```

---

### Task 4: Master Key Management API & Safe Atomic Database Re-Encryption

**Files:**
- Modify: `Components/Capabilities/AdminEndpoints.cs`
- Modify: `Core/Routing/AdminMcpServer.cs`
- Modify: `Infrastructure/Persistence/Repositories.cs`
- Create: `McpRouter.Tests/MasterKeyReEncryptionTests.cs`

**Interfaces:**
- Consumes: New master key string via `POST /api/config/master-key` or `manage_system(action: "set_master_key")`
- Produces: Atomic re-encryption of all database secrets, updating `./data/.master.key` and switching `KeySource` to `Configured`

- [ ] **Step 1: Write failing integration test for database re-encryption**

Create `McpRouter.Tests/MasterKeyReEncryptionTests.cs`:
```csharp
[Fact]
[Requirement("SEC-MASTERKEY-ATOMIC-REENCRYPTION", "SEC", RequirementType.Positive, "Atomically re-encrypts database credentials when setting a custom master key.")]
public async Task SetMasterKey_AtomicallyReEncryptsDatabaseCredentials()
{
    // 1. Seed servers and secret providers with Initial Key
    // 2. Invoke Re-encryption endpoint with New Master Key
    // 3. Verify all credentials decrypt successfully with the New Master Key
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~SetMasterKey_AtomicallyReEncryptsDatabaseCredentials`
Expected: FAIL.

- [ ] **Step 3: Implement atomic re-encryption service and endpoints**

- Add re-encryption method in `DatabaseRepository` / `SymmetricEncryptionHelper`:
  - Within a transaction, iterate `SecretProviders`, `AuthProviderConfigs`, `Servers`, `UserSecrets`.
  - Decrypt with old key, encrypt with new key, save rows.
  - Update `./data/.master.key`, update in-memory cache, and transition `KeySource` to `Configured`.
- Add `POST /api/config/master-key` endpoint in `AdminEndpoints.cs` and `manage_system(action: "set_master_key", newKey: "...")` in `AdminMcpServer.cs`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test McpRouter.slnx --filter FullyQualifiedName~SetMasterKey_AtomicallyReEncryptsDatabaseCredentials`
Expected: PASS.

- [ ] **Step 5: Commit changes**

```bash
git add Components/Capabilities/AdminEndpoints.cs Core/Routing/AdminMcpServer.cs Infrastructure/Persistence/Repositories.cs McpRouter.Tests/MasterKeyReEncryptionTests.cs
git commit -m "feat(security): add master key re-encryption endpoint and atomic database rotation"
```

---

### Task 5: Web UI Master Key Status Banner & Custom Key Modal

**Files:**
- Modify: `frontend/src/pages/Settings/GeneralSettingsTab.tsx` (or settings component)
- Modify: `frontend/src/shared/types/settings.ts`

**Interfaces:**
- Consumes: `GET /api/config/system-info` or settings status
- Produces: Warning banner when `keySource === "AutoGenerated"` with a modal to set custom Master Key

- [ ] **Step 1: Update frontend settings types and API integration**

Expose `masterKeySource` (`"External" | "Vault" | "SecretFile" | "Configured" | "AutoGenerated"`) in system settings response.

- [ ] **Step 2: Add UI warning alert and Set Master Key modal**

- When `AutoGenerated`: Show warning banner with "Set Custom Master Key" action button.
- When `External` / `Vault`: Show locked badge "Managed externally (Vault / Environment)".
- Provide confirmation modal with explanation of disaster recovery and atomic re-encryption.

- [ ] **Step 3: Run frontend tests & lint**

Run:
```bash
cd frontend
npm run lint
npm run test
```
Expected: PASS with 0 lint errors and all tests passing.

- [ ] **Step 4: Commit frontend changes**

```bash
git add frontend/
git commit -m "feat(ui): add master key status badge and custom key configuration modal in settings"
```

---

### Task 6: Update Setup Skills (`mcp-router-setup`, `mcp-router-admin`), Templates & Guides

**Files:**
- Modify: `.agents/skills/mcp-router-setup/SKILL.md` and `skills/mcp-router-setup/SKILL.md`
- Modify: `.agents/skills/mcp-router-admin/SKILL.md` and `skills/mcp-router-admin/SKILL.md`
- Modify: `.agents/skills/mcp-router-admin/templates/automate-setup.sh` and `.ps1`
- Modify: `docs/features-guide.md`
- Modify: `docs/secret-providers.md`
- Modify: `docs/deployment-guide.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: New compact key taxonomy, `ROUTER_ADMIN_KEY`, and master key lifecycle
- Produces: Complete, synchronized documentation and skill templates

- [ ] **Step 1: Update skills with interactive `ROUTER_ADMIN_KEY` prompt & compact key examples**

Update Phase 5 in `mcp-router-setup` to prompt for `ROUTER_ADMIN_KEY` and omit `ROUTER_MASTER_KEY` by default. Update `mcp-router-admin` templates to use compact keys.

- [ ] **Step 2: Update feature and deployment guides**

Document key taxonomy table (`mcp-adm-`, `mcp-glb-`, `mcp-{domain}-`, `mcp-usr-`, `mcp-srv-`), Base62 entropy, `ROUTER_ADMIN_KEY`, and Vault Master Key Bootstrapping.

- [ ] **Step 3: Commit documentation updates**

```bash
git add .agents/skills/ skills/ docs/ README.md
git commit -m "docs(skills): update setup skills, templates, and guides for compact keys and master key lifecycle"
```

---

### Task 7: Comprehensive Verification & Version Bump (v4.35.0)

**Files:**
- Modify: `mcp-router.csproj`
- Modify: `frontend/src/shared/stores/useUserStore.ts`
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `docs/software-requirements-and-test-catalog.md`
- Modify: `docs/requirements-catalog.json`

**Interfaces:**
- Consumes: All completed tasks
- Produces: v4.35.0 release with 100% green tests, verified requirements catalog, and zero format drift

- [ ] **Step 1: Run solution format check & full test suite**

Run:
```bash
dotnet format McpRouter.slnx --verify-no-changes
dotnet test McpRouter.slnx --configuration Release
```
Expected: PASS (0 format violations, all tests green).

- [ ] **Step 2: Regenerate SRS requirements catalog**

Run:
```bash
dotnet run --project scripts/CatalogGenerator
dotnet run --project scripts/CatalogGenerator -- --verify-only
```
Expected: PASS (zero drift).

- [ ] **Step 3: Bump version to 4.35.0 across release files**

Update `mcp-router.csproj` (`4.35.0`), `useUserStore.ts` (`'4.35.0'`), `CHANGELOG.md`, `README.md`.

- [ ] **Step 4: Run release verification gate**

Run:
```bash
python3 scripts/verify_release.py --skip-tests
```
Expected: 9/9 checks passed.

- [ ] **Step 5: Commit v4.35.0 release synchronization**

```bash
git add mcp-router.csproj frontend/src/shared/stores/useUserStore.ts CHANGELOG.md README.md docs/
git commit -m "chore(release): bump version to 4.35.0 with compact AppKeys and master key lifecycle"
```
