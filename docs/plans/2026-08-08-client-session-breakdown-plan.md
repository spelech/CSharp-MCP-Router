# ClientSession Deep Modular Refactoring Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce `ClientSession.cs` from ~989 lines down to ~350 lines by extracting Authorization, Backend Initializer, and Proxy Forwarding partial class files.

**Architecture:** 
Use C# `partial class` files under `Core/ClientSession/`:
1. `ClientSession.Authorization.cs` (User identity resolution, RBAC evaluation, audit logging)
2. `ClientSession.BackendInitializer.cs` (Backend connection retries, auto-init tasks, cache triggers)
3. `ClientSession.ProxyForwarder.cs` (Client sampling, cancellation tokens, response handling, broadcast execution)

**Tech Stack:** C# ASP.NET Core (.NET 10), EF Core, Dapper, System.Text.Json, xUnit.

## Global Constraints

- Retain partial class `ClientSession` in namespace `McpRouter`.
- 100% backward compatibility of all internal and public methods.
- Ensure all 277 unit tests pass after each task.

---

### Task 1: Extract `ClientSession.Authorization.cs`

**Files:**
- Create: `Core/ClientSession/ClientSession.Authorization.cs`
- Modify: `Core/ClientSession.cs`
- Test: `McpRouter.Tests/ClientSessionTests.cs`

**Interfaces:**
- Consumes: `HttpContext`, User Claims, Database Policies
- Produces: `UserIdentityContext`, Authorization Boolean verdicts

- [ ] **Step 1: Create `Core/ClientSession/ClientSession.Authorization.cs`**

Move `ResolveUserIdentityAsync`, `IsUserAuthorizedAsync`, `FilterAuthorizedAsync`, and `AuditInvocationAsync` into partial class `ClientSession`.

- [ ] **Step 2: Remove extracted methods from `Core/ClientSession.cs`**

Remove the extracted authorization methods from `Core/ClientSession.cs`.

- [ ] **Step 3: Stage and commit**

```bash
git add Core/ClientSession/ClientSession.Authorization.cs
./commit.sh "refactor(session): extract ClientSession.Authorization partial class"
```

---

### Task 2: Extract `ClientSession.BackendInitializer.cs`

**Files:**
- Create: `Core/ClientSession/ClientSession.BackendInitializer.cs`
- Modify: `Core/ClientSession.cs`
- Test: `McpRouter.Tests/ClientSessionTests.cs`

**Interfaces:**
- Consumes: Backend `McpServer` configuration
- Produces: Connected & initialized `BackendConnection` instances

- [ ] **Step 1: Create `Core/ClientSession/ClientSession.BackendInitializer.cs`**

Move `EnsureBackendsInitializedAsync`, `StartInitialization`, `InitializeBackendsAsync`, `ConnectAndInitializeBackendAsync`, and `StartInitializationForBackend` into partial class `ClientSession`.

- [ ] **Step 2: Remove extracted methods from `Core/ClientSession.cs`**

Remove the extracted initialization methods from `Core/ClientSession.cs`.

- [ ] **Step 3: Stage and commit**

```bash
git add Core/ClientSession/ClientSession.BackendInitializer.cs
./commit.sh "refactor(session): extract ClientSession.BackendInitializer partial class"
```

---

### Task 3: Extract `ClientSession.ProxyForwarder.cs`

**Files:**
- Create: `Core/ClientSession/ClientSession.ProxyForwarder.cs`
- Modify: `Core/ClientSession.cs`
- Test: `McpRouter.Tests/ClientSessionTests.cs`

**Interfaces:**
- Consumes: `JsonRpcRequest`, request IDs
- Produces: `JsonRpcResponse`, cancellation signals, broadcast results

- [ ] **Step 1: Create `Core/ClientSession/ClientSession.ProxyForwarder.cs`**

Move `CancelRequest`, `TryHandleClientResponse`, `ForwardRequestToClientAsync`, and `BroadcastRequestAsync` into partial class `ClientSession`.

- [ ] **Step 2: Remove extracted methods from `Core/ClientSession.cs`**

Remove the extracted proxy forwarding methods from `Core/ClientSession.cs`.

- [ ] **Step 3: Run full test suite**

Run: `dotnet test McpRouter.slnx`
Expected: PASS (All 277 tests pass)

- [ ] **Step 4: Stage and commit**

```bash
git add Core/ClientSession/ClientSession.ProxyForwarder.cs
./commit.sh "refactor(session): extract ClientSession.ProxyForwarder partial class"
```
