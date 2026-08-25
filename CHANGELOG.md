# 📜 Release Changelog

All notable changes to the **MCP Router Gateway & Semantic Proxy** are documented in this file.

For summary details and quick references, see [README.md](README.md).

---

| Version | Release Date | Summary of Key Changes |
| :--- | :--- | :--- |
| **`v4.35.0`** | 2026-08-24 | feat(security): compact Base62 AppKeys with semantic prefix taxonomy (`mcp-adm-`, `mcp-glb-`, `mcp-{domain}-`, `mcp-usr-`, `mcp-srv-`), custom `ROUTER_ADMIN_KEY` seeding, Master Key `KeySource` tracking, Vault bootstrapping, and Web UI dynamic re-encryption |
| **`v4.34.4`** | 2026-08-24 | chore(formatting): standardize C# formatting with `.editorconfig`, enforce CI `dotnet format --verify-no-changes` quality gate, and codebase housekeeping |
| **`v4.34.3`** | 2026-08-24 | chore(hygiene): repository cleanup removing straggler patch scripts, subagent artifacts, and updating `.gitignore` with report/test output patterns |
| **`v4.34.2`** | 2026-08-24 | docs(auth): introduce comprehensive MCP Server Authentication & Integration Cookbook (`docs/mcp-server-auth-cookbook.md`) with 11 scenario-driven recipes, decision matrix, and copy-paste recipes for Bearer, Custom Headers, Basic Auth, Query Params, STDIO, Vault, DPAPI, BYOK, Pass-Through, and Identity-Forwarding |
| **`v4.34.1`** | 2026-08-24 | docs(user-guide): comprehensive overhaul and cleanup of user guides, removing outdated manual approval references, synchronizing Settings tabs, documenting JSON-RPC console, user quotas, and My MCP Servers |
| **`v4.34.0`** | 2026-08-23 | feat(skills): introduce universal `mcp-router-admin` automation skill, blank-slate safe defaults documentation, provider scaffolding templates (Authentik, Keycloak, Entra ID, Active Directory LDAPS, Cloudflare, Vault, Embeddings), and comprehensive DevOps automation guide |
| **`v4.33.0`** | 2026-08-22 | feat(testing): integrate playwright-layout-inspector for spatial layout auditing, eliminate background DOM collisions, enhance interactive touch targets and focus rings |
| **`v4.32.0`** | 2026-08-22 | feat(ui): dynamic multi-brand logo resolution (favicons, manifests, meta icons) and centered page layout containerization across all dashboard tabs |
| **`v4.31.0`** | 2026-08-22 | feat(frontend): implement dynamic multi-target client connection guide |
| **`v4.30.0`** | 2026-08-22 | feat(ui): comprehensive aesthetic UI overhaul implementing stark monochrome mode, vibrant neon green/orange dark mode accents, cyan/blue light mode accents, and mobile responsive flex refactoring for toolbar dropdowns and stat grids |
| **`v4.29.0`** | 2026-08-22 | Security fixes: resolved CodeQL alerts for log forging, path injection, cleartext sensitive info storage, and missing X-Frame-Options headers. |
| **`v4.28.0`** | 2026-08-22 | chore(repo): large repository health sweep refactoring backend, frontend, auth, secrets, CI, and rewriting all documentation guides |
| **`v4.27.2`** | 2026-08-22 | refactor(reqs): normalize requirement taxonomy IDs across all C#, Vitest, and Playwright test suites to eliminate `REQ-` prefixes and strictly enforce standard category codes (`AUTH-`, `UI-`, `DB-`, `GUARD-`) |
| **`v4.27.1`** | 2026-08-22 | test(e2e): add comprehensive Playwright E2E test suites for self-service personal AppKeys, personal quota limits, and admin custom user quota overrides |
| **`v4.27.0`** | 2026-08-22 | feat(auth): self-service personal AppKeys, App-Level keys separation, UserQuotas table & management endpoints, and role-adaptive frontend UI |
| **`v4.26.1`** | 2026-08-22 | feat(ui): implement accessible dark-mode ConfirmModal with useConfirmStore and migrate all destructive window.confirm dialogs across settings and server management to custom modal with toast notifications |
| **`v4.26.0`** | 2026-08-22 | feat(testing): containerized multi-service E2E testing stack (OpenLDAP, Vault, MySQL, Mock MCP), comprehensive Playwright E2E suites (24 proofs, 100% pass), OAuth consent SPA routing fixes, and automated live user guide screenshots |
| **`v4.25.0`** | 2026-08-22 | feat(appkeys): removed LiteLLM branding from AppKeys UI/docs and enabled unlimited AppKeys by default (`0` = Unlimited) |
| **`v4.24.0`** | 2026-08-22 | feat(auth): added native React UI for OAuth Consent Screen, and enabled `refresh_token` flows for multi-tenant clients |
| **`v4.23.0`** | 2026-08-22 | feat(auth): added interactive Per-User OAuth Consent Screen to support true multi-tenant scenarios via authorization_code flows |
| **`v4.22.2`** | 2026-08-21 | patch |
| **`v4.22.1`** | 2026-08-21 | 4.22.0 |
| **`v4.20.3`** | 2026-08-21 | fix: resolve typescript ERESOLVE in frontend dependencies |
| **`v4.20.2`** | 2026-08-21 | chore(deps): bump dependencies (npm, nuget, github-actions) and fix SQLCipher version conflict |
| **`v4.20.1`** | 2026-08-21 | fix(e2e): fix backend DI scope and SQLite schema syntax errors, and add comprehensive Playwright E2E coverage for User Credentials across Vault and SQLite |
| **`v4.20.0`** | 2026-08-20 | feat(auth): User-Specific MCP Server Authentication - Added `UserProvided` secret provider to allow users to set personal auth for target MCP servers via a new self-service portal (`/my-mcp-servers`). Supported across DB and Vault. |
| **`v4.19.1`** | 2026-08-18 | feat(skills): introduce universal `mcp-router-setup` agentic skill, interactive setup workflow, bundled scaffold templates for Docker Compose & Windows IIS, and zero-clone setup documentation |
| **`v4.19.0`** | 2026-08-18 | feat(admin): implement in-process virtual Admin MCP Server (`/admin`, `/router-admin`), 10 consolidated management tools (`manage_servers`, `manage_appkeys`, `manage_clients`, `manage_policies`, `manage_group_mappings`, `manage_providers`, `manage_settings`, `manage_custom_files`, `manage_system`, `test_tool_call`), standalone hybrid network authorization, and comprehensive audit logging |
| **`v4.18.2`** | 2026-08-18 | refactor(reqs): normalize requirement taxonomy IDs across test suites and regenerate living SRS catalog |
| **`v4.18.1`** | 2026-08-18 | fix(ci): fix frontend test assertions, preserve provider display names, and update test catalog |
| **`v4.18.0`** | 2026-08-18 | feat(ui): implement dynamic DB-backed dashboard branding customization and CSS variable centralization |
| **`v4.17.6`** | 2026-08-17 | docs(refactor): refine documentation for conciseness and add pitch deck |
| **`v4.17.5`** | 2026-08-17 | chore(release): bump version to 4.17.5 and align verification badges |
| **`v4.17.4`** | 2026-08-17 | test(coverage): close frontend unit, Playwright E2E, and live MySQL repository coverage gaps |
| **`v4.17.3`** | 2026-08-17 | test(reqs): add formal requirement annotations for SEC-04 DPAPI and AUTH-04 Windows Identity |
| **`v4.17.2`** | 2026-08-17 | docs(coverage): update test coverage report and evaluation with Windows IIS and DPAPI validation findings |
| **`v4.17.1`** | 2026-08-17 | fix(windows): resolve IIS ANCM in-process web.config schema, default registry secret path, and deployment scripts |
| **`v4.17.0`** | 2026-08-16 | feat(windows): add production Windows IIS deployment scripts, Windows Service runner, environment diagnostic validation suite, and comprehensive Windows deployment guide |
| **`v4.16.1`** | 2026-08-16 | refactor(auth): genericize OIDC and SSO identity references, sanitize seed personas, and standardize header provider UI naming |
| **`v4.16.0`** | 2026-08-15 | feat(auth): enable hybrid OIDC & Active Directory admin authorization with multi-group support, fallback resolution, and CIDR-aware trusted proxy parsing |
| **`v4.15.0`** | 2026-08-15 | feat(testing): implement automated Living Software Requirements Specification (SRS) & Test Catalog generator with bidirectional test-to-requirement traceability and CI verification quality gate |
| **`v4.14.0`** | 2026-08-14 | refactor(router): decouple media tools to standalone server, decommission approvals, and add Windows test abstractions |
| **`v4.13.0`** | 2026-08-14 | feat(release): complete Stage 3 — unified product, user, developer, and operations documentation journey (#58), and automated release verification engine with CI version consistency quality gate (#59) |
| **`v4.12.2`** | 2026-08-14 | docs: comprehensive enterprise architecture guide with system context, component models, sequence diagrams, 4-stage authorization, transports, database ERD, and envelope encryption (#57) |
| **`v4.12.1`** | 2026-08-14 | docs: complete Stage 1 documentation — database-provider support matrix (#53), canonical AppKey scope and authorization guide (#54), transport capability & STDIO lifecycle guide (#55), and secret-provider security reference (#56) |
| **`v4.12.0`** | 2026-08-14 | refactor(architecture): complete Sprint 4 merge — modularize backend into `Components/` and `Infrastructure/` domain boundaries with decomposed endpoint mappers (#51), and refactor frontend into domain `components/` with typed API layer and modular settings tabs (#52) |
| **`v4.11.0`** | 2026-08-14 | feat(testing): complete Sprint 3 merge — frontend unit/component test suite (#48), pull-request CI quality gates & security scanning (#49), and pairwise integration matrix with multi-user E2E fixtures (#50) |
| **`v4.10.0`** | 2026-08-14 | feat(core): complete Sprint 2 merge — provider settings encryption & dynamic reload (#44), unified MCP capability authorization (#45), and category-scoped AppKeys (#46) |
| **`v4.9.0`** | 2026-08-14 | feat(core): complete Sprint 1 merge — database schema alignment, STDIO transport, AppKey security hardening, SSE concurrency isolation, and lint/build baseline |
| **`v4.7.0`** | 2026-08-12 | feat(diagnostics): add diagnostics API and soak test suite |
| **`v4.6.0`** | 2026-08-12 | feat(identity): implement cross-platform Active Directory SID resolution via LDAP |
| **`v4.5.9`** | 2026-08-12 | fix(keys): target user SID resolution for admin-minted app keys |
| **`v4.5.8`** | 2026-08-12 | fix(vault): support VAULT_TOKEN fallback for dev testing and init test secrets via docker-compose |
| **`v4.5.7`** | 2026-08-12 | fix(vault): support VAULT_TOKEN fallback for dev testing and init test secrets via docker-compose |
| **`v4.5.6`** | 2026-08-10 | fix(auth): make gateway admin SID-only and fail-closed on missing OpenIddict prod certs |
| **`v4.5.5`** | 2026-08-09 | docs(hygiene): fix test/coverage badges, header-routing claim, phantom type refs |
| **`v4.5.4`** | 2026-08-09 | fix(layout): lock body to block centering and header to flex-wrap nowrap to prevent tab navigation width shifts |
| **`v4.5.3`** | 2026-08-09 | fix(build): add SecurityView.tsx component and asset screenshots to git tracking |
| **`v4.5.2`** | 2026-08-09 | fix(ui): remove RegisteredClientsCard from Overview and stabilize tab layout navigation width |
| **`v4.5.1`** | 2026-08-09 | fix(ui): remove RegisteredClientsCard from Overview and stabilize tab layout navigation width |
| **`v4.5.0`** | 2026-08-09 | feat(ui): add dedicated App Keys & Security tab with LiteLLM-style key management |
| **`v4.4.2`** | 2026-08-09 | docs(plan): add implementation plan for dedicated App Keys & Security tab |
| **`v4.4.1`** | 2026-08-09 | docs(spec): add design spec for dedicated App Keys & Security management tab |
| **`v4.4.0`** | 2026-08-08 | feat(auth): add X-App-Key header extraction and default admin AppKey seeder |
| **`v4.3.0`** | 2026-08-08 | feat(ui): implement interactive multi-target client setup guide generator |
| **`v4.2.19`** | 2026-08-08 | docs(license): track and commit official Apache License 2.0 file |
| **`v4.2.18`** | 2026-08-08 | docs(license): add official Apache License 2.0 file and update README badge |
| **`v4.2.17`** | 2026-08-08 | docs(readme): add shield badges to top of README |
| **`v4.2.16`** | 2026-08-08 | docs(rules): update agent rules for CHANGELOG.md extraction |
| **`v4.2.15`** | 2026-08-08 | fix(inspect): pass httpContext to inspect capability listing calls and await backend initialization |
| **`v4.2.14`** | 2026-08-08 | fix(inspect): standardize ROUTER_SECRET encryption derivation and add resilient multi-primitive capabilities inspect |
| **`v4.2.13`** | 2026-08-08 | refactor(db): complete 100% EF Core removal and Dapper refactoring with isolated header identity provider |
| **`v4.2.12`** | 2026-08-08 | fix(discovery): resolve IDbConnectionFactory in DockerAutoDiscoveryService.ScanContainersAsync |
| **`v4.2.11`** | 2026-08-08 | fix(discovery): use IDbConnectionFactory and Dapper for DockerAutoDiscoveryService upserts |
| **`v4.2.10`** | 2026-08-08 | fix(health): refactor BackendHealthCheckService to use IDbConnectionFactory with EF Core fallback |
| **`v4.2.9`** | 2026-08-08 | fix(api): final cleanup of debug logging in OidcHeaderAuthenticationHandler |
| **`v4.2.8`** | 2026-08-08 | fix(api): handle Dapper dynamic long/bool conversions for Enabled/Hidden fields |
| **`v4.2.7`** | 2026-08-08 | fix(api): safely deserialize Categories JSON in GET /api/servers endpoint |
| **`v4.2.6`** | 2026-08-08 | fix(api): refactor GET /api/servers to use Dapper IDbConnectionFactory |
| **`v4.2.5`** | 2026-08-08 | fix(auth): update TrustedProxyHelper to auto-trust Docker container subnets and preserve XFF chain validation |
| **`v4.2.4`** | 2026-08-08 | fix(auth): update AdminPolicy and OidcHeaderAuthenticationHandler to authorize TinyAuth full_admin group |
| **`v4.2.3`** | 2026-08-08 | fix(auth): update certificate fallback logic for production container deployment |
| **`v4.2.2`** | 2026-08-08 | fix(build): update package-lock.json for @playwright/test compatibility in Dockerfile |
| **`v4.2.1`** | 2026-08-08 | chore(release): merge Playwright E2E testing framework and User Guide documentation suite |
| **`v4.2.0`** | 2026-08-08 | feat(e2e): refactor Playwright E2E suite using Page Object Models and data-testid attributes |
| **`v4.1.0`** | 2026-08-08 | feat(testing): add Playwright E2E test suite and comprehensive User Guide documentation suite |
| **`v4.0.25`** | 2026-08-08 | fix(auth): use development encryption and signing certificates when custom certificate path is unconfigured |
| **`v4.0.24`** | 2026-08-08 | fix(seeder): add explicit DDL notice logging for database migration verification |
| **`v4.0.23`** | 2026-08-08 | fix(seeder): add detailed DDL logging and robust column alterations |
| **`v4.0.22`** | 2026-08-08 | docs(plans): add documentation and coverage implementation plan |
| **`v4.0.21`** | 2026-08-08 | docs(architecture): add Mermaid class diagrams for session, interfaces, and routing engine |
| **`v4.0.20`** | 2026-08-08 | docs(xml): add descriptive C# XML docstrings to core services |
| **`v4.0.19`** | 2026-08-08 | docs(xml): enable C# XML documentation generation and annotate core services |
| **`v4.0.18`** | 2026-08-08 | docs(xml): enable C# XML documentation generation and annotate core services |
| **`v4.0.17`** | 2026-08-08 | docs(coverage): add comprehensive code coverage report and README section |
| **`v4.0.16`** | 2026-08-08 | docs(spec): add documentation, coverage report, and class diagrams design spec |
| **`v4.0.15`** | 2026-08-08 | docs(plans): include refactoring plans and extracted tool helpers |
| **`v4.0.14`** | 2026-08-08 | refactor(session): extract ClientSession.ProxyForwarder partial class |
| **`v4.0.13`** | 2026-08-08 | refactor(session): extract ClientSession.BackendInitializer partial class |
| **`v4.0.12`** | 2026-08-08 | refactor(session): extract ClientSession.Authorization partial class |
| **`v4.0.11`** | 2026-08-07 | refactor(resources): extract ResourceCatalogManager and modularize ResourceRoutingManager |
| **`v4.0.10`** | 2026-08-07 | refactor(seeder): extract CatalogDatabaseSeeder and ClientAppKeySeeder helpers |
| **`v4.0.9`** | 2026-08-07 | refactor(endpoints): extract modular endpoint extension classes from ApplicationBuilderExtensions |
| **`v4.0.8`** | 2026-08-07 | refactor(session): extract ClientSession rewriter and notification broadcaster partials |
| **`v4.0.7`** | 2026-08-07 | break apart ToolRoutingManager and expand unit tests for Docker, LDAP, AD, and ONNX |
| **`v4.0.6`** | 2026-08-07 | test(coverage): expand test suites for moderate core modules to reach >= 80% coverage |
| **`v4.0.5`** | 2026-08-07 | test(coverage): expand core service unit test suites and bump version to v4.0.4 |
| **`v4.0.4`** | 2026-08-07 | test(coverage): expand core service unit test suites (+1,500 covered lines, 217 passing tests) |
| **`v4.0.3`** | 2026-08-07 | test(coverage): expand integration test suite and protocol handlers (v4.0.2) |
| **`v4.0.2`** | 2026-08-07 | test(coverage): expand integration test suite & protocol handlers (+1,224 lines covered, 202 tests) |
| **`v4.0.1`** | 2026-08-07 | test(coverage): expand controller, secret retriever, and audit logger unit tests |
| **`v4.0.0`** | 2026-08-07 | feat(security): Remediation Round 3 complete security hardening (R3-1 through R3-11) |
| **`v3.9.7`** | 2026-08-07 | fix(security): resolve Remediation Round 2 P0 vulnerabilities (P0-1 through P0-7) |
| **`v3.9.6`** | 2026-08-07 | fix(security): request-scoped SSE authorization — evaluate identity per-message, not from cached handshake context |
| **`v3.9.5`** | 2026-08-07 | fix(security): make IsBlockedIp strictly fail-closed on null input |
| **`v3.9.4`** | 2026-08-07 | fix(security): socket-level ConnectCallback SSRF validation |
| **`v3.9.3`** | 2026-08-07 | fix(crypto): prevent credential corruption on migration failure and add thread-safe key locking |
| **`v3.9.2`** | 2026-08-07 | fix(crypto): implement AES-GCM, PBKDF2 derivation, and key hashing migration |
| **`v3.9.1`** | 2026-08-07 | fix(secrets): fix database schema and Vault HTTPS enforcement |
| **`v3.9.0`** | 2026-08-07 | feat(secrets): implement per-server Vault integration and JIT renewal |
| **`v3.8.4`** | 2026-08-07 | fix(security): resolve IPv6 mapping and default trust in proxy validation |
| **`v3.8.3`** | 2026-08-07 | fix(security): implement actual trusted proxy header stripping and mapping |
| **`v3.8.2`** | 2026-08-07 | fix(security): strip proxy headers for untrusted remote IPs |
| **`v3.8.1`** | 2026-08-07 | fix(identity): sanitize LDAP search inputs and configure LDAPS secure channel |
| **`v3.8.0`** | 2026-08-07 | feat(identity): implement cross-platform AD LDAP resolution |
| **`v3.7.1`** | 2026-08-07 | fix(security): handle unresolved IAuditLogger and safely parse json request ids |
| **`v3.7.0`** | 2026-08-07 | feat(security): implement fail-closed try-finally logging |
| **`v3.6.2`** | 2026-08-07 | fix(security): resolve production startup crash, fail-closed stored procedures, and backend invocation auditing |
| **`v3.6.1`** | 2026-08-07 | test(security): add fail-closed, SHA-256 app keys hashing, and sequential execution verification tests |
| **`v3.6.0`** | 2026-08-07 | feat(security): implement fail-closed authorization, OIDC proxy default enforcement, session overwrites leak prevention, SHA-256 app keys hashing, and CORS/cert production gating |
| **`v3.5.0`** | 2026-08-07 | feat(security): implement LogBuffer PII sanitization and structured admin audit logging |
| **`v3.4.0`** | 2026-08-07 | feat(security): implement column-level encryption, HTTPS URL validation, connection string password masking, and add DB encryption verification tests |
| **`v3.3.0`** | 2026-08-07 | feat(security): implement strict namespace validation, SSRF private IP blocks, and disable automatic redirects |
| **`v3.2.0`** | 2026-08-07 | feat(security): implement OidcHeader auth handler and secure control-plane endpoints |
| **`v3.1.3`** | 2026-08-07 | fix(hygiene): resolve NuGet vulnerabilities, clean up AD warnings, and enable dynamic frontend Docker building |
| **`v3.1.2`** | 2026-08-07 | chore(scripts): update bump_version.py to sync fallback version in useUserStore.ts |
| **`v3.1.1`** | 2026-08-07 | docs: add design spec for security hardening |
| **`v3.1.0`** | 2026-08-07 | feat(scripts): add commit and version autobump helper scripts |
| **`v3.0.8`** | 2026-08-07 | **Performance Release: Optimized Database Seeder.** Resolved a critical N+1 query issue in the custom server database initialization by pre-fetching database records into an in-memory dictionary. This cuts down database round-trips from $O(M)$ down to $O(1)$ during configuration ingestion. |
| **`v3.0.7`** | 2026-08-07 | **Security Bug Fix: Resolve Hardcoded Fallback DB Encryption Key.** Replaced cleartext fallback secret patterns in `RouterDbContext`, `DbConnectionFactory`, and `SymmetricEncryptionHelper` with a robust dynamic fallback. Missing encryption key settings will now generate and persist a high-entropy cryptographically secure random key file inside the `/app/data/` persistent volume, avoiding any hardcoded credential risk. |
| **`v3.0.6`** | 2026-08-07 | **Code Health Improvement.** Resolved code health issue by implementing and refactoring the `SearchResourcesAsync` method in `ResourceRoutingManager.cs` cleanly, ensuring there are no unused variables or code paths. |
| **`v3.0.5`** | 2026-08-07 | **Performance Optimization: SSE Notification Polling Loop Refactoring.** Optimized backend upstream SSE connection notification loop by waiting directly on the `_messageUrlTcs` task instead of using a `Task.Delay(100)` polling loop, completely eliminating polling overhead and reducing notification dispatch latency to sub-milliseconds. |
| **`v3.0.4`** | 2026-08-07 | **Security Release: Hardcoded DB Encryption Key Fallback Fix.** Removed the hardcoded database encryption key fallback and replaced it with a dynamically generated, cryptographically secure, and persistent key fallback. |
| **`v3.0.3`** | 2026-08-07 | **Security Release: Fixed Overly Permissive CORS Vulnerability.** Replaced the wildcard `AllowAnyOrigin()` CORS policy with a secure default policy allowing only standard localhost origins, and implemented custom CORS domain registration via the `CORS_ALLOWED_ORIGINS` (or `AllowedOrigins`) environment variables and configuration settings. |
| **`v3.0.2`** | 2026-08-07 | **Code Health Improvement.** Cleaned up the `SseTransport` class to resolve static analysis and maintainability concerns. Replaced the unused/intermediate local `tcs` (TaskCompletionSource) variables in both `SendRequestAsync` and `CallMethodAsync` with direct `requestTask` references, which simplifies code readability and aligns with best practice .NET async patterns. |
| **`v3.0.1`** | 2026-08-06 | **Performance Optimization: SSE Polling Loop Refactoring.** Optimized backend upstream SSE connection synchronization by replacing legacy `Task.Delay(100)` polling loops with a thread-safe `TaskCompletionSource<string>`. This completely eliminates polling CPU overhead and connection setup latency, allowing instant request/method dispatching as soon as the SSE endpoint event is received. |
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
