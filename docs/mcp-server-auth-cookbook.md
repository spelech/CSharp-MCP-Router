# 📖 MCP Server Authentication & Integration Cookbook

### *"If Your Backend MCP Server Requires X ➔ Here Is Exact Setup Y"*

This guide provides a practical, scenario-driven decision matrix and copy-paste recipes for connecting any backend Model Context Protocol (MCP) server to **Model Context Gateway (MCG)**, regardless of how that backend requires authentication.

---

## ⚡ Quick-Lookup Decision Matrix

Find the authentication mechanism your downstream MCP server requires in the left column to get the exact configuration settings needed in the router:

| If Your MCP Server Requires... | Transport Type | Secret Provider | Auth Shape | Key Router Configuration Fields |
| :--- | :--- | :--- | :--- | :--- |
| **1. No Auth / Public / Local** | `sse` / `http` | `None` | `bearer` *(ignored)* | Leave `ApiKey` and `SecretPath` blank. |
| **2. Standard Bearer Token** (`Authorization: Bearer <token>`) | `sse` / `http` | `None`, `Environment`, or `Vault` | `bearer` | Enter token in `ApiKey` OR set `SecretProvider: Environment` with env var name OR `Vault`. |
| **3. Custom HTTP Header** (e.g. `X-API-Key`, `X-Plex-Token`) | `sse` / `http` | `None`, `Environment`, or `Vault` | `custom-header` or `x-api-key` | Set `AuthShape: custom-header`, `SecretField: <Header-Name>`, and provide the token. |
| **4. HTTP Basic Auth** (`Authorization: Basic <base64>`) | `sse` / `http` | `None`, `Environment`, or `Vault` | `basic` | Enter `username:password` string as the secret; router auto-formats Base64. |
| **5. URL Query Parameter** (`http://host/sse?token=<key>`) | `sse` / `http` | `None`, `Environment`, or `Vault` | `query` | Set `AuthShape: query`, `SecretField: <param_name>` (defaults to `token`). |
| **6. Local CLI Binary / Subprocess (`stdio`)** | `stdio` | `None`, `Environment`, or `Vault` | *(Auto-handled)* | Command & arguments. Secrets injected exclusively into process `EnvironmentVariables` (Zero CLI leak). |
| **7. HashiCorp Vault Secrets** (Enterprise Key Rotation) | `sse`, `http`, `stdio` | `Vault` | *(Matches backend)* | `SecretProvider: Vault`, `Vault Mount: secret`, `Path: <path>`, `Field: <key>`. |
| **8. Windows DPAPI Registry Secrets** (Windows Server / IIS) | `sse`, `http`, `stdio` | `WindowsRegistry` | *(Matches backend)* | `SecretProvider: WindowsRegistry`, `Registry Path: SOFTWARE\McpRouter\Secrets`, `Key Name: <Key>`. |
| **9. Per-User Personal Access Tokens (BYOK)** | `sse` / `http` | `UserProvided` | *(Matches backend)* | `SecretProvider: UserProvided`. Users store personal tokens in **My MCP Servers** tab. |
| **10. Pass-Through Dynamic JWTs** | `sse` / `http` | `AllowPassThroughAuth` | *(Matches backend)* | Enable `AllowPassThroughAuth: true`. Client sends JWT in `X-Target-Auth` header. |
| **11. Identity-Forwarding Gateway** (Downstream RLS) | `sse` / `http` | *(Any)* | *(Matches backend)* | Router passes service account token + `X-Forwarded-User: <username>` header for Row-Level Security. |

---

## 🍳 Detailed Recipes & Implementation Examples

---

### Recipe 1: No Authentication (Local Sidecars, Public Services)

* **Common Use Cases**: Local test servers, unauthenticated Docker container sidecars, read-only internal MCP tools.
* **How It Works**: The router connects directly without injecting authorization headers.

#### Web UI Configuration:
1. Click **`+ Add Server`**.
2. **Server ID**: `mock-tools`
3. **Transport Type**: `SSE Stream` or `HTTP JSON-RPC`
4. **URL**: `http://mock-service:8080/sse`
5. **Secret Provider**: `None`
6. **API Key**: *(Leave empty)*
7. Click **Save Server**.

