# 📚 MCP Gateway Router - Official User Guide & Manual

Welcome to the official documentation for the **Model Context Protocol (MCP) Gateway Router**. This router acts as a central control plane and high-performance proxy for aggregating 100+ backend MCP services, standardizing security, managing identity, and optimizing tool calling for AI agents and IDEs.

---

## 🗺️ User Guide Sitemap

1. [**01. Dashboard & Navigation Interface**](01-dashboard-and-navigation.md)
   - Interface layout, real-time status indicators, server categories, search filters, and statistics cards.
2. [**02. Server Management & Secret Providers**](02-server-management-and-secrets.md)
   - Registering backend MCP servers (`http`, `sse`, `stdio`), custom tool JSON definitions, and configuring Secret Providers:
     - Direct API Keys
     - Environment Variables (`ENV:KEY`)
     - HashiCorp Vault (KV v1/v2 engine integration)
     - Windows Registry / File Secrets (`/etc/mcp-secrets`)
3. [**03. RBAC, Security & Approvals**](03-rbac-and-security.md)
   - Fine-grained Access Control (RBAC) rules, Active Directory / OIDC group mappings, OAuth shapes, and the manual tool execution approval queue.
4. [**04. Client Setup & App Key Management**](04-client-setup-and-app-keys.md)
   - Generating App Keys, configuring scopes, and integration guides for Cursor IDE, Claude Desktop, OpenClaw Agent, and custom SSE/HTTP clients.
5. [**05. Interactive Test Bench**](05-interactive-test-bench.md)
   - Executing tools manually, querying virtual resource URIs (`mcp://...`), rendering prompt templates, testing semantic vector routing (`search_tools`), and live SSE diagnostic logging.
6. [**06. System Settings & Vector Embeddings**](06-settings-and-embeddings.md)
   - Configuring vector embedding engines (Local ONNX `All-MiniLM-L6-v2` vs OpenAI/Ollama API), global approval modes, and OpenIddict OAuth settings.

---

## 💡 Key Architectural Concepts

- **Meta-Mode (`/sse`)**: Exposes only `search_tools` and `execute_tool` to AI clients, hiding 100+ backend tools from the context window until dynamically queried.
- **Namespaced Routing**: Backend tools are automatically namespaced as `<serverId>__<toolName>` (e.g. `docker__restart_container` or `homeassistant__turn_off`).
- **Pluggable Security**: Supports Active Directory / LDAP Windows Principal resolution, OIDC header authentication (`Remote-User`, `Remote-Groups`), AppKeys, and HashiCorp Vault.
