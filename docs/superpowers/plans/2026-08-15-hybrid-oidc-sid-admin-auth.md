# Unified Hybrid OIDC and Active Directory Admin Authorization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable seamless administrative authorization for both OpenID Connect (OIDC / PocketID / Reverse Proxy) group names and Windows Active Directory SIDs while maintaining strict reverse-proxy security invariants.

**Architecture:** Extend `SecurityValidationHelper.IsAdmin`, `OidcHeaderAuthenticationHandler`, and `OpenIddictExtensions.AdminPolicy` to evaluate both `Admin:GroupSid` (e.g., `S-1-5-32-544`) and `Admin:GroupName`/`Admin:Groups` (e.g., `full_admin`), assigning `Claim("Sid", adminSid)` and `Claim(ClaimTypes.Role, "Administrator")` to authorized principals.

**Tech Stack:** C# .NET 10, ASP.NET Core Authorization & Authentication, OpenIddict, xUnit, Roslyn CatalogGenerator.

## Global Constraints

- Target Version: `v4.16.0`
- Mandatory Synchronized Version Bump Files: `mcp-router.csproj`, `frontend/package.json`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, `README.md`
- Mandatory Test Requirement Annotations: `[Requirement("AUTH-01", "AUTH", RequirementType.Positive, "...")]`
- Zero-drift catalog check: `dotnet run --project scripts/CatalogGenerator -- --verify-only`

---

### Task 1: Extend `SecurityValidationHelper.IsAdmin` for SIDs & Group Names

**Files:**
- Modify: `/containers/dev/csharp-mcp-router/Components/Authorization/SecurityValidationHelper.cs:304-313`
- Modify: `/containers/dev/csharp-mcp-router/McpRouter.Tests/IdentityProviderTests.cs:220-270`

**Interfaces:**
- Consumes: `UserIdentityContext` (`AllSids`, `GroupNames`, `Username`), `IConfiguration` (`Admin:GroupSid`, `Admin:GroupName`, `Admin:Groups`)
- Produces: `bool SecurityValidationHelper.IsAdmin(UserIdentityContext? identity, IConfiguration? config, IEnumerable<string>? mappedGroups = null)`

- [ ] **Step 1: Write unit tests in `IdentityProviderTests.cs` testing SID, group name, and custom groups**

```csharp
[Fact]
[Requirement("AUTH-01", "SecurityValidationHelper authorizes principals via Admin Group Name", Type = RequirementType.Positive, Category = "AUTH")]
public void SecurityValidationHelper_IsAdmin_AllowsAdminGroupName()
{
    var configDict = new Dictionary<string, string?>
    {
        { "Admin:GroupName", "full_admin" }
    };
    var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

    var identity = new UserIdentityContext("steve", "HeaderAuth", new List<string> { "full_admin", "house_member" });
    Assert.True(McpRouter.Components.Authorization.SecurityValidationHelper.IsAdmin(identity, config));
}

[Fact]
[Requirement("AUTH-01", "SecurityValidationHelper rejects non-admin groups and guest identities", Type = RequirementType.Negative, Category = "AUTH")]
public void SecurityValidationHelper_IsAdmin_RejectsNonAdminGroups()
{
    var configDict = new Dictionary<string, string?>
    {
        { "Admin:GroupName", "full_admin" }
    };
    var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

    var identity = new UserIdentityContext("alice", "HeaderAuth", new List<string> { "house_member" });
    Assert.False(McpRouter.Components.Authorization.SecurityValidationHelper.IsAdmin(identity, config));

    var guestIdentity = new UserIdentityContext("guest", "HeaderAuth", new List<string> { "full_admin" });
    Assert.False(McpRouter.Components.Authorization.SecurityValidationHelper.IsAdmin(guestIdentity, config));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test McpRouter.Tests/McpRouter.Tests.csproj --filter "FullyQualifiedName~SecurityValidationHelper_IsAdmin_AllowsAdminGroupName"`
Expected: FAIL (method requires SID only)

- [ ] **Step 3: Update `SecurityValidationHelper.IsAdmin` implementation**

```csharp
public static bool IsAdmin(UserIdentityContext? identity, IConfiguration? config, IEnumerable<string>? mappedGroups = null)
{
    if (identity == null || string.IsNullOrWhiteSpace(identity.Username) || identity.Username.Equals("guest", StringComparison.OrdinalIgnoreCase) || identity.Username.Equals("anonymous", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var adminGroupSid = config?["Admin:GroupSid"] ?? "S-1-5-32-544";
    if (identity.AllSids.Contains(adminGroupSid, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    var configuredAdminGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var singleGroupName = config?["Admin:GroupName"];
    if (!string.IsNullOrWhiteSpace(singleGroupName))
    {
        configuredAdminGroups.Add(singleGroupName.Trim());
    }
    else
    {
        configuredAdminGroups.Add("full_admin");
        configuredAdminGroups.Add("Administrator");
        configuredAdminGroups.Add("Administrators");
    }

    var adminGroupsSection = config?.GetSection("Admin:Groups").Get<string[]>();
    if (adminGroupsSection != null)
    {
        foreach (var g in adminGroupsSection)
        {
            if (!string.IsNullOrWhiteSpace(g)) configuredAdminGroups.Add(g.Trim());
        }
    }

    if (identity.GroupNames.Any(g => configuredAdminGroups.Contains(g)))
    {
        return true;
    }

    if (mappedGroups != null && mappedGroups.Any(mg => configuredAdminGroups.Contains(mg) || string.Equals(mg, adminGroupSid, StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    return false;
}
```

