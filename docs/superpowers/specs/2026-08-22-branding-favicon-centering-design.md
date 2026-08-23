# Design Specification: Branding Customization, Dynamic Favicon, PNG Logo Upload, and Navigation Centering

- **Date:** 2026-08-22
- **Topic:** Branding, Favicon, PNG Logo Upload, and Layout Centering
- **Status:** Approved

---

## 1. Overview & Goals

This specification defines the implementation of:
1. **Dynamic Favicon and Browser Tab Branding**: Adding default favicon assets, dynamically updating `document.title` and the browser favicon `<link rel="icon">` when custom branding settings (`DashboardTitle`, `DashboardIcon`) are updated.
2. **PNG / Image Logo Support & File Upload**: Extending the gateway's branding settings to support PNG/image logos in addition to FontAwesome icon classes. Admins can upload custom logo image files directly in the Settings dashboard, which are stored persistently in `data/branding/` and served via `/api/config/branding/logo`.
3. **Centered Navigation Layout**: Centering the top navigation bar (`.tabs-nav`) and all sub-navigation bars (`.settings-sub-nav`, `.tester-tabs`, `.sub-tabs-nav`) across the application for visual consistency.

---

## 2. Architecture & Backend Endpoints

### 2.1 Backend Endpoints

1. **`POST /api/config/branding/logo`** (Admin Protected):
   - Accepts multipart/form-data upload (`IFormFile`) containing an image (`.png`, `.jpg`, `.jpeg`, `.svg`, `.ico`, `.webp`).
   - Validates file type and limits size (max 2MB).
   - Writes image to `data/branding/logo.<extension>` (and creates directory if missing).
   - Updates `RouterSettings.DashboardIcon` to `/api/config/branding/logo` and saves settings to persistence repository.
   - Returns JSON: `{ "url": "/api/config/branding/logo", "success": true }`.

2. **`GET /api/config/branding/logo`** (Public / Unauthenticated):
   - Checks `data/branding/` directory for existing logo file.
   - If found, streams file with appropriate `Content-Type` (e.g. `image/png`, `image/svg+xml`) and `Cache-Control: public, max-age=3600`.
   - If not found, returns `404 Not Found`.

3. **`GET /api/config/branding`** (Public):
   - Returns `{ "title": settings.DashboardTitle, "icon": settings.DashboardIcon }`.

4. **`POST /api/settings`** & **`GET /api/settings`** (Admin Protected):
   - Existing endpoints continue to retrieve and update `DashboardTitle` and `DashboardIcon`.

---

## 3. Frontend Architecture & Components

### 3.1 Dynamic Favicon & Tab Branding Hook / Sync

- Default favicon assets (`favicon.ico`, `favicon.svg`) placed in `frontend/public/` and referenced in `frontend/index.html`.
- Dynamic sync logic:
  - On application load and when `branding` store updates:
    - Update `document.title = branding.title ? `${branding.title} - MCP Router` : 'MCP Gateway'`.
    - If `branding.icon` is an image URL (starts with `/`, `http`, `data:` or has image extension), set `<link rel="icon">` `href` to the image URL.
    - If `branding.icon` is a FontAwesome class (or default), create an SVG Data URI favicon rendering a modern branded glyph in the primary orange palette (`#f97316`) and set `<link rel="icon">` `href`.

### 3.2 Logo Rendering in Header

In [`Header.tsx`](file:///containers/dev/csharp-mcp-router/frontend/src/shared/components/Header.tsx):
- Detect whether `branding.icon` represents an image URL (`isImageUrl` helper).
- If true: Render `<img src={branding.icon} alt="Logo" className="logo-icon logo-img" />`.
- If false: Render `<i className={`${branding?.icon || 'fa-solid fa-network-wired'} logo-icon`}></i>`.
- Style `.logo-img` with `height: 36px; width: auto; max-width: 48px; object-fit: contain; vertical-align: middle;`.

### 3.3 General Settings Tab Enhancement

In [`GeneralTab.tsx`](file:///containers/dev/csharp-mcp-router/frontend/src/components/settings/GeneralTab.tsx):
- Add an Image Upload button / file input next to the Header Icon field.
- When an image file is selected:
  - Calls `settingsApi.uploadBrandingLogo(file)`.
  - On success, sets `dashboardIcon` to `/api/config/branding/logo` and refreshes branding.
- Add a live preview display showing how the logo will render (both FontAwesome icon and PNG/image modes).

### 3.4 Navigation Alignment & Centering

In CSS files:
- **`layout.css`**: `.tabs-nav { justify-content: center; }`
- **`tester.css`**: `.tester-tabs { justify-content: center; }`
- **`SettingsView.tsx`**: `.settings-sub-nav { justify-content: center; }`
- **`AppKeysCard.tsx`**: `.sub-tabs-nav { justify-content: center; }`

---

## 4. Requirement Tracing & Verification

- **Requirement Annotations**:
  - Positive requirement tests for branding logo upload, retrieval, and dynamic rendering (`[Requirement("UI-05", ...)]`, `[Requirement("UI-06", ...)]` as needed).
  - Vitest component tests verifying header logo rendering (image vs FA icon) and settings image upload.
  - Verification of SRS catalog: `dotnet run --project scripts/CatalogGenerator -- --verify-only`.
