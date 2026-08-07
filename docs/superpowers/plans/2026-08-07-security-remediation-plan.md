# Security Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Active Directory LDAP resolution, fail-closed auditing, socket-level SSRF Connect validation, per-server Vault, AES-GCM encryption, AppKey migration, and request-scoped SSE authorization.

**Architecture:** Encapsulate security tasks into dedicated cross-platform services, register them in Dependency Injection, and wrap validation/connection steps with fail-closed handlers.

**Tech Stack:** C# ASP.NET Core (.NET 10.0), OpenIddict, SQLite/MySQL/MSSQL, System.DirectoryServices.Protocols.

## Global Constraints
- **Fail-Closed Default:** Any exception or missing configuration blocks bootstrap or request processing.
- **Cross-Platform:** Directory queries must run on Linux containers without Windows-specific APIs.

---

### Task 1: Fail-Closed Invocation Audit (P0-1)

**Files:**
- Modify: `Core/ClientSession.cs`
- Modify: `Core/Logging/AuditLogger.cs`
- Test: `McpRouter.Tests/AppKeyAuthenticationTests.cs`

**Interfaces:**
- Consumes: `IAuditLogger` from DI.
- Produces: Fail-closed try-finally blocks around `CallToolAsync`, `ReadResourceAsync`, and `GetPromptAsync`.

- [ ] **Step 1: Write the failing test**
  Add a test to verify that when `LogInvocationAsync` fails, the API call returns/throws a security failure.
  ```csharp
  [Fact]
  public async Task CallTool_FailsClosed_WhenAuditLogFails()
  {
      var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
          { "Audit:FailClosed", "true" }
      }).Build();
      // Setup a broken logger throwing DB exception...
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test --filter CallTool_FailsClosed_WhenAuditLogFails`
  Expected: FAIL

- [ ] **Step 3: Write implementation**
  Add `Audit:FailClosed` configuration check. In `CallToolAsync`, `ReadResourceAsync`, and `GetPromptAsync`, if `FailClosed` is true and audit throws, propagate the error (throwing `SecurityException` resulting in 503).
  ```csharp
  var failClosed = configuration.GetValue<bool>("Audit:FailClosed", true);
  ```

