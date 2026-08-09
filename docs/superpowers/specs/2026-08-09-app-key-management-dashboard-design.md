# App Key & API Security Management Dashboard Design Spec

## Overview
This specification details the architecture, UI design, and frontend state management for the dedicated **App Keys & Security** tab on the C# MCP Router Dashboard (`https://mcp.wileyriley.com`).

The tab introduces LiteLLM-style API key management for CLI tools, IDEs, and autonomous agents, while preserving 100% of existing OpenIddict OAuth 2.0 Dynamic Client Registrations and integration generators.

---

## 1. Top Navigation & Routing (`App.tsx`)

A 4th navigation button is added to the primary header tab bar:

```tsx
<nav className="tabs-nav">
  <button className={`tab-btn ${currentView === 'dashboard' ? 'active' : ''}`} onClick={() => setCurrentView('dashboard')}>
    <i className="fa-solid fa-gauge"></i> Overview
  </button>
  <button className={`tab-btn ${currentView === 'security' ? 'active' : ''}`} onClick={() => setCurrentView('security')}>
    <i className="fa-solid fa-key"></i> App Keys & Security
  </button>
  <button className={`tab-btn ${currentView === 'testbench' ? 'active' : ''}`} onClick={() => setCurrentView('testbench')}>
    <i className="fa-solid fa-vial"></i> Test Bench
  </button>
  <button className={`tab-btn ${currentView === 'settings' ? 'active' : ''}`} onClick={() => setCurrentView('settings')}>
    <i className="fa-solid fa-gear"></i> Settings
  </button>
</nav>
```

---

## 2. Security Tab View (`SecurityView.tsx`)

The main view renders three primary functional sections:

### Section A: App Keys Management Table (`AppKeysCard.tsx`)
- Displays an interactive table of active LiteLLM-style App Keys queried from `GET /api/appkeys`.
- **Columns**:
  - **Key Name & Prefix**: Display Name and prefix badge (e.g. `mcp-global-steve...`).
  - **Assigned User**: Owner username (`steve`).
  - **Scopes / Permissions**: Pills for `All Access` (`all`), `server:<name>`, or `category:<name>`.
  - **Expiration**: Status badge (`Never`, `Expires in 30d`, `Expired`).
  - **Created Date**: Relative or ISO formatted date.
  - **Actions**:
    - **Copy Snippet (`📋`)**: One-click copies a ready-to-use `mcp_config.json` payload with pre-filled `X-App-Key` header.
    - **Revoke Key (`🗑️`)**: Calls `DELETE /api/appkeys/{id}` with confirmation toast.
- **Usage Limits Bar**: Renders active key stats from `GET /api/appkeys/limits` (e.g., `1 / 5 User Keys Used`).

### Section B: Dynamic Client Registrations (`RegisteredClientsCard.tsx`)
- Relocates the existing OpenIddict OAuth 2.0 Client Registration card from the overview dashboard into the Security tab.
- Preserves full backward compatibility for dynamic client registration, client deletion, and client secret display.

### Section C: Client Setup Guide (`ClientSetupGuide.tsx`)
- Embeds the interactive multi-target setup guide at the bottom of the tab so users can immediately generate configuration files for Claude Desktop, Cursor, VS Code, or TypeScript SDK using their App Keys.

---

## 3. App Key Creation Modal (`AppKeyModal.tsx`) & Store (`useAppKeyStore.ts`)

### Modal Controls (`AppKeyModal.tsx`)
- **Key Name**: Text input (Required).
- **Target User**: Text input / Dropdown (Admins can specify another user; defaults to active SSO user).
- **Scope Type Selection**:
  - `Full Gateway Access (all)`
  - `Server Scope` (select server ID e.g., `ha`, `docker`, `actual`)
  - `Category Scope` (select category e.g., `Smarthome`, `Media`)
- **Expiration Period**: Dropdown options: `Never`, `30 Days`, `90 Days`, `365 Days`.
- **Secret Display**: Upon successful creation (`POST /api/appkeys`), renders the plaintext key (`mcp-global-...`) **once** with a copy button and a formatted `mcp_config.json` snippet.

### State Management (`useAppKeyStore.ts`)
```ts
export interface AppKeyItem {
  id: string;
  name: string;
  username: string;
  keyPrefix: string;
  scopes: string[];
  expiresAt?: string;
  createdAt: string;
}

export interface AppKeyLimits {
  globalMax: number;
  userMax: number;
  totalActiveKeys: number;
  userActiveKeys: number;
  isLimitReached: boolean;
}

interface AppKeyStore {
  appKeys: AppKeyItem[];
  limits: AppKeyLimits | null;
  isLoading: boolean;
  isModalOpen: boolean;
  createdResult: { name: string; keyPrefix: string; plaintextKey: string; scopes: string[]; expiresAt?: string } | null;

  fetchAppKeys: () => Promise<void>;
  fetchLimits: () => Promise<void>;
  createAppKey: (payload: { name: string; username?: string; scopes: string[]; expiresInDays?: number }) => Promise<void>;
  revokeAppKey: (id: string, name: string) => Promise<void>;
  openModal: () => void;
  closeModal: () => void;
}
```

---

## 4. Verification Plan

### Automated Tests
- Run full frontend build (`npm run build` inside `frontend/`).
- Run solution unit tests: `dotnet test McpRouter.slnx --filter "AppKeysControllerTests|AppKeyAuthenticationTests"`.

### Manual Verification
- Navigate to `https://mcp.wileyriley.com`, click **App Keys & Security** tab.
- Click **"+ Create New App Key"**, generate a 30-day key for `Cursor IDE`.
- Verify plaintext key and `mcp_config.json` snippet display.
- Copy key and test curling `/sse` endpoint:
  ```bash
  curl -s -N -H "X-App-Key: <generated_key>" http://10.0.0.10:8026/sse | head -n 3
  ```
