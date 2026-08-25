# Admin MCP Automation & Provider Configuration Guide

This guide provides developers, DevOps engineers, and autonomous AI coding agents with the architectural blueprint, safe defaults, and automation playbooks to configure and provision **Model Context Gateway (MCG)** from a blank slate.

---

## 🚀 1. Architecture & Safe Defaults

When **Model Context Gateway** boots in a fresh environment without existing configuration databases, it automatically provisions a safe, zero-dependency baseline:

```
+---------------------------------------------------------------------------------------+
|                                Model Context Gateway (MCG)                            |
+---------------------------------------------------------------------------------------+
|  Out-of-the-Box Safe Defaults:                                                        |
|  - Database: SQLite (./data/mcg.db auto-created & migrated)                           |
|  - Encryption: AES-256-GCM using MCG_MASTER_KEY                                       |
|  - Network Trust: Loopback only (127.0.0.1, ::1) via Admin:StandaloneAllowedNetworks  |
|  - Admin Key: mcp-adm-prod-bootstrap-token-99 (Owner: admin, Scopes: ["all"])        |
|  - Admin Endpoint: http://<host>:8080/admin/sse (JSON-RPC 2.0)                        |
+---------------------------------------------------------------------------------------+
                                           |
                                           v
+---------------------------------------------------------------------------------------+
|                                Autonomous Automation                                  |
|  - AI Agent Skill: .agents/skills/mcg-admin/SKILL.md                                  |
|  - Non-Interactive Scripts: cURL (Bash), PowerShell (Windows), Python                 |
|  - 10 Consolidated MCP Tools covering 100% of Gateway Admin Operations                |
+---------------------------------------------------------------------------------------+
```

### Safe Defaults Reference Matrix

| Parameter | Default Value | Description |
| :--- | :--- | :--- |
| **`DB_PROVIDER`** | `sqlite` | Default storage provider. Automatically creates `./data/mcg.db`. |
| **Master Encryption Key** | `./data/.master.key` (Auto-Generated) or `MCG_MASTER_KEY` / `MCG_MASTER_KEY_FILE` | Master key used to encrypt all provider credentials and API tokens at rest (AES-256-GCM). |
| **`Admin:StandaloneAllowedNetworks`** | `127.0.0.1, ::1` | CIDR allowlist for admin endpoints when no external IDP is configured. |
| **Default Admin Key** | Seeded Base62 token (`mcp-adm-...`) | Seeded in DB on first boot to allow instant administrative connection. |
| **`CORS_ALLOWED_ORIGINS`** | `http://localhost:3000, http://localhost:8080` | Allowed web dashboard CORS origins. |

---

## 🔑 2. Connecting to the Admin MCP Server

The Admin MCP Server listens on `/admin/sse` or `/mcg-admin/sse` (and accepts messages on `/admin/message`).

### AI Agent Configuration (Claude, Cursor, Cline, Windsurf, Antigravity)

Add the following to your AI client configuration file:

```json
{
  "mcpServers": {
    "mcg-admin": {
      "url": "http://localhost:8080/admin/sse",
      "headers": {
        "Authorization": "Bearer mcp-adm-bootstrap-token-99"
      }
    }
  }
}
```

### JSON-RPC 2.0 Direct HTTP Dispatch

You can send tool execution requests directly via standard HTTP `POST` to `/admin`:

```bash
curl -X POST http://localhost:8080/admin \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer mcp-global-admin-default-cli-key-99" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "tools/call",
    "params": {
      "name": "manage_system",
      "arguments": {
        "action": "diagnostics"
      }
    }
  }'
```

---

## 🛠️ 3. Provider Configuration Cookbooks

All provider configurations are managed dynamically via the **`manage_providers`** tool.

### 3.1 Authentication Providers (`save_auth` & `test_ldap`)

#### A. Authentik / Authelia / Forward-Auth Reverse Proxy
Used when running behind Nginx, Traefik, Caddy, or an ingress controller that terminates authentication:

