# MCP Router Gateway & Semantic Proxy

![Version](https://img.shields.io/badge/version-v4.11.0-orange?style=for-the-badge)
![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MCP Spec](https://img.shields.io/badge/MCP%20Spec-2026--07--28-0052CC?style=for-the-badge)
![Tests](https://img.shields.io/badge/tests-515%20passing-2ea44f?style=for-the-badge)
![Docker Ready](https://img.shields.io/badge/docker-ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![React 19](https://img.shields.io/badge/frontend-Vite%20React%2019-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=for-the-badge)

An enterprise-ready, high-performance C# ASP.NET Core gateway router, OAuth 2.0 provider, and semantic proxy for the **Model Context Protocol (MCP)**. 

The `mcp-router` aggregates multiple internal backend MCP servers (Docker, Plex, Home Assistant, Actual Budget, Excel, etc.) and presents them to client LLMs, IDEs, and agents as a single unified connection.

![MCP Router Gateway Dashboard](docs/assets/dashboard.jpg)

---

## 🌟 Key Features

* **MCP 2026-07-28 Spec Support**: Spec-compliant header annotation; routing is body/path based (`Mcp-Method` & `Mcp-Name`) via `McpDualSpecMiddleware` with legacy JSON body fallback.
* **Pluggable Identity Providers**: Dual authentication support for **Active Directory** (Kerberos/NTLM Windows SIDs) and **PocketID / TinyAuth OIDC** (`Remote-User`, `Remote-Groups` headers).
* **Pluggable Secret Retrievers**: Fetch downstream server API keys and tokens dynamically from **HashiCorp Vault (KV v2)**, **Windows Registry (DPAPI)**, or **Environment Variables** per server (`SecretProvider` column).
* **Multi-Database & Stored Procedure Engine**: Complete stored procedure suites for **MS SQL Server** (`Microsoft.Data.SqlClient`), **MySQL** (`MySqlConnector`), and **SQLite** (`Microsoft.Data.Sqlite`) using Dapper.
* **Observability & PII Audit Logging**: Automatic payload redaction of Bearer tokens, API keys, and passwords (`PiiSanitizer`) paired with stored procedure audit logging (`sp_InsertAuditLog`).
* **Consolidated Tools Gateway:** Merges 300+ tools from dozens of isolated backend servers into a single endpoint.
* **Meta-Mode Dynamic Tool Filtering:** 
  * Defaults to Meta-Mode on the main `/sse` connection path to prevent context window bloat and tool confusion.
  * Instantly returns only two bootstrap tools: `search_tools` and `execute_tool`.
  * Asynchronously warms backend caches in the background using a thread-safe, single-execution initialization lock.
  * Performs semantic scoring and ranking of backend tools on-demand when `search_tools` is called.
* **Dual-Provider Semantic Search**:
  * **Local ONNX (In-Process)**: CPU-friendly vector embeddings using a local `all-MiniLM-L6-v2` model and `Microsoft.ML.Tokenizers` (no external APIs). Automatically downloads model/vocab files into persistent volumes.
  * **API Provider**: OpenAI-compatible embedding calls (LiteLLM, Open WebUI, OpenAI, etc.).
  * **Secure DB Storage**: Embedding configurations and API keys are stored securely inside the SQLCipher-encrypted SQLite database.
* **Developer Test Bench & Dashboard**: 
  * **Interactive UI**: Form builder renders interactive input controls directly from tools' JSON schema specs.
  * **Logs Console**: Styled real-time terminal rendering thread-safe in-memory gateway logs.
  * **Search Simulator**: Real-time evaluation panel for intent ranking.
  * **Provider Management Controls**: Interactive UI cards in Settings to toggle and configure Auth and Secret providers.
* **Target-Specific Proxying:** Exposes separate endpoints (`/{targetServerId}`) to route directly to specific backends (e.g., `/plex`, `/docker`).
* **OAuth 2.0 Security & CORS Config:** Integrates a lightweight OAuth 2.0 authorization server for secure API access. Leverages strict, configurable CORS protection with `CORS_ALLOWED_ORIGINS` to prevent cross-origin request hijacking / forgery vulnerabilities.
* **Built-in Web Dashboard:** A responsive, dark-mode, glassmorphic UI to monitor connected clients, stats, and backend health status.

---

## 🏗️ Architecture, Requirements & Connection Flow

For deep technical details on the router's internal design, dependency injection, routing managers, transport layers, and architectural requirements, see [ARCHITECTURE.md](ARCHITECTURE.md) and the [Pairwise Testing Matrix](docs/testing-matrix.md).

---

## 📖 Official User Guide & Manual

For step-by-step UI guides, server registration, secret provider configuration (Env, HashiCorp Vault, Registry), RBAC group policies, client setup snippets, and interactive test bench operations, see the [**Official User Guide Suite**](docs/user-guide/README.md).

---

## 📡 Features & Usage Guide

For deep technical walkthroughs, setup configuration examples, connection guidelines, secret retrievers, and usage instructions for the Web UI/Test Bench, see [docs/features-guide.md](docs/features-guide.md).

---

## 🤖 Client Agent Integration Guidelines

When using agentic coding assistants (such as Antigravity/AGY) connected to this gateway, the agent should follow these core patterns:

1. **Bootstrap Search (Meta-Mode)**: By default, the gateway hides all underlying tools to prevent context bloat. The agent must first query `search_tools` with a natural language query describing the desired action (e.g., `"restart actual budget container"`).
2. **Namespaced Execution**: After `search_tools` returns matching namespaced tools (e.g. `docker__restart_container`), the agent must invoke it via `execute_tool(name, arguments)`.
3. **Semantic Knowledge Retrieval (`notes-rag`)**: AI agents **MUST** query the `notes-rag` service first (using the `search_notes` tool) for system architecture or setup questions before attempting to grep the filesystem. This leverages the local SilverBullet/Obsidian notes database.

## 📜 Release Changelog

For complete release history and version logs, see [**CHANGELOG.md**](CHANGELOG.md).

| Version | Release Date | Summary of Key Changes |
| :--- | :--- | :--- |
| **`v4.11.0`** | 2026-08-14 | feat(testing): build pairwise integration matrix theories across auth, roles, scopes, and capabilities (#50) |
| **`v4.10.0`** | 2026-08-14 | feat(core): complete Sprint 2 merge — provider settings encryption & dynamic reload (#44), unified MCP capability authorization (#45), and category-scoped AppKeys (#46) |
| **`v4.9.0`** | 2026-08-14 | feat(core): complete Sprint 1 merge — database schema alignment, STDIO transport, AppKey security hardening, SSE concurrency isolation, and lint/build baseline |
| **`v4.7.0`** | 2026-08-12 | feat(diagnostics): add diagnostics API and soak test suite |
| **`v4.6.0`** | 2026-08-12 | feat(identity): implement cross-platform Active Directory SID resolution via LDAP |

---

## 🧪 Code Coverage & Quality Gate

Our core modules maintain high code coverage to ensure reliability and secure execution. For the full breakdown and module-specific metrics, please see the [Detailed Coverage Report](docs/coverage-report.md).

| Module | Line Coverage | Branch Coverage | Status |
| :--- | :--- | :--- | :--- |
| **Core Session** | 92.4% | 88.1% | 🟢 Passing |
| **Routing Engine** | 89.7% | 85.3% | 🟢 Passing |
| **Controllers** | 94.2% | 91.0% | 🟢 Passing |
| **Security & Providers** | 98.5% | 95.8% | 🟢 Passing |

---

## 🛠️ Contributor Guide: Formatting & Linting

To maintain consistent formatting and catch potential bugs early, this repository enforces shared style guidelines and static analysis policies.

### C# Backend (Roslyn & .NET Analyzers)
- **EditorConfig**: Supported globally across C#, TSX, JSON, and YAML. Indentation is 4 spaces for C# and 2 spaces for web files.
- **Analysis Policy**: Rules are configured via `Directory.Build.props` at the workspace root, applying implicit usings, nullable context, deterministic builds, and latest-recommended Roslyn analyzers.
- **Verification Command**:
  ```bash
  dotnet format McpRouter.slnx --verify-no-changes
  ```

### TypeScript / React Frontend (ESLint Flat Config)
- **ESLint v10**: Managed via flat configuration (`frontend/eslint.config.js`) supporting React 19, TypeScript-ESLint, and React Hooks/Refresh checks.
- **Verification Command**:
  ```bash
  cd frontend
  npm run lint
  ```
