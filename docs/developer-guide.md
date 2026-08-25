# Developer & Contributor Guide

This document defines setup instructions, architectural conventions, coding guidelines, testing protocols, version management rules, and release verification workflows for the **MCP Router Gateway & Semantic Proxy**.

---

## 📑 Table of Contents

- [Prerequisites & Development Environment](#prerequisites-development-environment)
- [Repository Structure & Architecture Conventions](#repository-structure-architecture-conventions)
- [Local Development Workflow](#local-development-workflow)
  - [Backend (.NET 10 C#)](#backend-net-10-c)
  - [Frontend (React 19 / Vite / TypeScript)](#frontend-react-19-vite-typescript)
- [Automated Testing & Code Coverage](#automated-testing-code-coverage)
  - [Backend Test Suite](#backend-test-suite)
  - [Frontend Vitest Suite](#frontend-vitest-suite)
  - [End-to-End Testing (Playwright)](#end-to-end-testing-playwright)
- [Formatting, Linting & Static Analysis](#formatting-linting-static-analysis)
- [Version Synchronization & Release Verification](#version-synchronization-release-verification)
  - [Mandatory Version Synchronization Contract](#mandatory-version-synchronization-contract)
  - [Release Verification Script (`verify-release.sh`)](#release-verification-script-verify-releasesh)
  - [CLI Flags & Options Reference](#cli-flags-options-reference)
  - [Automated Version Bumping & Atomic Commits](#automated-version-bumping-atomic-commits)
- [Continuous Integration & Quality Gates](#continuous-integration-quality-gates)

---

## 🛠️ Prerequisites & Development Environment

Ensure the following toolchains are installed:

| Tool / Runtime | Minimum Version | Purpose |
| :--- | :--- | :--- |
| **.NET SDK** | `10.0.x` | Compiling C# backend, minimal APIs, Dapper repositories, and xUnit tests |
| **Node.js** | `22.x LTS` | Vite development server, ESLint v10, Vitest, and React 19 UI build |
| **npm** | `10.x+` | Package management for the frontend SPA |
| **Python** | `3.10+` | Release verification and automated version bump scripts |
| **Docker** | `24.x+` | Multi-stage container builds and integration test environments |

---

## 🏛️ Repository Structure & Architecture Conventions

The repository uses the following domain boundaries:

```
├── Components/                 # Decomposed domain modules & Minimal API mappers
│   ├── AppKeys/                # High-entropy AppKey models, crypto, and endpoints
│   ├── Authorization/          # RBAC access policies, group mappings, and controllers
│   ├── Capabilities/           # Proxy endpoints, SSE/HTTP mappers, and custom tools
│   ├── Clients/                # Registered client profiles and setup guide generators
│   ├── Providers/              # Identity and secret provider configurations & controllers
│   └── Servers/                # Upstream server registry, health checks, and discovery
├── Core/                       # Core routing engine & MCP protocol handlers
│   ├── Protocol/               # JSON-RPC 2.0 message contracts and spec models
│   └── Routing/                # ClientSession, DynamicEmbeddingService, SemanticSearch
├── Infrastructure/             # Persistence, secrets, identity, and logging adapters
│   ├── Identity/               # Active Directory (LDAP), OIDC, and AppKey providers
│   ├── Logging/                # PII sanitization, structured audit logging, ring buffers
│   ├── Persistence/            # DbConnectionFactory & Repositories (see [Database ERD](database-providers.md#unified-database-entity-relationship-diagram-erd))
│   ├── Secrets/                # Vault KV v2, DPAPI, Environment, and AES-256-GCM crypto
│   └── Transports/             # SseTransport, HttpTransport, and StdioTransport
├── frontend/                   # React 19 + Vite + TypeScript glassmorphic SPA
│   ├── src/api/                # Typed API client layer
│   ├── src/components/         # Domain-decomposed UI views, modals, and tabs
│   ├── src/shared/stores/      # Zustand state management stores
│   └── src/test/               # Vitest component, store, and unit test suites
├── McpRouter.Tests/            # 500+ xUnit integration, security, and contract tests
├── scripts/                    # Release verification, version bumping, and DB DDL scripts
└── docs/                       # Architectural specifications and user guides
```

---

## 💻 Local Development Workflow

### Backend (.NET 10 C#)

1. **Restore dependencies & build**:
   ```bash
   dotnet restore McpRouter.slnx
   dotnet build McpRouter.slnx --configuration Debug
   ```

2. **Run the gateway locally**:
   ```bash
   # Runs on http://localhost:8080 with ephemeral SQLite database
   dotnet run --project mcp-router.csproj
   ```

3. **Configure environment overrides**:
   ```bash
   # Custom database path and admin bypass SID
   ROUTER_DATABASE_PATH="./data/dev.db" \
   ROUTER_MASTER_KEY="dev-master-key-32-chars-long!" \
   dotnet run --project mcp-router.csproj
   ```

### Frontend (React 19 / Vite / TypeScript)

1. **Install dependencies**:
   ```bash
   cd frontend
   npm ci
   ```

2. **Start Vite development server**:
   ```bash
   npm run dev
   ```
   The development proxy routes `/api`, `/sse`, and `/mcp` traffic directly to `http://localhost:8080`.

3. **Build production bundle**:
   ```bash
   npm run build
   ```

---

## 🧪 Automated Testing & Code Coverage

### Backend Test Suite

The C# suite includes 500+ unit, integration, and security contract tests:

```bash
# Run all backend tests
CI=true dotnet test McpRouter.slnx --configuration Release

# Collect code coverage
CI=true dotnet test McpRouter.slnx --configuration Release --collect:"XPlat Code Coverage"
```

### Frontend Vitest Suite

The frontend suite covers Zustand stores, typed API handlers, and React components:

```bash
cd frontend

# Run test suite once
npm test

# Run tests with coverage
npm run test:coverage
```

### End-to-End Testing (Playwright)

Execute UI workflows across multi-user security matrices:

```bash
cd frontend
npx playwright test
```

### Living Software Requirements Specification (SRS) & Test Catalog

Requirements and safety guardrails are annotated in C# and TypeScript tests. To regenerate or verify the catalog:

```bash
# Generate human-readable Markdown and machine JSON matrix
dotnet run --project scripts/CatalogGenerator

# Verify zero-drift in CI quality gates
dotnet run --project scripts/CatalogGenerator -- --verify-only
```

* **Living SRS Document:** [`docs/software-requirements-and-test-catalog.md`](software-requirements-and-test-catalog.md)
* **Test Catalog & Annotation Guide:** [`docs/test-catalog-guide.md`](test-catalog-guide.md)


---

## 🎨 Formatting, Linting & Static Analysis

1. **C# Backend Formatting & Roslyn Analyzers**:
   ```bash
   dotnet format McpRouter.slnx --verify-no-changes
   ```
   Rules are defined in `.editorconfig` and `Directory.Build.props`.

2. **Frontend ESLint (Zero-Warning Policy)**:
   ```bash
   cd frontend
   npm run lint
   ```
   Uses ESLint v10 flat configuration (`frontend/eslint.config.js`).

---

## 🚀 Version Synchronization & Release Verification

### Mandatory Version Synchronization Contract

Every release, pull request, and commit to `main` must synchronize the version number across **four mandatory locations**:

1. **`mcp-router.csproj`**:
   - `<Version>X.Y.Z</Version>`
   - `<AssemblyVersion>X.Y.Z.0</AssemblyVersion>`
   - `<FileVersion>X.Y.Z.0</FileVersion>`
2. **`frontend/src/shared/stores/useUserStore.ts`**:
   - `version: 'X.Y.Z', // fallback default`
3. **`CHANGELOG.md`**:
   - Top entry row in Release Changelog table matching `| **`vX.Y.Z`** | YYYY-MM-DD | ... |`
4. **`README.md`**:
   - Shield badge: `![Version](https://img.shields.io/badge/version-vX.Y.Z-orange?style=for-the-badge)`
   - Top entry row in the top-5 release preview table

### Release Verification Script (`verify-release.sh`)

The release verification engine is located at `scripts/verify_release.py` with a bash wrapper `scripts/verify-release.sh`.

```bash
# 🛡️ Run full verification suite (versions, links, tests, builds)
./scripts/verify-release.sh
```

```
==================================================================
  🛡️  MCP Router - Release & Quality Verification Engine  🛡️  
==================================================================
  Repository Root: /containers/dev/csharp-mcp-router

🏷️  1. Version Synchronization & Consistency
------------------------------------------------------------------
  [PASS] Canonical Version (4.12.2) in mcp-router.csproj
  [PASS] Csproj <AssemblyVersion> Alignment
  [PASS] Csproj <FileVersion> Alignment
  [PASS] React Store Fallback Version (frontend/src/shared/stores/useUserStore.ts)
  [PASS] CHANGELOG.md Top Entry Alignment
  [PASS] README.md Version Badge Alignment
  [PASS] README.md Release Preview Top Entry

🔗  2. Markdown Link & Anchor Integrity
------------------------------------------------------------------
  [PASS] Scanned Markdown Files (45 files discovered)
  [PASS] Relative Links & Anchor Validity (151 links verified)

🧪  3. Backend .NET Build & Test Verification
------------------------------------------------------------------
  [PASS] .NET Backend Test Suite (500+ tests)

⚛️  4. Frontend Quality, Lint, Build & Vitest Verification
------------------------------------------------------------------
  [PASS] Frontend ESLint Quality Check (0 warnings)
  [PASS] Frontend Vite Production Build (SPA)
  [PASS] Frontend Vitest Component & Store Suite

==================================================================
  📊  Release Verification Summary Report  📊  
==================================================================
  Total Checks:    13
  Passed Checks:   13
  Failed Checks:   0
------------------------------------------------------------------
  🎉 ALL RELEASE & QUALITY GATES PASSED CLEANLY! 🎉
==================================================================
```

### CLI Flags & Options Reference

Available flags:

| Flag | Purpose | Example |
| :--- | :--- | :--- |
| **`--skip-tests`** | Skips slow backend/frontend test execution; executes fast version sync and markdown link verification in <2s. | `./scripts/verify-release.sh --skip-tests` |
| **`--skip-links`** | Skips markdown link and anchor validation. | `./scripts/verify-release.sh --skip-links` |
| **`--skip-versions`** | Skips version synchronization checks. | `./scripts/verify-release.sh --skip-versions` |
| **`--check-versions-only`** | Executes only the version synchronization validation. | `python3 scripts/verify_release.py --check-versions-only` |
| **`--check-links-only`** | Executes only the markdown relative link and anchor validation. | `python3 scripts/verify_release.py --check-links-only` |
| **`--check-tests-only`** | Executes only backend and frontend test/build suites. | `python3 scripts/verify_release.py --check-tests-only` |
| **`--ci`** | Streamlined output mode designed for automated CI environments. | `python3 scripts/verify_release.py --ci` |
| **`-v`, `--verbose`** | Enables verbose logging with detailed check descriptions. | `./scripts/verify-release.sh -v` |

### Automated Version Bumping & Atomic Commits

Bump the version and commit atomically:

```bash
./commit.sh "feat(auth): add fine-grained category scopes"
```

The script executes the following:
1. Validates the .NET project build.
2. Invokes `scripts/bump_version.py` to increment the version (minor for `feat:`/breaking changes, patch for `fix:`/`docs:`).
3. Synchronizes all version references (`.csproj`, `useUserStore.ts`, `CHANGELOG.md`, `README.md`).
4. Creates a clean, atomic git commit.

---

## 🔒 Continuous Integration & Quality Gates

Pull requests to `main` execute the quality gates defined in `.github/workflows/ci.yml`:

1. **`release-verification`**: Validates version synchronization and ensures 0 broken markdown links/anchors.
2. **`backend`**: Runs `dotnet build` (`Release`) and the full 500+ xUnit test suite with coverage collection.
3. **`frontend`**: Enforces strict zero-warning ESLint, builds the Vite production SPA, and runs Vitest suites.
4. **`integration-smoke`**: Boots the compiled Release binary on an ephemeral Kestrel port with an isolated SQLite database, testing health probes, AppKey minting, and live MCP discovery.
5. **`docker-check`**: Validates multi-stage Docker build integrity.
6. **`CodeQL` & `Dependency Review`**: Static security analysis and vulnerability scanning.

For further details on CI workflows, branch protection rules, and coverage metrics, see [**CI Quality Gates & Security Workflows**](ci-quality-gates.md) and [**Code Coverage Report**](coverage-report.md).
