---
name: mcp-router-admin
description: Use when configuring, managing, provisioning, or automating CSharp-MCP-Router settings, authentication providers (Authentik, Keycloak, Entra ID, Active Directory, Cloudflare Access), secret providers (HashiCorp Vault, AES Master Key, DPAPI), access policies, group mappings, backend servers, or client keys via the Admin MCP Server (/admin/sse).
---

# Universal Admin MCP Automation Skill (`mcp-router-admin`)

## Overview

**CSharp-MCP-Router** includes a built-in, encrypted database-backed **Admin MCP Server** (`/admin/sse`) exposing 10 consolidated tools. This skill equips autonomous AI coding agents and DevOps engineers to configure, manage, and verify any gateway deployment from a blank slate without manual UI interaction.

---

## Blank-Slate Safe Defaults

When `CSharp-MCP-Router` starts in a new environment, it initializes with secure out-of-the-box defaults:

| Component | Default Configuration | Notes |
| :--- | :--- | :--- |
| **Database** | SQLite (`./data/mcp_router.db`) | Zero external database dependencies required. Auto-seeded on startup. |
| **Master Key** | `ROUTER_MASTER_KEY` (AES-256-GCM) | Encrypts all sensitive provider credentials and keys at rest in the DB. |
| **Default Admin Key** | `mcp-global-admin-default-cli-key-99` | Scoped to `["all"]` for user `admin`. Seeded automatically in the database. |
| **Network Trust** | `127.0.0.1, ::1` (Loopback) | Configurable via `Admin:StandaloneAllowedNetworks` for LAN/CIDR subnets. |
| **Admin Endpoint** | `http://<host>:8080/admin/sse` | MCP SSE transport for administrative JSON-RPC tool calling. |

---

## Workflow: 7-Phase Autonomous Administration

```
                   [Connect to /admin/sse]
                    (Bearer Default Admin Key)
                              │
                              ▼
                 Phase 1: Gateway Diagnostics
                 (manage_system: diagnostics)
                              │
                              ▼
                Phase 2: Secret Provider Setup
                 (manage_providers: save_secret)
                  ├── HashiCorp Vault KV v2 (test_vault)
                  └── Built-in AES-256-GCM Master Key
                              │
                              ▼
                Phase 3: Auth Provider Setup
                 (manage_providers: save_auth)
                  ├── Authentik / Authelia / Forward-Auth
                  ├── Keycloak / OIDC Headers
                  ├── Microsoft Entra ID (Azure AD)
                  ├── Active Directory LDAPS (test_ldap)
                  └── Standalone Mode (Local Keys)
                              │
                              ▼
                Phase 4: RBAC & Group Mappings
                  ├── manage_group_mappings (SSO -> Internal)
                  └── manage_policies (Allow / Deny Rules)
                              │
                              ▼
                Phase 5: Embeddings & Search
                 (manage_settings: update)
                  ├── OpenAI / Azure OpenAI / Ollama
                  └── FastEmbed / ONNX Local Embeddings
                              │
                              ▼
                Phase 6: Backend Servers & Clients
                  ├── manage_servers (Add / Reconnect)
                  ├── manage_appkeys (Create Client Keys)
                  └── manage_clients (Dynamic OAuth)
                              │
                              ▼
                Phase 7: End-to-End Verification
                  ├── test_tool_call (Dispatch tool)
                  └── manage_system (query_audit / get_logs)
```

---

## Phase 1: Gateway Connection & Diagnostics

### 1.1 Connect to Admin MCP Server
Configure your client with the gateway's `/admin/sse` endpoint and the admin bearer token:
```json
{
  "mcpServers": {
    "mcp-router-admin": {
      "url": "http://localhost:8080/admin/sse",
      "headers": {
        "Authorization": "Bearer mcp-global-admin-default-cli-key-99"
      }
    }
  }
}
```

### 1.2 Inspect Gateway Status
Call `manage_system`:
```json
{
  "tool": "manage_system",
  "arguments": {
    "action": "diagnostics"
  }
}
```
Inspect current active sessions, OS version, process uptime, and memory usage.

---

