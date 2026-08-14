# Issue 33 Implementation Plan (Sustained Load / Soak Test)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a sustained load/soak test to verify that socket/handle leaks and `PendingApprovals` growth are bounded under high traffic and reconnect scenarios.

**Architecture:** We will expose an `/api/diagnostics` endpoint on the router that reports current memory, open handles (FDs), active sessions, and pending approvals. A Node.js soak test script (`tests/load/soak-test.mjs`) will hammer the router with concurrent SSE connections and dropped sessions, asserting that the diagnostic metrics remain stable.

**Tech Stack:** C# (ASP.NET Core), Node.js (for load testing script)

## Global Constraints

- Must run correctly against the `docker-compose.test.yml` environment (which provides Vault).
- Must measure handle/FD count accurately on Linux.
- Must verify that `PendingApprovals` are cleaned up when clients disconnect.

---

### Task 1: Add `/api/diagnostics` Endpoint

**Files:**
- Modify: `Extensions/Endpoints/AdminEndpointsExtensions.cs`

**Interfaces:**
- Consumes: `SessionManager` state, `Process.GetCurrentProcess()` metrics.
- Produces: JSON response with `activeSessions`, `pendingApprovals`, `workingSet64`, `handleCount`.

- [ ] **Step 1: Write the endpoint implementation**
  Add the following endpoint to `AdminEndpointsExtensions.cs` (inside the `MapAdminEndpoints` extension method, grouping it with other admin APIs):

  ```csharp
  api.MapGet("/api/diagnostics", ([FromServices] McpRouter.Core.SessionManager sessionManager) => 
  {
      var proc = System.Diagnostics.Process.GetCurrentProcess();
      int fdCount = 0;
      
      // On Linux, count the file descriptors in /proc/self/fd to get an accurate handle/socket count.
      if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
      {
          try 
          {
              fdCount = System.IO.Directory.GetFiles($"/proc/{proc.Id}/fd").Length;
          }
          catch { /* Fallback if restricted */ }
      }
      
      return Microsoft.AspNetCore.Http.Results.Ok(new {
          activeSessions = sessionManager.ActiveSessionsCount,
          pendingApprovals = sessionManager.PendingApprovals.Count,
          workingSet64 = proc.WorkingSet64,
          handleCount = fdCount > 0 ? fdCount : proc.HandleCount
      });
  });
  ```

- [ ] **Step 2: Compile to verify**
  Run `dotnet build mcp-router.csproj` to ensure the new endpoint compiles cleanly.

- [ ] **Step 3: Commit**
  ```bash
  git add Extensions/Endpoints/AdminEndpointsExtensions.cs
  git commit -m "feat: add diagnostics API endpoint for soak testing metrics"
  ```

### Task 2: Create Node.js Soak Test Script

**Files:**
- Create: `tests/load/soak-test.mjs`

**Interfaces:**
- Consumes: Router running on `http://localhost:8080`, native Node 18+ `fetch`.

- [ ] **Step 1: Create the script directory**
  ```bash
  mkdir -p tests/load
  ```

- [ ] **Step 2: Write the soak test script**
  Create `tests/load/soak-test.mjs` with the full test logic using native Node `fetch` and `AbortController`. The test must run 20 concurrent workers, connect to `/sse`, abort connection rapidly, and verify diagnostics before and after.

- [ ] **Step 3: Commit**
  ```bash
  git add tests/load/soak-test.mjs
  git commit -m "test: add soak test script for connection leak validation"
  ```

### Task 3: Execute the Soak Test

**Files:**
- Modify: `tests/load/README.md` to document how to run it.

- [ ] **Step 1: Start the test environment**
  Run `docker-compose -f docker-compose.test.yml up -d --build` to ensure the router and Vault are running.

- [ ] **Step 2: Run the test**
  Run `node tests/load/soak-test.mjs`.

- [ ] **Step 3: Verify the output**
  Ensure the script reports success and the assertions pass (no handle leaks, no pending approval leaks).

- [ ] **Step 4: Commit Documentation**
  Create documentation `tests/load/README.md` on how to run the soak test.
  ```bash
  git add tests/load/README.md
  git commit -m "docs: document soak testing procedure"
  ```