```json
{
  "name": "manage_providers",
  "arguments": {
    "action": "save_auth",
    "providerName": "HeaderAuth",
    "displayName": "Authentik SSO",
    "userHeader": "Remote-User",
    "groupsHeader": "Remote-Groups",
    "isEnabled": true,
    "configJson": "{\"trustedProxies\":[\"127.0.0.1\",\"10.0.0.0/8\",\"172.16.0.0/12\",\"192.168.0.0/16\"],\"requireTrustedProxy\":true}"
  }
}
```

#### B. Keycloak / OIDC Realm
```json
{
  "name": "manage_providers",
  "arguments": {
    "action": "save_auth",
    "providerName": "Keycloak",
    "displayName": "Keycloak Realm",
    "userHeader": "X-Forwarded-User",
    "groupsHeader": "X-Forwarded-Groups",
    "isEnabled": true,
    "configJson": "{\"authority\":\"https://keycloak.internal.corp/realms/master\",\"clientId\":\"mcg\",\"requireHttps\":true,\"groupClaim\":\"groups\"}"
  }
}
```

#### C. Microsoft Entra ID (Azure AD)
```json
{
  "name": "manage_providers",
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

#### D. Active Directory / LDAP (LDAPS Port 636)
> [!IMPORTANT]
> Plaintext LDAP over port 389 is rejected for security. Always configure LDAPS on port 636 or set `useSsl=true`.

1. **Test LDAP Connection & Bind**:
```json
{
  "name": "manage_providers",
  "arguments": {
    "action": "test_ldap",
    "server": "dc01.internal.corp",
    "port": 636,
    "useSsl": true,
    "bindDn": "CN=svc-mcg,OU=ServiceAccounts,DC=internal,DC=corp",
    "bindPassword": "ServiceAccountPassword123!"
  }
}
```

2. **Save Active Directory Provider**:
```json
{
  "name": "manage_providers",
  "arguments": {
    "action": "save_auth",
    "providerName": "ActiveDirectory",
    "displayName": "Corporate Active Directory",
    "isEnabled": true,
    "configJson": "{\"server\":\"dc01.internal.corp\",\"port\":636,\"useSsl\":true,\"domain\":\"INTERNAL\",\"baseDn\":\"DC=internal,DC=corp\",\"bindDn\":\"CN=svc-mcg,OU=ServiceAccounts,DC=internal,DC=corp\",\"bindPassword\":\"ServiceAccountPassword123!\"}"
  }
}
```

---

### 3.2 Secret Providers (`save_secret` & `test_vault`)

#### A. Built-in AES-256-GCM Master Key (Default)
By default, all secrets (API keys, custom headers, tokens) associated with backend MCP servers are automatically encrypted at rest in the database using the 256-bit `MCG_MASTER_KEY` (or legacy `ROUTER_MASTER_KEY`).

#### B. HashiCorp Vault KV v2 (AppRole Authentication)
1. **Test Vault Connection**:
```json
{
  "name": "manage_providers",
  "arguments": {
    "action": "test_vault",
    "address": "https://vault.internal.corp:8200",
    "authMethod": "approle",
    "roleId": "11111111-2222-3333-4444-555555555555",
    "secretId": "66666666-7777-8888-9999-000000000000"
  }
}
```

2. **Save Vault Secret Provider**:
```json
{
  "name": "manage_providers",
  "arguments": {
    "action": "save_secret",
    "providerName": "HashiCorpVault",
    "displayName": "Enterprise Vault KV",
    "isEnabled": true,
    "configJson": "{\"address\":\"https://vault.internal.corp:8200\",\"authMethod\":\"approle\",\"roleId\":\"11111111-2222-3333-4444-555555555555\",\"secretId\":\"66666666-7777-8888-9999-000000000000\",\"mountPath\":\"secret\"}"
  }
}
```

---

## 👥 4. Group Mappings & Access Policies

### 4.1 Group Mappings (`manage_group_mappings`)
Map external SSO groups, roles, or Active Directory domain SIDs to internal gateway roles:

```json
{
  "name": "manage_group_mappings",
  "arguments": {
    "action": "save",
    "externalId": "S-1-5-21-1234567890-123456789-123456789-512",
    "internalGroup": "full_admin"
  }
}
```

### 4.2 Access Policies (`manage_policies`)
Control access to specific backend servers or tools:

```json
{
  "name": "manage_policies",
  "arguments": {
    "action": "save",
    "targetId": "docker",
    "requiredGroup": "devops",
    "isAllowed": true
  }
}
```

---

## 🔍 5. Semantic Search & Embedding Providers

Configure semantic tool discovery via **`manage_settings`**:

### OpenAI Embeddings
```json
{
  "name": "manage_settings",
  "arguments": {
    "action": "update",
    "embeddingProvider": "OpenAI",
    "embeddingApiUrl": "https://api.openai.com/v1",
    "embeddingApiKey": "sk-proj-...",
    "embeddingApiModel": "text-embedding-3-small"
  }
}
```

### Local Ollama Embeddings
```json
{
  "name": "manage_settings",
  "arguments": {
    "action": "update",
    "embeddingProvider": "Ollama",
    "embeddingApiUrl": "http://ollama:11434/api/embeddings",
    "embeddingApiModel": "nomic-embed-text"
  }
}
```

---

## 🖥️ 6. Backend Servers & Client AppKeys

### 6.1 Register Backend Server (`manage_servers`)
```json
{
  "name": "manage_servers",
  "arguments": {
    "action": "create",
    "id": "github",
    "displayName": "GitHub Integration MCP",
    "url": "http://github-mcp:8080/sse",
    "type": "sse",
    "enabled": true,
    "categories": ["source-control", "ci-cd"]
  }
}
```

### 6.2 Issue Developer AppKey (`manage_appkeys`)
```json
{
  "name": "manage_appkeys",
  "arguments": {
    "action": "create",
    "name": "Developer Personal Key",
    "username": "steve",
    "scopes": ["all"],
    "expiresInDays": 90
  }
}
```

---

## 🧪 7. Live Tool Verification & Diagnostics

### 7.1 Test Backend Tool Dispatch (`test_tool_call`)
Directly verify backend tool execution through the gateway:
```json
{
  "name": "test_tool_call",
  "arguments": {
    "serverId": "github",
    "toolName": "search_repositories",
    "arguments": {
      "query": "model-context-gateway"
    }
  }
}
```

### 7.2 View Audit Logs (`manage_system`)
```json
{
  "name": "manage_system",
  "arguments": {
    "action": "query_audit",
    "take": 50
  }
}
```

---

## 📋 8. Admin MCP Tool Reference

| Tool Name | Action | Key Parameters | Purpose |
| :--- | :--- | :--- | :--- |
| **`manage_servers`** | `list`, `get`, `create`, `update`, `delete`, `toggle`, `reconnect`, `reconnect_all` | `id`, `displayName`, `url`, `type`, `enabled`, `secretProvider` | Backend MCP server lifecycle. |
| **`manage_appkeys`** | `list`, `get_limits`, `create`, `revoke` | `name`, `username`, `scopes`, `expiresInDays`, `id` | Client API key provisioning. |
| **`manage_clients`** | `list`, `register`, `delete` | `displayName`, `scopes`, `expiresInDays`, `id` | Dynamic OAuth2 client management. |
| **`manage_policies`** | `list`, `save`, `delete` | `targetId`, `requiredGroup`, `isAllowed`, `id` | RBAC target access rules. |
| **`manage_group_mappings`** | `list`, `save`, `delete` | `externalId`, `internalGroup`, `id` | SSO group & SID role mappings. |
| **`manage_providers`** | `list`, `save_secret`, `test_vault`, `save_auth`, `test_ldap` | `providerName`, `displayName`, `configJson`, `isEnabled`, `address`, `server` | Identity & secret providers with live test probes. |
| **`manage_settings`** | `get`, `update` | `dashboardTitle`, `embeddingProvider`, `embeddingApiUrl`, `globalMaxKeys` | System branding & embeddings. |
| **`manage_custom_files`** | `list`, `get`, `save`, `delete` | `type`, `name`, `content` | Virtual MCP prompts & resources. |
| **`manage_system`** | `diagnostics`, `get_logs`, `clear_logs`, `query_audit` | `limit`, `user`, `server`, `since`, `take`, `skip` | Gateway observability & audit. |
| **`test_tool_call`** | *(default)* | `serverId`, `toolName`, `arguments` | Live tool test execution. |