## Phase 2: Secret Provider Configuration

Configure where backend API keys, tokens, and credentials are encrypted and stored.

### Option A: Built-in Master Key (AES-256-GCM / Default)
No extra provider needed. All backend credentials stored via `manage_servers` are automatically encrypted at rest using `ROUTER_MASTER_KEY`.

### Option B: HashiCorp Vault KV v2 (Token Auth)
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "save_secret",
    "providerName": "HashiCorpVault",
    "displayName": "Enterprise Vault KV",
    "isEnabled": true,
    "configJson": "{\"address\":\"https://vault.internal.corp:8200\",\"authMethod\":\"token\",\"token\":\"s.yourVaultToken\",\"mountPath\":\"secret\"}"
  }
}
```

### Option C: HashiCorp Vault KV v2 (AppRole Auth)
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "save_secret",
    "providerName": "HashiCorpVault",
    "displayName": "Production AppRole Vault",
    "isEnabled": true,
    "configJson": "{\"address\":\"https://vault.internal.corp:8200\",\"authMethod\":\"approle\",\"roleId\":\"11111111-2222-3333-4444-555555555555\",\"secretId\":\"66666666-7777-8888-9999-000000000000\",\"mountPath\":\"secret\"}"
  }
}
```

### 2.1 Test Vault Connection
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "test_vault",
    "address": "https://vault.internal.corp:8200",
    "authMethod": "approle",
    "roleId": "11111111-2222-3333-4444-555555555555",
    "secretId": "66666666-7777-8888-9999-000000000000"
  }
}
```

---

## Phase 3: Authentication Provider Configuration

Configure single sign-on, reverse proxy forward-auth, or enterprise directory integration.

### Option A: Authentik / Authelia / Forward-Auth (Reverse Proxy Headers)
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "save_auth",
    "providerName": "HeaderAuth",
    "displayName": "Authentik Forward-Auth",
    "userHeader": "Remote-User",
    "groupsHeader": "Remote-Groups",
    "isEnabled": true,
    "configJson": "{\"trustedProxies\":[\"127.0.0.1\",\"10.0.0.0/8\",\"172.16.0.0/12\",\"192.168.0.0/16\"]}"
  }
}
```

### Option B: Keycloak / OIDC SSO
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "save_auth",
    "providerName": "Keycloak",
    "displayName": "Corporate Keycloak Realm",
    "userHeader": "X-Forwarded-User",
    "groupsHeader": "X-Forwarded-Groups",
    "isEnabled": true,
    "configJson": "{\"authority\":\"https://keycloak.internal.corp/realms/master\",\"clientId\":\"mcp-router\",\"requireHttps\":true}"
  }
}
```

### Option C: Microsoft Entra ID (Azure AD)
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "save_auth",
    "providerName": "EntraID",
    "displayName": "Microsoft Entra ID",
    "userHeader": "Remote-User",
    "groupsHeader": "Remote-Groups",
    "isEnabled": true,
    "configJson": "{\"tenantId\":\"00000000-0000-0000-0000-000000000000\",\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"groupClaim\":\"groups\"}"
  }
}
```

