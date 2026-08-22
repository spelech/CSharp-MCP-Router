# Personal App Keys, App-Level Keys & User Quotas Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable self-service personal App Key minting and management for authenticated users, implement per-user custom quota overrides with a default of 5 keys, provide explicit separation of App-Level (System/Service) keys for administrators, and adapt dashboard UI based on role.

**Architecture:** Update `AppKeys` schema with `KeyType`, add `UserQuotas` table across SQLite/MSSQL/MySQL, update `AppKeysController` to `[Authorize]` with quota resolution and role checks, add admin quota endpoints, and update React frontend views and stores.

**Tech Stack:** C# ASP.NET Core, Dapper, SQLite/MSSQL/MySQL, TypeScript, React 19, Zustand, Vitest, xUnit.

## Global Constraints

- Must bump version from `4.26.1` to `4.27.0` (minor bump for new feature and schema additions) across `mcp-router.csproj`, `useUserStore.ts`, `CHANGELOG.md`, and `README.md`.
- All C# and Vitest tests must include requirement metadata tags (`[Requirement("REQ-ID", "AUTH", ...)]` and `@requirement REQ-ID`).
- Must run `dotnet run --project scripts/CatalogGenerator` and verify zero-drift with `--verify-only`.
- All unit and integration tests must pass (`dotnet test McpRouter.slnx`, `npm test -- --run`).

---

### Task 1: Database Schema & Seeder Updates

**Files:**
- Modify: `Infrastructure/Persistence/DatabaseSeederService.cs`
- Modify: `Components/AppKeys/AppKey.cs`
- Modify: `Components/AppKeys/AppKeyModels.cs`
- Create: `Components/AppKeys/UserQuota.cs`
- Test: `McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs`

- [ ] **Step 1: Write model classes and update AppKey**

Create `Components/AppKeys/UserQuota.cs`:
```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace McpRouter.Components.AppKeys
{
    public class UserQuota
    {
        [Key]
        public string Username { get; set; } = string.Empty;
        public int MaxKeys { get; set; } = 5;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SetUserQuotaRequest
    {
        public string Username { get; set; } = string.Empty;
        public int MaxKeys { get; set; } = 5;
    }
}
```

In `Components/AppKeys/AppKey.cs`, add:
```csharp
public string KeyType { get; set; } = "personal"; // "personal" | "system"
```

In `Components/AppKeys/AppKeyModels.cs`, add `KeyType`:
```csharp
public class CreateAppKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string KeyType { get; set; } = "personal"; // "personal" | "system"
    public List<string>? Scopes { get; set; }
    public int? ExpiresInDays { get; set; }
}
```

- [ ] **Step 2: Update `DatabaseSeederService.cs` for SQLite, MSSQL, and MySQL**

In SQLite, MSSQL, MySQL table seeders:
1. Ensure `AppKeys` table has `KeyType TEXT DEFAULT 'personal'`.
2. Ensure `UserQuotas` table is created with `Username`, `MaxKeys`, `CreatedAt`, `UpdatedAt`.

- [ ] **Step 3: Update `DatabaseSchemaUpgradeAndContractTests.cs` to verify schema changes**

- [ ] **Step 4: Run database tests**
Run: `dotnet test McpRouter.Tests --filter FullyQualifiedName~DatabaseSchemaUpgradeAndContractTests`
Expected: PASS

- [ ] **Step 5: Commit**
```bash
git add Components/AppKeys/ Infrastructure/Persistence/DatabaseSeederService.cs McpRouter.Tests/
git commit -m "feat(db): add UserQuotas table and AppKeys.KeyType schema support"
```

---

### Task 2: Repository Layer for UserQuotas and AppKeys

**Files:**
- Modify: `Infrastructure/Persistence/Repositories.cs`
- Test: `McpRouter.Tests/AppKeysControllerTests.cs`

- [ ] **Step 1: Add `IUserQuotaRepository` and update `IAppKeyRepository`**