#### Admin MCP Tool JSON (`manage_servers`):
```json
{
  "action": "create",
  "id": "mock-tools",
  "displayName": "Mock Tools Service",
  "url": "http://mock-service:8080/sse",
  "type": "sse",
  "category": "testing",
  "secretProvider": "None",
  "enabled": true
}
```

---

### Recipe 2: Static Bearer Token (`Authorization: Bearer <token>`)

* **Common Use Cases**: **Home Assistant** (Long-Lived Access Token), **OpenAI/LiteLLM MCP**, **Docker Socket Proxy**, standard SaaS APIs.
* **How It Works**: Router formats the resolved secret as `Authorization: Bearer <secret>` on downstream requests.

#### Option A: Direct Static Token in DB (AES-256-GCM Encrypted)
* **Secret Provider**: `None`
* **API Key / Token**: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
* **Auth Shape**: `bearer`

#### Option B: Environment Variable (Recommended for Docker / 12-Factor)
* In your host/compose environment: `HOMEASSISTANT_TOKEN=eyJhbGci...`
* In Router Server Modal:
  * **Secret Provider**: `Environment`
  * **Secret Field / Env Var**: `HOMEASSISTANT_TOKEN`
  * **Auth Shape**: `bearer`

#### Admin MCP Tool JSON (`manage_servers`):
```json
{
  "action": "create",
  "id": "homeassistant",
  "displayName": "Home Assistant Smart Home",
  "url": "http://ha-mcp:8086/sse",
  "type": "sse",
  "category": "smarthome",
  "secretProvider": "Environment",
  "secretField": "HOMEASSISTANT_TOKEN",
  "authShape": "bearer",
  "enabled": true
}
```

---

### Recipe 3: Custom HTTP Header Auth (`X-API-Key`, `X-Plex-Token`, etc.)

* **Common Use Cases**: **Plex** (`X-Plex-Token`), **Radarr/Sonarr** (`X-Api-Key`), **Anthropic** (`x-api-key`), custom enterprise microservices.
* **How It Works**: Router extracts the secret and injects it into the exact custom header name specified in `SecretField`.

#### Example 3A: Plex Media Server (`X-Plex-Token`)
* **Transport**: `SSE Stream`
* **URL**: `http://plex-mcp:8000/sse`
* **Secret Provider**: `Environment`
* **Secret Field (Env Var & Header)**: `PLEX_TOKEN`
* **Auth Shape**: `custom-header`
* **Custom Header Name**: `X-Plex-Token`

#### Example 3B: Radarr / Sonarr (`X-Api-Key`)
* **Secret Provider**: `None` (or `Environment: RADARR_API_KEY`)
* **Auth Shape**: `x-api-key` (or `custom-header` with `SecretField: X-Api-Key`)

#### Admin MCP Tool JSON (`manage_servers`):
```json
{
  "action": "create",
  "id": "plex",
  "displayName": "Plex Media Server",
  "url": "http://plex-mcp:8000/sse",
  "type": "sse",
  "category": "media",
  "secretProvider": "Environment",
  "secretField": "PLEX_TOKEN",
  "authShape": "custom-header",
  "customHeaderName": "X-Plex-Token",
  "enabled": true
}
```

---

### Recipe 4: HTTP Basic Authentication (`Authorization: Basic ...`)

* **Common Use Cases**: Legacy internal APIs, password-protected proxies, services requiring `username:password` or `apiKey:` format.
* **How It Works**: Enter the `username:password` string as the secret; the router automatically Base64-encodes it and sends `Authorization: Basic <base64>`.

#### Web UI Configuration:
* **Secret Provider**: `None` (or `Environment: SERVICE_BASIC_AUTH`)
* **API Key / Secret**: `admin:SuperSecretPassword123`
* **Auth Shape**: `basic`

#### Admin MCP Tool JSON (`manage_servers`):
```json
{
  "action": "create",
  "id": "legacy-db-mcp",
  "displayName": "Legacy Database MCP",
  "url": "http://internal-db-mcp:8080/mcp",
  "type": "http",
  "category": "database",
  "apiKey": "admin:SuperSecretPassword123",
  "authShape": "basic",
  "enabled": true
}
```

---

### Recipe 5: URL Query Parameter Authentication (`?token=<key>`)

* **Common Use Cases**: Webhook-style MCP backends, legacy streaming servers that reject custom HTTP headers during SSE handshake.
* **How It Works**: Router appends `?<param_name>=<secret>` to the request URL.

