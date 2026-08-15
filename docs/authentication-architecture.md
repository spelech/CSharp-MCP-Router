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
- **The Admin SID Claim:** A special claim of type `"Sid"` is ONLY added if the user's `AllSids` collection contains the designated Admin SID (configured via `Admin:GroupSid`, default `S-1-5-32-544`). 

## 3. Policy Enforcement (The "Access")

### `AdminPolicy`
All management plane API endpoints (`/api/*`, including Settings, AppKeys, and Providers) are protected by the `AdminPolicy`.

- **Requirement:** `AdminPolicy` explicitly requires the principal to possess the `"Sid"` claim matching the `Admin:GroupSid`.
- **Consequence of Separation:** Because of the strict separation between `GroupNames` and `Sids`, an OIDC user providing `Remote-Groups: Administrators` or `Remote-Groups: S-1-5-32-544` will **NOT** be granted access to `/api/*`. The string goes into `GroupNames` (Roles), not `Sids`.
- **To Grant Admin via Proxy:** The upstream proxy MUST send the SID via a designated SID header (e.g., `Remote-User-Sid: S-1-5-32-544`).

## Summary

The current architecture successfully achieves the goal of keeping AD SIDs completely separate from OIDC provider headers. 

If a user relies purely on `Remote-User` and `Remote-Groups` (OIDC), they will receive standard Role claims but will be explicitly denied from the Gateway's internal management APIs (`AdminPolicy`) which are strictly guarded by SID claims.
