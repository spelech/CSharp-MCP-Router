# 📚 Model Context Gateway (MCG) - Official User Guide & Manual

Welcome to the official user manual for the **Model Context Protocol (MCP) Gateway Router**. This router acts as a unified control plane, semantic proxy, and security gateway aggregating 100+ backend MCP services, optimizing tool calling for AI agents, and enforcing enterprise RBAC.

---

## 🗺️ User Guide Sitemap

1. [**01. Dashboard & Navigation Interface**](01-dashboard-and-navigation.md)
   - Layout, primary navigation tabs (`Overview`, `App Keys & Security`, `Test Bench`, `Settings`, `My MCP Servers`), real-time stats cards, search filters, sorting, category/status grouping, and pagination.
2. [**02. Server Management & Secret Providers**](02-server-management-and-secrets.md)
   - Registering backend MCP servers across transports (`SSE`, `HTTP`, `STDIO`), inspect modal schemas, custom JSON specifications, and configuring secret resolution strategies:
     - Direct Static Keys
     - Host Environment Variables (`ENV:KEY`)
     - HashiCorp Vault (KV v2 engine, AppRole, JIT token renewal)
     - Windows Registry (DPAPI decryption)
     - OAuth2 / OIDC Token Exchange (RFC 8693)
3. [**03. RBAC, Security & Policies**](03-rbac-and-security.md)
   - 4-Stage Authorization Pipeline (`Explicit Deny` > `Explicit Allow` > `AppKey Scope` > `Default Policy`), Identity Providers (OIDC headers, Active Directory Windows SIDs, AppKeys, Standalone CIDR allowlists, OAuth2), user quota limits, and group mappings.
4. [**04. Client Setup & App Key Management**](04-client-setup-and-app-keys.md)
   - Generating cryptographically hashed AppKeys, scope grammar (`*`, `category:*`, `server:*`, granular capabilities), dynamic client setup generator, and integration guides for Cursor IDE, Claude Desktop, Antigravity CLI, VS Code Cline, and TypeScript/Python SDKs.
5. [**05. Interactive Test Bench**](05-interactive-test-bench.md)
   - Interactive developer playground: Tool Execution Tester (dynamic JSON schema form builder & raw JSON editor), Virtual Resource Tester (`mcp://...`), Prompt Template Tester, Semantic Router Simulator (`search_tools`), direct JSON-RPC Console, and live gateway terminal logs.
6. [**06. System Settings & Vector Embeddings**](06-settings-and-embeddings.md)
   - Multi-tab configuration plane: Vector & Search (Local ONNX `All-MiniLM-L6-v2` vs OpenAI/Ollama API), Identity & Auth, Secret Providers, Prompts & Resources File Manager, and Access Control matrices.

---

## 💡 Core Architecture & Concepts

- **Meta-Mode (`/sse?meta=true`)**: Exposes only 2 bootstrap tools (`search_tools` and `execute_tool`) to prevent context window bloat and tool confusion.
- **Namespaced Tool Routing**: Backend tools are automatically namespaced as `<serverId>__<toolName>` (e.g. `docker__restart_container` or `homeassistant__turn_off`).
- **Zero CLI Secret Leakage**: STDIO subprocesses receive credentials strictly via process environment dictionaries, never exposed in command-line arguments.
- **AES-256-GCM Envelope Encryption**: All sensitive tokens, API keys, and provider secrets are encrypted at rest with authenticated 128-bit GCM tags.
- **Multi-Database Support**: Seamless operation across SQLite (SQLCipher), Microsoft SQL Server (`Microsoft.Data.SqlClient`), and MySQL (`MySqlConnector`).
- **Universal Admin MCP Automation**: Fully automated headless gateway provisioning and hot-reloading via `/admin/sse` or `POST /admin`.

---

## 🧭 Related Technical Documentation

* 📖 [**MCP Server Auth & Integration Cookbook**](../mcp-server-auth-cookbook.md)
* 🎯 [**Evaluation & Product Overview Guide**](../evaluation-guide.md)
* 🏛️ [**Comprehensive Enterprise Architecture Guide**](../architecture.md)
* 🔐 [**Enterprise Secret Providers Guide**](../secret-providers.md)
* 🔑 [**AppKey Scopes & Authorization Guide**](../appkey-scopes.md)
* 🚀 [**Transport Capability & Configuration Guide**](../transports.md)
* 🗄️ [**Database Provider Support & Deployment Matrix**](../database-providers.md)
* 💻 [**Developer & Contributing Guide**](../developer-guide.md)
* 🛠️ [**Operations & Production Runbook**](../runbook.md)
* 🤖 [**Admin MCP Server & Automation Guide**](../admin-mcp-automation-guide.md)

