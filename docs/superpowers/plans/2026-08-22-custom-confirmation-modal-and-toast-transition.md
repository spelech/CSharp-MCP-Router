# Custom Confirmation Modal & Toast Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate all browser-native `alert()` and `confirm()` popup windows from the MCP Router dashboard by introducing a centralized Promise-based `useConfirmStore`, a glassmorphic `<ConfirmModal />`, and transitioning remaining alerts to `showToast()`.

**Architecture:** A Zustand-backed store (`useConfirmStore`) exposes `confirmAction(options): Promise<boolean>` which renders a glassmorphic modal (`<ConfirmModal />`) in the root view. Stores await this promise for destructive user actions, while inform/error dialogs use `showToast()`.

**Tech Stack:** TypeScript, React 19, Zustand, Vitest, Testing Library, CSS Variables / Glassmorphism design tokens.

## Global Constraints

- Must bump version from `4.26.0` to `4.26.1` across `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `frontend/src/shared/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`.
- All Vitest tests must include requirement metadata tags (`@requirement`, `@category UI`, `@type PositiveFeature`).
- Must run `dotnet run --project scripts/CatalogGenerator` and verify zero-drift with `--verify-only`.
- All unit tests must pass (`npm test -- --run`).

---

### Task 1: Create `useConfirmStore` and `confirmAction` Helper

**Files:**
- Create: `frontend/src/shared/stores/useConfirmStore.ts`
- Create: `frontend/src/stores/useConfirmStore.ts`
- Modify: `frontend/src/stores/index.ts`
- Modify: `frontend/src/shared/stores/index.ts` (if applicable)
- Test: `frontend/src/test/stores/useConfirmStore.test.ts`

**Interfaces:**
- Consumes: none
- Produces: `useConfirmStore`, `confirmAction`, `ConfirmOptions`, `ConfirmState`

- [ ] **Step 1: Write the failing test for `useConfirmStore`**

```typescript
import { describe, it, expect, beforeEach } from 'vitest';
import { useConfirmStore, confirmAction } from '../../stores/useConfirmStore';

/**
 * @requirement REQ-UI-CONFIRM-MODAL
 * @category UI
 * @type PositiveFeature
 * @description Centralized promise-based confirmation store resolves true on confirmation and false on cancellation.
 */