- [ ] **Step 4: Run unit tests to verify pass**

Run: `dotnet test McpRouter.Tests/McpRouter.Tests.csproj --filter "FullyQualifiedName~SecurityValidationHelper_IsAdmin"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Components/Authorization/SecurityValidationHelper.cs McpRouter.Tests/IdentityProviderTests.cs
git commit -m "feat(auth): support group names and SIDs in SecurityValidationHelper.IsAdmin"
```

---

### Task 2: Update `OidcHeaderAuthenticationHandler` and `OpenIddictExtensions.AdminPolicy`

**Files:**
- Modify: `/containers/dev/csharp-mcp-router/Middleware/OidcHeaderAuthenticationHandler.cs:34-73`
- Modify: `/containers/dev/csharp-mcp-router/Extensions/OpenIddictExtensions.cs:35-47`

**Interfaces:**
- Consumes: `SecurityValidationHelper.IsAdmin(identityContext, config)`
- Produces: Claims `Claim("Sid", adminGroupSid)`, `Claim(ClaimTypes.Role, "Administrator")`, `AdminPolicy` authorization check

- [ ] **Step 1: Update `OidcHeaderAuthenticationHandler.cs`**

```csharp
protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
{
    var config = _configuration ?? Context.RequestServices?.GetService<IConfiguration>();
    var identityContext = await _identityProvider.ResolveIdentityAsync(Context);
    if (identityContext == null || identityContext.Username == "guest" || identityContext.Username == "anonymous")
    {
        return AuthenticateResult.NoResult();
    }

    var adminGroupSid = config?["Admin:GroupSid"] ?? "S-1-5-32-544";
    var username = identityContext.Username;

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.NameIdentifier, username)
    };

    if (SecurityValidationHelper.IsAdmin(identityContext, config))
    {
        claims.Add(new Claim("Sid", adminGroupSid));
        claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
    }

    foreach (var group in identityContext.GroupNames)
    {
        claims.Add(new Claim(ClaimTypes.Role, group));
    }

    foreach (var sid in identityContext.AllSids)
    {
        claims.Add(new Claim(ClaimTypes.GroupSid, sid));
    }

    var identity = new ClaimsIdentity(claims, Scheme.Name);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, Scheme.Name);

    return AuthenticateResult.Success(ticket);
}
```

- [ ] **Step 2: Update `OpenIddictExtensions.cs` `AdminPolicy`**

```csharp
options.AddPolicy("AdminPolicy", policy =>
{
    policy.RequireAuthenticatedUser()
          .RequireAssertion(ctx =>
          {
              var httpContext = ctx.Resource as Microsoft.AspNetCore.Http.HttpContext;
              var cfg = httpContext?.RequestServices?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
              var adminSid = cfg?["Admin:GroupSid"] ?? "S-1-5-32-544";
              
              var configuredAdminGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
              var singleGroupName = cfg?["Admin:GroupName"];
              if (!string.IsNullOrWhiteSpace(singleGroupName))
              {
                  configuredAdminGroups.Add(singleGroupName.Trim());
              }
              else
              {
                  configuredAdminGroups.Add("full_admin");
                  configuredAdminGroups.Add("Administrator");
                  configuredAdminGroups.Add("Administrators");
              }

              var adminGroupsSection = cfg?.GetSection("Admin:Groups").Get<string[]>();
              if (adminGroupsSection != null)
              {
                  foreach (var g in adminGroupsSection)
                  {
                      if (!string.IsNullOrWhiteSpace(g)) configuredAdminGroups.Add(g.Trim());
                  }
              }

              return ctx.User.HasClaim("Sid", adminSid) ||
                     ctx.User.IsInRole("Administrator") ||
                     ctx.User.Claims.Any(c => c.Type == ClaimTypes.Role && configuredAdminGroups.Contains(c.Value));
          })
          .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, "AppKey", "OidcHeader");
});
```

- [ ] **Step 3: Run existing authentication tests**

Run: `dotnet test McpRouter.Tests/McpRouter.Tests.csproj --filter "FullyQualifiedName~OidcHeaderAuthenticationHandlerTests|FullyQualifiedName~AdminPolicy"`
Expected: Check results and prepare Task 3 test suite updates.

- [ ] **Step 4: Commit**

