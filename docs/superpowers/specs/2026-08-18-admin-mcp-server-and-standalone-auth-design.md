# Admin MCP Server & Standalone Hybrid Authorization Design Specification

**Author**: Antigravity  
**Date**: 2026-08-18  
**Target Version**: v4.19.0  
**Status**: Approved (Design Spec)

---

## 1. Problem Statement & Executive Summary

The MCP Router gateway provides administrative REST API endpoints and a web dashboard for configuring backend servers, access policies, group mappings, credentials, secret/auth providers, custom prompt/resource files, and viewing audit logs.

However, external agentic platforms and autonomous LLM agents (Claude Desktop, Cursor, Cline, Windsurf, LangChain, AutoGen, etc.) currently lack a direct, native Model Context Protocol (MCP) interface to administer the router.

Furthermore, authorization in environments without an external identity provider (Active Directory / OIDC SSO) must accommodate standalone/personal and private network deployments gracefully, allowing local loopback and user-configured subnets (e.g. LAN, Docker network, or `0.0.0.0/0`) administrative access without compromising enterprise deployments.

This specification introduces:
1. **Admin MCP Server (`AdminMcpServer`)**: An in-process virtual MCP server with 10 consolidated entity tools covering 100% of dashboard management flows.
2. **Dedicated Admin Endpoints**: `/admin` (and `/admin/sse`, `/router-admin`) supporting MCP `2026-07-28` and `2024-11-05` protocol negotiation.
3. **Hybrid Standalone Authorization**: Configurable network access (`Admin:StandaloneAllowedNetworks`) with loopback defaults, combined with strict AD/OIDC group resolution and Admin AppKey authentication.
4. **Comprehensive Audit Logging**: Native recording of all administrative MCP tool calls to the persistent `AuditLogs` store.

---

## 2. Architecture & Components

```
       [ Agentic Platform / IDE / LLM ]
         (Claude, Cursor, Cline, etc.)
                       │
                       │ Authorization: Bearer <AdminAppKey>
                       │ OR Standalone LAN/Loopback Network
                       ▼
             ┌───────────────────┐
             │   /admin (/sse)   │
             │   /router-admin   │
             └─────────┬─────────┘
                       │ (AdminPolicy Verified)
                       ▼
         ┌───────────────────────────┐
         │    AdminMcpServer         │
         │  (In-Process Virtual MCP) │
         └─────────────┬─────────────┘
                       │
      ┌────────────────┼────────────────┬────────────────┐
      ▼                ▼                ▼                ▼
[Repositories]  [HealthCheckSvc] [DynamicEmbeddings] [AuditLogger]
(Servers, Keys,  (Probe, Reconn) (Branding, Model)  (Audit Trails)
 Policies, Prov)
```

### 2.1 Virtual Admin MCP Server (`Core/Routing/AdminMcpServer.cs`)
* An in-process virtual server registered in DI.
* Handles JSON-RPC 2.0 requests for `initialize`, `notifications/initialized`, `ping`, `tools/list`, and `tools/call`.
* Protocol version: Default `2026-07-28`, negotiating down to `2024-11-05` when requested by client.

### 2.2 Endpoint Mapping (`Components/Capabilities/AdminEndpoints.cs`)
* Maps the following endpoints:
  * `GET/POST /admin` & `GET/POST /admin/sse`: MCP Server-Sent Events transport.
  * `POST /admin/message`: Client-to-server JSON-RPC endpoint.
  * Integration into `/{targetServerId}` proxying for `targetServerId == "router-admin"` and `targetServerId == "admin"`.

### 2.3 Standalone & Hybrid Authorization (`Components/Authorization/SecurityValidationHelper.cs`)
* **When External IDP (AD or OIDC) is Configured**:
  * AD SIDs: Evaluates `Admin:GroupSid` (default `S-1-5-32-544`).
  * OIDC / Reverse Proxy Groups: Evaluates `Admin:Groups` (default `["full_admin", "Administrator", "Administrators"]` or `Admin:GroupName`).
  * Dynamic `GroupMappings`: Maps external group IDs to internal roles.
  * AppKeys: Keys with `admin` or `all` scope owned by an admin are granted role `Administrator`.
* **When NO External IDP is Configured (Standalone Mode)**:
  * Check client IP address against `Admin:StandaloneAllowedNetworks` (configured CIDRs or exact IPs).
  * Default allowed networks: Loopback (`127.0.0.1`, `::1`).
  * Configurable in `appsettings.json` / environment variables to LAN CIDRs (e.g. `10.0.0.0/8`, `192.168.0.0/16`) or `0.0.0.0/0` for central deployments.
  * Startup info log indicates standalone mode and active network filters.
  * External non-matching IPs require a valid Admin AppKey.

---

## 3. Consolidated Admin Tool Definitions

| Tool Name | Actions | Description |
| :--- | :--- | :--- |
| `manage_servers` | `list`, `get`, `create`, `update`, `delete`, `toggle`, `reconnect`, `reconnect_all` | Manage backend MCP servers, connection parameters, status, and health checks. |
| `manage_appkeys` | `list`, `get_limits`, `create`, `revoke` | Manage client API/AppKeys, key limits, expiration, and scopes. |
| `manage_clients` | `list`, `register`, `delete` | Manage dynamic OAuth clients and credentials. |
| `manage_policies` | `list`, `save`, `delete` | Manage role-based access control (RBAC) authorization policies. |
| `manage_group_mappings` | `list`, `save`, `delete` | Map external IDP SSO groups to internal administrator/user roles or SIDs. |
| `manage_providers` | `list`, `save_secret`, `test_vault`, `save_auth`, `test_ldap` | Manage and test secret providers (Vault, WinReg, Env) and auth providers (AD, OIDC). |
| `manage_settings` | `get`, `update` | Manage dashboard branding, title, icon, and semantic embedding providers/models. |
| `manage_custom_files` | `list`, `get`, `save`, `delete` | Manage user-configured local prompt and resource files in `data/` directories. |
| `manage_system` | `diagnostics`, `get_logs`, `clear_logs`, `query_audit` | Inspect server diagnostics, memory logs, clear logs, and query audit trails. |
| `test_tool_call` | `execute` | Test execution of tools on downstream MCP servers via the testbench engine. |

---

## 4. Configuration Specification

```json
{
  "Admin": {
    "GroupSid": "S-1-5-32-544",
    "GroupName": "full_admin",
    "Groups": [
      "full_admin",
      "Administrator",
      "Administrators"
    ],
    "StandaloneAllowedNetworks": [
      "127.0.0.1",
      "::1"
    ]
  }
}
```

Environment variable equivalents:
* `ADMIN__STANDALONE_ALLOWED_NETWORKS__0="127.0.0.1"`
* `ADMIN__STANDALONE_ALLOWED_NETWORKS__1="::1"`
* `ADMIN__STANDALONE_ALLOWED_NETWORKS__2="10.0.0.0/8"` (for central network access)

---

## 5. Verification & Testing Matrix

* **C# Tests (`McpRouter.Tests`)**:
  * Protocol Handshake & Version Negotiation (`initialize` with `2026-07-28` and `2024-11-05`).
  * Tools Discovery (`tools/list` returns all 10 tools with schema validation).
  * Tools Execution (`tools/call` for each tool and action).
  * Auth Enforcement (Admin AppKeys, Standalone LAN/Loopback, and rejection of unauthorized callers).
  * Audit Logging (Verification that `AuditLogs` records tool name, parameters, caller, and success flag).
* **Catalog Regeneration**:
  * Regenerate and verify `docs/software-requirements-and-test-catalog.md` and `docs/requirements-catalog.json`.
