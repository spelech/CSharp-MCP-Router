# Design Specification: Custom Confirmation Modal & Toast Transition

## 1. Overview & Objectives

This specification outlines the removal of all browser-native popup dialogs (`window.alert` and `window.confirm`) from the MCP Router Web Dashboard and their replacement with a unified, dark glassmorphic UI/UX design:
1. **Toast Notifications (`showToast`)**: All inform/error alerts are converted into animated toast cards via `useToastStore`.
2. **Confirmation Dialog (`ConfirmModal` & `useConfirmStore`)**: All destructive deletion and revocation prompts are converted into asynchronous, promise-based custom glassmorphic modals.

---

## 2. Architecture & Data Flow

### 2.1 Confirmation Store (`useConfirmStore`)
A centralized Zustand store managing confirmation dialog states:
- `isOpen: boolean`
- `options: ConfirmOptions`
  - `title?: string` (Default: `'Confirm Action'`)
  - `message: string`
  - `confirmText?: string` (Default: `'Confirm'`)
  - `cancelText?: string` (Default: `'Cancel'`)
  - `danger?: boolean` (Default: `false`, renders danger styling when `true`)
- `resolve: ((value: boolean) => void) | null`
- `confirmAction(options: ConfirmOptions | string): Promise<boolean>`

When `confirmAction` is invoked:
1. It opens the modal with the specified options.
2. It returns a `Promise<boolean>` whose resolution function is stored in state.
3. Clicking **Confirm** invokes `resolve(true)` and closes the modal.
4. Clicking **Cancel** or closing the modal invokes `resolve(false)` and closes the modal.

### 2.2 Modal Component (`ConfirmModal`)
- Rendered in root `App.tsx` alongside other global modals.
- Uses the existing `Modal.tsx` wrapper for animations, backdrop blur, and responsive styling.
- Displays an optional warning icon for `danger: true` actions.
- Action buttons:
  - Cancel button (`btn btn-secondary`)
  - Confirm button (`btn btn-danger` or `btn btn-primary`)

---

## 3. Scope of Replacements

### 3.1 Toast Replacements (`alert(...)` -> `showToast(...)`)
- `AppKeysCard.tsx`: Clipboard copy feedback.
- `CustomFileModal.tsx`: Visual builder invalid JSON and filename validation.
- `IdentityAuthTab.tsx`: Auth Provider save success/failure notifications.
- `SecretProvidersTab.tsx`: Secret Provider save success/failure notifications.
- `MyMcpServers.tsx`: Server config save failure notification.

### 3.2 Confirmation Replacements (`window.confirm(...)` -> `confirmAction(...)`)
- `useAppKeyStore.ts`: Revoking App Key.
- `useClientStore.ts`: Deleting registered client.
- `useServerStore.ts`: Deleting backend MCP server.
- `useSettingsStore.ts`: Deleting custom config file.
- `useSettingsStore.ts`: Deleting access control policy.
- `useSettingsStore.ts`: Deleting group mapping.

---

## 4. Requirements & Testing Strategy

### 4.1 Requirement Identifiers
- `REQ-UI-CONFIRM-MODAL`: Provides a promise-based glassmorphic confirmation modal for destructive user actions.
- `REQ-UI-TOAST-TRANSITION`: All application notifications use the unified toast notification system instead of browser-native popups.

### 4.2 Test Suite Updates
- `ConfirmModal.test.tsx`: Tests rendering, options configuration, confirming, and cancelling.
- `useConfirmStore.test.ts`: Tests store state transitions and Promise resolution.
- Update `useClientStore.test.ts`, `usePolicyStore.test.ts`, and `useServerStore.test.ts` to mock/verify `confirmAction`.
- Verify full catalog generation with `dotnet run --project scripts/CatalogGenerator -- --verify-only`.
