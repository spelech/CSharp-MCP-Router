# Housekeeping: Code Coverage Expansion & Documentation Synchronization Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand test coverage across untested API controllers (`AppKeysController`, `PermissionsController`, `ProvidersController`), core secret retrievers, audit loggers, and routing managers, while comprehensively updating all documentation (`README.md`, `docs/features-guide.md`, `AGENTS.md`, `.agents/GEMINI.md`) to accurately reflect v4.0.0 features and configurations.

**Architecture:** Add targeted xUnit test suites utilizing ASP.NET Core test harnesses and Moq / SQLite in-memory databases to test controller actions, secret retrieval fallbacks, and audit logging. Update markdown documentation to align with all v4.0.0 API endpoints, environment variables, security constraints, and database schemas.

**Tech Stack:** C# .NET 10, ASP.NET Core, Entity Framework Core, Dapper, SQLite, xUnit, Moq, Coverlet.

## Global Constraints

- **Rule 1: ALL TESTS MUST PASS** — Run `dotnet test McpRouter.slnx` after every task.
- **Rule 2: NO PRODUCT CODE BREAKING CHANGES** — Tests and docs must validate existing code behavior without altering established v4.0.0 security contracts.
- **Rule 3: ACCURATE DOCS** — All endpoint paths, environment flags, and configuration keys in docs must match exact strings in code.

---

### Task 1: Controller Unit & Integration Test Suites

**Files:**
- Create: `McpRouter.Tests/AppKeysControllerTests.cs`
- Create: `McpRouter.Tests/PermissionsControllerTests.cs`
- Create: `McpRouter.Tests/ProvidersControllerTests.cs`

**Interfaces:**
- Consumes: `AppKeysController`, `PermissionsController`, `ProvidersController`, `RouterDbContext`, `IDbConnectionFactory`
- Produces: Test coverage for API controller actions (`GetAppKeys`, `CreateAppKey`, `RevokeAppKey`, `GetAppKeysLimits`, `GetPolicies`, `SavePolicy`, `DeletePolicy`, `GetMappings`, `SaveMapping`, `DeleteMapping`, `GetSecretProviders`, `SaveSecretProvider`, `GetAuthProviders`, `SaveAuthProvider`)

- [ ] **Step 1: Write test suite for AppKeysController**
- [ ] **Step 2: Write test suite for PermissionsController**
- [ ] **Step 3: Write test suite for ProvidersController**
- [ ] **Step 4: Run controller unit tests**
- [ ] **Step 5: Commit controller test suites**

---

### Task 2: Core Secret Retrievers, AuditLogger & Routing Unit Tests

**Files:**
- Create: `McpRouter.Tests/SecretRetrieverTests.cs`
- Create: `McpRouter.Tests/AuditLoggerTests.cs`
- Create: `McpRouter.Tests/RoutingManagerTests.cs`

- [ ] **Step 1: Write SecretRetrieverTests**
- [ ] **Step 2: Write AuditLoggerTests**
- [ ] **Step 3: Write RoutingManagerTests**
- [ ] **Step 4: Run core unit tests**
- [ ] **Step 5: Commit core test suites**

---

### Task 3: Comprehensive Documentation Refresh

**Files:**
- Modify: `README.md`
- Modify: `docs/features-guide.md`
- Modify: `AGENTS.md`
- Modify: `.agents/GEMINI.md`

- [ ] **Step 1: Update README.md**
- [ ] **Step 2: Update docs/features-guide.md**
- [ ] **Step 3: Update AGENTS.md & .agents/GEMINI.md**
- [ ] **Step 4: Commit documentation updates**

---

### Task 4: Code Coverage Audit & Final Verification

- [ ] **Step 1: Run full test suite with code coverage collection**
- [ ] **Step 2: Generate coverage summary**
- [ ] **Step 3: Commit final housekeeping changes**
