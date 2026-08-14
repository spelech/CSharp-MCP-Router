# 🏛️ MCP Router Enterprise Architecture Guide & System Specification

The **Model Context Protocol (MCP) Router Gateway & Semantic Proxy** is an enterprise-grade C# ASP.NET Core gateway, OAuth 2.0 provider, and high-performance protocol multiplexer. It consolidates hundreds of heterogeneous downstream MCP servers (Docker, Home Assistant, Plex, Actual Budget, SQL databases, filesystem tools, cloud APIs, and local CLI scripts) into a unified, secure, and semantic entry point for LLMs, IDEs, and autonomous coding agents.

This document serves as the **definitive architectural specification**, detailing the end-to-end system context, component boundaries, frontend design, protocol multiplexing, multi-stage authorization, subprocess lifecycles, database persistence models, cryptographic envelope pipelines, and operational characteristics.

---

## 📑 Table of Contents

1. [Executive Summary & Architectural Tenets](#1-executive-summary--architectural-tenets)
2. [High-Level System Architecture & Context](#2-high-level-system-architecture--context)
   - [Client Ecosystem & Ingress](#client-ecosystem--ingress)
   - [7-Layer Gateway Architecture](#7-layer-gateway-architecture)
   - [System Context & Architecture Diagram](#system-context--architecture-diagram)
3. [Backend Component & Boundary Model](#3-backend-component--boundary-model)
   - [Domain Components (`Components/`)](#domain-components-components)
   - [Infrastructure Services (`Infrastructure/`)](#infrastructure-services-infrastructure)
   - [Core Protocol & Routing Engine (`Core/`)](#core-protocol--routing-engine-core)
   - [Architectural Dependency Rules](#architectural-dependency-rules)
   - [Component Subsystem Diagram](#component-subsystem-diagram)
4. [Frontend Component & Typed Architecture](#4-frontend-component--typed-architecture)
   - [React 19 & Vite SPA Architecture](#react-19--vite-spa-architecture)
   - [Domain Component Decomposition](#domain-component-decomposition)
   - [Typed API Layer & Zustand State Stores](#typed-api-layer--zustand-state-stores)
   - [Frontend Architecture & State Flow Diagram](#frontend-architecture--state-flow-diagram)
5. [Protocol & Routing Engine Deep-Dive](#5-protocol--routing-engine-deep-dive)
   - [Meta-Mode Dynamic Capability Hiding (`/sse` & `/message`)](#meta-mode-dynamic-capability-hiding-sse--message)
   - [Target-Specific Virtual Proxying (`/{targetServerId}`)](#target-specific-virtual-proxying-targetserverid)
   - [JSON-RPC 2.0 In-Flight Multiplexing & ID Preservation](#json-rpc-20-in-flight-multiplexing--id-preservation)
   - [Sequence Diagram: Stateful SSE Session Lifecycle](#sequence-diagram-stateful-sse-session-lifecycle)
   - [Sequence Diagram: Stateless HTTP Stream Execution](#sequence-diagram-stateless-http-stream-execution)
6. [Authorization Pipeline & RBAC Decision Engine](#6-authorization-pipeline--rbac-decision-engine)
   - [4-Stage Hierarchical Decision Flow](#4-stage-hierarchical-decision-flow)
   - [Scope Grammar & Resolution](#scope-grammar--resolution)
   - [Admin SID Bypass & Database-Backed RBAC Evaluation](#admin-sid-bypass--database-backed-rbac-evaluation)
   - [Mermaid Authorization Decision Flowchart](#mermaid-authorization-decision-flowchart)
7. [Transport Subsystem & Subprocess Lifecycle](#7-transport-subsystem--subprocess-lifecycle)
   - [Strategy Pattern (`ITransport`)](#strategy-pattern-itransport)
   - [Subprocess STDIO Architecture & Security Hardening](#subprocess-stdio-architecture--security-hardening)
   - [Child Process Tree Lifecycle & Signal Handling](#child-process-tree-lifecycle--signal-handling)
   - [Stderr Log Capture & PII Token Masking](#stderr-log-capture--pii-token-masking)
   - [Sequence Diagram: STDIO Subprocess Execution](#sequence-diagram-stdio-subprocess-execution)
8. [Database & Persistence Architecture](#8-database--persistence-architecture)
   - [Unified Entity-Relationship Diagram (Mermaid ERD)](#unified-entity-relationship-diagram-mermaid-erd)
   - [Engine Dialect Strategies (SQLite, MS SQL Server, MySQL)](#engine-dialect-strategies-sqlite-ms-sql-server-mysql)
   - [In-Process DDL Migrations & Stored Procedure Suites](#in-process-ddl-migrations--stored-procedure-suites)
9. [Secret Provider & Envelope Encryption Pipeline](#9-secret-provider--envelope-encryption-pipeline)
   - [AES-256-GCM Envelope Encryption Specification](#aes-256-gcm-envelope-encryption-specification)
   - [Master Key Derivation (PBKDF2 SHA-256)](#master-key-derivation-pbkdf2-sha-256)
   - [Pluggable Retrievers & Dynamic Reload Without Restart](#pluggable-retrievers--dynamic-reload-without-restart)
   - [Secret Resolution & Encryption Pipeline Flowchart](#secret-resolution--encryption-pipeline-flowchart)
10. [Cross-References, Verification & Operational Guide](#10-cross-references-verification--operational-guide)

---

## 1. Executive Summary & Architectural Tenets

The MCP Router Gateway is engineered to solve the **Context Explosion & Security Fragmentation Problem** inherent in large-scale Model Context Protocol deployments. When an AI agent connects directly to dozens of independent MCP servers, several critical bottlenecks occur:
1. **Context Window Saturation**: Loading tool schemas for 300+ tools exhausts tens of thousands of tokens before user input begins.
2. **Tool Selection Confusion**: Overlapping tool names across independent backends lead to hallucinations and execution errors.
3. **Security & Credential Sprawl**: Plaintext API keys and database credentials scattered across client configurations create severe vulnerability vectors.
4. **Lack of Centralized Audit & Governance**: Enterprise compliance requires unified invocation auditing, caller identity attribution, PII redaction, and fine-grained access control.

To address these challenges, the MCP Router enforces seven **core architectural tenets**:

```
+---------------------------------------------------------------------------------------------------+
|                                 CORE ARCHITECTURAL TENETS                                         |
+---------------------------------------------------------------------------------------------------+
|  1. Sub-Millisecond Routing Decisions                                                             |
|     Header inspection (Mcp-Method, Mcp-Name) and lightweight path resolution allow fast triage    |
|     without buffering large request bodies.                                                       |
|                                                                                                   |
|  2. Zero Token Waste via Meta-Mode                                                                |
|     Default client connections expose only two bootstrap tools: `search_tools` and `execute_tool`.|
|     Target tools are ranked on-demand using local in-process ONNX embeddings or API embeddings.   |
|                                                                                                   |
|  3. Fail-Closed, Multi-Stage RBAC                                                                 |
|     All capability invocations pass through AppKey scope boundaries, identity group mappings,     |
|     and stored procedure RBAC evaluations. Any missing policy or exception results in DENY.      |
|                                                                                                   |
|  4. Strict Isolation & Concurrency Fidelity                                                       |
|     Clients maintain separate session contexts. JSON-RPC request IDs (integer, string, GUID) are  |
|     faithfully preserved while being mapped upstream to prevent collisions in multiplexed streams.|
|                                                                                                   |
|  5. Zero Credential Exposure (Environment-Only Injection)                                         |
|     Downstream secrets are fetched from Vault, Registry DPAPI, or Env, and injected into headers   |
|     or process environments. Credentials are NEVER passed via CLI arguments or logged to disk.    |
|                                                                                                   |
|  6. Pluggable, Strategy-Driven Subsystems                                                         |
|     All major subsystems (`ITransport`, `IIdentityProvider`, `ISecretRetriever`,                  |
|     `IDbConnectionFactory`, `IEmbeddingService`) are decoupled through strategy interfaces.      |
|                                                                                                   |
|  7. Observability & Mandatory PII Masking                                                         |
|     Every client request, downstream execution, and admin modification is logged to audit tables  |
|     after passing through regex-based PII sanitization.                                           |
+---------------------------------------------------------------------------------------------------+
```

---

## 2. High-Level System Architecture & Context

### Client Ecosystem & Ingress

The MCP Router supports a wide array of AI developer tools, autonomous coding agents, and custom clients:

* **Cursor IDE**: Connects over Server-Sent Events (`/sse`) or target proxy routes (`/{serverId}`) using MCP extension settings.
* **Claude Desktop**: Configured via `claude_desktop_config.json` connecting to SSE or local CLI bridges.
* **Antigravity CLI**: Agentic coding assistant connecting to Meta-Mode `/sse` with automatic tool discovery and execution.
* **OpenClaw Agent**: Dedicated autonomous agent host (`10.0.10.10`) communicating over internal networks (`net_cloud`).
* **VS Code / Cline / Roo Code / Continue.dev**: IDE extensions leveraging standard MCP SSE protocols.
* **Custom SDKs & Scripts**: Python (`mcp` library), TypeScript/Node.js, cURL, LangChain, and AutoGen clients communicating over HTTP POST or SSE.

### 7-Layer Gateway Architecture

The router architecture is partitioned into seven distinct, cohesive layers:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                 LAYER 1: INGRESS & EDGE SECURITY                                │
│   Reverse Proxy (Caddy/Nginx) ──► McpDualSpecMiddleware ──► McpAuthorizationSpecMiddleware     │
└────────────────────────────────────────────────┬────────────────────────────────────────────────┘
                                                 │
┌────────────────────────────────────────────────▼────────────────────────────────────────────────┐
│                           LAYER 2: AUTHENTICATION & IDENTITY MAPPING                            │
│   CompositeIdentityProvider ──► ActiveDirectory (LDAP SIDs) ──► OIDC Headers ──► AppKey Auth   │
└────────────────────────────────────────────────┬────────────────────────────────────────────────┘
                                                 │
┌────────────────────────────────────────────────▼────────────────────────────────────────────────┐
│                     LAYER 3: PROTOCOL, SESSION & MULTIPLEXING ENGINE                            │
│   ProxyEndpoints ──► SessionManager ──► ClientSession ──► JsonRpcStateManager & ID Rewriter    │
└────────────────────────────────────────────────┬────────────────────────────────────────────────┘
                                                 │
┌────────────────────────────────────────────────▼────────────────────────────────────────────────┐
│                        LAYER 4: SEMANTIC INTELLIGENCE & META-MODE                               │
│   DynamicEmbeddingService ──► OnnxEmbeddingService (all-MiniLM-L6-v2) ──► SemanticSearchService│
└────────────────────────────────────────────────┬────────────────────────────────────────────────┘
                                                 │
┌────────────────────────────────────────────────▼────────────────────────────────────────────────┐
│                          LAYER 5: AUTHORIZATION & RBAC DECISION ENGINE                          │
│   AppKey Scope Filter ──► Admin SID Bypass ──► Database RBAC (sp_EvaluateUserAccess)            │
└────────────────────────────────────────────────┬────────────────────────────────────────────────┘
                                                 │
┌────────────────────────────────────────────────▼────────────────────────────────────────────────┐
│                         LAYER 6: PERSISTENCE & SECRET RESOLUTION                                │
│   DbConnectionFactory (SQLite/MSSQL/MySQL) ──► CompositeSecretRetriever ──► AES-256-GCM Crypto  │
└────────────────────────────────────────────────┬────────────────────────────────────────────────┘
                                                 │
┌────────────────────────────────────────────────▼────────────────────────────────────────────────┐
│                        LAYER 7: DOWNSTREAM MCP SERVER FLEET & TRANSPORTS                        │
│   SseTransport (Duplex) ──► HttpTransport (Stateless) ──► StdioTransport (Subprocess NDJSON)    │
└─────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### System Context & Architecture Diagram

```mermaid
graph TD
    subgraph Clients ["Client Ecosystem"]
        Cursor["Cursor IDE"]
        Claude["Claude Desktop"]
        Antigravity["Antigravity CLI"]
        OpenClaw["OpenClaw Agent (10.0.10.10)"]
        VSCode["VS Code / Cline"]
        CustomSDK["Custom HTTP/SSE Clients"]
    end

    subgraph Edge ["Layer 1: Ingress & Edge Security"]
        Proxy["Reverse Proxy (Caddy / TinyAuth)"]
        DualSpec["McpDualSpecMiddleware<br>(2026-07-28 Spec & Legacy Body Fallback)"]
        AuthSpec["McpAuthorizationSpecMiddleware<br>(WWW-Authenticate Challenges)"]
    end

    subgraph AuthLayer ["Layer 2: Identity & Authentication"]
        CompositeId["CompositeIdentityProvider"]
        AD["ActiveDirectoryIdentityProvider<br>(LDAP Windows SIDs)"]
        OIDC["OidcIdentityProvider<br>(Remote-User / Remote-Groups)"]
        AppKeyAuth["AppKeyIdentityProvider<br>(KeyPrefix Index & SHA-256)"]
    end

    subgraph CoreEngine ["Layer 3 & 4: Protocol, Routing & Semantic Intelligence"]
        ProxyEp["ProxyEndpoints<br>(/sse, /message, /{serverId})"]
        SessionMgr["SessionManager"]
        CSession["ClientSession (Partial Classes)"]
        StateManager["JsonRpcStateManager<br>(Out-of-Order Demux & ID Rewriter)"]
        SemanticEngine["DynamicEmbeddingService & SemanticSearchService<br>(Local ONNX & API Embeddings)"]
        ApprovalMgr["ToolApprovalManager"]
    end

    subgraph SecurityRbac ["Layer 5: Authorization & Governance"]
        SecHelper["SecurityValidationHelper<br>(Admin SIDs S-1-5-32-544 / CIDR Checks)"]
        RbacProc["sp_EvaluateUserAccess<br>(GroupMappings & AccessPolicies)"]
        AuditSvc["AuditLogger & PiiSanitizer<br>(sp_InsertAuditLog)"]
    end

    subgraph PersistenceLayer ["Layer 6: Persistence & Secret Resolution"]
        DbFactory["DbConnectionFactory<br>(SQLite WAL / MS SQL / MySQL)"]
        SecretComp["CompositeSecretRetriever<br>(5m In-Memory Sliding Cache)"]
        VaultRetriever["VaultSecretRetriever (KV v2)"]
        RegRetriever["WindowsRegistrySecretRetriever (DPAPI)"]
        EnvRetriever["EnvironmentSecretRetriever"]
        CryptoHelper["SymmetricEncryptionHelper<br>(AES-256-GCM Envelope Encryption)"]
    end

    subgraph Fleet ["Layer 7: Downstream MCP Server Fleet"]
        SSETarget["SseTransport<br>(Docker Containers, Remote MCP)"]
        HttpTarget["HttpTransport<br>(Serverless APIs, Webhooks)"]
        StdioTarget["StdioTransport<br>(Local CLI Tools, uvx, npx)"]
        NativeTools["Native In-Process Tools<br>(Plex, Overseerr, Custom)"]
    end

    Clients -->|HTTP / SSE / Bearer Key| Proxy
    Proxy --> DualSpec
    DualSpec --> AuthSpec
    AuthSpec --> CompositeId
    CompositeId --> AD
    CompositeId --> OIDC
    CompositeId --> AppKeyAuth

    AuthSpec --> ProxyEp
    ProxyEp --> SessionMgr
    SessionMgr --> CSession
    CSession --> StateManager
    CSession --> SemanticEngine
    CSession --> ApprovalMgr

    CSession --> SecHelper
    SecHelper --> RbacProc
    RbacProc --> DbFactory
    CSession --> AuditSvc
    AuditSvc --> DbFactory

    CSession --> SecretComp
    SecretComp --> VaultRetriever
    SecretComp --> RegRetriever
    SecretComp --> EnvRetriever
    SecretComp --> CryptoHelper

    CSession --> SSETarget
    CSession --> HttpTarget
    CSession --> StdioTarget
    CSession --> NativeTools
```

---

## 3. Backend Component & Boundary Model

The C# codebase is strictly partitioned into modular bounded contexts: [`Components/`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components), [`Infrastructure/`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure), and [`Core/`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core).

```
├── Components/
│   ├── Servers/         # Upstream server registry, validation, health checks, discovery & MapServerEndpoints
│   ├── Clients/         # Registered clients, credential service, OAuth endpoints & MapClientEndpoints
│   ├── AppKeys/         # AppKey models, authorization keys, hashing, scope checks & MapAppKeyEndpoints
│   ├── Providers/       # Auth/Secret provider settings, envelope crypto & MapProviderEndpoints
│   ├── Authorization/   # Access policies, group mappings, RBAC evaluation & MapPolicyEndpoints
│   └── Capabilities/    # Native tools, virtual proxy execution, tool/prompt/resource handlers & MapCapabilityEndpoints
├── Infrastructure/
│   ├── Persistence/     # Dapper repositories, database connection factory, migrations & seeders
│   ├── Transports/      # SSE, HTTP, STDIO, state manager & target proxy
│   ├── Identity/        # Active Directory, OIDC, AppKey identity providers & LDAP service
│   ├── Secrets/         # Vault, Windows Registry, Environment secret retrievers & encryption
│   └── Logging/         # Audit logger, PII sanitization & in-memory log providers
└── Core/
    ├── Protocol/        # JSON-RPC 2.0 protocol models & polymorphic converter
    └── Routing/         # ClientSession, SessionManager, BackendConnection & Semantic Search
```

### Domain Components (`Components/`)

1. **`Servers`**:
   - [`McpServer.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Servers/McpServer.cs): Data model representing upstream MCP servers, endpoints, transport types, categories, headers, and secret associations.
   - [`ServerEndpoints.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Servers/ServerEndpoints.cs): Minimal API mapping for CRUD operations, inspections, and health states (`GET /api/servers`, `POST /api/servers`, `DELETE /api/servers/{id}`).
   - [`ServerValidationHelper.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Servers/ServerValidationHelper.cs): URL syntax checking, SSRF prevention, and parameter sanitization.
   - [`DockerAutoDiscoveryService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Servers/DockerAutoDiscoveryService.cs): Dynamic label-based container discovery (`mcp.enable=true`, `mcp.name`, `mcp.port`).
   - [`BackendHealthCheckService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Servers/BackendHealthCheckService.cs): Background periodic health prober (15s HTTP GET + 30s JSON-RPC ping).

2. **`Clients`**:
   - [`ClientEndpoints.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Clients/ClientEndpoints.cs) & [`ClientsController.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Clients/ClientsController.cs): Client registration and OAuth client management.
   - [`CredentialService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Clients/CredentialService.cs): Automated client configuration generation (`claude_desktop_config.json`, Cursor config, environment templates).

3. **`AppKeys`**:
   - [`AppKey.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/AppKeys/AppKey.cs) & [`AppKeyModels.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/AppKeys/AppKeyModels.cs): High-entropy machine keys (`mcp-*-*-*`), key prefixes (`KeyPrefix`), AES-256-GCM encrypted keys, scopes (`ScopesJson`), and attribution (`OwnerSid`).
   - [`AppKeyEndpoints.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/AppKeys/AppKeyEndpoints.cs) & [`AppKeysController.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/AppKeys/AppKeysController.cs): Endpoints for key minting, revocation, prefix lookups, and validation.

4. **`Providers`**:
   - [`ProviderEndpoints.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Providers/ProviderEndpoints.cs) & [`ProvidersController.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Providers/ProvidersController.cs): Management of Identity and Secret Provider configurations.
   - [`ProviderConfigSecurityHelper.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Providers/ProviderConfigSecurityHelper.cs): Transparent encryption/decryption of provider JSON configuration payloads.

5. **`Authorization`**:
   - [`PolicyEndpoints.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Authorization/PolicyEndpoints.cs) & [`PermissionsController.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Authorization/PermissionsController.cs): Endpoints for RBAC policies and group mappings.
   - [`SecurityValidationHelper.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Authorization/SecurityValidationHelper.cs): CIDR subnet evaluation, private IP blocking, SSRF prevention, and Administrator SID verification (`S-1-5-32-544`).

6. **`Capabilities`**:
   - [`ProxyEndpoints.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Capabilities/ProxyEndpoints.cs): Core HTTP/SSE entrypoints (`/sse`, `/message`, `/{targetServerId}`).
   - [`CapabilityEndpoints.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Capabilities/CapabilityEndpoints.cs): Tool, prompt, resource, and custom file catalog management.
   - [`CustomToolRegistry.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Capabilities/NativeTools/CustomToolRegistry.cs): In-process native tools for Plex (`PlexGetLibrarySectionsTool`, `PlexSearchLibraryTool`, `PlexGetSessionsTool`, etc.) and Overseerr (`SeerrSearchMediaTool`, `SeerrRequestMediaTool`, `SeerrGetRequestsTool`, etc.).

### Infrastructure Services (`Infrastructure/`)

1. **`Persistence`**:
   - [`DbConnectionFactory.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Persistence/DbConnectionFactory.cs): Multi-database factory supporting SQLite (WAL), MS SQL Server (`Microsoft.Data.SqlClient`), and MySQL (`MySqlConnector`).
   - [`Repositories.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Persistence/Repositories.cs): High-performance Dapper repository layer.
   - [`DatabaseSeederService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Persistence/DatabaseSeederService.cs): Automatic in-process migrations, schema compatibility validation, and default configuration seeding.

2. **`Transports`**:
   - [`ITransport.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/ITransport.cs): Unified abstraction for downstream transport channels.
   - [`SseTransport.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/SseTransport.cs): Full-duplex persistent Server-Sent Events channel with POST message forwarding.
   - [`HttpTransport.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/HttpTransport.cs): Stateless half-duplex HTTP POST and chunked streaming transport.
   - [`StdioTransport.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/StdioTransport.cs): Subprocess STDIO transport communicating via newline-delimited JSON-RPC (NDJSON).
   - [`JsonRpcStateManager.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/JsonRpcStateManager.cs): Manages pending request completion sources (`PendingRequestTcs`), ID rewriting, cancellation tokens, and connection resets.

3. **`Identity`**:
   - [`CompositeIdentityProvider.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Identity/CompositeIdentityProvider.cs): Aggregates Active Directory, OIDC, and AppKey authenticators.
   - [`ActiveDirectoryIdentityProvider.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Identity/ActiveDirectoryIdentityProvider.cs) & [`LdapActiveDirectoryService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Identity/LdapActiveDirectoryService.cs): Resolves Windows Kerberos/NTLM caller SIDs via LDAP.
   - [`OidcIdentityProvider.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Identity/OidcIdentityProvider.cs): Extracts authenticated user contexts from reverse-proxy headers (`Remote-User`, `Remote-Groups`, `Remote-User-Sid`).

4. **`Secrets`**:
   - [`CompositeSecretRetriever.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Secrets/CompositeSecretRetriever.cs): Dispatches to Vault, Windows Registry, or Environment retrievers with a 5-minute sliding in-memory cache.
   - [`VaultSecretRetriever.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Secrets/VaultSecretRetriever.cs): Fetches secrets from HashiCorp Vault KV v2 using AppRole or Token authentication.
   - [`WindowsRegistrySecretRetriever.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Secrets/WindowsRegistrySecretRetriever.cs): Fetches Windows DPAPI-protected registry keys (`HKLM` / `HKCU`).
   - [`EnvironmentSecretRetriever.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Secrets/EnvironmentSecretRetriever.cs): Reads container environment variables.
   - [`SymmetricEncryptionHelper.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Secrets/SymmetricEncryptionHelper.cs): AES-256-GCM envelope encryption for database fields and config payloads.

5. **`Logging`**:
   - [`AuditLogger.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Logging/AuditLogger.cs): Writes structured invocation and admin audit trails.
   - [`PiiSanitizer.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Logging/PiiSanitizer.cs): Regex sanitizer stripping Bearer tokens, API keys, passwords, and connection strings.
   - [`InMemoryLogger.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Logging/InMemoryLogger.cs): Thread-safe ring buffer powering the real-time Test Bench UI console.

### Core Protocol & Routing Engine (`Core/`)

1. **`ClientSession` (Partial Class Architecture)**:
   - [`ClientSession.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ClientSession.cs): Main session lifecycle coordinator.
   - [`ClientSession.Authorization.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ClientSession/ClientSession.Authorization.cs): Multi-stage RBAC authorization, caller identity resolution, and audit logging.
   - [`ClientSession.BackendInitializer.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ClientSession/ClientSession.BackendInitializer.cs): Asynchronous concurrent warming of downstream backend connections and tool schemas.
   - [`ClientSession.ProxyForwarder.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ClientSession/ClientSession.ProxyForwarder.cs): Dispatches requests to downstream servers, tracks upstream GUIDs, and demultiplexes responses.
   - [`ClientSession.JsonRpcRewriter.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ClientSession/ClientSession.JsonRpcRewriter.cs): Uses `System.Text.Json.Nodes.JsonNode` to un-namespace tool names and rewrite message bodies safely without fragile string manipulation.
   - [`ClientSession.NotificationBroadcaster.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ClientSession/ClientSession.NotificationBroadcaster.cs): Fan-out broadcasting of notifications across active client channels.

2. **`Routing Coordinators`**:
   - [`SessionManager.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/SessionManager.cs): Thread-safe tracking and cleanup of active client connections.
   - [`BackendConnection.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/BackendConnection.cs): Manages a single upstream server connection channel, health state, and cached capabilities.
   - [`SemanticSearchService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/SemanticSearchService.cs): Hybrid BM25 keyword matching combined with cosine similarity vector scoring.
   - [`DynamicEmbeddingService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/DynamicEmbeddingService.cs), [`OnnxEmbeddingService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/OnnxEmbeddingService.cs), and [`ApiEmbeddingService.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ApiEmbeddingService.cs): Vector embedding calculation via local CPU ONNX runtime (`all-MiniLM-L6-v2`) or remote OpenAI-compatible APIs.
   - [`ToolRoutingManager.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ToolRoutingManager.cs), [`ResourceRoutingManager.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ResourceRoutingManager.cs), [`PromptRoutingManager.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/PromptRoutingManager.cs): Namespacing, un-namespacing, and routing of individual MCP capabilities.

### Architectural Dependency Rules

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       ARCHITECTURAL BOUNDARY CONSTRAINTS                    │
├─────────────────────────────────────────────────────────────────────────────┤
│ 1. Core NEVER depends on specific database drivers or concrete transports.  │
│    Core interacts strictly through `IDbConnectionFactory`, `ITransport`,    │
│    and `ISecretRetriever`.                                                  │
│                                                                             │
│ 2. Infrastructure implements strategy interfaces defined in Core and DI.    │
│    All database dialects (SQLite, MSSQL, MySQL) adhere to common contracts.│
│                                                                             │
│ 3. Components expose Minimal API endpoints and controllers that consume      │
│    Core session managers, repositories, and security helpers.               │
│                                                                             │
│ 4. No Raw String JSON Manipulation: All JSON-RPC modifications MUST use     │
│    `JsonNode`, `JsonObject`, or `JsonDocument` DOM trees.                   │
│                                                                             │
│ 5. Thread-Safe State: Shared state MUST use `ConcurrentDictionary` or       │
│    explicit monitor locks. Single-execution locks guard initialization.     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Component Subsystem Diagram

```mermaid
classDiagram
    class ProxyEndpoints {
        +MapProxyEndpoints()
    }
    class ServerEndpoints {
        +MapServerEndpoints()
    }
    class AppKeyEndpoints {
        +MapAppKeyEndpoints()
    }
    class PolicyEndpoints {
        +MapPolicyEndpoints()
    }
    class CapabilityEndpoints {
        +MapCapabilityEndpoints()
    }

    class SessionManager {
        +CreateSessionAsync()
        +GetSession()
        +CloseSession()
    }
    class ClientSession {
        <<partial>>
        +ResolveUserIdentityAsync()
        +IsUserAuthorizedAsync()
        +CallToolAsync()
        +ListToolsAsync()
        +StartInitialization()
    }
    class BackendConnection {
        +InitializeAsync()
        +SendRequestAsync()
        +CachedTools
    }

    class JsonRpcStateManager {
        +CreateTrackedRequest()
        +TryCompleteRequest()
        +MarkDisconnected()
    }

    class ITransport {
        <<interface>>
        +InitializeAsync()
        +SendRequestAsync()
        +CloseAsync()
        +IsHealthyAsync()
    }
    class SseTransport
    class HttpTransport
    class StdioTransport
    ITransport <|.. SseTransport
    ITransport <|.. HttpTransport
    ITransport <|.. StdioTransport

    class IDbConnectionFactory {
        <<interface>>
        +CreateConnection()
        +ProviderName
    }
    class DbConnectionFactory
    IDbConnectionFactory <|.. DbConnectionFactory

    class ISecretRetriever {
        <<interface>>
        +GetSecretAsync()
    }
    class CompositeSecretRetriever
    class VaultSecretRetriever
    class WindowsRegistrySecretRetriever
    class EnvironmentSecretRetriever
    ISecretRetriever <|.. CompositeSecretRetriever
    ISecretRetriever <|.. VaultSecretRetriever
    ISecretRetriever <|.. WindowsRegistrySecretRetriever
    ISecretRetriever <|.. EnvironmentSecretRetriever

    class IEmbeddingService {
        <<interface>>
        +GenerateEmbeddingAsync()
    }
    class OnnxEmbeddingService
    class ApiEmbeddingService
    IEmbeddingService <|.. OnnxEmbeddingService
    IEmbeddingService <|.. ApiEmbeddingService

    ProxyEndpoints --> SessionManager
    SessionManager --> ClientSession
    ClientSession --> BackendConnection
    BackendConnection --> ITransport
    BackendConnection --> JsonRpcStateManager
    ClientSession --> IEmbeddingService
    ClientSession --> IDbConnectionFactory
    ClientSession --> ISecretRetriever
```

---

## 4. Frontend Component & Typed Architecture

### React 19 & Vite SPA Architecture

The web dashboard is built using **React 19**, **TypeScript**, **Vite**, and **Zustand** for state management. It features a responsive dark-mode glassmorphic interface styled with CSS variables (`variables.css`, `layout.css`, `dashboard.css`, `tester.css`).

### Domain Component Decomposition

```
frontend/src/
├── api/                    # Typed API Client Layer (Axios / Fetch Wrappers)
│   ├── api.ts              # Base API instance, headers & error handling
│   ├── serverApi.ts        # Server CRUD & discovery endpoints
│   ├── clientApi.ts        # Registered OAuth clients
│   ├── appKeyApi.ts        # AppKey minting, revocation & verification
│   ├── securityApi.ts      # Policies & group mappings
│   ├── settingsApi.ts      # Provider configs & system settings
│   ├── userApi.ts          # Authenticated user identity & version info
│   └── testbenchApi.ts     # Tool execution, search simulation & log streaming
├── components/             # Domain UI Component Trees
│   ├── servers/            # DashboardView, ServerModal, ServerInspectModal, ServerCard
│   ├── clients/            # RegisteredClientsCard, ClientSetupGuide, AppKeysCard, ClientModal
│   ├── security/           # SecurityView, PolicyModal, MappingModal
│   ├── settings/           # SettingsView, GeneralTab, IdentityAuthTab, SecretProvidersTab,
│   │                       # AccessControlTab, CustomFilesTab, BackupsTab, ApprovalsCard
│   ├── testbench/          # TestBenchView (Interactive JSON Schema Form Builder & Log Stream)
│   └── shared/             # Header, Footer, Toasts, Modal, StatusBadge, PaginationToolbar
├── stores/                 # Centralized Zustand Reactive Stores
│   ├── useServerStore.ts   # Servers list, health states, filter queries
│   ├── useClientStore.ts   # Registered clients & setup guides
│   ├── useAppKeyStore.ts   # Active keys, key creation modal, copy buffer
│   ├── useSettingsStore.ts # Provider configs, dynamic encryption flags
│   ├── useUserStore.ts     # Current user, roles, SIDs, version cache
│   ├── useLogStore.ts      # Real-time logs ring buffer & level filters
│   └── useToastStore.ts    # Notification toast queue with auto-dismiss
└── types/                  # Canonical TypeScript Domain Interfaces
    ├── server.ts, client.ts, appKey.ts, security.ts, settings.ts, user.ts, testbench.ts
```

### Typed API Layer & Zustand State Stores

The frontend maintains complete decoupling between UI rendering, API communication, and state management:
* **`useServerStore`**: Manages server inventory, active search/category filtering, inspect modal state, and health check refresh intervals.
* **`useAppKeyStore`**: Handles key creation workflows, reveals the unmasked high-entropy raw secret once upon creation, and manages scope assignments.
* **`useSettingsStore`**: Coordinates secret and identity provider forms, dynamically toggling password masking and encrypting configuration JSON payloads.
* **`useLogStore`**: Receives streaming gateway logs into an in-memory ring buffer, enabling live regex filtering and log-level toggling in the Test Bench.

### Frontend Architecture & State Flow Diagram

```mermaid
graph TD
    subgraph UIViews ["React 19 Views & Modals"]
        App["App Root & Layout"]
        Nav["Navigation Tabs (Overview, Security, TestBench, Settings)"]
        DashView["DashboardView<br>(ServerCards, ServerControlsToolbar, StatsCard)"]
        SecView["SecurityView<br>(RegisteredClientsCard, AppKeysCard, MappingModal, PolicyModal)"]
        BenchView["TestBenchView<br>(SchemaFormBuilder, SearchSimulator, LogConsole)"]
        SetView["SettingsView<br>(General, IdentityAuth, SecretProviders, AccessControl, CustomFiles, Backups)"]
    end

    subgraph Stores ["Zustand Reactive Stores"]
        ServerStore["useServerStore"]
        ClientStore["useClientStore"]
        AppKeyStore["useAppKeyStore"]
        SettingsStore["useSettingsStore"]
        UserStore["useUserStore"]
        LogStore["useLogStore"]
        ToastStore["useToastStore"]
    end

    subgraph ApiLayer ["Typed API Client Layer"]
        ServerApi["serverApi.ts"]
        ClientApi["clientApi.ts"]
        AppKeyApi["appKeyApi.ts"]
        SecurityApi["securityApi.ts"]
        SettingsApi["settingsApi.ts"]
        UserApi["userApi.ts"]
        TestBenchApi["testbenchApi.ts"]
    end

    subgraph BackendAPI ["Backend ASP.NET Core Minimal APIs"]
        ApiServers["/api/servers"]
        ApiClients["/api/clients"]
        ApiKeys["/api/appkeys"]
        ApiSecurity["/api/security/policies & mappings"]
        ApiSettings["/api/settings & /api/providers"]
        ApiUser["/api/user/me & /api/version"]
        ApiLogs["/api/logs & /api/tester/call"]
    end

    App --> Nav
    Nav --> DashView
    Nav --> SecView
    Nav --> BenchView
    Nav --> SetView

    DashView --> ServerStore
    SecView --> ClientStore
    SecView --> AppKeyStore
    BenchView --> LogStore
    SetView --> SettingsStore
    App --> UserStore
    App --> ToastStore

    ServerStore --> ServerApi
    ClientStore --> ClientApi
    AppKeyStore --> AppKeyApi
    SettingsStore --> SettingsApi
    SettingsStore --> SecurityApi
    UserStore --> UserApi
    LogStore --> TestBenchApi

    ServerApi --> ApiServers
    ClientApi --> ApiClients
    AppKeyApi --> ApiKeys
    SecurityApi --> ApiSecurity
    SettingsApi --> ApiSettings
    UserApi --> ApiUser
    TestBenchApi --> ApiLogs
```

---

## 5. Protocol & Routing Engine Deep-Dive

### Meta-Mode Dynamic Capability Hiding (`/sse` & `/message`)

When hundreds of tools are registered in the gateway, exposing all tool definitions during the initial `tools/list` handshake overwhelms LLM context windows.

**Meta-Mode Architecture**:
1. **Bootstrap Initialization**: When a client connects to `/sse` with Meta-Mode enabled (the default), the router returns only two native gateway bootstrap tools:
   - `search_tools`: Accepts a natural language query (e.g. `"restart plex container"`) and returns relevant ranked tools.
   - `execute_tool`: Executes a namespaced tool (`{serverId}__{toolName}`) with dynamic parameter forwarding.
2. **Background Cache Pre-Warming**: In the background, [`ClientSession.BackendInitializer.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/ClientSession/ClientSession.BackendInitializer.cs) concurrently initializes all enabled downstream transports and caches their tool, prompt, and resource schemas.
3. **Semantic Scoring & Ranking**: When `search_tools` is called:
   - The query is vectorized using [`DynamicEmbeddingService`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/DynamicEmbeddingService.cs) (local ONNX `all-MiniLM-L6-v2` or remote API).
   - [`SemanticSearchService`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Routing/SemanticSearchService.cs) scores cached tools using a hybrid formula: `Score = (0.4 * KeywordScore) + (0.6 * VectorCosineSimilarity)`.
   - Results are namespaced as `{serverId}__{toolName}` and returned to the client.
4. **Execution Dispatch**: When `execute_tool` is invoked:
   - The router un-namespaces the tool name to identify the target server.
   - Security policies and AppKey scopes are verified.
   - Secrets are resolved and injected.
   - The call is dispatched to the upstream transport, and the result is returned to the client.

### Target-Specific Virtual Proxying (`/{targetServerId}`)

For clients requiring direct, un-namespaced interaction with a specific backend (e.g. Cursor configured to talk directly to `docker` or `plex`):
* The client connects directly to `/{targetServerId}` (e.g. `/docker` or `/plex`).
* The gateway validates AppKey scopes (`server:{targetServerId}`, `category:{cat}`, or `*`).
* Capabilities are proxied directly without Meta-Mode filtering. All tool names remain in their native un-namespaced form.

### JSON-RPC 2.0 In-Flight Multiplexing & ID Preservation

The router multiplexes concurrent client requests across shared or isolated backend connections while ensuring complete response isolation:

1. **Polymorphic Serialization**: [`JsonRpcMessageConverter.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Core/Protocol/ProtocolModels.cs) handles bidirectional serialization of `JsonRpcRequest`, `JsonRpcResponse`, `JsonRpcNotification`, and `JsonRpcError`.
2. **Client ID Preservation**: The client's original ID (whether an integer `1`, string `"req-123"`, or GUID) is captured in [`PendingRequestTcs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/JsonRpcStateManager.cs).
3. **GUID Upstream Rewriting**: To prevent ID collisions when multiple clients send requests with `id: 1` to the same backend connection, the router rewrites the upstream request ID to a unique GUID (`upstreamRequestId = Guid.NewGuid().ToString("N")`).
4. **Out-of-Order Response Demultiplexing**: When the upstream backend responds, [`JsonRpcStateManager`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/JsonRpcStateManager.cs) matches the GUID, restores the client's original ID onto the response object (`response.Id = tracked.OriginalId`), and fulfills the waiting `TaskCompletionSource`.

### Sequence Diagram: Stateful SSE Session Lifecycle

```mermaid
sequenceDiagram
    autonumber
    actor Client as MCP Client (IDE / Agent)
    participant Edge as McpDualSpecMiddleware
    participant Proxy as ProxyEndpoints (/sse, /message)
    participant SessionMgr as SessionManager
    participant Session as ClientSession
    participant Search as SemanticSearchService
    participant StateMgr as JsonRpcStateManager
    participant Upstream as Downstream MCP Server

    Note over Client,Upstream: 1. Session Handshake & Connection Warming
    Client->>Edge: GET /sse (Authorization: Bearer mcp-appkey-123)
    Edge->>Edge: Validate Token & Resolve IdentityContext
    Edge->>Proxy: Forward authorized request
    Proxy->>SessionMgr: CreateSessionAsync(sessionId, responseStream)
    SessionMgr->>Session: Instantiate ClientSession
    Session-->>Proxy: Session Created (Guid: "sess-abc")
    Proxy-->>Client: HTTP 200 OK (text/event-stream)<br>event: endpoint\ndata: /message?sessionId=sess-abc
    
    par Async Background Warming
        Session->>Upstream: InitializeAsync() & tools/list
        Upstream-->>Session: Cached Tools & Capabilities
    end

    Note over Client,Upstream: 2. Capability Discovery (Meta-Mode)
    Client->>Proxy: POST /message?sessionId=sess-abc (tools/list)
    Proxy->>Session: ListToolsAsync()
    Session-->>Proxy: Return Bootstrap Tools (search_tools, execute_tool)
    Proxy-->>Client: HTTP 200 (tools: [search_tools, execute_tool])

    Note over Client,Upstream: 3. Semantic Tool Search
    Client->>Proxy: POST /message?sessionId=sess-abc (tools/call: search_tools, query="restart plex")
    Proxy->>Session: CallToolAsync("search_tools")
    Session->>Search: SearchToolsAsync("restart plex")
    Search->>Search: Hybrid BM25 + ONNX Vector Ranking
    Search-->>Session: Matches: ["plex__restart_server"]
    Session-->>Client: HTTP 200 (tools: [plex__restart_server])

    Note over Client,Upstream: 4. Tool Execution & Multiplexed Forwarding
    Client->>Proxy: POST /message?sessionId=sess-abc (tools/call: execute_tool, name="plex__restart_server", id=1)
    Proxy->>Session: CallToolAsync("execute_tool")
    Session->>Session: Verify AppKey Scopes & RBAC (sp_EvaluateUserAccess)
    Session->>Session: Un-namespace: Server="plex", Tool="restart_server"
    Session->>StateMgr: CreateTrackedRequest(upstreamGuid="up-999", originalId=1)
    Session->>Upstream: POST /message (tools/call: restart_server, id="up-999")
    Upstream-->>Session: JSON-RPC Response (id="up-999", result={status: "restarted"})
    Session->>StateMgr: TryCompleteRequest("up-999", response)
    StateMgr->>StateMgr: Restore Original ID (id=1)
    Session-->>Proxy: Tool Execution Result
    Proxy-->>Client: HTTP 200 (id=1, result={content: [{type: "text", text: "Success"}]})

    Note over Client,Upstream: 5. Session Termination
    Client->>Proxy: Disconnect / Abort Connection
    Proxy->>SessionMgr: CloseSession("sess-abc")
    SessionMgr->>Session: DisposeAsync()
    Session->>Upstream: CloseAsync() & Cleanup Transports
```

### Sequence Diagram: Stateless HTTP Stream Execution

```mermaid
sequenceDiagram
    autonumber
    actor Client as Single-Shot Client / Webhook
    participant Edge as McpDualSpecMiddleware
    participant Proxy as ProxyEndpoints (/sse POST or /{serverId})
    participant SessionMgr as SessionManager
    participant Session as ClientSession (Global Stateless)
    participant Rbac as RBAC & Security Validation
    participant Secrets as CompositeSecretRetriever
    participant Upstream as Target Backend Server

    Client->>Edge: POST /sse (tools/call: docker__list_containers, id="stateless-1")
    Edge->>Edge: Annotate Metadata (Mcp-Method: tools/call, Mcp-Name: docker__list_containers)
    Edge->>Proxy: Forward Single-Shot POST Request
    Proxy->>SessionMgr: GetSession("global-stateless-session")
    SessionMgr-->>Proxy: Return Global Stateless ClientSession

    Proxy->>Session: CallToolAsync("docker__list_containers")
    Session->>Rbac: IsUserAuthorizedAsync("tools/call", "docker__list_containers")
    Rbac-->>Session: Authorized (200 OK)

    Session->>Secrets: GetSecretForProviderAsync("Vault", "secret/docker", "api_key")
    Secrets-->>Session: Injected Bearer Token

    Session->>Upstream: POST /message (tools/call: list_containers, id="guid-777")
    Upstream-->>Session: HTTP 200 OK (id="guid-777", result={containers: [...]})

    Session->>Session: Restore Original ID ("stateless-1")
    Session-->>Proxy: Execution Output
    Proxy-->>Client: HTTP 200 OK (application/json, id="stateless-1", result={...})
```

---

## 6. Authorization Pipeline & RBAC Decision Engine

### 4-Stage Hierarchical Decision Flow

Every request entering the router passes through four concentric security gates:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 1: APPKEY SCOPE BOUNDARY (Fast-Path Key Filtering)                    │
│   Does the caller's AppKey allow the requested target?                      │
│   Grammar: `*`, `all`, `server:{id}`, `category:{cat}`, `tool:{id}`,        │
│            `prompt:{id}`, `resource:{id}`                                   │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ Pass
┌──────────────────────────────────────▼──────────────────────────────────────┐
│ STAGE 2: IDENTITY RESOLUTION & GROUP MAPPING                                │
│   Resolve caller username, Active Directory SIDs, and OIDC headers.         │
│   Translate external SIDs / groups to internal roles via `GroupMappings`.    │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
┌──────────────────────────────────────▼──────────────────────────────────────┐
│ STAGE 3: ADMINISTRATOR SID BYPASS CHECK                                     │
│   Does the caller possess the Admin SID (`S-1-5-32-544` / `full_admin`)?    │
└──────────────────────────────────────┬──────────────────────────────────────┘
                   │ No                                   │ Yes (Admin Bypass)
┌──────────────────▼───────────────────┐        ┌─────────▼───────────────────┐
│ STAGE 4: DATABASE-BACKED RBAC        │        │ AUTHORIZED (200 OK)         │
│   - Explicit Deny overrides Allow    │        │ Invocation Audit Logged     │
│   - Category & Server inheritance    │        └─────────────────────────────┘
│   - Fail-Closed Default (DENY)       │
└──────────────────┬───────────────────┘
                   │ Allowed
┌──────────────────▼───────────────────┐
│ AUTHORIZED (200 OK)                  │
│ Invocation Audit Logged              │
└──────────────────────────────────────┘
```

### Scope Grammar & Resolution

AppKeys support granular least-privilege scoping:

| Scope Pattern | Matches / Grants Access To |
| :--- | :--- |
| `*` or `all` or `mcp_client` | Full access to all servers, tools, prompts, and resources. |
| `server:{serverId}` | Full access to all tools, prompts, and resources under the specified server. |
| `category:{categoryName}` | Access to all servers tagged with the specified category (e.g. `category:Media`, `category:Smarthome`). |
| `tool:{toolName}` | Access to a specific un-namespaced or namespaced tool. |
| `prompt:{promptName}` | Access to a specific prompt template. |
| `resource:{uri}` | Access to a specific resource URI or wildcard pattern (`mcp://docker/*`). |

### Admin SID Bypass & Database-Backed RBAC Evaluation

1. **Administrator SID Verification**: [`SecurityValidationHelper.IsAdmin`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Authorization/SecurityValidationHelper.cs) verifies whether the resolved caller principal contains the well-known Windows Built-in Administrators SID (`S-1-5-32-544`), the `full_admin` group role, or any SID configured in `Admin:GroupSid`. Admin principals bypass database RBAC policy checks while their actions remain fully audited.
2. **Database Stored Procedure Evaluation (`sp_EvaluateUserAccess`)**:
   - For non-admin principals, the gateway executes [`sp_EvaluateUserAccess`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/scripts/db/mssql/02_procedures.sql) across MS SQL Server / MySQL, or direct parameterized queries on SQLite.
   - **Explicit Deny Rule**: If any policy matching the user's groups has `IsAllowed = 0`, access is immediately denied.
   - **Explicit Allow Rule**: Access is permitted only if at least one matching policy has `IsAllowed = 1`.
   - **Fail-Closed Default**: If no matching policies exist, or if a database failure occurs, access is denied (`403 Forbidden`).

### Mermaid Authorization Decision Flowchart

```mermaid
flowchart TD
    Start(["Incoming MCP Request"]) --> ExtractAuth["Extract Authorization Headers / AppKey"]
    
    ExtractAuth --> CheckAppKey{"Is AppKey Used?"}
    CheckAppKey -- Yes --> ValidateScopes{"Check AppKey Scopes<br>(*, server:*, category:*, tool:*)"}
    ValidateScopes -- Scope Violated --> DenyScope["403 Forbidden (Scope Violation)"]
    ValidateScopes -- Scope Valid --> ResolveId["Resolve UserIdentityContext<br>(Username, SIDs, OIDC Groups)"]
    CheckAppKey -- No --> ResolveId

    ResolveId --> MapGroups["Query GroupMappings<br>(Translate External SIDs -> Internal Groups)"]
    MapGroups --> CheckAdmin{"Is Caller Administrator?<br>(SID S-1-5-32-544 or full_admin)"}
    
    CheckAdmin -- Yes (Admin Bypass) --> AuditAndAllow["Log Invocation Audit<br>(sp_InsertAuditLog)"] --> Allow(["200 OK / Route Execution"])
    
    CheckAdmin -- No --> EvaluateRbac{"Evaluate DB Policies<br>(sp_EvaluateUserAccess)"}
    
    EvaluateRbac -- "Explicit Deny (IsAllowed = 0)" --> DenyPolicy["403 Forbidden (Explicit Deny)"]
    EvaluateRbac -- "No Matching Policies" --> DenyFailClosed["403 Forbidden (Fail-Closed Default)"]
    EvaluateRbac -- "Explicit Allow (IsAllowed = 1)" --> AuditAndAllow
```

---

## 7. Transport Subsystem & Subprocess Lifecycle

### Strategy Pattern (`ITransport`)

All downstream transports implement [`ITransport`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/ITransport.cs):

```csharp
public interface ITransport : IAsyncDisposable
{
    string TransportType { get; }
    bool IsConnected { get; }
    Task InitializeAsync(McpServer server, CancellationToken cancellationToken = default);
    Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default);
    Task SendNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
```

### Subprocess STDIO Architecture & Security Hardening

The [`StdioTransport`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/StdioTransport.cs) manages local executables, CLI tools, Python scripts (`uvx`), and Node packages (`npx`):

1. **Command Line Tokenization**: [`StdioTransport.ParseCommandLine`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/StdioTransport.cs) parses single/double quoted paths and arguments cleanly into an executable and argument array.
2. **Strict Process Security Policy**:
   - `UseShellExecute = false` and `CreateNoWindow = true`.
   - Executables are invoked directly without intermediate shells (`sh`, `bash`, `cmd.exe`, `powershell.exe`) to prevent shell injection and command chaining attacks (`&&`, `;`, `|`).
3. **Zero-Leakage Credential Injection**:
   - Downstream secrets resolved from Vault or Environment are injected strictly via `ProcessStartInfo.Environment`.
   - Credentials are **never** passed as command-line arguments, preventing exposure in process tables (`ps aux`, `/proc/$PID/cmdline`).
4. **Stream Synchronization & Buffer Draining**:
   - Requests are written to `StandardInput` as newline-delimited JSON (NDJSON) guarded by asynchronous write locks.
   - `StandardOutput` is read line-by-line using asynchronous loops to prevent buffer deadlocks.

### Child Process Tree Lifecycle & Signal Handling

```mermaid
stateDiagram-v2
    [*] --> Starting: Spawn Process (UseShellExecute=false)
    Starting --> Running: Redirect stdin/stdout/stderr & Inject Env Secrets
    Running --> Executing: Write NDJSON Request to stdin
    Executing --> Running: Read NDJSON Response from stdout
    
    Running --> Draining: Gateway Shutdown / Session Close
    Draining --> Terminated: Close stdin (EOF) -> Process Exits Cleanly
    
    Running --> Killing: Timeout / Request Cancellation
    Killing --> Terminated: Kill(entireProcessTree: true)
    
    Terminated --> [*]: Dispose Process & Free Pipes
```

* **Graceful Termination**: The router closes `stdin`, signaling EOF to the subprocess.
* **Process Tree Killing**: If the subprocess fails to exit within the grace period (5 seconds), [`process.Kill(entireProcessTree: true)`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Transports/StdioTransport.cs) terminates the entire process hierarchy, preventing orphaned worker threads.

### Stderr Log Capture & PII Token Masking

* A background listener continuously drains `StandardError`.
* Lines are passed through [`PiiSanitizer.SanitizePayload`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Logging/PiiSanitizer.cs) to redact tokens before writing to application loggers.

### Sequence Diagram: STDIO Subprocess Execution

```mermaid
sequenceDiagram
    autonumber
    participant Session as ClientSession
    participant Transport as StdioTransport
    participant Proc as Subprocess (stdin / stdout / stderr)
    participant Pii as PiiSanitizer & Logger
    participant State as JsonRpcStateManager

    Session->>Transport: InitializeAsync(serverConfig)
    Transport->>Transport: ParseCommandLine(serverConfig.Url)
    Transport->>Transport: Inject Secrets into ProcessStartInfo.Environment
    Transport->>Proc: Process.Start() (Redirect Standard Streams)
    
    par Stderr Background Drainer
        loop Continuous Drain
            Proc-->>Transport: Read Stderr Line
            Transport->>Pii: SanitizePayload(stderrLine)
            Pii-->>Transport: Redacted Log Line
            Transport->>Pii: LogDebug / LogWarning
        end
    end

    Session->>Transport: SendRequestAsync(JsonRpcRequest: tools/call, id="up-42")
    Transport->>State: CreateTrackedRequest("up-42")
    Transport->>Proc: Write to stdin ("{\"jsonrpc\":\"2.0\",\"id\":\"up-42\",...}\n")
    Transport->>Proc: Flush stdin
    
    Proc-->>Transport: Read line from stdout ("{\"jsonrpc\":\"2.0\",\"id\":\"up-42\",\"result\":{...}}\n")
    Transport->>State: TryCompleteRequest("up-42", response)
    State-->>Session: Return JsonRpcResponse
```

---

## 8. Database & Persistence Architecture

### Unified Entity-Relationship Diagram (Mermaid ERD)

```mermaid
erDiagram
    Servers ||--o{ Tools : "exposes"
    Servers ||--o{ AuditLogs : "generates"
    Servers }o--|| SecretProviders : "authenticates via"
    
    Tools ||--o{ ToolAccessPolicies : "governed by"
    AdGroups ||--o{ ToolAccessPolicies : "assigned to"
    
    GroupMappings ||--o{ AdGroups : "maps to"
    
    AppKeys ||--o{ AuditLogs : "attributed via OwnerSid"
    
    Servers {
        string Id PK
        string DisplayName
        string Url
        boolean Enabled
        boolean Hidden
        string Type
        string SecretProvider
        string SecretItemKey
        string SecretMount
        string SecretPath
        string SecretField
        string AuthShape
        string CustomHeaderName
        string Categories
        string ApiKey
        string HeadersJson
        boolean AutoDiscovered
    }

    AppKeys {
        string Id PK
        string Name
        string Username
        string KeyPrefix UK
        string EncryptedKey
        string ScopesJson
        datetime ExpiresAt
        datetime CreatedAt
        string OwnerSid
    }

    AccessPolicies {
        string Id PK
        string TargetId
        string RequiredGroup
        boolean IsAllowed
        datetime CreatedAt
    }

    GroupMappings {
        string Id PK
        string ExternalId
        string InternalGroup
        datetime CreatedAt
    }

    SecretProviders {
        int ProviderId PK
        string ProviderName UK
        string DisplayName
        string EncryptedConfigJson
        boolean IsEnabled
        datetime UpdatedAt
    }

    AuthProviderConfigs {
        int AuthId PK
        string ProviderName UK
        string DisplayName
        string UserHeader
        string GroupsHeader
        string EncryptedConfigJson
        boolean IsEnabled
        datetime UpdatedAt
    }

    AdGroups {
        int GroupId PK
        string ObjectSid UK
        string GroupName
        string Description
        boolean IsActive
        datetime CreatedAt
    }

    Tools {
        int ToolId PK
        string ServerId FK
        string ToolName
        string Description
        string InputSchemaJson
        string VaultSecretPath
        string SecretProvider
        boolean IsEnabled
        datetime CreatedAt
    }

    ToolAccessPolicies {
        int ToolPolicyId PK
        int ToolId FK
        int GroupId FK
        boolean IsAllowed
        int RateLimitPerMin
        datetime CreatedAt
    }

    AuditLogs {
        bigint AuditId PK
        string RequestId
        string UserPrincipalName
        string UserSid
        string ServerCodeName
        string ItemName
        string RequestMethod
        int ExecutionTimeMs
        int StatusCode
        string RequestPayload
        string ResponsePayload
        string ErrorMessage
        datetime Timestamp
    }

    AdminAuditLogs {
        string Id PK
        string Username
        string Action
        string Target
        string Details
        boolean Success
        string ErrorMessage
        datetime Timestamp
    }

    Settings {
        string Id PK
        string EmbeddingProvider
        string EmbeddingApiUrl
        string EmbeddingApiKey
        string EmbeddingApiModel
        string EmbeddingModelDir
        boolean RequireManualApproval
        int GlobalMaxKeys
        int UserMaxKeys
    }
```

### Engine Dialect Strategies (SQLite, MS SQL Server, MySQL)

| Persistence Dimension | 🪶 SQLite (Default) | 🏢 Microsoft SQL Server | 🐬 MySQL / MariaDB |
| :--- | :--- | :--- | :--- |
| **Provider Key** | `sqlite` | `mssql` | `mysql` |
| **Concurrency Mode** | WAL (Write-Ahead Logging) | Row-Level Locking / Always On | InnoDB MVCC Transactions |
| **Execution Paradigm** | Direct SQL & Parameterized Dapper | T-SQL Stored Procedures (`sp_*`) | Stored Procedures (`sp_*`) |
| **Upsert Syntax** | `ON CONFLICT(Id) DO UPDATE` | `IF EXISTS ... UPDATE ELSE INSERT` | `ON DUPLICATE KEY UPDATE` |
| **Parameter Prefix** | `@Param` | `@Param` | Strict `p_Param` |
| **Timestamp Generation** | `CURRENT_TIMESTAMP` (ISO-8601) | `SYSUTCDATETIME()` (DATETIME2) | `CURRENT_TIMESTAMP` / `NOW()` |
| **DDL Migrations** | In-Process Automatic (`DatabaseSeederService`) | Scripted DDL (`scripts/db/mssql/`) | Scripted DDL (`scripts/db/mysql/`) |

For comprehensive schema definitions, stored procedure listings, and migrations, see [**Database Provider Support & Deployment Matrix**](database-providers.md).

---

## 9. Secret Provider & Envelope Encryption Pipeline

### AES-256-GCM Envelope Encryption Specification

Sensitive database columns (e.g. `AppKeys.EncryptedKey`, `SecretProviders.EncryptedConfigJson`, `AuthProviderConfigs.EncryptedConfigJson`, `Servers.ApiKey`) are protected using **AES-256-GCM authenticated envelope encryption** via [`SymmetricEncryptionHelper`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Secrets/SymmetricEncryptionHelper.cs):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       AES-256-GCM ENVELOPE PACKET FORMAT                    │
├───────────────────────────┬──────────────────────────┬──────────────────────┤
│    Nonce (IV) [12 Bytes]  │  Auth Tag [16 Bytes]     │ Ciphertext [N Bytes] │
└───────────────────────────┴──────────────────────────┴──────────────────────┘
 ◄────────────────────── Base64 Encoded for Storage ────────────────────────►
```

1. **Nonce Generation**: A cryptographically secure 12-byte random nonce is generated for every encryption operation using `RandomNumberGenerator.GetBytes(12)`.
2. **Authenticated Tagging**: AesGcm computes a 16-byte authentication tag over the ciphertext, guaranteeing tamper detection and payload integrity.
3. **Master Key Derivation (PBKDF2)**:
   - Derives a 256-bit key from `ROUTER_SECRET`, `ROUTER_MASTER_KEY`, or `DB_ENCRYPTION_KEY`.
   - Uses `Rfc2898DeriveBytes.Pbkdf2` with **600,000 iterations** of **SHA-256** and a deployment-specific salt (`_McpRouter_Salt_v2`).

### Pluggable Retrievers & Dynamic Reload Without Restart

When an administrator updates a Secret Provider in the Settings UI:
1. The frontend submits the updated credentials to `POST /api/providers/secret`.
2. [`ProviderConfigSecurityHelper`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Components/Providers/ProviderConfigSecurityHelper.cs) encrypts the payload with AES-256-GCM and persists it to the database.
3. The in-memory cache in [`CompositeSecretRetriever`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-57/Infrastructure/Secrets/CompositeSecretRetriever.cs) is immediately invalidated.
4. Subsequent downstream requests fetch fresh tokens from HashiCorp Vault or the updated provider without requiring a container or service restart.

### Secret Resolution & Encryption Pipeline Flowchart

```mermaid
flowchart TD
    Request["Downstream Invocation Required"] --> CheckProv{"Server SecretProvider Type?"}
    
    CheckProv -- "None / Direct ApiKey" --> DecryptDirect["Decrypt Server ApiKey via AES-256-GCM"]
    CheckProv -- "Vault / HashiCorp" --> CheckCache["Check MemoryCache (10m TTL)"]
    CheckProv -- "WindowsRegistry" --> CheckCache
    CheckProv -- "Environment" --> CheckCache

    CheckCache -- "Cache Hit" --> ReturnToken["Inject Bearer / Custom Header"]
    CheckCache -- "Cache Miss" --> QueryProvider{"Dispatch Provider Retriever"}

    QueryProvider -- Vault --> VaultCall["VaultSecretRetriever (KV v2 AppRole)"]
    QueryProvider -- WindowsRegistry --> RegCall["WindowsRegistrySecretRetriever (DPAPI)"]
    QueryProvider -- Environment --> EnvCall["EnvironmentSecretRetriever (Container Env)"]

    VaultCall --> CacheResult["Store in MemoryCache (Sliding Expiry)"]
    RegCall --> CacheResult
    EnvCall --> CacheResult
    DecryptDirect --> ReturnToken
    CacheResult --> ReturnToken
    ReturnToken --> Forward["Forward to Downstream Transport"]
```

For complete setup guides, AppRole configuration commands, and DPAPI registry recipes, see [**Enterprise Secret Providers & Key Management Guide**](secret-providers.md).

---

## 10. Cross-References, Verification & Operational Guide

### Documentation Navigation Matrix

| Topic / Focus Area | Target Specification Guide |
| :--- | :--- |
| **AppKey Scopes & Authorization Rules** | [**AppKey Scopes & Authorization Guide**](appkey-scopes.md) |
| **Database Engines, Schemas & Stored Procs** | [**Database Provider Support & Deployment Matrix**](database-providers.md) |
| **Transports, Concurrency & STDIO Subprocesses** | [**Transport Capability & Configuration Guide**](transports.md) |
| **Vault, DPAPI & AES-256-GCM Encryption** | [**Enterprise Secret Providers & Key Management Guide**](secret-providers.md) |
| **CI Quality Gates, Static Analysis & Testing** | [**CI Quality Gates & Verification Guide**](ci-quality-gates.md) |
| **Pairwise Integration Matrix & E2E Tests** | [**Testing Matrix & Integration Guide**](testing-matrix.md) |
| **End-User Guides & Interactive UI Test Bench** | [**User Guide & Test Bench Operations**](user-guide/testbench.md) |
| **Core README & Getting Started** | [**Project Overview & Quickstart**](../README.md) |

### Verification & Test Suite Execution

All architectural contracts, concurrency guarantees, and security policies are validated by the comprehensive 515-test automated test suite:

```bash
# Execute full C# test suite in CI mode:
CI=true dotnet test McpRouter.slnx
```

---

*Document Version: `v4.12.2` | Maintained by the MCP Router Core Architecture Group.*
