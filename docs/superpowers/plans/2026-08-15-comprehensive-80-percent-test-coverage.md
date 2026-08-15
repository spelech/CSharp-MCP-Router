# Comprehensive 80%+ Test Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Elevate all frontend UI components, testbench tools, backend controllers, and resource routing managers currently below 80% to $\ge 85\%$ test coverage.

**Architecture:** Create targeted xUnit unit/integration tests for .NET backend controllers, connection testers, and resource routing managers, alongside Vitest/React Testing Library test suites for all remaining frontend components (`ToolTesterCard`, `ServerInspectModal`, `TestBenchView`, `LogsTerminalCard`, `ServerCard`, `DashboardView`, `ClientSetupGuide`, and shared wrappers).

**Tech Stack:** .NET 10, xUnit, Moq, ASP.NET Core MVC, React 19, TypeScript 5.7, Vitest 3.0, React Testing Library, Zustand 5.0.

## Global Constraints

- Backend tests must run in `McpRouter.Tests` without platform-specific dependencies.
- Frontend tests must execute cleanly in Vitest with happy-dom and mock APIs via `src/test/setup.ts`.
- All tests must pass cleanly in `dotnet test` and `npm run test:coverage`.
- Maintain 100% pass rate with zero flaky tests.

---

### Task 1: Frontend `ToolTesterCard.tsx` Test Suite

**Files:**
- Create: `frontend/src/test/components/ToolTesterCard.test.tsx`
- Component: `frontend/src/components/testbench/ToolTesterCard.tsx` (currently 47.0%)

**Interfaces:**
- Tests `useTestbenchStore` tool selection, parameter input schema rendering (string, number, boolean, array/object), argument value dispatch, tool execution (`executeTool`), execution success output formatting, and error state alerts.

- [ ] **Step 1: Write `ToolTesterCard.test.tsx`**
- [ ] **Step 2: Run `npx vitest run src/test/components/ToolTesterCard.test.tsx` and verify passing**
- [ ] **Step 3: Check line coverage for `ToolTesterCard.tsx` $\ge 85\%$**

---

### Task 2: Frontend `ServerInspectModal.tsx` Test Suite

**Files:**
- Create: `frontend/src/test/components/ServerInspectModal.test.tsx`
- Component: `frontend/src/components/servers/ServerInspectModal.tsx` (currently 49.3%)

**Interfaces:**
- Tests opening the modal with a server, switching between Tools, Prompts, and Resources inspector tabs, expanding JSON schema properties, copying tool definitions, and closing the modal.

- [ ] **Step 1: Write `ServerInspectModal.test.tsx`**
- [ ] **Step 2: Run `npx vitest run src/test/components/ServerInspectModal.test.tsx` and verify passing**
- [ ] **Step 3: Check line coverage for `ServerInspectModal.tsx` $\ge 85\%$**

---

### Task 3: Frontend `TestBenchView.tsx` & `LogsTerminalCard.tsx` Test Suites

**Files:**
- Create: `frontend/src/test/components/TestBenchView.test.tsx`
- Create: `frontend/src/test/components/LogsTerminalCard.test.tsx`
- Components: `TestBenchView.tsx` (currently 40.1%), `LogsTerminalCard.tsx` (currently 55.9%)

**Interfaces:**
- `TestBenchView.test.tsx`: Tests rendering the full workbench, tab switching, and card visibility.
- `LogsTerminalCard.test.tsx`: Tests log search filter, level filter (All/Info/Warn/Error), clearing logs, auto-scroll toggle, and copy log buffer.

- [ ] **Step 1: Write `LogsTerminalCard.test.tsx` and `TestBenchView.test.tsx`**
- [ ] **Step 2: Run `npx vitest run src/test/components/LogsTerminalCard.test.tsx src/test/components/TestBenchView.test.tsx`**
- [ ] **Step 3: Check line coverage for both components $\ge 85\%$**

---

### Task 4: Frontend `ServerCard.tsx` & `DashboardView.tsx` Test Suites

**Files:**
- Create: `frontend/src/test/components/ServerCard.test.tsx`
- Create: `frontend/src/test/components/DashboardView.test.tsx`
- Components: `ServerCard.tsx` (currently 63.9%), `DashboardView.tsx` (currently 56.1%)

**Interfaces:**
- `ServerCard.test.tsx`: Tests toggle enable/disable switch, delete confirmation modal, direct URL clipboard copy, test connection button, inspect modal opening, and transport badge styling.
- `DashboardView.test.tsx`: Tests grouping by category, type, and status, collapsing and expanding group sections, search filter matching, and pagination controls.

- [ ] **Step 1: Write `ServerCard.test.tsx` and `DashboardView.test.tsx`**
- [ ] **Step 2: Run `npx vitest run src/test/components/ServerCard.test.tsx src/test/components/DashboardView.test.tsx`**
- [ ] **Step 3: Check line coverage for both components $\ge 85\%$**

---

