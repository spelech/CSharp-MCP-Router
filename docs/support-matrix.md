# 🧩 Deployment & Authentication Support Matrix

This matrix outlines the supported combinations of hosting environments, authentication providers, and downstream delegation methods for Model Context Gateway (MCG).

## 🏗️ Hosting & Authentication Matrix

| Hosting Environment | Identity / Auth Mechanism | AppKey Support | Downstream Identity Delegation | Notes & Limitations |
| :--- | :--- | :--- | :--- | :--- |
| **Linux Container (Docker)** | **Reverse Proxy Headers** (OIDC, SAML, etc.) | ✅ Supported | **OAuth2 On-Behalf-Of JWT** or **Header Propagation** (`X-Forwarded-User`) | **Requires** configuring `Oidc:TrustedProxies`. Kerberos/NTLM impersonation (`S4U2Proxy`) is **not supported** in Linux containers. |
| **Windows Native (IIS)** | **Active Directory / Windows Auth** | ✅ Supported | **Kerberos/NTLM Impersonation** (`S4U2Proxy`) or **Header Propagation** | Application pool must run as a domain account with constrained delegation rights if using `S4U2Proxy`. |
| **Standalone (Kestrel)** | **AppKey Only** (Machine-to-Machine) | ✅ Supported | **None** (Executes in Router context) | Typically used for automated agents or internal microservices where user context is not applicable. |
| **Any Environment** | **Dynamic Auth Pass-Through** | ✅ Supported | **Direct Target Auth** (`X-Target-Auth`) | Router intercepts 401s from tools and prompts the client/IDE for credentials dynamically. |

---

## 🔑 Authentication Context Limitations

Depending on how a client authenticates to Model Context Gateway (MCG), different capabilities and downstream restrictions apply.

### 1. Reverse Proxy (OIDC / Header Auth)
* **Bound Context:** Human User.
* **Capabilities:** 
  * Provides granular user identity to the router.
  * Can be mapped to internal Admin roles via `manage_group_mappings`.
  * Fully supports auditing user activity.
* **Limitations:** 
  * Only works if the proxy IP is explicitly whitelisted in `Oidc:TrustedProxies`.
  * If the proxy is bypassed, headers are stripped and access degrades to Guest/Anonymous.

### 2. AppKey Authentication
* **Bound Context:** Machine / Autonomous Agent.
* **Capabilities:**
  * Highly granular scoping (`server:*`, `category:*`, `tool:*`).
  * Can act on behalf of a specific owner (via `OwnerSid`).
* **Limitations:**
  * Bypasses reverse proxy SSO headers.
  * Does not seamlessly map to downstream interactive SSO flows without exchanging for an On-Behalf-Of token.

### 3. Windows / Active Directory (IIS)
* **Bound Context:** Enterprise Domain User.
* **Capabilities:**
  * True Zero-Trust without external identity providers.
  * Router natively parses `WindowsIdentity` for rapid access evaluation.
* **Limitations:**
  * Tied to Windows Server hosting. Cannot easily port to Linux-based Kubernetes deployments without complex LDAP sidecars or gMSA setups.

---

## 🛡️ Downstream Delegation Support

When the Router forwards a request to a remote MCP Server, it must authenticate itself. The matrix below shows which outbound delegation strategies work based on your inbound authentication.

| Inbound Auth Method | Outbound: Header Propagation | Outbound: OAuth2 On-Behalf-Of | Outbound: Kerberos Impersonation |
| :--- | :---: | :---: | :---: |
| **Proxy SSO Header** | ✅ Supported | ✅ Supported | ❌ Not Supported |
| **AppKey** | ⚠️ AppKey Owner Context | ❌ Not Supported (App is not a user) | ❌ Not Supported |
| **Windows Auth (AD)** | ✅ Supported | ❌ Not Supported (No JWT) | ✅ Supported (Windows Only) |

> 💡 **Recommendation:** For modern containerized deployments, utilize a **Reverse Proxy (OIDC)** and rely on **Header Propagation** (`X-Forwarded-User`) or **OAuth2 Token Exchange** for backend security. Avoid Windows Auth unless migrating legacy domain-bound enterprise systems.
