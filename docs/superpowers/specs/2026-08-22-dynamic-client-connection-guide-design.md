# Design Spec: Dynamic Multi-Target Client Connection Guide

**Date**: 2026-08-22  
**Status**: Approved  

---

## 1. Overview
The MCP Router gateway provides both unified gateway capabilities (`/sse?meta=true` for lightweight tool-search, `/sse?meta=false` for eager tool aggregation) and individual direct server routes (`/{serverId}`). Users need an interactive, dynamic client setup guide in the web dashboard that allows customizing the host domain, target server scope, meta-mode toggle, and user App Key selection, producing standard JSON configurations for AI clients (Claude Desktop, AGY, Cursor, VS Code, and Generic SSE).

---

## 2. UI Components & Interaction Flow

### 2.1 Configuration Controls (`ClientSetupGuide.tsx`)
The `ClientSetupGuide` component exposes:
1. **Client Format Tabs**:
   - `standard`: Standard `mcpServers` JSON (Claude Desktop, Antigravity / AGY, Cursor, Cline, Roo Code).
   - `vscode`: VS Code Extension `mcp.json` format.
   - `generic`: Generic SSE and HTTP session endpoint breakdown.
2. **Domain / Host Selector**:
   - `Current Host`: Uses `window.location.origin` (e.g., `https://mcp.wileyriley.com`).
   - `Local LAN`: Uses `http://10.0.0.10:8026`.
   - `Custom`: Allows entering an arbitrary URL or IP.
3. **Server Target Scope Selector**:
   - `All Servers (Unified Gateway)`: Routes through `/sse`.
   - `Specific MCP Server`: Dropdown dynamically populated from the active MCP servers list (e.g., `ha`, `docker`, `actual`, `seerr`, `contextcortex`, `quickcreds`).
4. **Meta-Mode Toggle** (enabled when All Servers is selected):
   - `Meta-Mode (Recommended)`: Appends `?meta=true` for dynamic `search_tools` and `execute_tool`.
   - `Direct / All Tools`: Appends `?meta=false` to eagerly expose all registered backend tools.
5. **App Key Selector**:
   - Automatically queries user App Keys from `/api/appkeys` to populate a selection dropdown.
   - Defaults to `"mcp_live_YOUR_APP_KEY_HERE"` if no keys exist.

### 2.2 Live Code Preview & Actions
- Code snippet re-renders immediately on any state change with syntax formatting.
- **Copy Configuration** button copies the JSON directly to the clipboard and triggers a success toast notification.

---

## 3. Formats & Schemas

### Format 1: Standard `mcpServers` JSON
```json
{
  "mcpServers": {
    "mcp-router": {
      "url": "https://mcp.wileyriley.com/sse?meta=true",
      "headers": {
        "X-App-Key": "mcp_live_..."
      }
    }
  }
}
```

### Format 2: VS Code Settings (`mcp.json`)
```json
{
  "mcp": {
    "servers": {
      "mcp-router": {
        "type": "sse",
        "url": "https://mcp.wileyriley.com/sse?meta=true",
        "headers": {
          "X-App-Key": "mcp_live_..."
        }
      }
    }
  }
}
```

### Format 3: Generic SSE / Raw Endpoints
```json
{
  "sseEndpoint": "https://mcp.wileyriley.com/sse?meta=true",
  "messageEndpoint": "https://mcp.wileyriley.com/message?sessionId={sessionId}",
  "authHeader": "X-App-Key: mcp_live_..."
}
```

---

## 4. Placement & Visibility

The `ClientSetupGuide` component is rendered in three key areas:
1. **Overview (`DashboardView.tsx`)**: Underneath the main server grid.
2. **App Keys (`SecurityView.tsx`)**: Underneath the App Keys management card.
3. **My MCP Servers (`MyMcpServers.tsx`)**: Underneath the user credentials table.

---

## 5. Verification & Testing Plan
1. **Unit Tests**:
   - `frontend/src/test/components/ClientSetupGuide.test.tsx` verifying format selection, URL construction, custom domain input, and clipboard copying.
2. **Frontend Build**:
   - Run `npm test` and `npm run build` in `/containers/dev/csharp-mcp-router/frontend`.
3. **Container Build & Deployment**:
   - Rebuild `ghcr.io/spelech/csharp-mcp-router:latest` with the updated frontend assets.
   - Restart `mcp-router` container and verify live in the web dashboard.
