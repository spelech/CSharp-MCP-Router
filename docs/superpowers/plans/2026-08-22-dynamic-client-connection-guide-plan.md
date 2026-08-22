# Dynamic Client Connection Guide Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the Client Connection Guide in the MCP Router web dashboard to dynamically generate standard JSON configurations based on domain, server scope, meta-mode toggle, and user App Keys, and embed it across Overview, App Keys, and My MCP Servers pages.

**Architecture:** A unified React component `ClientSetupGuide.tsx` that queries active servers and user App Keys on mount, computes the exact client configuration JSON in real time for Standard (`mcpServers`), VS Code (`mcp.json`), and Generic SSE endpoints, provides a 1-click clipboard copy action, and is mounted cleanly in `DashboardView.tsx`, `SecurityView.tsx`, and `MyMcpServers.tsx`.

**Tech Stack:** React 18, TypeScript, Vitest, Testing Library, Vite, .NET 8, Docker.

## Global Constraints

- **Clients Covered:** Standard `mcpServers` JSON (Claude Desktop, Antigravity / AGY, Cursor, Cline, Roo), VS Code `mcp.json`, and Generic SSE.
- **Dynamic Selectors:** Host Domain (`window.location.origin` / `http://10.0.0.10:8026` / Custom), Server Scope (`all` vs specific server ID), Meta Mode (`?meta=true` vs `?meta=false`), App Key Selector.
- **Output:** Clean, formatted JSON code block with one-click "Copy JSON" button.
- **Placement:** Overview (`DashboardView`), App Keys (`SecurityView`), and My MCP Servers (`MyMcpServers`).

---

### Task 1: Redesign `ClientSetupGuide.tsx` Component

**Files:**
- Modify: `frontend/src/components/clients/ClientSetupGuide.tsx`
- Test: `frontend/src/test/components/ClientSetupGuide.test.tsx`

**Interfaces:**
- Consumes: `fetchServersApi()` from `api/serverApi.ts`, `fetchAppKeysApi()` from `api/appKeyApi.ts`, `showToast()` from `stores/useToastStore.ts`.
- Produces: `<ClientSetupGuide />` React functional component.

- [ ] **Step 1: Write the updated failing unit test suite in `frontend/src/test/components/ClientSetupGuide.test.tsx`**

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ClientSetupGuide } from '../../components/clients/ClientSetupGuide';
import * as serverApi from '../../api/serverApi';
import * as appKeyApi from '../../api/appKeyApi';

vi.mock('../../api/serverApi');
vi.mock('../../api/appKeyApi');

