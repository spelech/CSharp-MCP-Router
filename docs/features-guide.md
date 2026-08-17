# MCP Router Features Guide

This guide details the features of the MCP Router.

---

## 🖥️ 1. Dynamic Server Management

The MCP Router supports four methods to manage backend Model Context Protocol (MCP) servers:

### Method A: Web UI Dashboard (Recommended)
Manage servers dynamically without restarting the gateway:
1. Open the router dashboard in a browser.
2. Click **+ Add Server** (top right).
3. Complete the **Add MCP Server** modal:
   - **Display Name**: User-friendly label (e.g., `Home Assistant`).
   - **URL**: Backend SSE endpoint or HTTP server (e.g., `http://ha-mcp:8086/mcp`).
   - **Transport Type**: Select `sse` (stateful) or `http` (stateless).
   - **Category**: Classify the server (e.g., `homecontrol`, `infrastructure`, `development`).
   - **API Token/Key**: Downstream credentials.
   - **Secret Provider**: Secret retrieval method (`None`, `Vault`, `WindowsRegistry`, or `Environment`). See [Pluggable Secret Retrievers](#5-pluggable-secret-retrievers).
4. Click **Save Server**. The router registers the server and initializes connections.

![Add MCP Server Modal](../docs/assets/add_server_modal.jpg)

### Method B: Static JSON Seeding (`custom_servers.json`)
For declarative configurations:
1. Create `custom_servers.json` in the `/app/data/` directory.
2. Use this structure:
   ```json
   [
     {
       "id": "my-mcp-server",
       "displayName": "My Custom Server",
       "url": "http://10.0.0.15:3000/sse",
       "type": "sse",
       "category": "infrastructure",
       "enabled": true,
       "hidden": false,
       "apiKey": "optional-bearer-or-api-key",
       "headersJson": "{\"Custom-Header-Name\": \"Header-Value\"}"
     }
   ]
   ```
3. The gateway processes matching entries in the database during startup.

### Method C: Environment Seed Migration
The gateway auto-seeds common services on first run if environment variables exist (e.g., `HOMEASSISTANT_TOKEN`, `PLEX_TOKEN`, `SEERR_API_KEY`). Refer to `Program.cs`.

### Method D: Dynamic Docker Label Auto-Discovery (`mcp.*` labels)
If the router accesses the Docker daemon (`/var/run/docker.sock`), it dynamically registers backend containers labeled `mcp.enabled=true`.

```yaml
services:
  my-service-mcp:
    image: ghcr.io/org/my-service-mcp:latest
    container_name: my-service-mcp
    restart: unless-stopped
    networks:
      - net_mcp
    labels:
      - mcp.enabled=true
      - mcp.id=myservice
      - mcp.displayName=My Custom Service
      - mcp.port=8080
      - mcp.type=sse
      - mcp.path=/sse
      - mcp.categories=infrastructure,custom
```

#### Supported Docker Labels

| Label | Required | Default | Description |
| :--- | :--- | :--- | :--- |
| `mcp.enabled` | **Yes** | `false` | Enables router auto-discovery. Must be `"true"`. |
| `mcp.id` | **Yes** | — | Unique server identifier (e.g., `/myservice`). |
| `mcp.port` | **Yes** | — | Internal container port (e.g., `8080`, `3000`). |
| `mcp.displayName`| No | Value of `mcp.id` | Friendly name for dashboard and tools. |
| `mcp.type` | No | `sse` | Transport type (`sse`, `http`, or `stdio`). |
| `mcp.path` | No | `/sse` (or `/mcp`) | Message dispatch path. |
| `mcp.categories` | No | `general` | Comma-separated categories for RBAC. |
| `mcp.authType` | No | `none` | Authentication header format (`bearer`, `x-api-key`, `custom-header`). |
| `mcp.secretProvider`| No | `none` | Secret retriever backend (`vault`, `env`, `none`). |
| `mcp.secretKey` | No | — | Vault path or environment variable for the API key. |

---

## 📡 2. Routing Modes

Connect clients via these SSE endpoints:

| Route Path | Mode | Description |
| :--- | :--- | :--- |
| `/sse` or `/sse?meta=true` | **Meta-Mode (Default)** | Hides backend tools during bootstrap; exposes only `search_tools` and `execute_tool`. Conserves context window. |
| `/sse?meta=false` | **Full-List Mode** | Exposes all underlying tools from all connected servers. |
| `/{targetServerId}` | **Target-Specific Proxying** | Proxies connections directly to the specified target server (e.g., `/docker` or `/ha`). |

> For transport protocol comparisons (`sse`, `http`, `stdio`), concurrency, security policies, and error recovery, see [**Transport Capability & Configuration Guide**](transports.md).

### Client Setup Examples

#### Claude Desktop Configuration (`config.json`)
```json
{
  "mcpServers": {
    "mcp-router-meta": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/client-sse", "http://localhost:8026/sse"]
    }
  }
}
```

#### Antigravity CLI Configuration (`.gemini/settings.json`)
```json
{
  "mcpServers": {
    "mcp-router": {
      "url": "http://localhost:8026/sse",
      "type": "sse",
      "trust": true,
      "serverUrl": "http://localhost:8026/sse"
    }
  }
}
```

---

## 🔍 3. Semantic Search

In **Meta-Mode**, clients must semantically search for tools before execution.

### Search Flow:
1. **Tool Inquiry**: Client calls `search_tools(query: "restart actual budget container")`.
2. **Hybrid Scoring Engine**:
   - Computes semantic similarity of tools using a **Local ONNX model** (`all-MiniLM-L6-v2`) or **LiteLLM/OpenAI APIs**.
   - Applies **Keyword Boosting** (exact phrase match: +2.0 weight; individual words: +1.0/+0.5 weight).
3. **Execution Routing**: Client executes returned namespaced tools (e.g., `docker__restart_container`) via `execute_tool`.

### Embeddings Configuration:
Configure via the Settings panel:
* **Local ONNX (In-Process)**: Offline execution via `Microsoft.ML.OnnxRuntime`. Downloads weights on first run to `/app/data/`.
* **OpenAI API / LiteLLM Provider**: Uses remote models. Credentials are encrypted in the database via SQLite SQLCipher.

---

## 🔐 4. Authentication, Group Mapping & Unified MCP Capability Authorization

The MCP Router implements a **Unified Authorization Pipeline** across MCP capabilities:
- **Tools**: `tools/list`, `tools/call`
- **Prompts**: `prompts/list`, `prompts/get`
- **Resources**: `resources/list`, `resources/read`, `resources/templates/list`
- **Completions**: `completion/complete`

All requests undergo this pipeline:
1. **AppKey Scope Validation**: Validates scopes (`*`, `all`, `server:{id}`, `tool:{id}`, `prompt:{id}`, `resource:{id}`, `resource_template:{id}`, `completion:{id}`).
2. **Admin SID Bypass**: Checks caller SIDs against `Admin:GroupSid` (e.g., `S-1-5-32-544`).
3. **Database Access Policies**: Evaluates allows and denies in `AccessPolicies` and `sp_EvaluateUserAccess` against mapped groups/SIDs.
4. **Discovery Filtering**: Automatically omits unauthorized items from list endpoints.
5. **Fail-Closed Default**: Unknown capabilities or targets return audited 403 errors without data leakage.

### Identity Providers
- **Active Directory (Kerberos/NTLM)**: Resolves caller identities via AD SIDs (`WindowsIdentity`).
- **OIDC Header Proxy**: Extracts SSO headers (e.g., `Remote-User`, `Remote-Groups`) from reverse proxies.

### Group & SID Mapping Policy
External groups map to internal groups via the `GroupMappings` table (Settings -> Identity & Auth):
1. **Create Mapping**: Map an AD SID or OIDC group to an internal security group (`admin`, `operator`, `readonly`).
2. **Evaluate Access**: Capability invocation triggers access evaluation against the user's mapped groups.

### AppKey Credentials & Category-Scoped Authorization
The router issues fine-grained AppKeys and Client credentials:
- **Scope Granularity**:
  - `all` / `*` / `mcp_client`: Full access to all backend servers.
  - `server:<serverId>` / `<serverId>`: Full access to a specific backend.
  - `category:<name>` / `group:<name>`: Dynamic access to capabilities of all servers in the specified category.
  - `tool:<name>`, `prompt:<name>`, `resource:<uri>`: Pinpoint access to specific capabilities.
- **Dynamic Membership**: Category scopes evaluate server memberships in real time. Changes to server categories apply instantly.
- **Creation Validation**: Category scopes are validated against registered categories during credential creation. Unknown categories yield a 400 Bad Request unless admin-provisioned.

For detailed rules and pipelines, see the [**AppKey Scopes & Authorization Guide**](appkey-scopes.md).

### CORS & Cross-Origin Security Configuration
By default, the gateway restricts CORS to local development origins (`http://localhost:3000`, `http://localhost:5000`, `https://localhost:5001`).

For production, configure allowed origins via the `CORS_ALLOWED_ORIGINS` (or `AllowedOrigins`) environment variable/setting:
- **`CORS_ALLOWED_ORIGINS`**: Delimited list of allowed URLs (e.g., `https://my-mcp-dashboard.internal, https://cursor-plugin.internal`).

---

## 🔑 5. Pluggable Secret Retrievers

The router dynamically fetches downstream API keys and passwords via pluggable retrievers to prevent plaintext storage in the database.

The `CompositeSecretRetriever` resolves secrets from:
1. **HashiCorp Vault (KV v2)**: Fetches secrets via path configurations (e.g., `/secret/data/mcp/plex`) with AppRole/Token auth and JIT token renewal.
2. **Windows Registry (DPAPI)**: Retrieves DPAPI-secured strings from registry hives (`HKLM`).
3. **Environment Variables**: Resolves secrets bound as container environment variables (`env:MY_SECRET`).

> [!TIP]
> For configuration recipes, AppRole policies, and AES-256-GCM encryption architecture, see [**docs/secret-providers.md**](secret-providers.md).

### Configuration
1. Register the secret in your store (e.g., environment variable `DOCKER_API_KEY=my-secret`).
2. In the Add/Edit Server modal, select `Environment` for **Secret Provider** and enter `DOCKER_API_KEY` under **SecretItemKey**.
3. The gateway fetches, decrypts, and caches the token (`IMemoryCache` with rolling TTL) at execution time.

---

## 🧪 6. Developer Test Bench & Diagnostics

The Web Dashboard includes a developer environment to debug and verify setups:

1. **Interactive Form Builder**: Generates forms matching the JSON schemas of registered backend tools.
2. **Logs Console**: Thread-safe, real-time console displaying JSON-RPC traffic, request IDs, and security classifications.
3. **Search Simulator**: Evaluation panel to test queries against the semantic search engine and inspect scores.
4. **Manual Approval Modal**: Pauses dangerous tool executions pending administrator approval via the UI.

![Test Bench View](../docs/assets/test_bench_view.jpg)

---

## 🗄️ 7. Database Engine Support & Deployment

For SQLite, MS SQL Server, and MySQL dialect specifications, the 12-table [**Entity-Relationship Diagram (ERD)**](database-providers.md#unified-database-entity-relationship-diagram-erd), stored procedure catalogs, AES-256-GCM envelope encryption, and Docker Compose configurations, see the [**Database Provider Support & Deployment Matrix**](database-providers.md).

---

## 📋 8. Software Requirements Specification & Automated Test Catalog

For requirements traceability, feature proofs, guardrails, and verified invariants across test suites, reference:
* [**Software Requirements Specification (SRS) & Test Verification Catalog**](software-requirements-and-test-catalog.md)
* [**Test Catalog & Annotation Developer Guide**](test-catalog-guide.md)