### Option D: Active Directory / LDAP (LDAPS Port 636)
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "save_auth",
    "providerName": "ActiveDirectory",
    "displayName": "Corporate Active Directory",
    "isEnabled": true,
    "configJson": "{\"server\":\"dc01.internal.corp\",\"port\":636,\"useSsl\":true,\"domain\":\"INTERNAL\",\"baseDn\":\"DC=internal,DC=corp\",\"bindDn\":\"CN=svc-mcp,OU=ServiceAccounts,DC=internal,DC=corp\",\"bindPassword\":\"StrongPassword123!\"}"
  }
}
```

### 3.1 Test LDAP Connection
```json
{
  "tool": "manage_providers",
  "arguments": {
    "action": "test_ldap",
    "server": "dc01.internal.corp",
    "port": 636,
    "useSsl": true,
    "bindDn": "CN=svc-mcp,OU=ServiceAccounts,DC=internal,DC=corp",
    "bindPassword": "StrongPassword123!"
  }
}
```

---

## Phase 4: RBAC, Group Mappings & Access Policies

### 4.1 Create Group Mappings
Map external SSO roles or Active Directory SIDs to internal router roles:
```json
{
  "tool": "manage_group_mappings",
  "arguments": {
    "action": "save",
    "externalId": "S-1-5-21-1234567890-123456789-123456789-512",
    "internalGroup": "full_admin"
  }
}
```

### 4.2 Create Target Access Policies
Restrict backend MCP servers or tools to specific roles:
```json
{
  "tool": "manage_policies",
  "arguments": {
    "action": "save",
    "targetId": "github",
    "requiredGroup": "developer",
    "isAllowed": true
  }
}
```

---

## Phase 5: Dynamic Embeddings & Gateway Settings

Configure semantic tool search embeddings (OpenAI, Azure, Ollama, ONNX):

```json
{
  "tool": "manage_settings",
  "arguments": {
    "action": "update",
    "dashboardTitle": "Enterprise MCP Gateway",
    "embeddingProvider": "OpenAI",
    "embeddingApiUrl": "https://api.openai.com/v1",
    "embeddingApiKey": "sk-proj-...",
    "embeddingApiModel": "text-embedding-3-small",
    "globalMaxKeys": 250,
    "userMaxKeys": 20
  }
}
```

For local Ollama embeddings:
```json
{
  "tool": "manage_settings",
  "arguments": {
    "action": "update",
    "embeddingProvider": "Ollama",
    "embeddingApiUrl": "http://ollama:11434/api/embeddings",
    "embeddingApiModel": "nomic-embed-text"
  }
}
```

---

## Phase 6: Backend Servers & Client AppKeys

### 6.1 Register Backend MCP Server
```json
{
  "tool": "manage_servers",
  "arguments": {
    "action": "create",
    "id": "docker",
    "displayName": "Docker Host Engine",
    "url": "http://docker-mcp:8000/sse",
    "type": "sse",
    "enabled": true,
    "hidden": false,
    "categories": ["infrastructure", "devops"]
  }
}
```

### 6.2 Issue Client AppKey for Developer
```json
{
  "tool": "manage_appkeys",
  "arguments": {
    "action": "create",
    "name": "Dev User Key",
    "username": "steve",
    "scopes": ["all"],
    "expiresInDays": 90
  }
}
```

---

## Phase 7: Verification & Diagnostics

### 7.1 Test Backend Tool Dispatch
Execute a tool directly through the gateway to confirm routing:
```json
{
  "tool": "test_tool_call",
  "arguments": {
    "serverId": "docker",
    "toolName": "list_containers",
    "arguments": {}
  }
}
```

### 7.2 Inspect System Audit Logs
```json
{
  "tool": "manage_system",
  "arguments": {
    "action": "query_audit",
    "take": 20
  }
}
```

---

## Tool Reference Matrix

| Tool Name | Key Actions | Description |
| :--- | :--- | :--- |
| `manage_servers` | `list`, `get`, `create`, `update`, `delete`, `toggle`, `reconnect`, `reconnect_all` | Manages backend MCP servers and connection status. |
| `manage_appkeys` | `list`, `get_limits`, `create`, `revoke` | Issues and audits developer/agent API keys. |
| `manage_clients` | `list`, `register`, `delete` | Manages dynamic OAuth2 client credentials. |
| `manage_policies` | `list`, `save`, `delete` | Configures fine-grained RBAC access policies. |
| `manage_group_mappings` | `list`, `save`, `delete` | Maps external SSO groups/SIDs to internal roles. |
| `manage_providers` | `list`, `save_secret`, `test_vault`, `save_auth`, `test_ldap` | Configures auth & secret providers with live connection tests. |
| `manage_settings` | `get`, `update` | Controls branding, quotas, and semantic embedding providers. |
| `manage_custom_files` | `list`, `get`, `save`, `delete` | Manages virtual prompt and resource JSON files. |
| `manage_system` | `diagnostics`, `get_logs`, `clear_logs`, `query_audit` | Gateway health metrics, logs, and security audit trail. |
| `test_tool_call` | *(default)* | Sends a live test payload to any backend server. |