In `Infrastructure/Persistence/Repositories.cs`:
```csharp
public interface IUserQuotaRepository
{
    Task<UserQuota?> GetUserQuotaAsync(string username);
    Task<IEnumerable<UserQuota>> GetAllUserQuotasAsync();
    Task SetUserQuotaAsync(string username, int maxKeys);
    Task DeleteUserQuotaAsync(string username);
}
```

Implement `IUserQuotaRepository` in `Repositories` (with SQLite, MSSQL, MySQL queries).
Update `GetAppKeysAsync` to support `keyType` filter (`string? keyType = null`).
Register `IUserQuotaRepository` in DI (`Program.cs` / services collection).

- [ ] **Step 2: Run unit tests**
Run: `dotnet test McpRouter.Tests --filter FullyQualifiedName~AppKey`
Expected: PASS

- [ ] **Step 3: Commit**
```bash
git add Infrastructure/Persistence/Repositories.cs Program.cs
git commit -m "feat(repo): implement IUserQuotaRepository and keyType filtering"
```

---

### Task 3: Backend Controller & Quota Engine (`AppKeysController.cs`)

**Files:**
- Modify: `Components/AppKeys/AppKeysController.cs`
- Test: `McpRouter.Tests/AppKeysControllerTests.cs`

- [ ] **Step 1: Write failing controller tests in `AppKeysControllerTests.cs`**
- Test regular user listing personal keys (`[Requirement("REQ-AUTH-PERSONAL-APPKEY-LIST", "AUTH", RequirementType.Positive, "Non-admin users can view their personal App Keys")]`).
- Test regular user minting personal key up to default 5 (`[Requirement("REQ-AUTH-PERSONAL-APPKEY-CREATE", "AUTH", RequirementType.Positive, "Non-admin users can create personal App Keys up to quota")]`).
- Test regular user 400 when exceeding quota.
- Test custom quota override from `UserQuotas` allows higher limit (`[Requirement("REQ-AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE", "AUTH", RequirementType.Positive, "Custom user quotas override default limit")]`).
- Test system-level key creation requires admin and bypasses quota (`[Requirement("REQ-AUTH-SYSTEM-APPKEY-SEPARATION", "AUTH", RequirementType.Positive, "System keys are distinct and require admin permissions")]`).
- Test regular user cannot delete another user's key or system keys.

- [ ] **Step 2: Update `AppKeysController.cs`**
- Change attribute to `[Authorize]`.
- Update `GetAppKeys`, `GetAppKeysLimits`, `CreateAppKey`, `DeleteAppKey`.
- Add `[HttpGet("quotas")]`, `[HttpPost("quotas")]`, `[HttpDelete("quotas/{username}")]` with `[Authorize(Policy = "AdminPolicy")]`.

- [ ] **Step 3: Run controller tests**
Run: `dotnet test McpRouter.Tests --filter FullyQualifiedName~AppKeysControllerTests`
Expected: PASS

- [ ] **Step 4: Commit**
```bash
git add Components/AppKeys/AppKeysController.cs McpRouter.Tests/AppKeysControllerTests.cs
git commit -m "feat(auth): enable self-service personal AppKeys and user quota endpoints"
```

---

### Task 4: Frontend Types, API, and Store Updates

**Files:**
- Create: `frontend/src/api/userQuotaApi.ts`
- Modify: `frontend/src/api/appKeyApi.ts`
- Modify: `frontend/src/shared/types/appKey.ts`
- Modify: `frontend/src/stores/useAppKeyStore.ts`
- Modify: `frontend/src/shared/stores/useAppKeyStore.ts` (if applicable)
- Test: `frontend/src/test/stores/useClientStore.test.ts` (or `useAppKeyStore.test.ts`)

