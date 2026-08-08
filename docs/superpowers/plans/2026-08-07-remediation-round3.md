# Remediation Round 3 (v4.0.0) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement all 11 remediation items (R3-1 through R3-11) specified in `RE-03-round3-final-workorder.md` to close all residual security holes, hard-code secure defaults, add full governance query capability, and bump version to v4.0.0.

**Architecture:** Address each security item on its actual production call paths, enforce fail-closed checks, eliminate insecure defaults across C# code and SQL schemas, and expand unit/integration test coverage across all modified surfaces.

**Tech Stack:** C# .NET 10, ASP.NET Core, OpenIddict, Entity Framework Core / Dapper / SQLite / MSSQL / MySQL, xUnit.

## Global Constraints

- **Rule 1: WIRED, not just written** — grep for call sites on prod path.
- **Rule 2: NO FAIL-OPEN SIBLING** — fail closed on null/not configured/exception.
- **Rule 3: NO INSECURE DEFAULT** — default to secure settings ('None' SecretProvider, opaque session IDs, persistent cert required in Prod).
- **Rule 4: TEST THE PROD PATH** — assert on real endpoints, models, and handlers.
- **Rule 5: MANDATORY VERSION BUMP** — Bump `mcp-router.csproj`, `wwwroot/index.html`, and `README.md` simultaneously.

---

### Task 1: R3-1 — Audit Caller's SIDs (Not Scalar `Sid`)

**Files:**
- Modify: `Core/ClientSession.cs:1021-1033`
- Modify: `McpRouter.Tests/McpIntegrationTests.cs`

- [ ] **Step 1: Write failing test**
Extend `AuditLogger_RecordsPerRequestActor_NotHandshakeActor` in `McpIntegrationTests.cs` so identity carries a `Sid` claim (`S-1-5-21-1007`) in `Sids`/`AllSids` and assert `loggedSid == "S-1-5-21-1007"`.

- [ ] **Step 2: Run test to verify failure**
Run `dotnet test --filter AuditLogger_RecordsPerRequestActor_NotHandshakeActor` -> EXPECT FAIL (`""` != `"S-1-5-21-1007"`).

- [ ] **Step 3: Implement fix**
In `Core/ClientSession.cs`, replace `identity.Sid ?? ""` with `identity.AllSids.Count > 0 ? string.Join(";", identity.AllSids) : ""`.

- [ ] **Step 4: Verify test passes**
Run `dotnet test --filter AuditLogger_RecordsPerRequestActor_NotHandshakeActor` -> PASS.

- [ ] **Step 5: Prove gate**
`grep -n 'identity\.Sid' Core/ClientSession.cs` -> 0 hits in `AuditInvocationAsync`.

---

### Task 2: R3-3 — Opaque Session ID Capability (B11)

**Files:**
- Modify: `Extensions/ApplicationBuilderExtensions.cs:610-621`
- Modify: `McpRouter.Tests/McpIntegrationTests.cs`

- [ ] **Step 1: Write failing test**
Add `Mcp_SessionId_IsOpaque_NotBearerToken` in `McpIntegrationTests.cs` asserting `/mcp` SSE endpoint returns a 32-hex GUID `sessionId`, distinct from the Bearer token, and unique across calls.

- [ ] **Step 2: Run test to verify failure**
Run `dotnet test --filter Mcp_SessionId_IsOpaque_NotBearerToken` -> FAIL.

- [ ] **Step 3: Implement fix**
In `Extensions/ApplicationBuilderExtensions.cs`, replace token extraction as sessionId with `string sessionId = Guid.NewGuid().ToString("N");`. Remove `hasBearerToken` variable and dependent branches.

- [ ] **Step 4: Verify test passes**
Run `dotnet test --filter Mcp_SessionId_IsOpaque_NotBearerToken` -> PASS.

- [ ] **Step 5: Prove gate**
`grep -n 'Substring("Bearer ".Length)\|hasBearerToken' Extensions/ApplicationBuilderExtensions.cs` -> 0 hits.

---

### Task 3: R3-4 — Redact Secrets from Console & Debug Sinks (B7)

**Files:**
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Modify: `Core/Transports/HttpTransport.cs`
- Modify: `Core/Transports/SseTransport.cs`
- Modify: `Core/ClientSession.cs`
- Modify: `Core/Logging/PiiSanitizer.cs`
- Modify: `McpRouter.Tests/LoggingSanitizationTests.cs`

- [ ] **Step 1: Write failing test**
Add tests `PiiSanitizer_Redacts_Basic_ApiKey_Cookie_QueryToken_UrlUserInfo`, `RequestLogger_DoesNotLog_AuthorizationHeader_OrBody`, and `DeleteLogs_WritesAdminAudit`.

- [ ] **Step 2: Run tests to verify failure**
Run `dotnet test --filter "PiiSanitizer|RequestLogger|DeleteLogs"` -> FAIL.

