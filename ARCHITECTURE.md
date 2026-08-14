# 🏛️ MCP Router Architecture

This document provides an executive summary of the internal architecture, security mechanisms, design requirements, and key subsystem flows of the **Model Context Protocol (MCP) Router Gateway & Semantic Proxy**.

> 📖 **Definitive Specification**: For exhaustive architectural details, comprehensive Mermaid sequence diagrams, component models, entity-relationship diagrams (ERDs), and cryptographic specifications, see the [**Complete Enterprise Architecture Guide**](docs/architecture.md).

---

## 📐 Architectural Design Requirements

### 1. Performance & Latency Requirements
- **Sub-millisecond Routing Decision**: The gateway annotates request metadata based on headers (`Mcp-Method` and `Mcp-Name` compliant with the MCP 2026-07-28 Spec) without having to read or buffer the request body if present, enabling downstream components to inspect annotated metadata. Routing is ultimately body/path based.
- **Concurrent Request Handling**: Highly thread-safe design. The router manages simultaneous SSE client channels, background health probes, and on-demand semantic search requests using thread-safe state wrappers (`ConcurrentDictionary` and thread-safe locks).
- **Background Startup Warming**: Embedding models, backend connection channels, and configuration caches are preloaded asynchronously during server initialization (`ClientSession.BackendInitializer.cs`). This prevents first-request latency spikes (cold starts).

### 2. Security & Identity Requirements
- **Dual Authenticated Identities**: Supports enterprise-grade Windows/Kerberos environments via Active Directory SIDs (`LdapActiveDirectoryService.cs`) as well as modern containerized reverse-proxy identities via OIDC headers (`Remote-User`, `Remote-Groups`).
- **Granular AppKey Authorization**: Machine callers and autonomous agents authenticate via high-entropy AppKeys (`mcp-*-*-*`) with scope enforcement (`*`, `server:*`, `category:*`, `tool:*`).
- **Strict Role-Based Access Control (RBAC)**: Target servers and backend tools verify caller groups using database-backed stored procedures (`sp_EvaluateUserAccess`).
- **Compliant Error Handling & WWW-Authenticate Headers**: In accordance with the MCP 2026-07-28 Authorization specification, the gateway emits strict `WWW-Authenticate` challenge headers during `401 Unauthorized` and `403 Forbidden` states.
- **Data Redaction (PII)**: Any bearer tokens, credentials, API keys, or database passwords parsed in standard JSON-RPC communication are filtered and redacted (`PiiSanitizer.cs`) before being logged or stored.

### 3. Reliability & Resilience Requirements
- **Pluggable & Extensible Design**: Downstream transports (`ITransport`), identity providers (`IIdentityProvider`), secret managers (`ISecretRetriever`), and database providers (`IDbConnectionFactory`) are structured under clean strategy patterns.
- **Robust In-Flight Concurrency**: Uses `JsonRpcStateManager` with unique upstream GUID request rewriting and `PendingRequestTcs` to guarantee that out-of-order responses from multiplexed upstream servers are cleanly routed back to the exact requesting thread with their original client ID preserved.
- **Safe Resource Cleanup**: Active SSE client sessions handle connection terminations cleanly and capture cancellation tokens gracefully (`notifications/cancelled`).

---

## 🏛️ System Architecture Overview

```mermaid
graph TD
    Client["Client App / LLM Agent (Cursor, Claude, Antigravity, OpenClaw)"]
    Middleware["McpDualSpecMiddleware<br>(2026-07-28 Spec Headers & Body Fallback)"]
    Identity["CompositeIdentityProvider<br>(Active Directory LDAP & TinyAuth/PocketID OIDC)"]
    AuthEvaluator["sp_EvaluateUserAccess<br>(Provider-Specific Group SIDs / Roles)"]
    Secrets["CompositeSecretRetriever<br>(Vault KV v2, Windows Registry DPAPI, Env)"]
    Audit["AuditLogger & PiiSanitizer<br>(sp_InsertAuditLog)"]
    DbFactory["DbConnectionFactory<br>(MS SQL / MySQL / SQLite WAL)"]
    Downstream["Downstream MCP Backend Fleet (SSE, HTTP, STDIO, Native)"]

    Client -->|Mcp-Method & Mcp-Name| Middleware
    Middleware --> Identity
    Middleware --> AuthEvaluator
    AuthEvaluator --> DbFactory
    Middleware --> Secrets
    Middleware --> Downstream
    Middleware --> Audit
    Audit --> DbFactory
```

### 📦 Modular Domain & Infrastructure Boundaries

The backend is organized into clear bounded modules across domain components, infrastructure, and core routing logic:

```text
├── Components/
│   ├── Servers/         # Upstream server models, validation, health checks, discovery & MapServerEndpoints
│   ├── Clients/         # Client models, credential services, OAuth & MapClientEndpoints
│   ├── AppKeys/         # AppKey models, authorization keys, hashing, scope validation & MapAppKeyEndpoints
│   ├── Providers/       # Auth/secret provider settings, AES-256-GCM crypto & MapProviderEndpoints
│   ├── Authorization/   # Access policies, group mappings, RBAC evaluation & MapPolicyEndpoints
│   └── Capabilities/    # Native tools, proxy execution, tool/prompt/resource handlers & MapCapabilityEndpoints
├── Infrastructure/
│   ├── Persistence/     # Dapper repositories, database connection factory, migrations & seeders
│   ├── Transports/      # SSE, HTTP, STDIO, JSON-RPC state manager & target proxy
│   ├── Identity/        # Active Directory, OIDC, AppKey identity providers & LDAP service
│   ├── Secrets/         # Vault, Windows Registry, Environment secret retrievers & encryption
│   └── Logging/         # Audit logger, PII sanitization & in-memory log providers
└── Core/
    ├── Protocol/        # JSON-RPC protocol models & Polymorphic converter
    └── Routing/         # ClientSession (partial classes), SessionManager, BackendConnection & Semantic Search
```

---

## 📡 Key Message & Connection Flows

### 1. SSE Client Connection & Meta-Mode Tool Discovery
When an MCP client initiates a connection to `/sse`:

```mermaid
sequenceDiagram
    autonumber
    actor Client as MCP Client
    participant Router as MCP Router
    participant SessionMgr as SessionManager
    participant BackendConn as BackendConnection
    participant Downstream as MCP Backend

    Client->>Router: GET /sse (Default Meta-Mode)
    Router->>Router: Execute McpDualSpecMiddleware Auth Checks
    Router->>SessionMgr: Create & Register ClientSession
    SessionMgr-->>Router: Session Token Generated
    Router->>BackendConn: Warm & Connect Backend SSE streams (Concurrent)
    BackendConn->>Downstream: Handshake & Initialize
    Downstream-->>BackendConn: Return Capability Details
    BackendConn-->>Router: Cache warmed backend tools, prompts, resources
    Router-->>Client: Return 200 OK (text/event-stream)
    Note over Client,Router: Client gets bootstrap tools (search_tools, execute_tool)
```

---

### 2. Request Routing and Execution Flow (Meta-Mode)
When an agent client searches for and executes a capability:

```mermaid
sequenceDiagram
    autonumber
    actor Client as LLM / Agent
    participant Router as MCP Router
    participant SemanticSvc as SemanticSearchService
    participant DB as SQL Database
    participant BackendConn as BackendConnection
    participant Downstream as MCP Backend

    Client->>Router: POST /message?sessionId=1 (search_tools)
    Router->>SemanticSvc: Evaluate "restart container" query
    SemanticSvc->>SemanticSvc: Fetch ONNX / OpenAI Embeddings
    SemanticSvc->>SemanticSvc: Evaluate Hybrid Keyword + Semantic Weights
    SemanticSvc-->>Router: Return tool "docker__restart_container"
    Router-->>Client: Return namespaced search result JSON

    Client->>Router: POST /message?sessionId=1 (execute_tool: docker__restart_container)
    Router->>Router: Verify caller security & permissions (sp_EvaluateUserAccess)
    Router->>DB: Check secret provider rules (sp_GetServerSecrets)
    DB-->>Router: Returns Env, Vault, or Registry secrets config
    Router->>BackendConn: Relay request (unnamespaced tool: "restart_container")
    BackendConn->>Downstream: Send JSON-RPC Command
    Downstream-->>BackendConn: Command Output
    BackendConn-->>Router: Relay Output
    Router-->>Client: Return tool execution results
```

---

## 🔒 4-Stage Authorization Pipeline

Every request entering the router passes through four concentric security boundaries:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. AppKey Scope Boundary (Fast-Path Key Filtering)                          │
│    Does the caller's AppKey allow the target server, category, or tool?     │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ Pass
┌──────────────────────────────────────▼──────────────────────────────────────┐
│ 2. Identity Resolution & Group Mapping                                      │
│    Resolve username, external SIDs, and map them to internal groups         │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
┌──────────────────────────────────────▼──────────────────────────────────────┐
│ 3. Administrative Bypass Check                                              │
│    Does the caller possess the Admin SID (S-1-5-32-544 / full_admin)?       │
└──────────────────────────────────────┬──────────────────────────────────────┘
                   │ No                                   │ Yes (Admin Bypass)
┌──────────────────▼───────────────────┐        ┌─────────▼───────────────────┐
│ 4. RBAC Policy Evaluation            │        │ Authorized (200 OK)         │
│    - Explicit Deny overrides Allow   │        │ Invocation Audit Logged     │
│    - Category & Server inheritance   │        └─────────────────────────────┘
│    - Fail-Closed Default (DENY)      │
└──────────────────┬───────────────────┘
                   │ Allowed
