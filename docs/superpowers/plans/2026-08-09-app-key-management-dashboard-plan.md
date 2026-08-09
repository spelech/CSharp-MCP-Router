# App Key & API Security Management Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a dedicated "App Keys & Security" tab on the React frontend dashboard with LiteLLM-style API key management (table, limits bar, creation modal with one-time secret display), while preserving 100% of existing OAuth 2.0 Dynamic Client Registration.

**Architecture:** A new Zustand store (`useAppKeyStore.ts`) interacts with existing `/api/appkeys` endpoints. A dedicated view component (`SecurityView.tsx`) hosts the App Keys table (`AppKeysCard.tsx`), Dynamic Client Registrations (`RegisteredClientsCard.tsx`), and the interactive Client Setup Guide (`ClientSetupGuide.tsx`). `App.tsx` navigation is updated with a 4th top tab button (`App Keys & Security`).

**Tech Stack:** React 19, TypeScript, Zustand, Vanilla CSS / Glassmorphic UI design system, FontAwesome icons.

## Global Constraints

- Design must strictly adhere to the dark-mode glassmorphic design system using CSS variables in `frontend/src/index.css`.
- 100% backward compatibility of existing OpenIddict OAuth 2.0 Dynamic Client Registration (`/api/clients`).
- All C# unit tests must pass (`dotnet test McpRouter.slnx`).
- Frontend production bundle must build cleanly with 0 TypeScript/Vite errors (`cd frontend && npm run build`).

---

### Task 1: Create `useAppKeyStore.ts` Zustand Store

**Files:**
- Create: `frontend/src/stores/useAppKeyStore.ts`

**Interfaces:**
- Consumes: `/api/appkeys` and `/api/appkeys/limits` via `apiRequest` in `frontend/src/utils/api.ts`.
- Produces: `useAppKeyStore` Zustand hook for React components.

- [ ] **Step 1: Write `useAppKeyStore.ts` interface and store implementation**

```ts
import { create } from 'zustand';
import { apiRequest } from '../utils/api';
import { showToast } from './useToastStore';

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

export interface NewAppKeyResult {
  id: string;
  name: string;
  username: string;
  keyPrefix: string;
  plaintextKey: string;
  scopes: string[];
  expiresAt?: string;
  createdAt: string;
}

interface AppKeyStore {
  appKeys: AppKeyItem[];
  limits: AppKeyLimits | null;
  isLoading: boolean;
  isCreateModalOpen: boolean;
  createdResult: NewAppKeyResult | null;

  fetchAppKeys: () => Promise<void>;
  fetchLimits: () => Promise<void>;
  createAppKey: (payload: { name: string; username?: string; scopes: string[]; expiresInDays?: number }) => Promise<void>;
  revokeAppKey: (id: string, name: string) => Promise<void>;
  openModal: () => void;
  closeModal: () => void;
}

export const useAppKeyStore = create<AppKeyStore>((set, get) => ({
  appKeys: [],
  limits: null,
  isLoading: false,
  isCreateModalOpen: false,
  createdResult: null,

  fetchAppKeys: async () => {
    set({ isLoading: true });
    try {
      const data = await apiRequest<AppKeyItem[]>('/api/appkeys');
      set({ appKeys: data || [], isLoading: false });
    } catch (e: any) {
      console.error('Error fetching app keys:', e);
      set({ isLoading: false });
    }
  },

  fetchLimits: async () => {
    try {
      const data = await apiRequest<AppKeyLimits>('/api/appkeys/limits');
      set({ limits: data });
    } catch (e: any) {
      console.error('Error fetching app key limits:', e);
    }
  },

  createAppKey: async (payload) => {
    try {
      const result = await apiRequest<NewAppKeyResult>('/api/appkeys', {
        method: 'POST',
        body: payload
      });
      set({ createdResult: result });
      showToast('App Key created successfully', 'success');
      get().fetchAppKeys();
      get().fetchLimits();
    } catch (e: any) {
      showToast(`Error creating App Key: ${e.message}`, 'error');
      throw e;
    }
  },

  revokeAppKey: async (id, name) => {
    if (!window.confirm(`Are you sure you want to revoke the App Key '${name}'?`)) return;
    try {
      await apiRequest(`/api/appkeys/${id}`, { method: 'DELETE' });
      showToast('App Key revoked successfully', 'success');
      get().fetchAppKeys();
      get().fetchLimits();
    } catch (e: any) {
      showToast(`Error revoking App Key: ${e.message}`, 'error');
    }
  },

  openModal: () => set({ isModalOpen: true, createdResult: null }),
  closeModal: () => set({ isModalOpen: false, createdResult: null })
}));
```

