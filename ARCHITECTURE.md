# MCP Router Architecture

This document outlines the internal architecture and design patterns used within the C# MCP Router. The codebase follows SOLID principles to ensure maintainability, testability, and clear separation of concerns.

## Core Components

The router's core logic resides within the `/Core` namespace, which is broken down into several specialized sub-systems.

### 1. Connection & Session Management
- **`ClientSession`**: Acts as the central orchestrator for a single connected client. Rather than executing logic directly, it delegates processing to specialized routing managers.
- **`BackendConnection`**: A unified facade that manages the connection to a single backend MCP server. It encapsulates a transport strategy (HTTP or SSE) and state tracking, shielding the rest of the application from backend protocol differences.

### 2. Transport Layer (`McpRouter.Core.Transports`)
The transport system abstracts the physical connection to backend servers.
- **`ITransport`**: Defines the standard interface for sending requests, notifications, and starting background readers.
- **`SseTransport`**: Implements asynchronous, persistent Server-Sent Events connections.
- **`HttpTransport`**: Implements synchronous POST-based HTTP connections.
- **`JsonRpcStateManager`**: A thread-safe concurrency manager that tracks pending JSON-RPC requests across transports using `ConcurrentDictionary` and `TaskCompletionSource`.

### 3. Routing Layer (`McpRouter.Core.Routing`)
The routing layer is responsible for intercepting client requests, rewriting request payloads (such as virtual URIs or namespaced tools), and forwarding them to the appropriate backend.
- **`ToolRoutingManager`**: Manages the aggregation, caching, and execution of tools across all connected servers. It handles the `serverId__toolName` namespace mapping.
- **`ResourceRoutingManager`**: Manages the discovery and retrieval of MCP resources, mapping backend URIs to virtual `mcp://{serverId}/{uri}` endpoints.
- **`PromptRoutingManager`**: Handles prompt discovery and execution using the same namespacing strategy as tools.
- **`SemanticSearchService`**: An independent service responsible for in-memory TF-IDF vectorization and cosine similarity scoring. This powers the Meta-Mode `search_tools` functionality.

### 4. Native Tools (`McpRouter.CustomTools`)
- **`ICustomTool`**: An interface for defining natively executed C# tools.
- **`CustomToolRegistry`**: A dynamic service locator that registers and retrieves native tools on-demand. Native tools bypass the backend transport layer entirely.

## Dependency Injection & Pipeline Setup
The router is built on ASP.NET Core. To keep `Program.cs` lightweight, configuration logic is encapsulated in extension methods under the `/Extensions` folder:
- **`ServiceCollectionExtensions.cs`**: Registers Database, OpenIddict (OAuth), HTTP Clients, and custom singleton/scoped services.
- **`ApplicationBuilderExtensions.cs`**: Configures the HTTP request pipeline, including CORS, Authentication, Static Files, and minimal API endpoints.

## Message Flow Diagram

```mermaid
sequenceDiagram
    participant Client
    participant ClientSession
    participant RoutingManager
    participant BackendConnection
    participant ITransport
    participant BackendServer
    
    Client->>ClientSession: POST /message (tools/call)
    ClientSession->>RoutingManager: CallToolAsync(name, body)
    RoutingManager->>RoutingManager: Parse serverId from tool name
    RoutingManager->>BackendConnection: SendRequestAsync(modifiedBody)
    BackendConnection->>ITransport: SendRequestAsync()
    ITransport->>BackendServer: POST (HTTP or SSE endpoint)
    BackendServer-->>ITransport: JSON-RPC Response
    ITransport-->>BackendConnection: Resolve TaskCompletionSource
    BackendConnection-->>RoutingManager: JsonRpcResponse
    RoutingManager-->>ClientSession: Return Payload
    ClientSession-->>Client: 200 OK
```
