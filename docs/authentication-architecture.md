# Authentication & Authorization Architecture

This document outlines the current state of authentication and authorization within the MCP Router Gateway, specifically focusing on the separation of concerns between Active Directory (AD) Security Identifiers (SIDs) and OIDC/Reverse Proxy headers.

## 1. Identity Providers (The "Who")

The router uses a pluggable `IIdentityProvider` system to resolve the current user's identity (`UserIdentityContext`) from an incoming HTTP request.

### Active Directory (`ActiveDirectoryIdentityProvider`)
- **Mechanism:** Integrates with native Windows Authentication or Kerberos/NTLM.
- **Data Extraction:** 
  - Extracts the Windows username.
  - Extracts the user's Primary SID and all associated Group SIDs (e.g., `S-1-5-32-544`).
  - Optionally augments the SID list via direct LDAP queries.
- **Mapping:** SIDs are directly mapped into the `Sids` collection of the `UserIdentityContext`.

### OIDC / Header Proxy (`HeaderIdentityProvider` / `OidcIdentityProvider`)
- **Mechanism:** Trusts an upstream reverse proxy (like PocketID, TinyAuth, Authelia, or Nginx) to authenticate the user and inject HTTP headers.
- **Trust Validation:** Strictly requires the remote IP to be in the `Oidc:TrustedProxies` list (which now supports CIDR ranges). If untrusted, headers are stripped and the user becomes a `guest`.
- **Separation of Concerns:** 
  - **Groups vs SIDs:** The design intentionally separates arbitrary string-based group names (OIDC) from cryptographic/system SIDs (Active Directory).
  - **Group Headers:** Headers like `Remote-Groups` and `sso_groups` are parsed into the `GroupNames` collection.
  - **SID Headers:** To inject a SID via headers, the proxy must use explicit SID headers (e.g., `Remote-User-Sid`, `X-Auth-Request-Sid`). These are parsed into the `Sids` collection.

## 2. Authorization Handlers (The "How")

The `OidcHeaderAuthenticationHandler` takes the `UserIdentityContext` resolved by the providers and builds a standard ASP.NET Core `ClaimsPrincipal`.

- **Role Claims:** Elements in `GroupNames` are added as `ClaimTypes.Role` claims.
- **GroupSid Claims:** Elements in `AllSids` are added as `ClaimTypes.GroupSid` claims.
- **Hybrid Admin Resolution:** If the user's `AllSids` collection contains the designated Admin SID (configured via `Admin:GroupSid`, default `S-1-5-32-544`) **OR** the user's `GroupNames` match any configured admin group names (`Admin:GroupName`, `Admin:Groups`, or default `full_admin`, `Administrator`, `Administrators`), the principal is assigned both:
  - `Claim("Sid", adminSid)`
  - `Claim(ClaimTypes.Role, "Administrator")`

## 3. Policy Enforcement (The "Access")

### `AdminPolicy`
All management plane API endpoints (`/api/*`, including Settings, AppKeys, and Providers) are protected by the `AdminPolicy`.

- **Requirement:** `AdminPolicy` grants access if the principal possesses the `"Sid"` claim matching `Admin:GroupSid`, possesses the `Administrator` role claim, or possesses a role claim matching any configured group in `Admin:GroupName` or `Admin:Groups`.
- **Hybrid Access:** Both Windows Active Directory users (possessing the Admin SID) and upstream OIDC / Reverse Proxy users (belonging to designated admin groups such as `full_admin` or configured in `Admin:GroupName`/`Admin:Groups`) are seamlessly authorized.
- **Fallback Resolution:** Single-line string group headers (e.g. `sso_groups: "full_admin"`) or array-based headers (`Remote-Groups: ["full_admin", "house_member"]`) are parsed into `GroupNames` and mapped into admin roles.

## Summary

The hybrid architecture bridges enterprise Active Directory environments and cloud-native OIDC/Reverse Proxy architectures:
- **Active Directory:** Enterprise environments rely on native Windows authentication and LDAP group SIDs (`Admin:GroupSid`).
- **OIDC / Reverse Proxy:** SSO environments (such as PocketID, TinyAuth, Authelia, Authentik) pass group names (`Admin:GroupName`, `Admin:Groups`), granting authorized administrative access across all endpoints without requiring synthetic SID headers.

