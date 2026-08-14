# 02. Server Management & Secret Providers

The MCP Gateway Router allows registering any standard Model Context Protocol (MCP) server over `HTTP`, `SSE`, or `STDIO` transport, while supporting enterprise secret providers to keep credentials secure.

---

## ➕ Registering a New Server

Click the **`+ Add Server`** button in the dashboard toolbar to open the Server Registration modal.

### Required Parameters
1. **Server ID**: Unique string identifier (lowercase, alphanumeric, e.g. `actual_budget`).
2. **Server Name**: Human-readable name (e.g. `Actual Budget MCP`).
3. **Transport Type**:
   - `HTTP`: Standard HTTP POST JSON-RPC endpoint.
   - `SSE`: Server-Sent Events stream endpoint.
   - `STDIO`: Local subprocess command (e.g. `npx -y @modelcontextprotocol/server-filesystem`).
4. **Base URL / Command**: Full HTTP/SSE URL (e.g. `http://actual-mcp:8080/mcp`) or binary command string.
5. **Category**: Organizational group (`Smart Home`, `Media`, `Cloud`, `Infrastructure`).

---

## 🔐 Secret Providers Guide

To prevent hardcoding sensitive API keys or passwords in the database, the router supports 3 pluggable secret resolution providers in addition to direct static keys:

> [!TIP]
> For an exhaustive architectural deep-dive, Vault KV v2 policies, AppRole setup commands, AES-256-GCM encryption-at-rest specs, and Docker recipes, see the comprehensive [**Enterprise Secret Providers Guide**](../secret-providers.md).

### 1. Direct API Key (`None`)
- Store a static secret token directly in the server configuration.
- Best for local testing or un-isolated development environments.

### 2. Environment Variable (`Environment` / `Env`)
- Resolves secrets at runtime from environment variables on the host container.
- **Item Key / Path**: Name of the environment variable (e.g. `HOME_ASSISTANT_TOKEN`, `ACTUAL_API_KEY`, or `env:MY_SECRET`).
- Supports direct variable name lookups without storing credentials in the database.

### 3. HashiCorp Vault (`Vault` / `HashiCorpVault`)
- Integrates dynamically with HashiCorp Vault Key-Value Version 2 (`kv-v2`) secrets engines.
- **Supported Auth**: AppRole (`roleId` + `secretId`), Token auth, and `VAULT_TOKEN` environment fallback.
- **Parameters**:
  - **Secret Mount**: Secret engine mount path (default: `secret`).
  - **Secret Path**: Path to the secret entry (e.g. `services/radarr` or `services/docker`).
  - **Secret Field**: Field name inside the secret payload (e.g. `api_key` or `password`).
- The router fetches the secret at runtime via `VaultSecretRetriever`, applying automatic JIT token TTL inspection (< 5 min remaining), automatic re-authentication, and 10-minute in-memory caching.

### 4. Windows Registry (`WindowsRegistry` / `Registry`)
- Resolves secrets from Windows Registry `HKLM` hives (`RegistryHive.LocalMachine`).
- Automatically unprotects Windows DPAPI encrypted blobs (`byte[]`) or reads plaintext strings.
- **Requirements**: Windows host OS only (safely returns `null` on Linux/Docker containers).
- **Parameters**:
  - **Secret Path**: Subkey path (e.g. `SOFTWARE\Homelab\McpSecrets`).
  - **Secret Field**: Value name (e.g. `PlexToken`).

---

## 📄 Custom Tool JSON Specifications

For backend services that do not natively speak the MCP protocol, or for custom command mapping:
1. Click **`Custom Tools`** in the dashboard toolbar.
2. Paste or upload a standard OpenAPI / JSON tool specification array:
```json
[
  {
    "name": "custom_script_runner",
    "description": "Executes local maintenance script",
    "parameters": {
      "type": "object",
      "properties": {
        "script_name": { "type": "string" }
      },
      "required": ["script_name"]
    }
  }
]
```
3. Save changes to register the custom tools directly into the catalog.