#### Web UI Configuration:
* **Endpoint URL**: `http://streaming-service:9000/sse`
* **Secret Provider**: `Environment` (e.g. `STREAM_API_KEY`)
* **Auth Shape**: `query`
* **Secret Field**: `token` *(or custom query parameter name like `apiKey`)*

#### Resulting Outbound Request:
`GET http://streaming-service:9000/sse?token=ResolvedSecretKey123`

---

### Recipe 6: Local Subprocess / STDIO Process Environment (`stdio`)

* **Common Use Cases**: Running official MCP CLI tools (`@modelcontextprotocol/server-filesystem`, `@modelcontextprotocol/server-github`, `uvx`, Python scripts).
* **How It Works (Zero CLI Leakage)**: The router spawns the subprocess and injects the resolved secret directly into the process environment dictionary (`ProcessStartInfo.Environment["API_KEY"]`). Secrets **never** appear in command-line arguments or OS process monitors (`ps aux`).

#### Web UI Configuration:
* **Transport Type**: `STDIO CLI`
* **Command / Executable**: `npx`
* **Arguments**: `-y @modelcontextprotocol/server-filesystem /shared/data`
* **Secret Provider**: `Environment` (or `Vault`)
* **Secret Field**: `GITHUB_PERSONAL_ACCESS_TOKEN`

#### Admin MCP Tool JSON (`manage_servers`):
```json
{
  "action": "create",
  "id": "filesystem-mcp",
  "displayName": "Local Filesystem MCP",
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@modelcontextprotocol/server-filesystem", "/app/data"],
  "category": "infrastructure",
  "enabled": true
}
```

> [!TIP]
> When running in Docker, use the **`ghcr.io/spelech/model-context-gateway:latest-full`** container image, which comes pre-installed with Node.js 22, Python 3.12, `uv`, and `bun` for running `stdio` tools out of the box.

---

### Recipe 7: Enterprise Credential Rotation with HashiCorp Vault (KV v2)

* **Common Use Cases**: Enterprise production deployments requiring automated token rotation, zero secrets stored in gateway databases, and central audit compliance.
* **How It Works**: Router authenticates to Vault via **AppRole** (`roleId`/`secretId`) or direct token, reads the versioned secret from `secret/data/<path>`, caches it in memory for 10 minutes with automatic JIT renewal, and injects it into downstream requests.

#### Step 1: Configure Vault in Router Settings
In **`Settings`** -> **`Secret Providers`** -> **HashiCorp Vault**:
```json
{
  "address": "https://vault.corp.internal:8200",
  "mountPath": "secret",
  "roleId": "8f8c49e2-1234-5678-abcd-ef0123456789",
  "secretId": "3b2a1c0d-9876-5432-fedc-ba9876543210"
}
```

#### Step 2: Configure Server with Vault Path
* **Secret Provider**: `Vault`
* **Vault Mount**: `secret`
* **Secret Path**: `infrastructure/docker`
* **Secret Field**: `api_key`
* **Auth Shape**: `bearer`

#### Admin MCP Tool JSON (`manage_servers`):
```json
{
  "action": "create",
  "id": "docker-prod",
  "displayName": "Production Docker MCP",
  "url": "http://docker-mcp.internal:8080/sse",
  "type": "sse",
  "category": "infrastructure",
  "secretProvider": "Vault",
  "vaultMount": "secret",
  "vaultPath": "infrastructure/docker",
  "vaultKey": "api_key",
  "authShape": "bearer",
  "enabled": true
}
```

---

### Recipe 8: Enterprise Windows DPAPI Registry Secrets (Windows Server / IIS)

* **Common Use Cases**: Windows Server and IIS on-premise deployments using Active Directory machine trust and DPAPI hardware-bound encryption.
* **How It Works**: Secrets stored in `HKLM\SOFTWARE\McpRouter\Secrets` encrypted via Windows DPAPI are decrypted in-process by the router service account.

#### Web UI Configuration:
* **Secret Provider**: `WindowsRegistry`
* **Registry Path**: `SOFTWARE\McpRouter\Secrets`
* **Key Name**: `ProdDatabaseApiKey`
* **Auth Shape**: `bearer`

---

### Recipe 9: Multi-Tenant / Bring-Your-Own-Key (BYOK / `UserProvided`)

