# MCP Gateway Router: Developer Guide

A comprehensive guide for setting up, developing, testing, debugging, and extending the **CSharp-MCP-Router** gateway codebase.

---

## 🛠️ Prerequisites & Tooling

Ensure the following runtimes and tools are installed on your workstation:

| Tool | Minimum Version | Purpose |
| :--- | :--- | :--- |
| **.NET SDK** | `10.0.100+` | C# ASP.NET Core backend runtime and compiler |
| **Node.js** | `v22.x LTS` (Node 20+ supported) | Frontend Vite build toolchain and package manager |
| **npm** | `10.x+` | Frontend dependency management |
| **Docker & Compose** | `24.0+` | Container builds and local multi-service testing |
| **Git** | `2.40+` | Version control and worktree management |

Verify your environment:
```bash
dotnet --version
node -v
npm -v
```

---

## 🚀 Quickstart: Local Development Setup

### 1. Clone & Restore Dependencies

```bash
# Clone the repository
git clone https://github.com/spelech/CSharp-MCP-Router.git
cd CSharp-MCP-Router

# Restore .NET solution dependencies
dotnet restore McpRouter.slnx

# Install frontend npm packages
cd frontend
npm install
cd ..
```

---

### 2. Configuration & Secrets Setup

The backend reads configuration from `appsettings.json` and environment variables. For local development, create `appsettings.Development.json` if custom overrides are needed:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=mcp-router-dev.db"
  },
  "Security": {
    "MasterKey": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
  }
}
```

---

### 3. Running Backend & Frontend Concurrently

#### Option A: Run Backend (Kestrel Server)
```bash
# From repository root
dotnet run --project mcp-router.csproj --launch-profile http
```
* Backend listens on `http://localhost:5000` (or `http://localhost:8080` depending on profile).
* API endpoints: `/api/servers`, `/api/appkeys`, `/api/settings`, `/health`, `/sse`.

#### Option B: Run Frontend Development Server (Vite Hot-Module-Replacement)
```bash
cd frontend
npm run dev
```
* Vite dev server listens on `http://localhost:5173`.
* Vite automatically proxies API requests (`/api/*`, `/sse`, `/health`) to the backend server configured in `frontend/vite.config.ts`.

---

## 🏗️ Repository Architecture & Project Layout

The repository follows a clean domain-driven structure:

```
CSharp-MCP-Router/
├── Core/                      # Core MCP routing engine & session management
│   ├── ClientSession.cs       # Client SSE/HTTP session state & tool routing
│   ├── BackendConnection.cs   # Downstream server connection & health lifecycle
│   ├── SessionManager.cs      # Concurrent session tracking & broadcasts
│   └── CustomTools.cs         # In-memory virtual tool registration
├── Components/                # Modular domain endpoint mappers & services
│   ├── Servers/               # Server registration, health, and inspect endpoints
│   ├── AppKeys/               # AppKey generation, hashing, and scope validation
│   ├── TestBench/             # Interactive tool, resource, and prompt test endpoints
│   ├── Settings/              # Vector, security, and provider settings
│   └── Users/                 # Current identity & group context
├── Infrastructure/            # Pluggable persistence & security implementations
│   ├── Database/              # Dapper providers: SQLite, SqlServer, MySql
│   ├── Secrets/               # Secret retrievers: Vault KV v2, Registry, Env
│   ├── Identity/              # Identity providers: OIDC, Active Directory, AppKeys
│   ├── Embeddings/            # ONNX in-process & OpenAI API embedding engines
│   └── Security/              # AES-256-GCM envelope encryption & PII sanitizer
├── frontend/                  # Vite + React 19 SPA frontend
│   ├── src/
│   │   ├── components/        # Domain UI components (servers, security, testbench, settings)
│   │   ├── api/               # Typed API client services
│   │   ├── stores/            # Zustand reactive state stores
│   │   └── styles/            # Centralized CSS stylesheets & variables
│   └── package.json
├── McpRouter.Tests/           # xUnit integration & unit test suite (515+ tests)
├── McpRouter.slnx             # .NET XML-based Solution file
└── mcp-router.csproj          # C# Project specification
```

---

## 🧪 Testing Workflows

Our quality gates require that all backend and frontend test suites pass with zero warnings before any merge.

### 1. Running Backend Unit & Integration Tests (xUnit)
```bash
# Run all tests with standard output
CI=true dotnet test McpRouter.slnx

# Run tests with detailed code coverage collection
CI=true dotnet test McpRouter.slnx --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
```

### 2. Running Frontend Unit & Component Tests (Vitest)
```bash
cd frontend

# Run test suite once
npm test

# Run tests in watch mode
npm run test:watch

# Run coverage analysis
npm run test:coverage
```

### 3. Running Linting & Code Style Verifications
```bash
# Verify C# code formatting via Roslyn analyzers
dotnet format McpRouter.slnx --verify-no-changes

# Run ESLint on frontend TypeScript / React files
cd frontend
npm run lint
```

---

## 💻 Coding Guidelines & Architectural Rules

### 1. JSON Payload Handling & Serialization
* **NEVER** use string manipulation (`string.Replace`, `Substring`, regex) to modify or rewrite JSON-RPC payloads.
* **ALWAYS** use C# `JsonNode`, `JsonObject`, or `JsonDocument` parsing (see `ClientSession.RewriteRequestJson`).
* **`JsonRpcMessageConverter` Rule**: This custom polymorphic converter handles JSON-RPC messages at the network boundary. **Do NOT** register it globally or invoke it recursively in nested objects to prevent stack overflow errors.

### 2. Thread Safety & Concurrency
* Backend connection handlers, session lifecycles, and tool caches must use thread-safe collections (`ConcurrentDictionary<string, T>`).
* Downstream connection warming and token refreshes must use async synchronization primitives (`SemaphoreSlim`) to prevent thundering-herd issues.

### 3. Frontend Architecture & Design Rules
* **Design Consistency**: All colors must use CSS variables declared in `frontend/src/styles/variables.css`. Do **not** hardcode HEX or RGB values in components.
* **Theme**: Main accent uses vibrant Orange (`#f97316` / primary) and Yellow (`#eab308` / secondary) glassmorphic styling.
* **Prevent Layout Shifts**:
  * Set `align-items: flex-start` on body containers to prevent vertical jumps when switching tabs of varying heights.
  * Set `scrollbar-gutter: stable` on the `html` element to eliminate horizontal scrollbar shifts.

### 4. Mandatory Versioning Rule
* **EVERY COMMIT OR MERGE TO `main` MUST BUMP THE VERSION NUMBER.**
* When releasing changes:
  1. `mcp-router.csproj` (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`)
  2. `frontend/src/stores/useUserStore.ts` (React fallback version)
  3. `CHANGELOG.md` (Add release entry to the Release Changelog table)
  4. `README.md` (Update top-5 release preview table)

---

## 🔍 Debugging Tips

### 1. Inspecting Live JSON-RPC Traffic
Enable trace logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "McpRouter": "Trace"
    }
  }
}
```

### 2. Testing MCP Endpoints with MCP Inspector
Use the official Model Context Protocol Inspector to interactively debug endpoints:
```bash
npx @modelcontextprotocol/inspector http://localhost:5000/sse
```
