# CSharp-MCP-Router - Branch 2: Management Plane Authentication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Secure the management plane (/api/*) by introducing a proper ASP.NET Core authentication scheme for OIDC headers, mapping authorization policies, and enforcing authentication/authorization across all endpoints.

**Architecture:** We will implement an `OidcHeaderAuthenticationHandler` that parses forward-auth/OIDC headers, validates trusted proxies (relying on `OidcIdentityProvider`), and builds a `ClaimsPrincipal` with appropriate roles. We will register an `AdminPolicy` requiring the `Administrator` role, and configure all minimal APIs and controllers to require this policy.

**Tech Stack:** ASP.NET Core, OpenIddict, Authentication & Authorization middleware.

## Global Constraints
- Target Framework: `net10.0`
- Do not bypass authentication in production.
- Use built-in C# identity classes (`ClaimsPrincipal`, `ClaimsIdentity`).

---

### Task 1: Create OidcHeader Authentication Handler

**Files:**
- Create: `Middleware/OidcHeaderAuthenticationHandler.cs`

- [ ] **Step 1: Implement the OidcHeaderAuthenticationHandler**
  Create a new file `Middleware/OidcHeaderAuthenticationHandler.cs` inheriting from `AuthenticationHandler<AuthenticationSchemeOptions>`.
  It must:
  1. Retrieve `OidcIdentityProvider` via Dependency Injection.
  2. Call `ResolveIdentityAsync(Context)`.
  3. If resolved identity is not `guest` or `anonymous`:
     - Construct a `ClaimsIdentity` and `ClaimsPrincipal`.
     - Assign `ClaimTypes.Name` = `identity.Username`.
     - Assign `ClaimTypes.Role` for each group in `identity.GroupNames`.
     - Assign `ClaimTypes.Role` = `"Administrator"` if username is `"admin"` or groups contain `"Administrators"` or `"full_admin"`.
     - Set `Context.Items["AuthenticatedUser"] = identity.Username` (for backwards compatibility with `AppKeysController`).
     - Return `AuthenticateResult.Success(...)`.
  4. If proxy validation failed or no identity is found, return `AuthenticateResult.NoResult()`.

---

### Task 2: Register Auth Handler and AdminPolicy

**Files:**
- Modify: `Extensions/OpenIddictExtensions.cs`

- [ ] **Step 1: Register OidcHeader authentication scheme**
  In `Extensions/OpenIddictExtensions.cs`, add:
  ```csharp
  .AddScheme<AuthenticationSchemeOptions, OidcHeaderAuthenticationHandler>("OidcHeader", null);
  ```

- [ ] **Step 2: Define AdminPolicy**
  In `Extensions/OpenIddictExtensions.cs`, under `services.AddAuthorization(...)`, register the policy:
  ```csharp
  options.AddPolicy("AdminPolicy", policy =>
  {
      policy.RequireAuthenticatedUser()
            .RequireRole("Administrator")
            .AddAuthenticationSchemes("OidcHeader", OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, "AppKey");
  });
  ```

- [ ] **Step 3: Modify DefaultPolicy to support OidcHeader**
  Ensure the DefaultPolicy includes `OidcHeader` in its authentication schemes.

---

### Task 3: Secure Controllers & Remove Fallback Middleware

**Files:**
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Modify: `Controllers/AppKeysController.cs`
- Modify: `Controllers/ClientsController.cs`
- Modify: `Controllers/PermissionsController.cs`
- Modify: `Controllers/ProvidersController.cs`
- Modify: `Controllers/AuthorizationController.cs`

- [ ] **Step 1: Remove unsecure fallback middleware**
  In `Extensions/ApplicationBuilderExtensions.cs`, delete the `app.Use` middleware at lines 95-117 that hardcodes `user = "admin"` if headers are empty.

- [ ] **Step 2: Add Authorization attributes to Controllers**
  Add `[Authorize(Policy = "AdminPolicy")]` to:
  - `ClientsController.cs`
  - `PermissionsController.cs`
  - `ProvidersController.cs`
  - `AppKeysController.cs`
  - Gated action `RegisterClient` in `AuthorizationController.cs`

- [ ] **Step 3: Update AppKeysController username derivation**
  In `AppKeysController.cs`:
  - Update `GetAuthenticatedUser()` to use `User.Identity?.Name` or check `HttpContext.Items["AuthenticatedUser"]`.
  - Remove the fallback to `"admin"` string on line 37.
  - Update `IsAdmin()` to use `User.IsInRole("Administrator")`.

---

### Task 4: Secure Minimal APIs

**Files:**
- Modify: `Extensions/ApplicationBuilderExtensions.cs`

- [ ] **Step 1: Enforce AdminPolicy on all `/api/*` endpoints**
  In `Extensions/ApplicationBuilderExtensions.cs`, append `.RequireAuthorization("AdminPolicy")` to all minimal APIs matching `/api/*`, except:
  - `/api/me` (allow anonymous, but read `User.Identity?.Name` if logged in)

- [ ] **Step 2: Compile and run local test suite**
  Run: `dotnet build McpRouter.slnx --configuration Release`
  Run: `dotnet test McpRouter.slnx`
  Ensure everything builds cleanly and tests pass.

- [ ] **Step 3: Commit and run the version bump commit wrapper**
  Run: `./commit.sh "feat(security): implement OidcHeader auth handler and secure control-plane endpoints"`
  Expected: Version bumped to `3.2.0` (minor bump due to new auth framework features).
