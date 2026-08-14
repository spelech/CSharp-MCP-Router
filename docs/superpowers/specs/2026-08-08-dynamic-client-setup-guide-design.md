# Design Spec: Dynamic Multi-Target MCP Client Setup Guide

**Date**: 2026-08-08  
**Status**: Approved  

---

## 1. Overview
The current `ClientSetupGuide` component in the router dashboard offers static configuration snippets only for the root `/sse?meta=true` endpoint. Users need the ability to dynamically generate custom client setup configurations targeting specific backend servers (e.g. `docker`, `ha`, `actual`, `plex`), server categories (e.g. `media`, `smarthome`), or custom domains, along with optional App Key header authorization snippets.

---

## 2. Technical Architecture & UI Components

### 2.1 Controls & Layout (`ClientSetupGuide.tsx`)
The redesigned `ClientSetupGuide` component will features dual dropdown control bars and interactive toggles above the code preview box:

1. **Target Endpoint Selector (`targetEndpoint`)**:
   - `meta` (Default): Unified Meta-Mode Gateway (`{origin}/sse?meta=true`).
   - `server:{id}`: Direct server target (e.g. `docker` -> `{origin}/docker`).
   - `category:{cat}`: Category grouped target (e.g. `media` -> `{origin}/media`).

2. **Client App Selector (`clientApp`)**:
   - `claude`: Claude Desktop (`claude_desktop_config.json` via `@modelcontextprotocol/client-sse` or SSE transport).
   - `cursor`: Cursor IDE (`.cursor/mcp.json`).
   - `vscode`: VS Code / Cline / Roo Code (`mcpSettings.json`).
   - `sdk`: TypeScript / Node.js MCP SDK Code Snippet.

3. **Domain / Host Input (`customOrigin`)**:
   - Auto-detected default (`window.location.origin`).
   - Editable text input for custom domain override (e.g. `https://mcp.wileyriley.com`).

4. **App Key Authorization Toggle (`includeAppKey`)**:
   - Checkbox toggle to inject `"headers": { "X-App-Key": "YOUR_APP_KEY_HERE" }` or `env: { "X_APP_KEY": "YOUR_APP_KEY_HERE" }`.

### 2.2 Live Code Snippet Generator
- Recomputed on any input state change.
- One-click **Copy Configuration** button with feedback toast.

---

## 3. Verification Plan
- Unit tests for snippet generation logic.
- E2E Playwright verification of dropdown interactions and copy state.
- Solution build (`dotnet test McpRouter.slnx`, `npm test` in frontend).