* **Common Use Cases**: Multi-user shared gateway where users connect to services using their own personal access tokens (e.g. personal GitHub PAT, individual Actual Budget tokens, personal Notion keys).
* **How It Works**: 
  1. Admin registers the server with `SecretProvider: UserProvided`.
  2. Users open the **`My MCP Servers`** tab in the dashboard.
  3. Users enter their personal API token.
  4. When that user invokes tools, the router dynamically decrypts and injects their specific token.

#### Admin Server Registration:
* **Secret Provider**: `UserProvided`
* **Auth Shape**: `bearer` (or `custom-header`)

---

### Recipe 10: Dynamic OAuth 2.0 / OIDC Token Exchange & Pass-Through JWTs

* **Common Use Cases**: Downstream microservices requiring short-lived user JWTs minted by an identity provider (Keycloak, Authentik, Okta, Microsoft Entra ID).
* **How It Works**:
  * **Pass-Through Mode**: Set `AllowPassThroughAuth: true`. The calling client retrieves the JWT and passes it in the `X-Target-Auth` header. The router translates `X-Target-Auth` into the backend's expected `AuthShape` (e.g., standard `Authorization: Bearer <jwt>`).
  * **Interactive OAuth Consent**: Third-party apps register via Dynamic Client Registration and trigger `/connect/authorize`, where users approve access on the `/consent` screen.

---

### Recipe 11: Trusted Gateway Pattern (Identity-Forwarding for Row-Level Security)

* **Common Use Cases**: Backend MCP servers that maintain their own internal authorization models and need to know the human/user principal executing the tool call.
* **How It Works**: The router authenticates to the backend using a shared Service Account token, and automatically injects standard identity propagation headers:
  * `X-Forwarded-User: <username>` (e.g. `admin` or `DOMAIN\spelech`)
  * `X-Forwarded-Groups: <groups>` (e.g. `full_admin, engineering`)
  * `X-Mcp-Session-Id: <session_id>`

The backend MCP server trusts the router's IP/network and applies Row-Level Security (RLS) based on the forwarded user identity.

---

## 🎯 Common Backend MCP Server Cheat Sheet

| MCP Server | Typical Transport | Recommended Secret Provider | Configured Auth Shape | Example Secret Field / Header |
| :--- | :--- | :--- | :--- | :--- |
| **Docker Daemon MCP** | `sse` | `Environment` / `Vault` | `bearer` | `DOCKER_MCP_TOKEN` |
| **Home Assistant MCP** | `sse` | `Environment` / `Vault` | `bearer` | `HOMEASSISTANT_TOKEN` |
| **Plex MCP** | `sse` | `Environment` / `Vault` | `custom-header` | `X-Plex-Token` |
| **Overseerr / Seerr MCP** | `sse` | `Environment` / `Vault` | `x-api-key` | `SEERR_API_KEY` |
| **Radarr / Sonarr MCP** | `http` | `Environment` / `Vault` | `x-api-key` | `X-Api-Key` |
| **Actual Budget MCP** | `sse` | `UserProvided` (BYOK) | `bearer` | *(User PAT)* |
| **Google Workspace MCP** | `sse` | `Environment` | `bearer` | `GOOGLE_WORKSPACE_TOKEN` |
| **Unifi Network MCP** | `sse` | `Environment` | `x-api-key` | `UNIFI_API_KEY` |
| **PostgreSQL / MySQL MCP** | `http` / `stdio` | `Environment` / `Vault` | `basic` or Env | `DB_PASSWORD` |
| **Filesystem / GitHub MCP** | `stdio` | `Environment` | *(Auto Process Env)* | `GITHUB_TOKEN` |

---

## 📚 Related Documentation

* 🔐 [**Enterprise Secret Providers Guide**](secret-providers.md) — Deep-dive into Vault, DPAPI, and AES-256-GCM.
* 🚦 [**Authentication Support Matrix**](auth-flows/auth-support-matrix.md) — Technical end-to-end transport and delegation matrix.
* 🛡️ [**RBAC & Security Policies Guide**](user-guide/03-rbac-and-security.md) — 4-Stage authorization pipeline and group access controls.
* 🤖 [**Admin MCP Automation Guide**](admin-mcp-automation-guide.md) — Autonomous server provisioning via AI agent skills.
