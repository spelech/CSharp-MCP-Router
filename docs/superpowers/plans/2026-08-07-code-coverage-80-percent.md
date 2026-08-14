# 80% Code Coverage Target Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand the xUnit unit/integration test suite for `CSharp-MCP-Router` from 42.01% up to at least 80% total codebase line coverage.

**Architecture:** Create targeted xUnit test classes using Moq and in-memory test doubles/harnesses for un-tested subsystems: `VaultSecretRetriever`, `CompositeSecretRetriever`, `ToolRoutingManager`, `ResourceRoutingManager`, `LdapActiveDirectoryService`, `SessionManager`, `PlexGetSessionsTool`, `ClientSession`, and `AuthorizationController`.

**Tech Stack:** .NET 10.0, xUnit, Moq, Dapper, Microsoft.Data.Sqlite, Coverlet / XPlat Code Coverage.

---

## Task Breakdown

### Task 1: Secret Retrievers & Vault Test Suite
**Files:**
- Create: `McpRouter.Tests/VaultSecretRetrieverTests.cs`
- Create: `McpRouter.Tests/CompositeSecretRetrieverTests.cs`

- [ ] **Step 1: Write VaultSecretRetriever and CompositeSecretRetriever unit tests**
- [ ] **Step 2: Run tests and verify 100% pass rate**
- [ ] **Step 3: Commit**

---

### Task 2: Routing Managers Test Suite (`ToolRoutingManager` & `ResourceRoutingManager`)
**Files:**
- Create: `McpRouter.Tests/ToolRoutingManagerTests.cs`
- Create: `McpRouter.Tests/ResourceRoutingManagerTests.cs`

- [ ] **Step 1: Write ToolRoutingManager and ResourceRoutingManager unit tests**
- [ ] **Step 2: Run tests and verify 100% pass rate**
- [ ] **Step 3: Commit**

---

### Task 3: Identity & LDAP Service Test Suite (`LdapActiveDirectoryService` & `CompositeIdentityProvider`)
**Files:**
- Create: `McpRouter.Tests/LdapActiveDirectoryServiceTests.cs`

- [ ] **Step 1: Write LdapActiveDirectoryService unit tests**
- [ ] **Step 2: Run tests and verify 100% pass rate**
- [ ] **Step 3: Commit**

---

### Task 4: Session Infrastructure Test Suite (`SessionManager` & `ClientSession`)
**Files:**
- Create: `McpRouter.Tests/SessionManagerTests.cs`
- Modify: `McpRouter.Tests/ClientSessionTests.cs`

- [ ] **Step 1: Write SessionManager and ClientSession unit tests**
- [ ] **Step 2: Run tests and verify 100% pass rate**
- [ ] **Step 3: Commit**

---

### Task 5: Custom Tools & Authorization Controller Test Suite (`PlexGetSessionsTool` & `AuthorizationController`)
**Files:**
- Create: `McpRouter.Tests/PlexGetSessionsToolTests.cs`
- Modify: `McpRouter.Tests/AuthorizationControllerTests.cs`

- [ ] **Step 1: Write PlexGetSessionsTool and AuthorizationController tests**
- [ ] **Step 2: Run tests and verify 100% pass rate**
- [ ] **Step 3: Commit**

---

### Task 6: Final Coverage Verification & 80% Threshold Gate
**Files:**
- Output: `coverage.cobertura.xml`

- [ ] **Step 1: Run full Cobertura code coverage evaluation**
- [ ] **Step 2: Verify total line coverage >= 80%**
- [ ] **Step 3: Automated version bump & commit**