- [ ] **Step 3: Implement fix**
Update `PiiSanitizer.cs` regexes. Change request logging middleware in `ApplicationBuilderExtensions.cs` to log metadata only. Wrap payload logs in `PiiSanitizer.SanitizePayload(...)` and change `LogInformation` to `LogDebug` in `HttpTransport.cs`, `SseTransport.cs`, `ClientSession.cs`, and `ApplicationBuilderExtensions.cs`. Add admin audit logging to `DELETE /api/logs`.

- [ ] **Step 4: Verify tests pass**
Run `dotnet test --filter "PiiSanitizer|RequestLogger|DeleteLogs"` -> PASS.

- [ ] **Step 5: Prove gate**
`grep -nE 'LogInformation\(.*(Headers|\{Body\}|\{Payload\})' Core Extensions --include=*.cs` -> 0 hits.

---

### Task 4: R3-6 — Persistent OpenIddict Keys in Production

**Files:**
- Modify: `Extensions/OpenIddictExtensions.cs`
- Modify: `Extensions/ServiceCollectionExtensions.cs`
- Modify: `Program.cs`
- Modify: `McpRouter.Tests/OpenIddictProductionTests.cs`

- [ ] **Step 1: Write failing tests**
Add `OpenIddict_Production_RefusesBoot_WithoutCertificate` and `OpenIddict_Production_Boots_WithCertificate`.

- [ ] **Step 2: Run tests to verify failure**
Run `dotnet test --filter OpenIddict_Production` -> FAIL.

- [ ] **Step 3: Implement fix**
Update `AddMcpOpenIddict` to take `IConfiguration`. In Non-Development environment, require valid `OpenIddict:CertificatePath` / `OPENIDDICT_CERT_PATH`. Load `X509Certificate2` and call `AddSigningCertificate` + `AddEncryptionCertificate`. Remove `AddEphemeral*Key` calls.

- [ ] **Step 4: Verify tests pass**
Run `dotnet test --filter OpenIddict_Production` -> PASS.

- [ ] **Step 5: Prove gate**
`grep -n 'AddEphemeral' Extensions/OpenIddictExtensions.cs` -> 0 hits.

---

### Task 5: R3-2 — Docker Egress SSRF Hostname Resolution Check

**Files:**
- Modify: `Services/DockerAutoDiscoveryService.cs`
- Modify: `McpRouter.Tests/DockerAutoDiscoveryTests.cs`

- [ ] **Step 1: Write failing test**
Add `DockerDiscovery_SkipsContainer_ResolvingToPrivateIp` asserting container with hostname resolving to private/blocked IP is skipped.

- [ ] **Step 2: Run test to verify failure**
Run `dotnet test --filter DockerDiscovery_SkipsContainer_ResolvingToPrivateIp` -> FAIL.

- [ ] **Step 3: Implement fix**
In `DockerAutoDiscoveryService.cs`, use `Dns.GetHostAddresses` if parsed host is a hostname, then run `IsBlockedIp` check against all resolved IPs.

- [ ] **Step 4: Verify test passes**
Run `dotnet test --filter DockerDiscovery_SkipsContainer_ResolvingToPrivateIp` -> PASS.

---

### Task 6: R3-5 — Authorization Filtering for List Endpoints (B10)

**Files:**
- Modify: `Core/ClientSession.cs`
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Modify: `McpRouter.Tests/AuthorizationFilterTests.cs`

- [ ] **Step 1: Write failing test**
Add `ToolsList_FiltersByAuthorization` asserting non-admin user sees only authorized tools in `tools/list`, admin sees all, unauthorized user sees none.

- [ ] **Step 2: Run test to verify failure**
Run `dotnet test --filter ToolsList_FiltersByAuthorization` -> FAIL.

- [ ] **Step 3: Implement fix**
In `ClientSession.cs`, add `HttpContext? httpContext` to `ListToolsAsync`, `ListResourcesAsync`, `ListPromptsAsync` and implement `FilterAuthorizedAsync`. Pass `httpContext` / session context from `ApplicationBuilderExtensions.cs`.

- [ ] **Step 4: Verify test passes**
Run `dotnet test --filter ToolsList_FiltersByAuthorization` -> PASS.

---

### Task 7: R3-7 — Gate Legacy AES-CBC Migration Behind Flag

**Files:**
- Modify: `Services/DatabaseSeederService.cs`
- Modify: `McpRouter.Tests/DatabaseSeederServiceTests.cs`

- [ ] **Step 1: Write failing test**
Add `KeyMigration_NotRun_WhenFlagAbsent` asserting legacy CBC AppKey is unchanged without `RUN_KEY_MIGRATION=true` and converted when set.

- [ ] **Step 2: Run test to verify failure**
Run `dotnet test --filter KeyMigration_NotRun_WhenFlagAbsent` -> FAIL.