┌──────────────────▼───────────────────┐
│ Authorized (200 OK)                  │
│ Invocation Audit Logged              │
└──────────────────────────────────────┘
```

---

## 🗄️ Database & Entity-Relationship Architecture (Mermaid ERD)

The persistence layer supports SQLite (embedded/WAL), Microsoft SQL Server (stored procedure suite), and MySQL 8.0+ (`p_` parameter bindings). All 12 core entity tables and their relational constraints are modeled below:

```mermaid
erDiagram
    Servers ||--o{ Tools : "exposes (FK: ServerId)"
    Servers ||--o{ AccessPolicies : "governed by (TargetId)"
    Servers ||--o{ AuditLogs : "generates (ServerCodeName)"
    Servers }o--o| SecretProviders : "resolves credentials (SecretProvider)"
    
    Tools ||--o{ ToolAccessPolicies : "governed by (FK: ToolId)"
    Tools ||--o{ AccessPolicies : "governed by (TargetId)"
    
    AdGroups ||--o{ ToolAccessPolicies : "assigned to (FK: GroupId)"
    AdGroups ||--o{ GroupMappings : "maps external groups (InternalGroup)"
    
    AppKeys ||--o{ AuditLogs : "attributed via (UserSid -> OwnerSid)"
    
    Servers {
        string Id PK "Server unique identifier (e.g. docker, plex)"
        string DisplayName "Human-readable server name"
        string Url "Endpoint URL or command string"
        boolean Enabled "Active/inactive operational state"
        boolean Hidden "Hidden from client discovery list"
        string Type "Transport type: sse, http, stdio"
        string SecretProvider "Provider: None, HashiCorpVault, WindowsRegistry, Environment"
        string SecretItemKey "Target secret key identifier"
        string SecretMount "Vault secret engine mount path"
        string SecretPath "Vault secret subpath or registry key"
        string SecretField "Vault secret JSON field key"
        string AuthShape "Authentication shape: bearer, customHeader, query"
        string CustomHeaderName "Custom HTTP header name if shape=customHeader"
        string Categories "JSON array of category tags"
        string ApiKey "Static API key (redacted in API)"
        string HeadersJson "JSON dictionary of custom HTTP headers"
        boolean AutoDiscovered "Flag indicating dynamic auto-discovery"
    }

    Settings {
        string Id PK "Global singleton configuration ID"
        string EmbeddingProvider "Embedding backend: local, openai, custom"
        string EmbeddingApiUrl "Embedding inference API endpoint"
        string EmbeddingApiKey "Encrypted embedding API key"
        string EmbeddingApiModel "Embedding model name"
        string EmbeddingModelDir "Filesystem directory for local ONNX model"
        boolean RequireManualApproval "Require admin approval for high-risk tools"
        int GlobalMaxKeys "Maximum total active AppKeys allowed"
        int UserMaxKeys "Maximum active AppKeys allowed per user"
    }

    SecretProviders {
        int ProviderId PK "Provider integer surrogate key"
        string ProviderName UK "Unique provider identifier (e.g. HashiCorpVault)"
        string DisplayName "Human-readable provider name"
        string EncryptedConfigJson "AES-256-GCM encrypted provider configuration"
        boolean IsEnabled "Provider enabled state"
        datetime UpdatedAt "Timestamp of last modification"
    }

    AuthProviderConfigs {
        int AuthId PK "Auth provider surrogate key"
        string ProviderName UK "Unique identity provider identifier (e.g. ActiveDirectory)"
        string DisplayName "Human-readable identity provider name"
        string UserHeader "HTTP header for username (default: Remote-User)"
        string GroupsHeader "HTTP header for user groups (default: Remote-Groups)"
        string EncryptedConfigJson "AES-256-GCM encrypted provider settings"
        boolean IsEnabled "Identity provider enabled state"
        datetime UpdatedAt "Timestamp of last modification"
    }

    AdGroups {
        int GroupId PK "Group integer surrogate key"
        string ObjectSid UK "Active Directory Security Identifier (SID)"
        string GroupName "Active Directory / Enterprise group name"
        string Description "Group description and purpose"
        boolean IsActive "Group active state"
        datetime CreatedAt "Timestamp of creation"
    }

    Tools {
        int ToolId PK "Tool integer surrogate key"
        string ServerId FK "Foreign key referencing Servers.Id"
        string ToolName "MCP tool name (e.g. list_containers)"
        string Description "Tool description shown to LLMs"
        string InputSchemaJson "JSON Schema defining tool arguments"
        string VaultSecretPath "Optional tool-specific secret path"
        string SecretProvider "Tool-specific secret provider"
        boolean IsEnabled "Tool enabled state"
        datetime CreatedAt "Timestamp of tool discovery"
    }

    ToolAccessPolicies {
        int ToolPolicyId PK "Surrogate policy primary key"
        int ToolId FK "Foreign key referencing Tools.ToolId"
        int GroupId FK "Foreign key referencing AdGroups.GroupId"
        boolean IsAllowed "Allow (1) or Explicit Deny (0)"
        int RateLimitPerMin "Rate limit allocations per minute"
        datetime CreatedAt "Timestamp of policy creation"
    }

    AccessPolicies {
        string Id PK "Policy unique GUID identifier"
        string TargetId "Target server ID, category, or tool identifier"
        string RequiredGroup "AD group, SID, or role required for access"
        boolean IsAllowed "Allow (1) or Explicit Deny (0)"
        datetime CreatedAt "Timestamp of policy assignment"
    }

    GroupMappings {
        string Id PK "Mapping unique GUID identifier"
        string ExternalId "External group claim or SSO header value"
        string InternalGroup "Internal mapped router role / AD group"
        datetime CreatedAt "Timestamp of mapping creation"
    }

    AppKeys {
        string Id PK "AppKey unique GUID identifier"
        string Name "Friendly application/client name"
        string Username "Subject username associated with key"
        string KeyPrefix UK "High-entropy random key prefix"
        string EncryptedKey "Argon2id / PBKDF2 hash of secret key"
        string ScopesJson "JSON array of allowed scopes (*, category:*, server:*)"
        datetime ExpiresAt "Optional key expiration UTC timestamp"
        datetime CreatedAt "Timestamp of key creation"
        string OwnerSid "Target user SID (decoupled from admin creator)"
    }

    AuditLogs {
        bigint AuditId PK "Audit entry sequential identifier"
        string RequestId "Unique client request GUID"
        string UserPrincipalName "Caller username or identity"
        string UserSid "Caller Active Directory SID or AppKey OwnerSid"
        string ServerCodeName "Target MCP server code name"
        string ItemName "Target tool, prompt, or resource URI"
        string RequestMethod "MCP method: tools/call, prompts/get, etc."
        int ExecutionTimeMs "Total round-trip execution latency"
        int StatusCode "HTTP or JSON-RPC status code"
        string RequestPayload "Masked request payload"
        string ResponsePayload "Masked response payload"
        string ErrorMessage "Error message if invocation failed"
        datetime Timestamp "UTC timestamp of execution"
    }

    AdminAuditLogs {
        string Id PK "Admin audit GUID identifier"
        string Username "Administrator username"
        string Action "Administrative action performed"
        string Target "Target configuration entity"
        string Details "Detailed changes or audit payload"
        boolean Success "Action success status"
        string ErrorMessage "Error details if action failed"
        datetime Timestamp "UTC timestamp of administrative action"
    }
```

For complete database schema scripts, T-SQL and MySQL stored procedures, and Docker configurations, see [**`docs/database-providers.md`**](docs/database-providers.md).

---

## 📚 Architectural Guides & Specifications

For comprehensive guides covering each individual subsystem in depth, refer to the technical specification library:

| Specification Document | Focus Area |
| :--- | :--- |
| [**`docs/architecture.md`**](docs/architecture.md) | **Master Architectural Specification & Comprehensive Deep-Dive** |
| [**`docs/transports.md`**](docs/transports.md) | Downstream Transports, Concurrency & Subprocess STDIO Lifecycle |
| [**`docs/appkey-scopes.md`**](docs/appkey-scopes.md) | AppKey Scopes, Granular Permissions & Multi-Stage Authorization |
| [**`docs/database-providers.md`**](docs/database-providers.md) | SQLite WAL, MS SQL Server & MySQL Stored Procedure Dialects |
| [**`docs/secret-providers.md`**](docs/secret-providers.md) | HashiCorp Vault, Windows Registry DPAPI & AES-256-GCM Crypto |
| [**`docs/ci-quality-gates.md`**](docs/ci-quality-gates.md) | Automated PR Quality Gates, Static Analysis & Testing Contracts |
| [**`docs/testing-matrix.md`**](docs/testing-matrix.md) | Pairwise Integration Matrix & Multi-User E2E Fixtures |
| [**`docs/evaluation-guide.md`**](docs/evaluation-guide.md) | Product Evaluation, Context Reduction & Gateway Comparison |
| [**`docs/developer-guide.md`**](docs/developer-guide.md) | Developer Setup, Coding Standards & Testing Protocols |
| [**`docs/runbook.md`**](docs/runbook.md) | Production Operations, Deployment, Backups & Disaster Recovery |

---

*Last Updated: Release `v4.13.0`*
