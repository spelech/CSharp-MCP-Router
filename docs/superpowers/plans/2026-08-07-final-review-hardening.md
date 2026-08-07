# CSharp-MCP-Router - Final Review Hardening Plan (B2, B3, B11, B12, B13, B14)

**Goal:** Address the remaining security-critical defects highlighted by the review:
1.  **B3:** Invert default-allow logic to fail-closed in `IsUserAuthorizedAsync` and target-server routing gates.
2.  **B11:** Safely close and dispose of overwritten active sessions to prevent socket/reader leaks in `SessionManager`.
3.  **B12:** Use SHA-256 hashing for storing newly generated app keys, comparing via constant-time verification.
4.  **B13:** Default-deny CORS requests in production if allowed origins are not explicitly configured.
5.  **B14:** Gate OpenIddict development certificates to only run in Development environments.
6.  **B2:** Default `Oidc:RequireTrustedProxy` to `true` to prevent header-spoofing in unconfigured environments.

---

### Task 1: Fail Closed Authorization (B3)

**Files:**
- Modify: `Core/ClientSession.cs`
- Modify: `Extensions/ApplicationBuilderExtensions.cs`

- [ ] **Step 1: Invert ClientSession defaults to false (deny)**
  Update `IsUserAuthorizedAsync`:
  - Change `return true;` to `return false;` when `RequestServices == null`.
  - Change `return true;` to `return false;` when `dbFactory == null`.
  - Change `return true;` to `return false;` when `policyCount == 0`.
  - Change `return true;` in `catch` block to `return false;`.

- [ ] **Step 2: Require active policies in ApplicationBuilderExtensions.cs target gate**
  In `/containers/dev/csharp-mcp-router/Extensions/ApplicationBuilderExtensions.cs`:
  - If `policyCount == 0`, deny access (HTTP 403) to the target server instead of bypassing.

---

### Task 2: Close Overwritten Sessions (B11)

**Files:**
- Modify: `Core/SessionManager.cs`

- [ ] **Step 1: Close pre-existing session under the same key**
  In `CreateSessionAsync` (inside `SessionManager.cs`):
  - Before writing to `_sessions[sessionId]`, check if a session already exists for `sessionId`. If so, remove and close it properly.

---

### Task 3: Salted Hash App Keys (B12)

**Files:**
- Modify: `Controllers/AppKeysController.cs`
- Modify: `Middleware/AppKeyAuthenticationHandler.cs`

- [ ] **Step 1: Save SHA-256 hash in AppKeysController**
  When creating a key, compute the SHA-256 hash of `plaintextKey` and store the 64-character lowercase hex string in `EncryptedKey`.

- [ ] **Step 2: Constant-time validation with legacy fallback**
  In `AppKeyAuthenticationHandler.cs`:
  - Check if the stored key is 64 hex characters. If so, compare the SHA-256 hash using `FixedTimeEquals`.
  - Otherwise, decrypt using legacy AES and compare using `FixedTimeEquals`.

---

### Task 4: CORS Default-Deny in Production (B13)

**Files:**
- Modify: `Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Check environment before adding CORS localhost defaults**
  - If `builder.Environment.EnvironmentName` is not `"Development"` / `"Dev"`, map allowed origins to an invalid dummy origin instead of wildcard localhost ports.

---

### Task 5: Gate Dev Certificates (B14)

**Files:**
- Modify: `Extensions/OpenIddictExtensions.cs`
- Modify: `Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Accept environment in AddMcpOpenIddict**
  - Update method signature in `OpenIddictExtensions.cs` to take `IHostEnvironment env`.
  - Gate calling `AddDevelopmentEncryptionCertificate` and `AddDevelopmentSigningCertificate` behind `env.IsDevelopment()`.

- [ ] **Step 2: Pass environment from ServiceCollectionExtensions.cs**
  - Pass `builder.Environment` to `AddMcpOpenIddict()`.

---

### Task 6: OIDC RequireTrustedProxy Default (B2)

**Files:**
- Modify: `Core/Identity/OidcIdentityProvider.cs`

- [ ] **Step 1: Change default config value to true**
  - Change default fallback value from `false` to `true` when reading `Oidc:RequireTrustedProxy`.
