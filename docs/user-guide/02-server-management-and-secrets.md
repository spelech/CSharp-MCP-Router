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

To prevent hardcoding sensitive API keys or passwords in the database, the router supports 4 secret resolution providers:

### 1. Direct API Key (`None`)
- Store an static secret token directly in the server configuration.
- Best for local testing or un-isolated environments.

### 2. Environment Variable (`Env`)
- Resolves secrets at runtime from environment variables on the host container.
- **Item Key**: Name of the environment variable (e.g. `HOME_ASSISTANT_TOKEN` or `ACTUAL_API_KEY`).
- Format: `ENV:VARIABLE_NAME`.

### 3. HashiCorp Vault (`Vault`)
- Integrates dynamically with HashiCorp Vault Key-Value (KV v1 or v2) secrets engines.
- **Parameters**:
  - **Secret Mount**: Secret engine mount path (e.g. `secret` or `homelab`).
  - **Secret Path**: Path to the secret secret entry (e.g. `services/radarr` or `media/api-keys`).
  - **Secret Field**: Field name inside the secret payload (e.g. `api_key` or `password`).
- The router fetches the secret at runtime via `VaultSecretRetriever`, applying automatic token caching and lease renewal.

### 4. File / Windows Registry (`Registry`)
- Resolves secrets from local disk paths (e.g. `/etc/mcp-secrets/docker.key` or `/run/secrets/api_token`) or Windows Registry keys.
- **Item Key**: Absolute path or registry path key.

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