- [ ] **Step 2: Verify frontend compilation**

Run: `cd frontend && npm run build`
Expected: PASS

---

### Task 2: Create `AppKeyModal.tsx` Key Generation Component

**Files:**
- Create: `frontend/src/components/AppKeyModal.tsx`

**Interfaces:**
- Consumes: `useAppKeyStore` store (`isCreateModalOpen`, `createdResult`, `createAppKey`, `closeModal`, `limits`).
- Produces: React Modal Component for creating AppKeys and displaying secret.

- [ ] **Step 1: Implement `AppKeyModal.tsx`**

```tsx
import React, { useState } from 'react';
import { useAppKeyStore } from '../stores/useAppKeyStore';

export const AppKeyModal: React.FC = () => {
  const { isCreateModalOpen, createdResult, createAppKey, closeModal, limits } = useAppKeyStore();

  const [name, setName] = useState('');
  const [scopeType, setScopeType] = useState<'all' | 'server' | 'category'>('all');
  const [customScope, setCustomScope] = useState('');
  const [expiresInDays, setExpiresInDays] = useState<number | undefined>(undefined);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [copiedKey, setCopiedKey] = useState(false);

  if (!isCreateModalOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    let scopes = ['all'];
    if (scopeType === 'server' && customScope.trim()) {
      scopes = [`server:${customScope.trim()}`];
    } else if (scopeType === 'category' && customScope.trim()) {
      scopes = [`category:${customScope.trim()}`];
    }

    try {
      await createAppKey({
        name,
        scopes,
        expiresInDays
      });
    } catch (err) {
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const copyPlaintextKey = () => {
    if (createdResult?.plaintextKey) {
      navigator.clipboard.writeText(createdResult.plaintextKey);
      setCopiedKey(true);
      setTimeout(() => setCopiedKey(false), 2000);
    }
  };

  const getMcpConfigSnippet = () => {
    if (!createdResult?.plaintextKey) return '';
    return JSON.stringify({
      mcpServers: {
        "mcp-router": {
          url: "http://10.0.0.10:8026/sse",
          type: "sse",
          trust: true,
          headers: {
            "X-App-Key": createdResult.plaintextKey
          }
        }
      }
    }, null, 2);
  };

  return (
    <div id="add-appkey-modal" className="modal-backdrop" style={{ display: 'flex' }}>
      <div className="glass-card modal-card" style={{ maxWidth: '540px' }}>
        <div className="modal-header">
          <h2><i className="fa-solid fa-key"></i> Create New App Key</h2>
          <button className="btn-close" onClick={closeModal}>&times;</button>
        </div>

        {!createdResult ? (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="key-name">Key Name (e.g. Cursor IDE, OpenClaw Agent)</label>
              <input
                type="text"
                id="key-name"
                placeholder="e.g. My Laptop CLI"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label>Scope / Access Level</label>
              <select
                value={scopeType}
                onChange={(e) => setScopeType(e.target.value as any)}
                style={{ width: '100%', padding: '8px 12px', borderRadius: '6px', background: 'rgba(0,0,0,0.3)', color: '#fff', border: '1px solid var(--glass-border)' }}
              >
                <option value="all">Full Gateway Access (all)</option>
                <option value="server">Server Scope (server:&lt;name&gt;)</option>
                <option value="category">Category Scope (category:&lt;name&gt;)</option>
              </select>
            </div>

            {scopeType !== 'all' && (
              <div className="form-group">
                <label>Target Server / Category Name</label>
                <input
                  type="text"
                  placeholder={scopeType === 'server' ? 'e.g. ha, docker' : 'e.g. smarthome, media'}
                  value={customScope}
                  onChange={(e) => setCustomScope(e.target.value)}
                  required
                />
              </div>
            )}

            <div className="form-group">
              <label>Expiration</label>
              <select
                value={expiresInDays === undefined ? 'never' : expiresInDays}
                onChange={(e) => setExpiresInDays(e.target.value === 'never' ? undefined : Number(e.target.value))}
                style={{ width: '100%', padding: '8px 12px', borderRadius: '6px', background: 'rgba(0,0,0,0.3)', color: '#fff', border: '1px solid var(--glass-border)' }}
              >
                <option value="never">Never (No expiration)</option>
                <option value="30">30 Days</option>
                <option value="90">90 Days</option>
                <option value="365">1 Year (365 Days)</option>
              </select>
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={closeModal}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={isSubmitting || !!limits?.isLimitReached}>
                {isSubmitting ? 'Generating...' : 'Generate App Key'}
              </button>
            </div>
          </form>
        ) : (
          <div style={{ padding: '10px', background: 'rgba(249, 115, 22, 0.08)', border: '1px solid var(--accent)', borderRadius: '8px' }}>
            <h4 style={{ color: 'var(--accent)', margin: '0 0 10px 0' }}><i className="fa-solid fa-check-circle"></i> App Key Created!</h4>
            <p style={{ fontSize: '13px', margin: '4px 0 12px 0', color: 'var(--secondary)' }}>
              Copy your App Key now. It will <strong>never be shown again</strong>.
            </p>

            <div style={{ background: '#090d16', padding: '10px 14px', borderRadius: '6px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '10px' }}>
              <code style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: '12px', color: '#38bdf8', wordBreak: 'break-all' }}>
                {createdResult.plaintextKey}
              </code>
              <button type="button" className="btn btn-secondary btn-sm" onClick={copyPlaintextKey}>
                {copiedKey ? <i className="fa-solid fa-check"></i> : <i className="fa-solid fa-copy"></i>}
              </button>
            </div>

            <h5 style={{ margin: '14px 0 6px 0', fontSize: '12px', color: 'var(--secondary)' }}>Ready-to-Use mcp_config.json Snippet:</h5>
            <pre style={{ background: '#090d16', padding: '10px', borderRadius: '6px', fontSize: '11px', maxHeight: '140px', overflowY: 'auto', color: '#cbd5e1' }}>
              {getMcpConfigSnippet()}
            </pre>

            <button type="button" className="btn btn-primary" onClick={closeModal} style={{ marginTop: '14px', width: '100%' }}>
              Done
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
```