- [ ] **Step 1: Update frontend types and API functions**
- Add `keyType?: 'personal' | 'system'` to `AppKey`, `CreateAppKeyRequest`.
- Add `UserQuota` interface and API methods in `frontend/src/api/appKeyApi.ts` / `userQuotaApi.ts`.

- [ ] **Step 2: Update `useAppKeyStore`**
- Add state for `keyTypeTab: 'personal' | 'system'`, `userQuotas: UserQuota[]`, `setUserQuota`, `deleteUserQuota`, `fetchUserQuotas`.

- [ ] **Step 3: Run Vitest tests**
Run: `npm test -- --run src/test/stores/`
Expected: PASS

- [ ] **Step 4: Commit**
```bash
git add frontend/src/api/ frontend/src/shared/types/ frontend/src/stores/ frontend/src/test/stores/
git commit -m "feat(frontend-store): add keyType and user quota state management"
```

---

### Task 5: Frontend UI Components (`AppKeysCard`, `AppKeyModal`, `GeneralTab`, `App.tsx`)

**Files:**
- Modify: `frontend/src/components/clients/AppKeysCard.tsx`
- Modify: `frontend/src/components/clients/AppKeyModal.tsx`
- Modify: `frontend/src/components/settings/GeneralTab.tsx`
- Modify: `frontend/src/App.tsx`
- Test: `frontend/src/test/components/AppKeysCard.test.tsx`, `AppKeyModal.test.tsx`, `GeneralTab.test.tsx`

- [ ] **Step 1: Update `App.tsx` navigation**
- For non-admins, tab title is `"My App Keys"` and `"Settings"` is hidden.

- [ ] **Step 2: Update `AppKeysCard.tsx`**
- For regular users: shows `"My App Keys"`, personal quota count, personal keys, config snippet copy, revoke.
- For admins: shows segmented views for **"App-Level Keys (System/Service)"** and **"User Personal Keys"** (with user quota management).

- [ ] **Step 3: Update `AppKeyModal.tsx`**
- Admins can choose Key Type (`Personal` vs `App-Level / System`).
- Non-admins are locked to Personal Key.

- [ ] **Step 4: Update `GeneralTab.tsx`**
- Under Security Defaults, add inputs for `Default User Quota (UserMaxKeys)` and `GlobalMaxKeys`.

- [ ] **Step 5: Run component tests**
Run: `npm test -- --run src/test/components/`
Expected: PASS

- [ ] **Step 6: Commit**
```bash
git add frontend/src/components/ frontend/src/App.tsx frontend/src/test/components/
git commit -m "feat(ui): role-adaptive AppKeys views, App-Level key separation, and quota settings"
```

---

### Task 6: Release Bump to `v4.27.0`, Catalog Verification & Integration Tests

**Files:**
- Modify: `mcp-router.csproj` (`4.27.0`)
- Modify: `frontend/src/stores/useUserStore.ts` & `frontend/src/shared/stores/useUserStore.ts` (`4.27.0`)
- Modify: `CHANGELOG.md` (Add `v4.27.0` release entry)
- Modify: `README.md` (Update top-5 release preview table)
- Update: `docs/software-requirements-and-test-catalog.md` & `docs/requirements-catalog.json`

- [ ] **Step 1: Bump version numbers and update release docs**
- [ ] **Step 2: Run all tests**
Run: `npm test -- --run`
Run: `dotnet test McpRouter.slnx`
- [ ] **Step 3: Regenerate and verify requirements catalog**
Run: `dotnet run --project scripts/CatalogGenerator`
Run: `dotnet run --project scripts/CatalogGenerator -- --verify-only`
Expected: 0 drift
- [ ] **Step 4: Commit release**
```bash
git add mcp-router.csproj frontend/src/stores/useUserStore.ts frontend/src/shared/stores/useUserStore.ts CHANGELOG.md README.md docs/software-requirements-and-test-catalog.md docs/requirements-catalog.json
git commit -m "chore(release): v4.27.0 - self-service personal AppKeys, App-Level keys, and user quotas"
```
