# Unified Hybrid OIDC and Active Directory Admin Authorization Design

**Author**: Antigravity  
**Date**: 2026-08-15  
**Target Version**: v4.16.0  
**Status**: Approved (Draft Spec)  

---

## 1. Problem Statement & Context

In `v4.5.5` (commit `959dee4`), the admin policy (`AdminPolicy`) and administrative decision point (`SecurityValidationHelper.IsAdmin`) were changed to strictly require an Active Directory Windows Security Identifier (`Admin:GroupSid`, e.g. `S-1-5-32-544`).

While this satisfied strict Windows Active Directory environments, it caused a severe regression for OpenID Connect (OIDC) and reverse-proxy SSO providers (PocketID, Authentik, Keycloak, Authelia, etc.):
1. **OIDC Standards**: OIDC and reverse proxies do not issue Windows SIDs; they transmit RFC 7519 claims such as `Remote-User: steve` and string group names `Remote-Groups: full_admin,house_member`.
2. **403 Forbidden**: Because standard OIDC users have an empty `AllSids` collection, `OidcHeaderAuthenticationHandler` did not assign administrative claims, and `AdminPolicy` rejected all requests with `403 Forbidden` to `/api/permissions/*`, `/api/servers`, `/api/appkeys`, and `/api/clients`.

---

## 2. Design Goals & Invariants

1. **Dual Support**: Seamlessly authenticate and authorize administrators via Active Directory SIDs (`Admin:GroupSid`) **and** OIDC / Reverse Proxy group names (`Admin:GroupName`, `Admin:Groups`).
2. **Reverse Proxy Security**: Maintain strict header stripping (`TrustedProxyHelper.StripUntrustedHeaders`) for requests originating from untrusted IPs / CIDRs.
3. **Database GroupMappings Support**: Allow dynamic mapping of external groups (e.g. `PocketID_Admins`) to internal administrator groups/SIDs in the `GroupMappings` database table.
4. **Fail-Closed Default**: Principals lacking the configured admin SID, admin group names, or database-mapped admin groups MUST be denied access (403/401).

---

## 3. Configuration Specification

The `Admin` configuration block in `appsettings.json` and environment variables is updated to support both SIDs and Group Names:

```json
{
  "Admin": {
    "GroupSid": "S-1-5-32-544",
    "GroupName": "full_admin",
    "Groups": [
      "full_admin",
      "Administrators",
      "Domain Admins"
    ]
  }
}
```

### Environment Variable Equivalents:
* `Admin__GroupSid="S-1-5-32-544"`
* `Admin__GroupName="full_admin"`
* `Admin__Groups__0="full_admin"`
* `Admin__Groups__1="Administrators"`

---

## 4. Architectural Components & Flow

### 4.1 Evaluation Architecture

```
                                  [ Incoming HTTP Request ]
                                              │
                                              ▼
                             [ TrustedProxyHelper.IsTrustedProxy? ]
                                   ├── No ──► [ Strip Untrusted Headers -> Identity = guest ] ──► 403 Forbidden
                                   │
                                  Yes
                                   │
                                   ▼
                      [ Resolve UserIdentityContext ]
                     (Username, GroupNames, AllSids)
                                   │
                                   ▼
                   [ SecurityValidationHelper.IsAdmin ]
                                   │
       ┌───────────────────────────┼───────────────────────────┐
       ▼                           ▼                           ▼
[ AllSids contains        [ GroupNames contains        [ GroupMappings
  Admin:GroupSid? ]         Admin:GroupName /            ExternalId ->
                            Admin:Groups? ]              Admin Group/SID? ]
       │                           │                           │
       └───────────────────────────┼───────────────────────────┘
                                   │
                       Any Match? ─┼─ Yes ──► [ Grant Admin Access: Claim("Sid", adminSid), Role("Administrator") ]
                                   │
                                   No ─────► [ Deny Admin Access: 403 Forbidden ]
```

### 4.2 Detailed Component Modifications

