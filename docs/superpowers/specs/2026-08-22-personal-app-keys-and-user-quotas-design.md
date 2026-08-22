# Design Specification: Self-Service Personal App Keys, App-Level Keys & User Quotas

## 1. Overview & Objectives

This specification defines the system architecture, database changes, API enhancements, and frontend views for:
1. **Self-Service Personal App Keys**: Allowing authenticated non-admin users to mint, copy, and manage up to their allowed quota of personal App Keys for IDEs (Cursor, VS Code, Windsurf) and CLI tools.
2. **Explicit Separation of App-Level (System/Service) Keys**: Providing administrators with distinct management of system/daemon/CI keys vs. personal user keys.
3. **Quota Engine**: Default quota of 5 keys per user (configurable in Settings), with per-user custom quota overrides in a `UserQuotas` table.
4. **Role-Adaptive Dashboard UI**: Tailoring navigation and App Key views according to user roles.

---

## 2. Data Model & Database Schema

### 2.1 `AppKeys` Table Update
- Added column: `KeyType TEXT NOT NULL DEFAULT 'personal'` (values: `'personal'`, `'system'`).
- Added column: `OwnerSid TEXT DEFAULT ''`.

### 2.2 `UserQuotas` Table
```sql
CREATE TABLE IF NOT EXISTS UserQuotas (
    Username VARCHAR(255) NOT NULL PRIMARY KEY,
    MaxKeys INT NOT NULL DEFAULT 5,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### 2.3 `Settings` Table
- `UserMaxKeys INT NOT NULL DEFAULT 5` (Default per-user limit when no override exists; `0` = Unlimited).
- `GlobalMaxKeys INT NOT NULL DEFAULT 100` (Global maximum active keys across entire gateway; `0` = Unlimited).

---

## 3. Backend Architecture & API Contracts

### 3.1 `AppKeysController` (`/api/AppKeys`)
- Authorization: Class attribute updated to `[Authorize]`.
- Endpoints:
  - `GET /api/AppKeys?keyType={personal|system}&usernameFilter={user}`:
    - If non-admin: Forced to `keyType = "personal"` and `username = currentUser`.
    - If admin: Returns requested `keyType` or all keys, with optional username filter.
  - `GET /api/AppKeys/limits`:
    - Resolves effective user quota (`userMax`) from `UserQuotas` if override exists, else `Settings.UserMaxKeys`.
    - Returns `{ globalMax, userMax, totalActiveKeys, userActiveKeys, isLimitReached }`.
  - `POST /api/AppKeys`:
    - Non-admin: `KeyType` must be `"personal"`, `Username` forced to `currentUser`, scopes restricted to standard non-admin scopes (`all`, `server:*`, `category:*`), enforces `userActiveKeys < userMax`.
    - Admin: Can set `KeyType = "system"` or `KeyType = "personal"`, can set `Username`, can grant admin scopes, bypasses quotas.
  - `DELETE /api/AppKeys/{id}`:
    - Non-admin: Forbidden unless key belongs to `currentUser` and `KeyType == "personal"`.
    - Admin: Can delete any key.
  - `GET /api/AppKeys/quotas` (`[Authorize(Policy = "AdminPolicy")]`): Lists all custom user quotas.
  - `POST /api/AppKeys/quotas` (`[Authorize(Policy = "AdminPolicy")]`): Sets custom quota for a username `{ username, maxKeys }`.
  - `DELETE /api/AppKeys/quotas/{username}` (`[Authorize(Policy = "AdminPolicy")]`): Removes user quota override.

---

## 4. Frontend Experience

### 4.1 Role-Adaptive Navigation (`App.tsx`)
- Non-admin users: Tab titled **"My App Keys"**, **"Settings"** tab hidden.
- Administrators: Tabs include **"App Keys & Security"**, **"Settings"**, **"Overview"**, **"Test Bench"**, and **"My MCP Servers"**.

### 4.2 App Keys Card (`AppKeysCard.tsx`)
- Regular Users:
  - Header: `"My App Keys"` with personal quota indicator (`Personal Quota: 2 / 5 Keys Used`).
  - Lists only user's personal keys with copy config snippet and revoke button.
- Administrators:
  - Sub-views/tabs for:
    1. **"App-Level Keys"**: System and daemon service keys.
    2. **"User Personal Keys"**: User personal keys with username filtering and custom quota editor modal.

### 4.3 General Settings Tab (`GeneralTab.tsx`)
- Inputs for `Default User Quota (UserMaxKeys)` and `GlobalMaxKeys`.

---

## 5. Requirements Identifiers

- `REQ-AUTH-PERSONAL-APPKEY-LIST`: Non-admin users can view their own personal App Keys.
- `REQ-AUTH-PERSONAL-APPKEY-CREATE`: Non-admin users can self-mint personal App Keys up to their quota.
- `REQ-AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE`: Admins can configure per-user custom quota limits.
- `REQ-AUTH-SYSTEM-APPKEY-SEPARATION`: System/App-level keys are distinct from personal user keys and managed by admins.
