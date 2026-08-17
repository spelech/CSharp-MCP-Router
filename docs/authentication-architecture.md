# Authentication & Authorization Architecture

This document outlines authentication and authorization in the MCP Router Gateway, detailing the separation between Active Directory (AD) Security Identifiers (SIDs) and OIDC/Reverse Proxy headers.

## 1. Identity Providers

The router employs an `IIdentityProvider` to resolve the `UserIdentityContext` from incoming HTTP requests.

### Active Directory (`ActiveDirectoryIdentityProvider`)
- **Mechanism:** Utilizes native Windows Authentication or Kerberos/NTLM.
- **Data Extraction:** Extracts the Windows username, Primary SID, and associated Group SIDs (e.g., `S-1-5-32-544`). Optionally augments SIDs via LDAP queries.
- **Mapping:** Maps SIDs to the `UserIdentityContext` `Sids` collection.

### OIDC / Header Proxy (`HeaderIdentityProvider` / `OidcIdentityProvider`)
- **Mechanism:** Processes HTTP headers injected by trusted upstream reverse proxies (e.g., PocketID, TinyAuth, Authelia, Nginx).
- **Trust Validation:** Requires the remote IP to exist in `Oidc:TrustedProxies` (supports CIDR). Untrusted requests strip headers and default to `guest`.
- **Data Parsing:** 
  - **Group Headers:** Parses headers like `Remote-Groups` and `sso_groups` into `GroupNames`. Separates arbitrary OIDC string-based groups from AD SIDs.
  - **SID Headers:** Parses explicit SID headers (e.g., `Remote-User-Sid`, `X-Auth-Request-Sid`) into the `Sids` collection.

## 2. Authorization Handlers

`OidcHeaderAuthenticationHandler` constructs an ASP.NET Core `ClaimsPrincipal` from the `UserIdentityContext`.

- **Role Claims:** Maps `GroupNames` to `ClaimTypes.Role`.
- **GroupSid Claims:** Maps `AllSids` to `ClaimTypes.GroupSid`.
- **Admin Resolution:** If `AllSids` contains the Admin SID (`Admin:GroupSid`, default `S-1-5-32-544`) or `GroupNames` contains an admin group (`Admin:GroupName`, `Admin:Groups`, defaults `full_admin`, `Administrator`, `Administrators`), the principal receives:
  - `Claim("Sid", adminSid)`
  - `Claim(ClaimTypes.Role, "Administrator")`

## 3. Policy Enforcement

### `AdminPolicy`
Secures management plane API endpoints (`/api/*`, including Settings, AppKeys, Providers).

- **Requirement:** Grants access if the principal has a `"Sid"` claim matching `Admin:GroupSid`, the `Administrator` role claim, or a role claim matching groups in `Admin:GroupName` or `Admin:Groups`.
- **Hybrid Access:** Authorizes AD users via Admin SID and OIDC users via designated admin groups.
- **Header Parsing:** Parses single-line (e.g., `sso_groups: "full_admin"`) or array-based headers (`Remote-Groups: ["full_admin", "house_member"]`) into `GroupNames` mapped to admin roles.

## Architecture Summary

This hybrid model supports both enterprise and cloud-native environments:
- **Active Directory:** Uses native Windows authentication and LDAP group SIDs (`Admin:GroupSid`).
- **OIDC / Reverse Proxy:** Uses SSO group names (`Admin:GroupName`, `Admin:Groups`) to authorize administrative access without synthetic SID headers.