- [ ] **Step 2: Verify frontend compilation**

Run: `cd frontend && npm run build`
Expected: PASS

---

### Task 3: Create `AppKeysCard.tsx` Table & Limits Component

**Files:**
- Create: `frontend/src/components/dashboard/AppKeysCard.tsx`

**Interfaces:**
- Consumes: `useAppKeyStore` store (`appKeys`, `limits`, `fetchAppKeys`, `fetchLimits`, `revokeAppKey`, `openModal`).
- Produces: React Card Component displaying LiteLLM-style App Keys list and key limit meter.

- [ ] **Step 1: Implement `AppKeysCard.tsx`**

```tsx
import React, { useEffect } from 'react';
import { useAppKeyStore } from '../../stores/useAppKeyStore';

export const AppKeysCard: React.FC = () => {
  const { appKeys, limits, fetchAppKeys, fetchLimits, revokeAppKey, openModal } = useAppKeyStore();

  useEffect(() => {
    fetchAppKeys();
    fetchLimits();
  }, [fetchAppKeys, fetchLimits]);

  const copyConfigSnippet = (keyPrefix: string) => {
    const sampleKey = `${keyPrefix}...[YOUR_FULL_KEY]`;
    const snippet = JSON.stringify({
      mcpServers: {
        "mcp-router": {
          url: "http://10.0.0.10:8026/sse",
          type: "sse",
          trust: true,
          headers: {
            "X-App-Key": sampleKey
          }
        }
      }
    }, null, 2);
    navigator.clipboard.writeText(snippet);
    alert('Copied sample mcp_config.json snippet to clipboard!');
  };

  return (
    <div className="glass-card dcr-card">
      <div className="card-header-btn">
        <div>
          <h2>
            <i className="fa-solid fa-key"></i> LiteLLM-Style App Keys
          </h2>
          {limits && (
            <small style={{ color: 'var(--secondary)', display: 'block', marginTop: '2px' }}>
              User Quota: <strong>{limits.userActiveKeys} / {limits.userMax}</strong> Keys Used &bull; Global: {limits.totalActiveKeys} / {limits.globalMax}
            </small>
          )}
        </div>
        <button className="btn btn-primary btn-sm" onClick={openModal} disabled={!!limits?.isLimitReached}>
          <i className="fa-solid fa-plus"></i> Create App Key
        </button>
      </div>

      <div className="table-container">
        <table id="appkeys-table">
          <thead>
            <tr>
              <th>Key Name</th>
              <th>Prefix</th>
              <th>Owner</th>
              <th>Scopes</th>
              <th>Expires</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {appKeys.length === 0 ? (
              <tr>
                <td colSpan={7} className="empty-state">
                  No App Keys active. Click "+ Create App Key" to generate a credential for CLI or IDE tools.
                </td>
              </tr>
            ) : (
              appKeys.map((key) => (
                <tr key={key.id}>
                  <td><strong>{key.name}</strong></td>
                  <td>
                    <code style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: '11px', background: 'rgba(255,255,255,0.05)', padding: '2px 6px', borderRadius: '4px', color: 'var(--accent)' }}>
                      {key.keyPrefix}...
                    </code>
                  </td>
                  <td>{key.username}</td>
                  <td>
                    {key.scopes && key.scopes.length > 0 ? (
                      key.scopes.map((s, idx) => (
                        <span key={idx} className="server-badge" style={{ background: 'rgba(249, 115, 22, 0.1)', color: 'var(--accent)', marginRight: '4px' }}>
                          {s}
                        </span>
                      ))
                    ) : (
                      <span className="server-badge">all</span>
                    )}
                  </td>
                  <td>
                    {key.expiresAt ? (
                      new Date(key.expiresAt) < new Date() ? (
                        <span style={{ color: '#ef4444', fontWeight: 600 }}>Expired</span>
                      ) : (
                        <span>{new Date(key.expiresAt).toLocaleDateString()}</span>
                      )
                    ) : (
                      <span style={{ color: 'var(--secondary)' }}>Never</span>
                    )}
                  </td>
                  <td>{new Date(key.createdAt).toLocaleDateString()}</td>
                  <td>
                    <button
                      className="btn-icon"
                      title="Copy Config Snippet"
                      onClick={() => copyConfigSnippet(key.keyPrefix)}
                      style={{ marginRight: '6px' }}
                    >
                      <i className="fa-solid fa-copy"></i>
                    </button>
                    <button
                      className="btn-icon btn-delete"
                      title="Revoke App Key"
                      onClick={() => revokeAppKey(key.id, key.name)}
                    >
                      <i className="fa-solid fa-trash-can"></i>
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
```