```bash
git add Middleware/OidcHeaderAuthenticationHandler.cs Extensions/OpenIddictExtensions.cs
git commit -m "feat(auth): grant admin role and claims for validated admin groups in OidcHeaderAuthenticationHandler"
```

---

### Task 3: Update `AdminPolicy` Test Proofs & Annotations

**Files:**
- Modify: `/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicySidOnlyTests.cs`
- Create: `/containers/dev/csharp-mcp-router/McpRouter.Tests/AdminPolicyHybridAuthTests.cs`

**Interfaces:**
- Annotations: `[Requirement("AUTH-01", "AUTH", RequirementType.Positive, "AdminPolicy authorizes administrative access via configured Admin SID or validated Admin Group Name")]`
- Annotations: `[Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy rejects unauthenticated, non-admin, or untrusted principals")]`

- [ ] **Step 1: Update test file `AdminPolicyHybridAuthTests.cs`**

```csharp
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;
using McpRouter.Extensions;
using McpRouter.Tests.Attributes;

namespace McpRouter.Tests
{
    public class AdminPolicyHybridAuthTests
    {
        [Fact]
        [Requirement("AUTH-01", "AUTH", RequirementType.Positive, "AdminPolicy allows principal with configured Admin Group Name (e.g., full_admin)")]
        public async Task AdminPolicy_Allows_Principal_With_AdminGroupName()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                { "Admin:GroupName", "full_admin" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var httpContext = new DefaultHttpContext();
            var serviceProvider = services.BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;

            var authService = serviceProvider.GetRequiredService<IAuthorizationService>();

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "steve"),
                new Claim(ClaimTypes.Role, "full_admin")
            }, "OidcHeader");
            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, httpContext, "AdminPolicy");
            Assert.True(result.Succeeded);
        }

        [Fact]
        [Requirement("AUTH-01", "AUTH", RequirementType.Positive, "AdminPolicy allows principal with configured Admin SID")]
        public async Task AdminPolicy_Allows_Principal_With_AdminSid()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                { "Admin:GroupSid", "S-1-5-32-544" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var httpContext = new DefaultHttpContext();
            var serviceProvider = services.BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;

            var authService = serviceProvider.GetRequiredService<IAuthorizationService>();

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "ad_admin"),
                new Claim("Sid", "S-1-5-32-544")
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, httpContext, "AdminPolicy");
            Assert.True(result.Succeeded);
        }

        [Fact]
        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy rejects principal with unconfigured regular role without Admin SID or Admin Group")]
        public async Task AdminPolicy_Denies_StandardRole_WithoutAdminSidOrGroup()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                { "Admin:GroupSid", "S-1-5-32-544" },
                { "Admin:GroupName", "full_admin" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var httpContext = new DefaultHttpContext();
            var serviceProvider = services.BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;

            var authService = serviceProvider.GetRequiredService<IAuthorizationService>();

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "regular_user"),
                new Claim(ClaimTypes.Role, "house_member")
            }, "OidcHeader");
            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, httpContext, "AdminPolicy");
            Assert.False(result.Succeeded);
        }
    }
}
```

- [ ] **Step 2: Update `AdminPolicySidOnlyTests.cs` requirements annotations and assertions**

- [ ] **Step 3: Run full tests across solution**

Run: `CI=true dotnet test McpRouter.slnx`
Expected: ALL test suites pass (530+ tests).

- [ ] **Step 4: Commit**

```bash
git add McpRouter.Tests/AdminPolicySidOnlyTests.cs McpRouter.Tests/AdminPolicyHybridAuthTests.cs
git commit -m "test(auth): add hybrid AdminPolicy test proofs for OIDC group names and SIDs"
```

---

### Task 4: Documentation, Release Version Bump to v4.16.0 & Catalog Regeneration

**Files:**
- Modify: `mcp-router.csproj` (`<Version>4.16.0</Version>`)
- Modify: `frontend/package.json` (`"version": "4.16.0"`)
- Modify: `frontend/src/stores/useUserStore.ts` (`version: '4.16.0'`)
- Modify: `CHANGELOG.md` (Add v4.16.0 release row)
- Modify: `README.md` (Update top-5 release table and version badge)
- Modify: `appsettings.Production.json.example`
- Regenerate: `docs/software-requirements-and-test-catalog.md`, `docs/requirements-catalog.json`

- [ ] **Step 1: Update version numbers simultaneously**
- [ ] **Step 2: Update `appsettings.Production.json.example` with `Admin:GroupName` and `Admin:Groups`**
- [ ] **Step 3: Regenerate living requirements catalog**

Run: `dotnet run --project scripts/CatalogGenerator`
Run: `dotnet run --project scripts/CatalogGenerator -- --verify-only`
Expected: Zero drift.

- [ ] **Step 4: Run full verification suite**

Run: `CI=true dotnet test McpRouter.slnx`
Run: `cd frontend && npm run test && npm run build`

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "release: bump version to v4.16.0 and enable hybrid OIDC/SID admin authorization"
```