describe('ClientSetupGuide Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serverApi.fetchServersApi).mockResolvedValue([
      { id: 'ha', displayName: 'Home Assistant', url: 'http://ha:8086/mcp', enabled: true, hidden: false, type: 'http', categories: [] },
      { id: 'docker', displayName: 'Docker Containers', url: 'http://docker:8000/sse', enabled: true, hidden: false, type: 'sse', categories: [] }
    ]);
    vi.mocked(appKeyApi.fetchAppKeysApi).mockResolvedValue([
      { id: 'key1', name: 'Work Laptop', username: 'spelech', keyPrefix: 'mcp_live_abc123', keyType: 'personal', createdAt: '2026-08-01' }
    ]);
  });

  it('renders default standard mcpServers configuration with meta mode', async () => {
    render(<ClientSetupGuide />);

    expect(screen.getByText('Client Connection Guide')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText(/mcpServers/i)).toBeInTheDocument();
    });

    // Check default URL has meta=true
    expect(screen.getByText(/\/sse\?meta=true/i)).toBeInTheDocument();
  });

  it('switches between format tabs (Standard, VS Code, Generic SSE)', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByText(/mcpServers/i)).toBeInTheDocument();
    });

    // Switch to VS Code
    const vscodeBtn = screen.getByRole('button', { name: /VS Code/i });
    fireEvent.click(vscodeBtn);
    expect(screen.getByText(/"type":\s*"sse"/i)).toBeInTheDocument();

    // Switch to Generic SSE
    const genericBtn = screen.getByRole('button', { name: /Generic SSE/i });
    fireEvent.click(genericBtn);
    expect(screen.getByText(/sseEndpoint/i)).toBeInTheDocument();
  });

  it('switches server scope from all servers to individual server', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('all')).toBeInTheDocument();
    });

    const serverSelect = screen.getByTestId('server-scope-select');
    fireEvent.change(serverSelect, { target: { value: 'docker' } });

    expect(screen.getByText(/\/docker/i)).toBeInTheDocument();
  });

  it('updates domain when LAN or custom is chosen', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByText(/mcpServers/i)).toBeInTheDocument();
    });

    const lanBtn = screen.getByRole('button', { name: /Local LAN/i });
    fireEvent.click(lanBtn);
    expect(screen.getByText(/10\.0\.0\.10:8026/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run tests to verify failure**

Run: `npm --prefix /containers/dev/csharp-mcp-router/frontend test src/test/components/ClientSetupGuide.test.tsx`
Expected: FAIL with missing elements/data-testids.

- [ ] **Step 3: Implement `ClientSetupGuide.tsx`**

Implement `frontend/src/components/clients/ClientSetupGuide.tsx` with:
- Format tab buttons: Standard JSON, VS Code `mcp.json`, Generic SSE.
- Domain selector toolbar: Current Origin, Local LAN (`http://10.0.0.10:8026`), Custom URL input.
- Server scope dropdown: All Servers (`all`) + list of active MCP servers.
- Meta-mode toggle (when `all` is selected).
- App key dropdown listing fetched keys from `fetchAppKeysApi()` with fallback to `"mcp_live_YOUR_APP_KEY_HERE"`.
- One-click copy JSON button copying text to clipboard and calling `showToast('Configuration copied to clipboard!', 'success')`.

- [ ] **Step 4: Run unit tests to verify they pass**

Run: `npm --prefix /containers/dev/csharp-mcp-router/frontend test src/test/components/ClientSetupGuide.test.tsx`
Expected: PASS all tests.

- [ ] **Step 5: Commit task changes**

Run: `git -C /containers/dev/csharp-mcp-router add frontend/src/components/clients/ClientSetupGuide.tsx frontend/src/test/components/ClientSetupGuide.test.tsx && git -C /containers/dev/csharp-mcp-router commit -m "feat(frontend): implement dynamic multi-target client setup guide"`

---

### Task 2: Embed `ClientSetupGuide` Across Views

**Files:**
- Modify: `frontend/src/pages/MyMcpServers.tsx`
- Modify: `frontend/src/components/security/SecurityView.tsx`
- Modify: `frontend/src/components/servers/DashboardView.tsx`

- [ ] **Step 1: Embed `<ClientSetupGuide />` in `MyMcpServers.tsx`**

Import and render `<ClientSetupGuide />` inside `frontend/src/pages/MyMcpServers.tsx` below the credentials table in a container with margin-top: 25px.

- [ ] **Step 2: Verify `SecurityView.tsx` and `DashboardView.tsx` render `<ClientSetupGuide />` cleanly**

Ensure `<ClientSetupGuide />` is rendered consistently with responsive layout.

- [ ] **Step 3: Run full frontend test suite and production build**

Run: `npm --prefix /containers/dev/csharp-mcp-router/frontend test`
Run: `npm --prefix /containers/dev/csharp-mcp-router/frontend run build`
Expected: All tests pass, build succeeds cleanly into `wwwroot`.

- [ ] **Step 4: Commit task changes**

Run: `git -C /containers/dev/csharp-mcp-router add frontend/src/pages/MyMcpServers.tsx frontend/src/components/security/SecurityView.tsx frontend/src/components/servers/DashboardView.tsx && git -C /containers/dev/csharp-mcp-router commit -m "feat(frontend): embed dynamic client connection guide in My MCP Servers view"`

---

### Task 3: Build & Deploy Container Image to GHCR and Homelab Stack

**Files:**
- Modify: `/containers/mcp/docker-compose.yaml` (if needed)

- [ ] **Step 1: Build C# router solution and run test suite**

Run: `dotnet test /containers/dev/csharp-mcp-router/McpRouter.slnx`
Expected: All backend tests pass.

- [ ] **Step 2: Build new Docker container image**

Run: `docker build -t ghcr.io/spelech/csharp-mcp-router:latest /containers/dev/csharp-mcp-router`
Expected: Build succeeds and updates local image cache.

- [ ] **Step 3: Restart `mcp-router` container**

Run: `docker compose -f /containers/mcp/docker-compose.yaml up -d --force-recreate mcp-router`
Expected: Container starts cleanly and reports healthy status.

- [ ] **Step 4: Empirical verification of dashboard and endpoints**

Run: `curl -s http://10.0.0.10:8026/health`
Run: `curl -s -k --resolve mcp.wileyriley.com:443:10.0.0.10 https://mcp.wileyriley.com/health`
Verify HTTP 200 responses.

- [ ] **Step 5: Run homelab atomic commit workflow**

Run: `docker run --rm -v /containers/webservices/caddy/www:/www alpine chown -R 1000:1000 /www && ./commit.sh "feat(mcp-router): deploy updated dynamic client connection guide"`