#### A. `SecurityValidationHelper.cs`
Update `IsAdmin` to evaluate both SIDs and configured Group Names:
```csharp
public static bool IsAdmin(UserIdentityContext? identity, IConfiguration? config, IEnumerable<string>? mappedGroups = null)
{
    if (identity == null || identity.Username == "guest" || identity.Username == "anonymous") return false;

    var adminGroupSid = config?["Admin:GroupSid"] ?? "S-1-5-32-544";
    if (identity.AllSids.Contains(adminGroupSid, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    var adminGroupName = config?["Admin:GroupName"] ?? "full_admin";
    var adminGroups = config?.GetSection("Admin:Groups").Get<string[]>() ?? new[] { adminGroupName, "Administrator", "Administrators" };

    if (identity.GroupNames.Any(g => adminGroups.Contains(g, StringComparer.OrdinalIgnoreCase)))
    {
        return true;
    }

    if (mappedGroups != null && mappedGroups.Any(mg => 
        adminGroups.Contains(mg, StringComparer.OrdinalIgnoreCase) || 
        string.Equals(mg, adminGroupSid, StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    return false;
}
```

#### B. `OidcHeaderAuthenticationHandler.cs`
In `HandleAuthenticateAsync`:
```csharp
if (SecurityValidationHelper.IsAdmin(identityContext, config))
{
    claims.Add(new Claim("Sid", adminGroupSid));
    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
}
```

#### C. `OpenIddictExtensions.cs` (`AdminPolicy`)
Ensure `AdminPolicy` assertion recognizes `Claim("Sid", adminSid)` or role `Administrator` or matching role claims:
```csharp
options.AddPolicy("AdminPolicy", policy =>
{
    policy.RequireAuthenticatedUser()
          .RequireAssertion(ctx =>
          {
              var httpContext = ctx.Resource as Microsoft.AspNetCore.Http.HttpContext;
              var cfg = httpContext?.RequestServices?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
              var adminSid = cfg?["Admin:GroupSid"] ?? "S-1-5-32-544";
              var adminGroupName = cfg?["Admin:GroupName"] ?? "full_admin";
              var adminGroups = cfg?.GetSection("Admin:Groups").Get<string[]>() ?? new[] { adminGroupName, "Administrator", "Administrators" };

              return ctx.User.HasClaim("Sid", adminSid) ||
                     ctx.User.IsInRole("Administrator") ||
                     ctx.User.Claims.Any(c => c.Type == ClaimTypes.Role && adminGroups.Contains(c.Value, StringComparer.OrdinalIgnoreCase));
          })
          .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, "AppKey", "OidcHeader");
});
```

---

## 5. Traceability & Requirements Updates

* **`AUTH-01`**:
  * *Old Definition*: "AdminPolicy must require explicit Admin SID claim and reject role-only principals"
  * *New Definition*: "AdminPolicy authorizes administrative access via configured Admin SID or validated Admin Group Name (e.g., full_admin), rejecting unauthenticated or unauthorized principals"
* **Negative Guardrail**: Principals lacking both the admin SID and admin group names MUST be denied access by `AdminPolicy`.

---

## 6. Verification Plan

### Automated Tests (`McpRouter.Tests`)
1. `AdminPolicy_Allows_Principal_With_AdminSid` (Validates AD SID authorization).
2. `AdminPolicy_Allows_Principal_With_AdminGroupName` (Validates PocketID / OIDC `full_admin` authorization).
3. `AdminPolicy_Allows_Principal_With_ConfiguredAdminGroups` (Validates custom `Admin:Groups` array).
4. `AdminPolicy_Denies_StandardUser_WithoutAdminSidOrGroup` (Validates fail-closed guardrail for regular users).
5. `SecurityValidationHelper_IsAdmin_EvaluatesBothSidsAndGroups` (Unit test for helper logic).
6. Full Solution Tests: `dotnet test McpRouter.slnx`.
7. Catalog Drift Check: `dotnet run --project scripts/CatalogGenerator -- --verify-only`.