- [ ] **Step 2: Verify frontend compilation**

Run: `cd frontend && npm run build`
Expected: PASS

---

### Task 4: Create `SecurityView.tsx` & Update Header Tab Routing in `App.tsx`

**Files:**
- Create: `frontend/src/components/SecurityView.tsx`
- Modify: `frontend/src/App.tsx`

**Interfaces:**
- Consumes: `AppKeysCard`, `RegisteredClientsCard`, `ClientSetupGuide`, `AppKeyModal`.
- Produces: Dedicated Security View component rendered when `currentView === 'security'`.

- [ ] **Step 1: Create `frontend/src/components/SecurityView.tsx`**

```tsx
import React from 'react';
import { AppKeysCard } from './dashboard/AppKeysCard';
import { RegisteredClientsCard } from './dashboard/RegisteredClientsCard';
import { ClientSetupGuide } from './dashboard/ClientSetupGuide';

export const SecurityView: React.FC = () => {
  return (
    <div className="dashboard-grid" style={{ gridTemplateColumns: '1fr', gap: '24px' }}>
      <AppKeysCard />
      <RegisteredClientsCard />
      <ClientSetupGuide />
    </div>
  );
};
```

- [ ] **Step 2: Update `frontend/src/App.tsx` with 4th Tab & Modal Rendering**

