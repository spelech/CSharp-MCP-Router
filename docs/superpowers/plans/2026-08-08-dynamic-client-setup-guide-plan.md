# Dynamic Multi-Target Client Setup Guide Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the static `ClientSetupGuide` component into an interactive, multi-target configuration generator supporting server/category/meta routing, client app options (Claude, Cursor, VS Code, TS SDK), custom domain origins, and optional App Key header injection.

**Architecture:** Update `frontend/src/components/dashboard/ClientSetupGuide.tsx` to read active backend servers from `useServerStore`, render dual dropdown controls and an origin override input, dynamically generate formatted JSON/code snippets, and copy to clipboard.

**Tech Stack:** React 19, TypeScript, Vitest, Playwright, Tailwind/CSS variables.

## Global Constraints

- Backend server list must be populated from `useServerStore`.
- Generated configuration JSON must strictly adhere to MCP client specification formats.
- All solution unit tests (`dotnet test McpRouter.slnx`, `cd frontend && npm test`) must pass cleanly.

---

### Task 1: Refactor ClientSetupGuide.tsx Component & Snippet Generator

**Files:**
- Modify: `frontend/src/components/dashboard/ClientSetupGuide.tsx`
- Modify: `frontend/src/components/DashboardView.tsx` (if prop wiring needed)
- Test: `frontend/src/stores/useServerStore.ts`

**Interfaces:**
- Consumes: `useServerStore` servers array (`McpServer[]`).
- Produces: `ClientSetupGuide` component with dropdown selectors for target endpoint, client application, custom host origin, and App Key header injection.

- [ ] **Step 1: Implement snippet generation helper function**

Write pure helper function `generateClientConfig(app, targetEndpoint, origin, includeAppKey)` returning formatted JSON string or TypeScript snippet.

- [ ] **Step 2: Update ClientSetupGuide component UI with controls**

Add dropdown controls for target endpoint (Meta-mode vs specific server vs category), client app (Claude, Cursor, VS Code, TS SDK), custom origin input, and App Key toggle.

- [ ] **Step 3: Run Vitest frontend tests**

Run `cd frontend && npm test` to verify zero regressions.

- [ ] **Step 4: Commit component changes**

```bash
git add frontend/src/components/dashboard/ClientSetupGuide.tsx
git commit -m "feat(ui): convert ClientSetupGuide to interactive multi-target config generator"
```

---

### Task 2: Build Frontend Assets, Update User Guide Docs, & Rebuild Container

**Files:**
- Modify: `docs/user-guide/04-client-setup-and-app-keys.md`
- Build: `frontend/dist` -> `wwwroot`

**Interfaces:**
- Consumes: Dynamic setup guide component.
- Produces: Production frontend assets and updated user guide documentation.

- [ ] **Step 1: Update docs/user-guide/04-client-setup-and-app-keys.md**

Document the new dynamic setup guide features and options.

- [ ] **Step 2: Build frontend SPA**

Run `cd frontend && npm run build` to compile assets to `wwwroot/`.

- [ ] **Step 3: Sync and rebuild container**

Sync to `/containers/mcp/router/` via rsync and rebuild container `docker compose build --no-cache mcp-router && docker compose up -d --force-recreate mcp-router`.

- [ ] **Step 4: Run test suite & commit**

Run `dotnet test McpRouter.slnx` and `./commit.sh "feat(ui): complete dynamic client setup guide generator and docs update"`.