- [ ] **Step 4: Run test to verify it passes**
  Run: `dotnet test --filter CallTool_FailsClosed_WhenAuditLogFails`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add Core/ClientSession.cs Core/Logging/AuditLogger.cs && ./commit.sh "fix(audit): implement fail-closed try-finally logging"`

---

### Task 2: Active Directory LDAP Identity & SID Mapping (P0-2)

**Files:**
- Create: `Core/Identity/ILdapService.cs`
- Create: `Core/Identity/LdapActiveDirectoryService.cs`
- Modify: `Core/Identity/ActiveDirectoryIdentityProvider.cs`
- Modify: `Extensions/OpenIddictExtensions.cs`

- [ ] **Step 1: Write the failing test**
  Add a test verifying that `ActiveDirectoryIdentityProvider` successfully resolves a caller's SIDs and maps role to `Administrator` if SIDs contain the configured `Admin:GroupSid`.
  ```csharp
  [Fact]
  public async Task ADProvider_ResolvesUserSids_AndAllowsAdminRole()
  {
      // Mock ILdapService returning S-1-5-32-544
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test --filter ADProvider_ResolvesUserSids_AndAllowsAdminRole`
  Expected: FAIL

- [ ] **Step 3: Write implementation**
  Implement `LdapActiveDirectoryService` using `LdapConnection` from `System.DirectoryServices.Protocols`. Bind using Vault-provided credentials and query user group SIDs. Match against `Admin:GroupSid` to grant `Administrator` role.

- [ ] **Step 4: Run test to verify it passes**
  Run: `dotnet test --filter ADProvider_ResolvesUserSids_AndAllowsAdminRole`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add Core/Identity/ && ./commit.sh "feat(identity): implement cross-platform AD LDAP resolution"`

---

### Task 3: Trusted Proxy Header Stripping (P0-3)

**Files:**
- Modify: `Core/Identity/OidcIdentityProvider.cs`
- Modify: `Middleware/OidcHeaderAuthenticationHandler.cs`

- [ ] **Step 1: Write the failing test**
  Add a test verifying that `Remote-User` header is stripped if request IP is not in trusted proxies.
  ```csharp
  [Fact]
  public async Task HeaderAuth_StripsHeaders_ForUntrustedProxy()
  {
      // Mock HttpContext with untrusted IP and Remote-User header
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test --filter HeaderAuth_StripsHeaders_ForUntrustedProxy`
  Expected: FAIL

- [ ] **Step 3: Write implementation**
  In OIDC and header authentication paths, check immediate remote IP. If not trust-listed, clear/ignore headers.

- [ ] **Step 4: Run test to verify it passes**
  Run: `dotnet test --filter HeaderAuth_StripsHeaders_ForUntrustedProxy`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add Core/Identity/OidcIdentityProvider.cs Middleware/OidcHeaderAuthenticationHandler.cs && ./commit.sh "fix(security): strip proxy headers for untrusted remote IPs"`

---

### Task 4: Per-Server HashiCorp Vault Secrets & Renewal (P0-4)

**Files:**
- Modify: `Models/Models.cs`
- Modify: `Core/Secrets/VaultSecretRetriever.cs`
- Modify: `scripts/db/mysql/01_tables.sql`
- Modify: `scripts/db/mssql/01_tables.sql`

- [ ] **Step 1: Write the failing test**
  Add test verifying Vault retriever query matches Mount/Path/Field and performs JIT token renewal.
  ```csharp
  [Fact]
  public async Task Vault_RetrievesSpecificField_AndRenewsToken()
  {
      // Test per-server path secret extraction
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test --filter Vault_RetrievesSpecificField_AndRenewsToken`
  Expected: FAIL

- [ ] **Step 3: Write implementation**
  Add `SecretMount`, `SecretPath`, and `SecretField` properties to `McpServer`. Update Vault retriever to read from these fields. Implement JIT AppRole token re-login if token age is within 5 minutes of TTL.

- [ ] **Step 4: Run test to verify it passes**
  Run: `dotnet test --filter Vault_RetrievesSpecificField_AndRenewsToken`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add Models/ scripts/db/ Core/Secrets/ && ./commit.sh "feat(secrets): implement per-server Vault integration and JIT renewal"`

---

### Task 5: Cryptography Hardening (AES-GCM & PBKDF2) & AppKey Hashing Migration (P0-5)

**Files:**
- Modify: `Core/Secrets/SymmetricEncryptionHelper.cs`
- Modify: `Services/DatabaseSeederService.cs`

- [ ] **Step 1: Write the failing test**
  Add test validating that legacy AES-CBC keys are successfully decrypted, hashed using SHA-256, and rewritten to database.
  ```csharp
  [Fact]
  public async Task Startup_MigratesLegacyKeysToHashedKeys()
  {
      // Seed AES-CBC key and assert it becomes SHA-256 hash
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test --filter Startup_MigratesLegacyKeysToHashedKeys`
  Expected: FAIL

- [ ] **Step 3: Write implementation**
  Update `SymmetricEncryptionHelper` to use PBKDF2 key derivation and AES-GCM encryption. Implement startup migration in `DatabaseSeederService` to run at boot.

- [ ] **Step 4: Run test to verify it passes**
  Run: `dotnet test --filter Startup_MigratesLegacyKeysToHashedKeys`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add Core/Secrets/ Services/ && ./commit.sh "fix(crypto): implement AES-GCM, PBKDF2 derivation, and key hashing migration"`

---

### Task 6: Sockets Connect SSRF Protection (P0-6)

**Files:**
- Modify: `Core/Security/SecurityValidationHelper.cs`
- Modify: `Core/Transports/HttpTransport.cs`

- [ ] **Step 1: Write the failing test**
  Add test checking that connecting HttpClient to a hostname resolving to loopback is terminated at connection time.
  ```csharp
  [Fact]
  public async Task Connect_BlocksPrivateOrLoopbackIPs_AtSocketLevel()
  {
      // Attempt connect to resolving localhost mock
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test --filter Connect_BlocksPrivateOrLoopbackIPs_AtSocketLevel`
  Expected: FAIL

- [ ] **Step 3: Write implementation**
  Register custom `SocketsHttpHandler` with `ConnectCallback`. After DNS resolution, block loopback, link-local, private ranges, CGNAT, multicast, and IPv4-mapped IPv6, unless explicitly allowlisted in `Security:AllowedIpRanges`.

- [ ] **Step 4: Run test to verify it passes**
  Run: `dotnet test --filter Connect_BlocksPrivateOrLoopbackIPs_AtSocketLevel`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add Core/Security/ Core/Transports/ && ./commit.sh "fix(security): socket-level ConnectCallback SSRF validation"`

---

### Task 7: Request-Scoped SSE Authorization (P0-7)

**Files:**
- Modify: `Core/ClientSession.cs`
- Modify: `Extensions/ApplicationBuilderExtensions.cs`

- [ ] **Step 1: Write the failing test**
  Add test verifying that individual messages on the same SSE session are authorized using the caller's per-request context, not cached session handshake context.
  ```csharp
  [Fact]
  public async Task SSE_ValidatesIdentityPerMessage()
  {
      // Simulate two different users on same session connection
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test --filter SSE_ValidatesIdentityPerMessage`
  Expected: FAIL

- [ ] **Step 3: Write implementation**
  Modify SSE routing middleware to re-authenticate and resolve identity from headers for every request message, validating permissions against the fresh resolved context.

- [ ] **Step 4: Run test to verify it passes**
  Run: `dotnet test --filter SSE_ValidatesIdentityPerMessage`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add Core/ClientSession.cs Extensions/ApplicationBuilderExtensions.cs && ./commit.sh "fix(auth): enforce request-scoped SSE message authorization"`
