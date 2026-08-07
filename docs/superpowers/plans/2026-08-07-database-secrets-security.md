# CSharp-MCP-Router - Branch 4: Database & Secrets Security Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce database protection, encrypt sensitive fields in database tables (enabling multi-DB portability), mask connection credentials from logs, require HTTPS on external vaults/issuers, and write a database encryption verification test.

**Architecture:**
1. Update `PiiSanitizer` to regex-redact `Password=...` from connection strings.
2. Add Entity Framework Core Value Converters in `RouterDbContext` to encrypt sensitive properties (`ApiKey`, `SecretItemKey`, and `HeadersJson` for `McpServer`, and `EmbeddingApiKey` for `RouterSettings`).
3. Add HTTPS validation helper in `SecurityValidationHelper` and enforce it during `VaultSecretRetriever` initialization and `SaveSecretProvider` / `SaveAuthProvider` endpoint calls.
4. Add a test class `DatabaseEncryptionTests.cs` that attempts to read the created database file directly as a plain-text SQLite file, ensuring it fails (proving SQLCipher encryption).

---

### Task 1: Mask Connection String Passwords in Logs

**Files:**
- Modify: `Core/Logging/PiiSanitizer.cs`

- [ ] **Step 1: Add regex and replacement for connection string passwords**
  In `/containers/dev/csharp-mcp-router/Core/Logging/PiiSanitizer.cs`, add:
  ```csharp
  private static readonly Regex ConnStringPasswordRegex = new(@"Password\s*=\s*[^;]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
  ```
  And inside `SanitizePayload`:
  ```csharp
  sanitized = ConnStringPasswordRegex.Replace(sanitized, "Password=[REDACTED]");
  ```

---

### Task 2: Implement Column-Level Symmetric Encryption in RouterDbContext

**Files:**
- Modify: `Models/Models.cs`

- [ ] **Step 1: Track IConfiguration and register Value Converters in RouterDbContext**
  In `/containers/dev/csharp-mcp-router/Models/Models.cs`:
  1. Add a private read-only `IConfiguration _configuration` field.
  2. Set it in the constructor.
  3. In `OnModelCreating`, declare the value converter:
     ```csharp
     var apiConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<string?, string?>(
         v => v == null ? null : SymmetricEncryptionHelper.Encrypt(v, _configuration),
         v => v == null ? null : SymmetricEncryptionHelper.Decrypt(v, _configuration)
     );
     ```
  4. Apply the converter to the following properties:
     - `McpServer.ApiKey`
     - `McpServer.HeadersJson`
     - `McpServer.SecretItemKey`
     - `RouterSettings.EmbeddingApiKey`

---

### Task 3: Enforce HTTPS for Vault Address and Dynamic Providers

**Files:**
- Modify: `Core/Security/SecurityValidationHelper.cs`
- Modify: `Core/Secrets/VaultSecretRetriever.cs`
- Modify: `Controllers/ProvidersController.cs`

- [ ] **Step 1: Add JSON URL HTTPS validation helper**
  In `/containers/dev/csharp-mcp-router/Core/Security/SecurityValidationHelper.cs`, implement `ValidateJsonUrlsRequireHttps(string json)`:
  - Parses the JSON string.
  - Recursively or iteratively checks property names containing "url", "uri", "authority", "issuer", or "endpoint".
  - If a property value is a string and starts with a URL format (`http://` or `https://`), throws an `ArgumentException` if it doesn't start with `https://`.

- [ ] **Step 2: Add HTTPS enforcement to VaultSecretRetriever**
  In `/containers/dev/csharp-mcp-router/Core/Secrets/VaultSecretRetriever.cs`, check if `address` starts with `https://` (unless empty). If not, throw an `ArgumentException("Vault Address must use the HTTPS scheme.")`.

- [ ] **Step 3: Enforce HTTPS checks in ProvidersController**
  In `/containers/dev/csharp-mcp-router/Controllers/ProvidersController.cs`:
  - In `SaveSecretProvider`: call `SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson)`.
  - In `SaveAuthProvider`: call `SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson)`.
  - Wrap any thrown `ArgumentException` in a try-catch and return `BadRequest(new { error = ex.Message })`.

---

### Task 4: Write Database Encryption Integration Tests

**Files:**
- Create: `McpRouter.Tests/DatabaseEncryptionTests.cs`

- [ ] **Step 1: Write integration test verifying SQLCipher file encryption**
  Create `/containers/dev/csharp-mcp-router/McpRouter.Tests/DatabaseEncryptionTests.cs` containing a test that:
  - Generates a temporary SQLite database using `DbConnectionFactory` (which applies the encryption password).
  - Creates a table and writes a test value.
  - Closes the connection.
  - Attempts to open the database file using a plain SqliteConnection WITHOUT a password (e.g. `Data Source=temp.db`).
  - Verifies that attempting to read from it throws a `SqliteException` indicating the file is encrypted or not a database (proving SQLCipher is active and working).

---

### Task 5: Compile, Verify, and Commit

- [ ] **Step 1: Compile the project**
  Run: `dotnet build McpRouter.slnx --configuration Release`

- [ ] **Step 2: Run all tests**
  Run: `dotnet test McpRouter.slnx`

- [ ] **Step 3: Commit and bump version**
  Run: `./commit.sh "feat(security): implement column-level encryption, HTTPS enforcement, and DB password logging masking"`
  Expected: Version bumped to `3.4.0` (minor bump).
