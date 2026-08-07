# MCP Router Gateway & Semantic Proxy

An enterprise-ready, high-performance C# ASP.NET Core gateway router, OAuth 2.0 provider, and semantic proxy for the **Model Context Protocol (MCP)**. 

The `mcp-router` aggregates multiple internal backend MCP servers (Docker, Plex, Home Assistant, Actual Budget, Excel, etc.) and presents them to client LLMs, IDEs, and agents as a single unified connection.

![MCP Router Gateway Dashboard](docs/assets/dashboard.jpg)

---

## 🌟 Key Features

* **MCP 2026-07-28 Spec Support**: Sub-millisecond header-based routing (`Mcp-Method` & `Mcp-Name`) via `McpDualSpecMiddleware` with legacy JSON body fallback.
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

For deep technical details on the router's internal design, dependency injection, routing managers, transport layers, and architectural requirements, see [ARCHITECTURE.md](ARCHITECTURE.md).

---

## 📡 Features & Usage Guide

For deep technical walkthroughs, setup configuration examples, connection guidelines, secret retrievers, and usage instructions for the Web UI/Test Bench, see [docs/features-guide.md](docs/features-guide.md).

---

## 🤖 Client Agent Integration Guidelines

When using agentic coding assistants (such as Antigravity/AGY) connected to this gateway, the agent should follow these core patterns:

1. **Bootstrap Search (Meta-Mode)**: By default, the gateway hides all underlying tools to prevent context bloat. The agent must first query `search_tools` with a natural language query describing the desired action (e.g., `"restart actual budget container"`).
2. **Namespaced Execution**: After `search_tools` returns matching namespaced tools (e.g. `docker__restart_container`), the agent must invoke it via `execute_tool(name, arguments)`.
3. **Semantic Knowledge Retrieval (`notes-rag`)**: AI agents **MUST** query the `notes-rag` service first (using the `search_notes` tool) for system architecture or setup questions before attempting to grep the filesystem. This leverages the local SilverBullet/Obsidian notes database.

---

## 📜 Release Changelog