### Task 5: Frontend `ClientSetupGuide.tsx` & Shared Component Wrappers

**Files:**
- Create: `frontend/src/test/components/ClientSetupGuide.test.tsx`
- Create: `frontend/src/test/components/SharedComponents.test.tsx`
- Components: `ClientSetupGuide.tsx` (52.6%), `Modal.tsx` (4.5%), `StatusBadge.tsx` (3.3%), `PaginationToolbar.tsx` (62.1%)

**Interfaces:**
- `ClientSetupGuide.test.tsx`: Tests target client switching (Cursor, Claude Desktop, Windsurf, LibreChat, VS Code), transport type selector (SSE vs STDIO), and JSON config snippet generator.
- `SharedComponents.test.tsx`: Tests `Modal` open/close/backdrop/ESC triggers, `StatusBadge` connection states (`Connected`, `Disconnected`, `Error`, `Connecting`), and `PaginationToolbar` page navigation and size changes.

- [ ] **Step 1: Write `ClientSetupGuide.test.tsx` and `SharedComponents.test.tsx`**
- [ ] **Step 2: Run `npx vitest run src/test/components/ClientSetupGuide.test.tsx src/test/components/SharedComponents.test.tsx`**
- [ ] **Step 3: Verify all frontend component line coverages exceed 85%**

---

### Task 6: Backend `ProvidersController.cs` Connection Testing & Error Paths

**Files:**
- Modify/Create: `McpRouter.Tests/ProvidersControllerTests.cs`
- Class: `McpRouter.Components.Providers.ProvidersController` (`TestVaultConnection`, `TestLdapConnection`, `SaveAuthProvider`, `SaveSecretProvider`)

**Interfaces:**
- Unit tests for:
  - `TestVaultConnection`: Token auth success, AppRole auth success, invalid URL error, exception handling.
  - `TestLdapConnection`: Success bind, authentication failure, invalid host/network exception.
  - `SaveAuthProvider` and `SaveSecretProvider` validation errors and DB persistence.

- [ ] **Step 1: Write unit tests in `ProvidersControllerTests.cs`**
- [ ] **Step 2: Run `dotnet test --filter ProvidersControllerTests` and verify all tests pass**
- [ ] **Step 3: Verify `ProvidersController` line coverage $\ge 85\%$**

---

### Task 7: Backend `ClientsController.cs` & `PermissionsController.cs` Tests

**Files:**
- Modify/Create: `McpRouter.Tests/ClientsControllerTests.cs`
- Modify/Create: `McpRouter.Tests/PermissionsControllerTests.cs`
- Classes: `ClientsController`, `PermissionsController`

**Interfaces:**
- Unit tests for:
  - `ClientsController.GetRegisteredCategoriesAsync`
  - `ClientsController.DeleteClient` (existing vs non-existing client IDs)
  - `PermissionsController`: `SavePolicy`, `DeletePolicy`, `SaveMapping`, `DeleteMapping`, `GetMappings` error and validation paths.

- [ ] **Step 1: Write unit tests in `ClientsControllerTests.cs` and `PermissionsControllerTests.cs`**
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~ClientsController|FullyQualifiedName~PermissionsController"`**
- [ ] **Step 3: Verify line coverage $\ge 85\%$**

---

### Task 8: Backend `ResourceRoutingManager.cs` & `DynamicEmbeddingService.cs` Tests

**Files:**
- Modify/Create: `McpRouter.Tests/ResourceRoutingManagerTests.cs`
- Modify/Create: `McpRouter.Tests/DynamicEmbeddingServiceTests.cs`
- Classes: `ResourceRoutingManager` (37.9%), `DynamicEmbeddingService` (67.8%)

**Interfaces:**
- Unit tests for:
  - `ResourceRoutingManager`: `ListResourceTemplatesAsync`, URI template expansion matching, invalid resource URI errors.
  - `DynamicEmbeddingService`: `PreWarmAsync`, API provider fallback, ONNX tokenizer edge cases, database saving.

- [ ] **Step 1: Write unit tests in `ResourceRoutingManagerTests.cs` and `DynamicEmbeddingServiceTests.cs`**
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~ResourceRoutingManager|FullyQualifiedName~DynamicEmbeddingService"`**
- [ ] **Step 3: Verify line coverage $\ge 85\%$**

---

### Task 9: Full Suite Verification & Coverage Report Update

**Files:**
- Modify: `docs/test-coverage-evaluation.md`
- Run: `npm run test:coverage`, `dotnet test`, `python3 scripts/verify_release.py`

- [ ] **Step 1: Execute full frontend Vitest coverage and ensure 100% test file pass rate with 0 components < 80%**
- [ ] **Step 2: Execute full backend dotnet test coverage and verify all controllers/services $\ge 80\%$**
- [ ] **Step 3: Run release verification script `python3 scripts/verify_release.py --skip-tests`**
- [ ] **Step 4: Update `docs/test-coverage-evaluation.md` with new metrics and commit**