```tsx
import React, { useState } from 'react';
import { Header } from './components/Header';
import { DashboardView } from './components/DashboardView';
import { SecurityView } from './components/SecurityView';
import { TestBenchView } from './components/TestBenchView';
import { SettingsView } from './components/SettingsView';

import { ServerModal } from './components/ServerModal';
import { ServerInspectModal } from './components/ServerInspectModal';
import { ClientModal } from './components/ClientModal';
import { AppKeyModal } from './components/AppKeyModal';
import { CustomFileModal } from './components/CustomFileModal';
import { PolicyModal } from './components/PolicyModal';
import { MappingModal } from './components/MappingModal';
import { Toasts } from './components/Toasts';

const App: React.FC = () => {
  const [currentView, setCurrentView] = useState<'dashboard' | 'security' | 'testbench' | 'settings'>('dashboard');

  return (
    <>
      <div className="background-decor">
        <div className="circle circle-1"></div>
        <div className="circle circle-2"></div>
      </div>

      <div className="dashboard-container">
        <Header />

        <nav className="tabs-nav">
          <button
            className={`tab-btn ${currentView === 'dashboard' ? 'active' : ''}`}
            onClick={() => setCurrentView('dashboard')}
          >
            <i className="fa-solid fa-gauge"></i> Overview
          </button>
          <button
            className={`tab-btn ${currentView === 'security' ? 'active' : ''}`}
            onClick={() => setCurrentView('security')}
          >
            <i className="fa-solid fa-key"></i> App Keys & Security
          </button>
          <button
            className={`tab-btn ${currentView === 'testbench' ? 'active' : ''}`}
            onClick={() => setCurrentView('testbench')}
          >
            <i className="fa-solid fa-vial"></i> Test Bench
          </button>
          <button
            className={`tab-btn ${currentView === 'settings' ? 'active' : ''}`}
            onClick={() => setCurrentView('settings')}
          >
            <i className="fa-solid fa-gear"></i> Settings
          </button>
        </nav>

        {currentView === 'dashboard' && <DashboardView />}
        {currentView === 'security' && <SecurityView />}
        {currentView === 'testbench' && <TestBenchView />}
        {currentView === 'settings' && <SettingsView />}

        <footer className="dashboard-footer">
          <p>WileyRiley Infrastructure &bull; Protected by TinyAuth Forward Auth &bull; 2026</p>
        </footer>
      </div>

      {/* Modals */}
      <ServerModal />
      <ServerInspectModal />
      <ClientModal />
      <AppKeyModal />
      <CustomFileModal />
      <PolicyModal />
      <MappingModal />

      {/* Toast Manager */}
      <Toasts />
    </>
  );
};

export default App;
```

- [ ] **Step 3: Build frontend and run unit tests**

Run: `cd frontend && npm run build`
Run: `dotnet test McpRouter.slnx`
Expected: 0 errors, all tests pass.

- [ ] **Step 4: Commit and push via atomic script**

Run: `./commit.sh "feat(ui): add dedicated App Keys & Security tab with LiteLLM-style key management"`
Run: `git push origin main`
Expected: PASS and pushed cleanly.

---

## Verification Plan

### Automated Tests
1. Frontend build verification: `cd frontend && npm run build`
2. Backend unit test verification: `dotnet test McpRouter.slnx`

### Manual Verification
1. Access `https://mcp.wileyriley.com`.
2. Click **App Keys & Security** in top tab navigation bar.
3. Verify **AppKeysCard** shows active keys and user quota limit bar.
4. Click **"+ Create App Key"**, generate a key, verify plaintext display and `mcp_config.json` snippet.
5. Verify **Registered Clients** (Dynamic Client Registration) card renders below App Keys.
6. Verify **Client Setup Guide** renders cleanly at the bottom.