describe('useConfirmStore', () => {
  beforeEach(() => {
    useConfirmStore.setState({
      isOpen: false,
      options: { message: '' },
      resolve: null
    });
  });

  it('initializes in closed state', () => {
    const state = useConfirmStore.getState();
    expect(state.isOpen).toBe(false);
    expect(state.resolve).toBeNull();
  });

  it('opens confirmation modal and resolves true when confirmed', async () => {
    const confirmPromise = confirmAction({
      title: 'Delete Item',
      message: 'Are you sure?',
      confirmText: 'Delete',
      danger: true
    });

    const state = useConfirmStore.getState();
    expect(state.isOpen).toBe(true);
    expect(state.options.title).toBe('Delete Item');
    expect(state.options.message).toBe('Are you sure?');
    expect(state.options.confirmText).toBe('Delete');
    expect(state.options.danger).toBe(true);

    state.handleConfirm();
    const result = await confirmPromise;
    expect(result).toBe(true);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });

  it('resolves false when cancelled', async () => {
    const confirmPromise = confirmAction('Delete this file?');
    const state = useConfirmStore.getState();
    expect(state.isOpen).toBe(true);
    expect(state.options.message).toBe('Delete this file?');

    state.handleCancel();
    const result = await confirmPromise;
    expect(result).toBe(false);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run src/test/stores/useConfirmStore.test.ts`
Expected: FAIL (module not found)

- [ ] **Step 3: Implement `useConfirmStore` and `confirmAction`**

Create `frontend/src/shared/stores/useConfirmStore.ts`:
```typescript
import { create } from 'zustand';

export interface ConfirmOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  danger?: boolean;
}

export interface ConfirmState {
  isOpen: boolean;
  options: ConfirmOptions;
  resolve: ((value: boolean) => void) | null;
  confirm: (options: ConfirmOptions | string) => Promise<boolean>;
  handleConfirm: () => void;
  handleCancel: () => void;
}

export const useConfirmStore = create<ConfirmState>((set, get) => ({
  isOpen: false,
  options: { message: '' },
  resolve: null,
  confirm: (options: ConfirmOptions | string) => {
    const opts: ConfirmOptions = typeof options === 'string' ? { message: options } : options;
    return new Promise<boolean>((resolve) => {
      set({
        isOpen: true,
        options: {
          title: opts.title || 'Confirm Action',
          message: opts.message,
          confirmText: opts.confirmText || 'Confirm',
          cancelText: opts.cancelText || 'Cancel',
          danger: opts.danger ?? false
        },
        resolve
      });
    });
  },
  handleConfirm: () => {
    const { resolve } = get();
    if (resolve) resolve(true);
    set({ isOpen: false, resolve: null });
  },
  handleCancel: () => {
    const { resolve } = get();
    if (resolve) resolve(false);
    set({ isOpen: false, resolve: null });
  }
}));

export function confirmAction(options: ConfirmOptions | string): Promise<boolean> {
  return useConfirmStore.getState().confirm(options);
}
```

Create `frontend/src/stores/useConfirmStore.ts`:
```typescript
export * from '../shared/stores/useConfirmStore';
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run src/test/stores/useConfirmStore.test.ts`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add frontend/src/shared/stores/useConfirmStore.ts frontend/src/stores/useConfirmStore.ts frontend/src/test/stores/useConfirmStore.test.ts
git commit -m "feat(ui): add useConfirmStore and confirmAction helper"
```

---

### Task 2: Create `ConfirmModal` Component & Mount in `App.tsx`

**Files:**
- Create: `frontend/src/components/shared/ConfirmModal.tsx`
- Modify: `frontend/src/components/shared/index.ts`
- Modify: `frontend/src/App.tsx`
- Test: `frontend/src/test/components/ConfirmModal.test.tsx`

**Interfaces:**
- Consumes: `useConfirmStore`
- Produces: `<ConfirmModal />`

- [ ] **Step 1: Write failing test for `ConfirmModal`**

Create `frontend/src/test/components/ConfirmModal.test.tsx`:
```tsx
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { ConfirmModal } from '../../components/shared/ConfirmModal';
import { useConfirmStore } from '../../stores/useConfirmStore';

/**
 * @requirement REQ-UI-CONFIRM-MODAL
 * @category UI
 * @type PositiveFeature
 * @description Renders confirmation dialog with title, message, and trigger buttons for confirm and cancel.
 */
describe('ConfirmModal', () => {
  beforeEach(() => {
    useConfirmStore.setState({
      isOpen: false,
      options: { message: '' },
      resolve: null
    });
  });

  it('renders nothing when closed', () => {
    const { container } = render(<ConfirmModal />);
    expect(container.firstChild).toBeNull();
  });

  it('renders title, message, and action buttons when open', () => {
    useConfirmStore.setState({
      isOpen: true,
      options: {
        title: 'Revoke App Key',
        message: 'Are you sure you want to revoke this key?',
        confirmText: 'Revoke',
        cancelText: 'Keep Key',
        danger: true
      },
      resolve: () => {}
    });

    render(<ConfirmModal />);
    expect(screen.getByText('Revoke App Key')).toBeInTheDocument();
    expect(screen.getByText('Are you sure you want to revoke this key?')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Revoke' })).toHaveClass('btn-danger');
    expect(screen.getByRole('button', { name: 'Keep Key' })).toBeInTheDocument();
  });

  it('calls handleConfirm when confirm button clicked', () => {
    let resolvedValue: boolean | null = null;
    useConfirmStore.setState({
      isOpen: true,
      options: {
        title: 'Delete Server',
        message: 'Delete server docker?',
        confirmText: 'Delete'
      },
      resolve: (val) => { resolvedValue = val; }
    });

    render(<ConfirmModal />);
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    expect(resolvedValue).toBe(true);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });

  it('calls handleCancel when cancel button clicked', () => {
    let resolvedValue: boolean | null = null;
    useConfirmStore.setState({
      isOpen: true,
      options: {
        title: 'Delete Server',
        message: 'Delete server docker?',
        confirmText: 'Delete',
        cancelText: 'Cancel'
      },
      resolve: (val) => { resolvedValue = val; }
    });

    render(<ConfirmModal />);
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(resolvedValue).toBe(false);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run src/test/components/ConfirmModal.test.tsx`
Expected: FAIL (module not found)

- [ ] **Step 3: Implement `ConfirmModal.tsx` and register in `shared/index.ts` & `App.tsx`**

Create `frontend/src/components/shared/ConfirmModal.tsx`:
```tsx
import React from 'react';
import { Modal } from './Modal';
import { useConfirmStore } from '../../stores/useConfirmStore';

export const ConfirmModal: React.FC = () => {
  const { isOpen, options, handleConfirm, handleCancel } = useConfirmStore();

  if (!isOpen) return null;

  const titleNode = (
    <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      {options.danger ? (
        <i className="fa-solid fa-triangle-exclamation" style={{ color: 'var(--status-offline)' }}></i>
      ) : (
        <i className="fa-solid fa-circle-question" style={{ color: 'var(--primary)' }}></i>
      )}
      {options.title || 'Confirm Action'}
    </span>
  );

  return (
    <Modal
      id="confirm-modal"
      isOpen={isOpen}
      onClose={handleCancel}
      title={titleNode}
      maxWidth="460px"
    >
      <div style={{ padding: '8px 0 20px 0', color: 'var(--text-main)', fontSize: 'var(--font-size-md)', lineHeight: '1.5' }}>
        <p style={{ margin: 0 }}>{options.message}</p>
      </div>
      <div className="modal-footer" style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
        <button
          type="button"
          className="btn btn-secondary"
          onClick={handleCancel}
        >
          {options.cancelText || 'Cancel'}
        </button>
        <button
          type="button"
          className={`btn ${options.danger ? 'btn-danger' : 'btn-primary'}`}
          onClick={handleConfirm}
          autoFocus
        >
          {options.confirmText || 'Confirm'}
        </button>
      </div>
    </Modal>
  );
};
```

Export `ConfirmModal` in `frontend/src/components/shared/index.ts`:
```typescript
export * from './Header';
export * from './Footer';
export * from './StatusBadge';
export * from './PaginationToolbar';
export * from './Modal';
export * from './ConfirmModal';
export * from './Toasts';
```

Mount `<ConfirmModal />` in `frontend/src/App.tsx`.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run src/test/components/ConfirmModal.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/shared/ConfirmModal.tsx frontend/src/components/shared/index.ts frontend/src/App.tsx frontend/src/test/components/ConfirmModal.test.tsx
git commit -m "feat(ui): add ConfirmModal component and mount in App"
```

---

### Task 3: Replace `alert(...)` with `showToast(...)`

**Files:**
- Modify: `frontend/src/components/clients/AppKeysCard.tsx`
- Modify: `frontend/src/components/settings/CustomFileModal.tsx`
- Modify: `frontend/src/components/settings/IdentityAuthTab.tsx`
- Modify: `frontend/src/components/settings/SecretProvidersTab.tsx`
- Modify: `frontend/src/pages/MyMcpServers.tsx`
- Test: Update tests in `frontend/src/test/components/AppKeysCard.test.tsx`, `CustomFileModal.test.tsx`, `IdentityAuthTab.test.tsx`

- [ ] **Step 1: Update components to use `showToast`**

In `AppKeysCard.tsx`:
Replace `alert('Copied sample mcp_config.json snippet to clipboard!');`
With `showToast('Copied sample mcp_config.json snippet to clipboard!', 'success');` (import `showToast` from `../../stores/useToastStore`).

In `CustomFileModal.tsx`:
Replace `alert('Cannot switch to Visual Builder: JSON in editor is invalid.');`
With `showToast('Cannot switch to Visual Builder: JSON in editor is invalid.', 'error');`
Replace `alert('Please enter a file name.');`
With `showToast('Please enter a file name.', 'error');`
Replace `alert('Invalid JSON content. Please check syntax or use the Visual Builder.');`
With `showToast('Invalid JSON content. Please check syntax or use the Visual Builder.', 'error');` (import `showToast` from `../../stores/useToastStore`).

In `IdentityAuthTab.tsx`:
Replace `alert('Auth Provider configurations saved successfully!');`
With `showToast('Auth Provider configurations saved successfully!', 'success');`
Replace `alert('Failed to save Auth Providers');`
With `showToast('Failed to save Auth Providers', 'error');` (import `showToast` from `../../stores/useToastStore`).

In `SecretProvidersTab.tsx`:
Replace `alert('Secret Provider configurations saved successfully!');`
With `showToast('Secret Provider configurations saved successfully!', 'success');`
Replace `alert('Failed to save Secret Providers');`
With `showToast('Failed to save Secret Providers', 'error');` (import `showToast` from `../../stores/useToastStore`).

In `MyMcpServers.tsx`:
Replace `alert('Invalid JSON or failed to save.');`
With `showToast('Invalid JSON or failed to save.', 'error');` (import `showToast` from `../stores/useToastStore`).

- [ ] **Step 2: Update existing component tests to assert `showToast` instead of `window.alert`**

Verify and update tests in `CustomFileModal.test.tsx`, `AppKeysCard.test.tsx`, `IdentityAuthTab.test.tsx`.

- [ ] **Step 3: Run component tests**

Run: `npm test -- --run src/test/components/CustomFileModal.test.tsx src/test/components/AppKeysCard.test.tsx src/test/components/IdentityAuthTab.test.tsx`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/clients/AppKeysCard.tsx frontend/src/components/settings/CustomFileModal.tsx frontend/src/components/settings/IdentityAuthTab.tsx frontend/src/components/settings/SecretProvidersTab.tsx frontend/src/pages/MyMcpServers.tsx frontend/src/test/components/
git commit -m "refactor(ui): replace browser alert calls with showToast"
```

---

### Task 4: Replace `window.confirm(...)` in Stores with `confirmAction(...)`

**Files:**
- Modify: `frontend/src/stores/useAppKeyStore.ts`
- Modify: `frontend/src/stores/useClientStore.ts`
- Modify: `frontend/src/stores/useServerStore.ts`
- Modify: `frontend/src/stores/useSettingsStore.ts`
- Test: Update `frontend/src/test/stores/useClientStore.test.ts`, `usePolicyStore.test.ts`, `useServerStore.test.ts`

- [ ] **Step 1: Replace `window.confirm` in stores**

In `useAppKeyStore.ts`:
```typescript
import { confirmAction } from './useConfirmStore';

revokeKey: async (id: string, name: string) => {
  const confirmed = await confirmAction({
    title: 'Revoke App Key',
    message: `Are you sure you want to revoke the App Key '${name}'? This cannot be undone.`,
    confirmText: 'Revoke Key',
    danger: true
  });
  if (!confirmed) return;
  ...
}
```

In `useClientStore.ts`:
```typescript
import { confirmAction } from './useConfirmStore';

deleteClient: async (id: string, name: string) => {
  const confirmed = await confirmAction({
    title: 'Delete Client',
    message: `Are you sure you want to delete the registered client '${name}'?`,
    confirmText: 'Delete Client',
    danger: true
  });
  if (!confirmed) return;
  ...
}
```

In `useServerStore.ts`:
```typescript
import { confirmAction } from './useConfirmStore';

deleteServer: async (id: string, name: string) => {
  const confirmed = await confirmAction({
    title: 'Delete Server',
    message: `Are you sure you want to delete the MCP server '${name}'?`,
    confirmText: 'Delete Server',
    danger: true
  });
  if (!confirmed) return;
  ...
}
```

In `useSettingsStore.ts`:
```typescript
import { confirmAction } from './useConfirmStore';

deleteCustomFile: async (name: string) => {
  const confirmed = await confirmAction({
    title: 'Delete Custom File',
    message: `Are you sure you want to delete the custom file '${name}'? This action cannot be undone.`,
    confirmText: 'Delete File',
    danger: true
  });
  if (!confirmed) return;
  ...
},
deletePolicy: async (policyId: string) => {
  const confirmed = await confirmAction({
    title: 'Delete Access Policy',
    message: 'Are you sure you want to delete this access policy?',
    confirmText: 'Delete Policy',
    danger: true
  });
  if (!confirmed) return;
  ...
},
deleteGroupMapping: async (mappingId: string) => {
  const confirmed = await confirmAction({
    title: 'Delete Group Mapping',
    message: 'Are you sure you want to delete this group mapping?',
    confirmText: 'Delete Mapping',
    danger: true
  });
  if (!confirmed) return;
  ...
}
```

- [ ] **Step 2: Update store tests**

Update store tests to mock `confirmAction` or trigger confirmation via `useConfirmStore.getState().handleConfirm()` / `handleCancel()`.

- [ ] **Step 3: Run all store tests**

Run: `npm test -- --run src/test/stores/`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add frontend/src/stores/ frontend/src/test/stores/
git commit -m "refactor(stores): replace window.confirm with confirmAction"
```

---

### Task 5: Version Bump, Catalog Generation, & Verification

**Files:**
- Modify: `mcp-router.csproj` (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`: `4.26.1`)
- Modify: `frontend/src/stores/useUserStore.ts` (`version: '4.26.1'`)
- Modify: `frontend/src/shared/stores/useUserStore.ts` (`version: '4.26.1'`)
- Modify: `CHANGELOG.md` (Add `v4.26.1` release entry)
- Modify: `README.md` (Update top-5 release preview table)
- Update: `docs/software-requirements-and-test-catalog.md` & `docs/requirements-catalog.json`

- [ ] **Step 1: Bump version numbers and update docs**
- [ ] **Step 2: Run all frontend tests**
Run: `npm test -- --run`
Expected: PASS
- [ ] **Step 3: Run catalog generator and verify**
Run: `dotnet run --project scripts/CatalogGenerator`
Run: `dotnet run --project scripts/CatalogGenerator -- --verify-only`
Expected: Verification PASS
- [ ] **Step 4: Commit release bump**
```bash
git add mcp-router.csproj frontend/src/stores/useUserStore.ts frontend/src/shared/stores/useUserStore.ts CHANGELOG.md README.md docs/software-requirements-and-test-catalog.md docs/requirements-catalog.json
git commit -m "chore(release): v4.26.1 - custom confirmation modal and toast migration"
```
