# CSharp-MCP-Router - Branch 3: Secure Routing & Namespace Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hardening client routing security by disabling auto-redirects, blocking SSRF private IP access on embedding settings, and enforcing strict namespacing split constraints to isolate backend routes.

**Architecture:** We will create a `SecurityValidationHelper` containing validation logic for private IP detection and namespace verification. We will configure `AddHttpClient` to disable redirect following. We will enforce that all tool, prompt, and resource actions validate their namespaces and server IDs against registered, active backend servers.

**Tech Stack:** ASP.NET Core, HttpClientHandler, Regex.

## Global Constraints
- Target Framework: `net10.0`
- Do not disable existing security checks.
- Keep validation logic modular and unit-testable.

---

### Task 1: Implement SecurityValidationHelper

**Files:**
- Create: `Core/Security/SecurityValidationHelper.cs`

- [ ] **Step 1: Write SecurityValidationHelper class**
  Create `/containers/dev/csharp-mcp-router/Core/Security/SecurityValidationHelper.cs` containing:
  1. `IsPrivateOrLoopback(string url)`: parses the host/IP of the URL and performs DNS lookup to check if any resolved IP belongs to loopback, link-local, or private RFC1918 IPv4/IPv6 address blocks.
  2. `IsValidServerId(string serverId)`: matches `^[a-zA-Z0-9_-]+$`.
  3. `ValidateToolOrPromptName(string name, System.Collections.Generic.IEnumerable<string> validServerIds)`:
     - Returns true if native (`search_tools`, `execute_tool`, starts with `router__`).
     - Otherwise, splits by `__`. Must contain exactly one `__` delimiter. The first segment must be in `validServerIds` and be a valid server ID.
  4. `ValidateResourceUri(string uri, System.Collections.Generic.IEnumerable<string> validServerIds)`:
     - Returns true if starts with `router://` or `logs://`.
     - If starts with `mcp://`, extracts host (the serverId). That serverId must be in `validServerIds` and be a valid server ID. Must not contain `__`.
     - Otherwise returns false.

---

### Task 2: Disable HTTP Redirect Following (Anti-SSRF)

**Files:**
- Modify: `Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Configure HttpClient registration**
  In `Extensions/ServiceCollectionExtensions.cs`, update `builder.Services.AddHttpClient();` to:
  ```csharp
  builder.Services.AddHttpClient().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
  {
      AllowAutoRedirect = false
  });
  ```

---

### Task 3: Enforce Private IP SSRF Gating on Embedding Settings

**Files:**
- Modify: `Services/ApiEmbeddingService.cs`
- Modify: `Services/DynamicEmbeddingService.cs`
- Modify: `Extensions/ApplicationBuilderExtensions.cs`

- [ ] **Step 1: Add validation check in ApiEmbeddingService**
  In `ApiEmbeddingService.GetEmbeddingAsync`, check if `_settings.EmbeddingApiUrl` is private/loopback using `SecurityValidationHelper.IsPrivateOrLoopback`. If so, throw `InvalidOperationException` unless the environment variable `ALLOW_PRIVATE_IPS` equals `"true"`.

- [ ] **Step 2: Add validation check in DynamicEmbeddingService**
  In `DynamicEmbeddingService.SaveSettings`, if `newSettings.EmbeddingProvider.Equals("api", StringComparison.OrdinalIgnoreCase)`:
  - If `SecurityValidationHelper.IsPrivateOrLoopback(newSettings.EmbeddingApiUrl)` and `Environment.GetEnvironmentVariable("ALLOW_PRIVATE_IPS") != "true"`, throw an `ArgumentException("Embedding URL points to a blocked private or loopback IP range.")`.

- [ ] **Step 3: Wrap SaveSettings in Minimal API try-catch**
  In `Extensions/ApplicationBuilderExtensions.cs`, wrap `embeddingService.SaveSettings(settings);` inside a `try-catch` for `ArgumentException` and return `Results.BadRequest(new { error = ex.Message })`.

---

### Task 4: Enforce Strict Namespace Routing Constraints

**Files:**
- Modify: `Core/ClientSession.cs`
- Modify: `Core/Routing/ToolRoutingManager.cs`

- [ ] **Step 1: Add namespace validation checks in ClientSession**
  In `ClientSession.cs`:
  - In `CallToolAsync(toolName, ...)`: check that the tool name is validated by `SecurityValidationHelper.ValidateToolOrPromptName`. If not, return a security error payload.
  - In `ReadResourceAsync(resourceUri, ...)`: check that the resource URI is validated by `SecurityValidationHelper.ValidateResourceUri`. If not, throw an `UnauthorizedAccessException`.
  - In `GetPromptAsync(promptName, ...)`: check that the prompt name is validated by `SecurityValidationHelper.ValidateToolOrPromptName`. If not, throw an `UnauthorizedAccessException`.
  - Pass the active server IDs (e.g. `_servers.Where(s => s.Enabled).Select(s => s.Id)`) into the validator.

- [ ] **Step 2: Add namespace validation inside execute_tool**
  In `ToolRoutingManager.cs`, inside `CallToolInternalAsync`'s `execute_tool` block:
  - Validate that the target tool `name` (the nested target tool name) is valid using `SecurityValidationHelper.ValidateToolOrPromptName`. If not, return an error payload.

- [ ] **Step 3: Compile and run test suite**
  Run: `dotnet build McpRouter.slnx --configuration Release`
  Run: `dotnet test McpRouter.slnx`

- [ ] **Step 4: Commit and run the version bump commit wrapper**
  Run: `./commit.sh "feat(security): implement strict namespace validation and SSRF private IP gating"`
  Expected: Version bumped to `3.3.0` (minor bump due to namespace validation logic).