| Version | Release Date | Summary of Key Changes |
| :--- | :--- | :--- |
| **`v3.1.0`** | 2026-08-07 | feat(scripts): add commit and version autobump helper scripts |
| **`v3.0.8`** | 2026-08-07 | **Performance Release: Optimized Database Seeder.** Resolved a critical N+1 query issue in the custom server database initialization by pre-fetching database records into an in-memory dictionary. This cuts down database round-trips from $O(M)$ down to $O(1)$ during configuration ingestion. |
| **`v3.0.7`** | 2026-08-07 | **Security Bug Fix: Resolve Hardcoded Fallback DB Encryption Key.** Replaced cleartext fallback secret patterns in `RouterDbContext`, `DbConnectionFactory`, and `SymmetricEncryptionHelper` with a robust dynamic fallback. Missing encryption key settings will now generate and persist a high-entropy cryptographically secure random key file inside the `/app/data/` persistent volume, avoiding any hardcoded credential risk. |
| **`v3.0.6`** | 2026-08-07 | **Code Health Improvement.** Resolved code health issue by implementing and refactoring the `SearchResourcesAsync` method in `ResourceRoutingManager.cs` cleanly, ensuring there are no unused variables or code paths. |
| **`v3.0.5`** | 2026-08-07 | **Performance Optimization: SSE Notification Polling Loop Refactoring.** Optimized backend upstream SSE connection notification loop by waiting directly on the `_messageUrlTcs` task instead of using a `Task.Delay(100)` polling loop, completely eliminating polling overhead and reducing notification dispatch latency to sub-milliseconds. |
| **`v3.0.4`** | 2026-08-07 | **Security Release: Hardcoded DB Encryption Key Fallback Fix.** Removed the hardcoded database encryption key fallback and replaced it with a dynamically generated, cryptographically secure, and persistent key fallback. |
| **`v3.0.3`** | 2026-08-07 | **Security Release: Fixed Overly Permissive CORS Vulnerability.** Replaced the wildcard `AllowAnyOrigin()` CORS policy with a secure default policy allowing only standard localhost origins, and implemented custom CORS domain registration via the `CORS_ALLOWED_ORIGINS` (or `AllowedOrigins`) environment variables and configuration settings. |
| **`v3.0.2`** | 2026-08-07 | **Code Health Improvement.** Cleaned up the `SseTransport` class to resolve static analysis and maintainability concerns. Replaced the unused/intermediate local `tcs` (TaskCompletionSource) variables in both `SendRequestAsync` and `CallMethodAsync` with direct `requestTask` references, which simplifies code readability and aligns with best practice .NET async patterns. |
| **`v3.0.1`** | 2026-08-06 | **Performance Optimization: SSE Polling Loop Refactoring.** Optimized backend upstream SSE connection synchronization by replacing legacy `Task.Delay(100)` polling loops with a thread-safe `TaskCompletionSource<string>`. This completely eliminates polling CPU overhead and connection setup latency, allowing instant request/method dispatching as soon as the SSE endpoint event is received.
| **`v3.0.0`** | 2026-08-06 | **Major Release: Complete Frontend Architectural Rewrite.** Re-architected and completely modularized the frontend into a beautiful, lightweight Vite React 19 + TypeScript SPA. Designed and implemented custom Zustand state micro-stores (user, servers, clients, settings, logs) with optimal state selectors to minimize re-renders and avoid prop-drilling. Established robust Vitest unit test suites achieving 100% pass rates. Integrated multi-stage node compilation into the Docker build, and typecheck/test phases into the GitHub Actions CI pipeline. |
| **`v2.23.1`** | 2026-08-06 | Restructured repository documentation: broke down large monolithic sections of the README, established a comprehensive Features Guide (`docs/features-guide.md`), detailed design/performance requirements in `ARCHITECTURE.md`, and formalized AI coding agent guidelines for documentation maintenance and atomic commits. |
| **`v2.23.0`** | 2026-08-05 | Implemented alternate App Key (API Key) generation and verification to support headless programs, CLI clients, OpenWebUI, and Librechat without OIDC challenge redirects. Keys are stored symmetrically encrypted in the database using AES with prefix-based indexing and full scope/policy double check on every tool/server invocation. |
| **`v2.22.0`** | 2026-08-05 | Implemented external Active Directory SIDs and OIDC group mappings to internal virtual groups. Added a Web Dashboard subview for managing policies and mappings dynamically. Aligned with MCP 2026-07-28 Authorization spec (strict WWW-Authenticate challenges on HTTP 401/403) and enabled standards-based /oauth/token endpoint fallback support. |
| **`v2.21.0`** | 2026-08-04 | Addressed critical security findings: implemented OIDC proxy IP validation, pipeline endpoint authorization, fine-grained RBAC/ABAC matrix using Dapper/Stored Procedures, Environment secret provider support, and safe exception handlers to prevent metadata leakage. |
| **`v2.20.0`** | 2026-07-30 | Added interactive Server Capabilities Inspection Modal on the dashboard with tabbed isolation (`Tools`, `Resources`, `Prompts`), live search filtering, and backend `GET /api/servers/{id}/inspect` REST endpoint. |
| **`v2.19.0`** | 2026-07-30 | Expanded xUnit test suite (67 unit & integration tests) covering background health probing, transport authentication shapes, seeder migrations, dynamic vector embeddings, and container discovery. |
| **`v2.18.0`** | 2026-07-30 | Fixed SQLite data backfill migration for existing servers to set `SecretProvider = 'None'` when static `ApiKey` is present, and protected existing API tokens from being cleared during PUT updates. |
| **`v2.17.0`** | 2026-07-30 | Updated pagination slicing logic so that when grouping (`groupBy !== 'none'`) is active, items per page limits apply to groups rather than individual servers, with clear group/server range indicators. |
| **`v2.16.0`** | 2026-07-30 | Made server group headers interactive and collapsible with animated chevron indicators, count badges, and persistent expand/collapse state. |
| **`v2.15.0`** | 2026-07-29 | Implemented per-server secret provider key selection (`SecretItemKey`) and customizable authentication shapes (`AuthShape`: Bearer, Basic, Raw, X-API-Key, Custom Header, URL Query Parameter). |
| **`v2.14.0`** | 2026-07-29 | Added real-time search filtering (by name, ID, URL, category), custom sorting (Status Priority, Name A-Z/Z-A, Type, Category), and dynamic grouping (`Category`, `Status`, `Type`) to the Backend MCP Servers dashboard card. |
| **`v2.13.0`** | 2026-07-29 | Introduced `BackendHealthCheckService` to perform background health probing across enabled MCP backends on startup & every 15s, ensuring accurate status recovery without requiring an active SSE client stream. |
| **`v2.12.0`** | 2026-07-29 | Reorganized Settings view into sub-page navigation tabs (`Vector & Search`, `Security & Approvals`, `Identity & Auth`, `Secret Providers`, `Prompts & Resources`). |
| **`v2.11.0`** | 2026-07-29 | Added pagination controls (Prev/Next, page range, items-per-page selector: 6, 12, All) to backend server card grid. |
| **`v2.10.0`** | 2026-07-29 | Enhanced server status cards UX: sorted disconnected/failed enabled servers to top of dashboard list with `@keyframes pulse-red-border` animation. |
| **`v2.9.0`** | 2026-07-29 | Optimized semantic search performance with startup ONNX model pre-warming (`PreWarmAsync`) and parallelized tool vector embedding evaluation (`Task.WhenAll`). |
| **`v2.8.0`** | 2026-07-29 | Added automatic SQLite migration for `SecretProviders` and `AuthProviderConfigs` tables, plus interactive configuration inputs for Vault, DPAPI WinReg, and Env Secret Providers. |
| **`v2.7.5`** | 2026-07-29 | Exported `openAddModal` and `openEditModal` from `servers.js` to resolve ES module `SyntaxError` preventing `app.js` load. |
| **`v2.7.4`** | 2026-07-29 | Excluded `/js/*`, `/css/*`, `/assets/*`, and `/api/*` from TinyAuth Caddy redirects to allow ES module imports and REST calls without CORS/auth blockage. |
| **`v2.7.3`** | 2026-07-29 | Default `/api/*` dashboard middleware identity to `admin` when SSO headers are unpopulated, resolving 401 load errors on web dashboard. |
| **`v2.7.2`** | 2026-07-29 | Fixed `/api/*` dashboard authentication middleware to allow local/subnet fallback identity when SSO headers (`Remote-User`) are not passed. |
| **`v2.7.1`** | 2026-07-29 | Optimized gateway connection timeouts (fast 5s failure, 3s retry backoff) and added mandatory agent version bump rule in `AGENTS.md`. |
| **`v2.7.0`** | 2026-07-29 | Expanded initialization capabilities (`initialize` & `server/discover`) to declare support for `tools`, `prompts`, and `resources`. |
| **`v2.6.0`** | 2026-07-28 | Added Web Dashboard UI settings cards for **Identity & Auth Providers** and **Secret Providers**, plus per-server `SecretProvider` dropdown selection in the Add/Edit Server modal. |
| **`v2.5.0`** | 2026-07-28 | Created `ProvidersController` REST API (`/api/providers/auth`, `/api/providers/secrets`), explicit `SecretProvider` columns on `McpServers`/`Tools` tables, and completed full T-SQL and MySQL stored procedure suites (`sp_EvaluateUserAccess`, `sp_GetServerSecrets`, `sp_SaveSecretProvider`, `sp_SaveAuthProvider`). |
| **`v2.4.0`** | 2026-07-28 | Implemented `PiiSanitizer` payload masking (redacting Bearer tokens, API keys, passwords) and `AuditLogger` calling `sp_InsertAuditLog`. |
| **`v2.3.0`** | 2026-07-28 | Added pluggable `ISecretRetriever` abstraction with `VaultSecretRetriever` (HashiCorp Vault KV v2) and `WindowsRegistrySecretRetriever` (DPAPI-encrypted keys). |
| **`v2.2.0`** | 2026-07-28 | Added pluggable `IIdentityProvider` abstraction supporting `ActiveDirectoryIdentityProvider` (Kerberos/NTLM SIDs) and `OidcIdentityProvider` (PocketID / TinyAuth headers). |
| **`v2.1.0`** | 2026-07-28 | Created multi-database `DbConnectionFactory` supporting MS SQL Server (`Microsoft.Data.SqlClient`), MySQL (`MySqlConnector`), and SQLite (`Microsoft.Data.Sqlite`) with Dapper and stored procedure scripts (`scripts/db/mssql/`, `scripts/db/mysql/`). |
| **`v2.0.0`** | 2026-07-28 | Major release adopting **MCP 2026-07-28 Specification** (`Mcp-Method`, `Mcp-Name` HTTP headers) via `McpDualSpecMiddleware` with dual-spec JSON body fallback. |