- [ ] **Step 3: Implement fix**
Wrap `appKeys` migration loop in `DatabaseSeederService.cs` with `if (string.Equals(runKeyMigration, "true", StringComparison.OrdinalIgnoreCase))`.

- [ ] **Step 4: Verify test passes**
Run `dotnet test --filter KeyMigration_NotRun_WhenFlagAbsent` -> PASS.

- [ ] **Step 5: Prove gate**
`grep -n 'DecryptLegacyAppKey' Services/DatabaseSeederService.cs` -> call site is inside flag guard only.

---

### Task 8: R3-8 — Make SecretProvider Default 'None' & Fix Real Seeder Tests

**Files:**
- Modify: `Models/Models.cs`
- Modify: `Services/DatabaseSeederService.cs`
- Modify: `scripts/db/mssql/01_tables.sql`
- Modify: `scripts/db/mysql/01_tables.sql`
- Modify: `McpRouter.Tests/DatabaseSeederServiceTests.cs`

- [ ] **Step 1: Write failing test**
Update `DatabaseSeederServiceTests` to run `SeedDatabase` against SQLite and assert unconfigured server defaults to `'None'`, and explicit `'Vault'` stays `'Vault'`.

- [ ] **Step 2: Run test to verify failure**
Run `dotnet test --filter DatabaseSeeder` -> FAIL.

- [ ] **Step 3: Implement fix**
Change all `DEFAULT 'Vault'` to `DEFAULT 'None'` and `SecretProvider = "None"`. Delete SQLite backfill block `SET SecretProvider='None'`. Add warning log for misconfigured servers.

- [ ] **Step 4: Verify tests pass**
Run `dotnet test --filter DatabaseSeeder` -> PASS.

- [ ] **Step 5: Prove gate**
`grep -rniE "DEFAULT 'Vault'" scripts` -> 0
`grep -n '= "Vault"' Models/Models.cs Services/DatabaseSeederService.cs` -> 0
`grep -rn "SET SecretProvider = 'None'\|SET SecretProvider='None'" Services scripts` -> 0

---

### Task 9: R3-9 — Delete Dead Salt Constant

**Files:**
- Modify: `Core/Secrets/SymmetricEncryptionHelper.cs`

- [ ] **Step 1: Implement fix**
Remove `private static readonly byte[] Salt = ...` at line 12.

- [ ] **Step 2: Prove gate**
`grep -n 'readonly byte\[\] Salt' Core/Secrets/SymmetricEncryptionHelper.cs` -> 0.

---

### Task 10: R3-10 — Authenticated Audit Query API (REQ-GOV)

**Files:**
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Create/Modify: `McpRouter.Tests/AuditQueryApiTests.cs`

- [ ] **Step 1: Write failing test**
Add `AuditQuery_RequiresAdmin_AndReturnsRows` asserting non-admin gets 403, admin gets filtered rows, and endpoint writes an admin audit record.

- [ ] **Step 2: Run test to verify failure**
Run `dotnet test --filter AuditQuery_RequiresAdmin_AndReturnsRows` -> FAIL.

- [ ] **Step 3: Implement fix**
Add `GET /api/audit` endpoint in admin group in `ApplicationBuilderExtensions.cs` with `user`, `server`, `since`, `take`, `skip` params, dialect-specific paging (`OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY` for MSSQL, `LIMIT @take OFFSET @skip` for SQLite/MySQL), and audit logging.

- [ ] **Step 4: Verify test passes**
Run `dotnet test --filter AuditQuery_RequiresAdmin_AndReturnsRows` -> PASS.

---

### Task 11: R3-11 & Version Bump (v4.0.0)

**Files:**
- Modify: `McpRouter.Tests/McpIntegrationTests.cs` / other test files
- Modify: `mcp-router.csproj`
- Modify: `wwwroot/index.html`
- Modify: `README.md`

- [ ] **Step 1: Implement R3-11 residual prod-path tests**
Add:
- `SseTransport` fail-closed refusal test.
- API 400 test for `POST`/`PUT /api/servers` with blocked SSRF URL.
- Negative app-key auth test via `AppKeyAuthenticationHandler`.
- Production boot smoke test (with master key & cert).
- `McpClient` named-client SSRF test using real `AddMcpRouterServices()`.

- [ ] **Step 2: Verify all tests pass**
Run `dotnet test McpRouter.slnx` -> PASS all.

- [ ] **Step 3: Version bump v4.0.0**
Bump `<Version>4.0.0</Version>`, `<AssemblyVersion>4.0.0.0</AssemblyVersion>`, `<FileVersion>4.0.0.0</FileVersion>` in `mcp-router.csproj`. Update `wwwroot/index.html` badge to `v4.0.0`. Add v4.0.0 release notes in `README.md`.

- [ ] **Step 4: Run full verification gate**
Run full `dotnet test McpRouter.slnx` and execute all verification grep gates.
