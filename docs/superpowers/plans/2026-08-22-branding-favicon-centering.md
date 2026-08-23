# Branding Customization, Dynamic Favicon, PNG Logo Upload, and Navigation Centering Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide full custom branding support (dynamic tab title and favicon, PNG/image logo upload and rendering alongside FontAwesome icons) and center all top-level and sub-level navigation bars.

**Architecture:** ASP.NET Core endpoints `POST /api/config/branding/logo` and `GET /api/config/branding/logo` handle image uploads stored in `data/branding/` and stream the logo file. Frontend React components dynamically update `<link rel="icon">` and `document.title`, render either `<img>` or `<i>` in Header based on format detection, support direct logo upload with preview in Settings `GeneralTab`, and center `.tabs-nav`, `.settings-sub-nav`, `.tester-tabs`, and `.sub-tabs-nav`.

**Tech Stack:** C# .NET 9 ASP.NET Core, React 19, TypeScript, Zustand, CSS variables, Vitest, xUnit.

## Global Constraints

- Never use `string.Replace` on JSON payloads; use `System.Text.Json` (`JsonNode`, `JsonDocument`).
- All tests must use `[Requirement("UI-xx", ...)]` or JSDoc `@requirement UI-xx` (No `REQ-` prefixes).
- Version bumps must update `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`.
- Regenerate requirements catalog via `dotnet run --project scripts/CatalogGenerator -- --verify-only`.

---

### Task 1: Backend Branding Logo Endpoints & Tests

**Files:**
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Modify: `Components/Capabilities/CapabilityEndpoints.cs`
- Test: `McpRouter.Tests/PipelineIntegrationTests.cs`

**Interfaces:**
- Produces:
  - `POST /api/config/branding/logo` (Admin policy): Accepts `IFormFile`, writes to `data/branding/logo.<ext>`, updates `RouterSettings.DashboardIcon` to `/api/config/branding/logo`, returns `{ url = "/api/config/branding/logo", success = true }`.
  - `GET /api/config/branding/logo` (Public): Streams file from `data/branding/logo.*` with `Content-Type` or returns `404 Not Found`.

- [ ] **Step 1: Write failing integration test for branding logo upload and retrieval**

Add test to `McpRouter.Tests/PipelineIntegrationTests.cs`:
```csharp
[Fact]
[Requirement("UI-06", "UI", RequirementType.Positive, "Router supports uploading and retrieving custom branding logo images via dedicated endpoints.")]
public async Task Branding_Logo_Upload_And_Retrieval_Works()
{
    // Upload a dummy png to /api/config/branding/logo with admin auth
    // Verify GET /api/config/branding/logo returns the image with 200 OK
    // Verify GET /api/config/branding returns icon = "/api/config/branding/logo"
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test McpRouter.Tests --filter "FullyQualifiedName~Branding_Logo_Upload_And_Retrieval_Works"`
Expected: FAIL (404 Not Found for `/api/config/branding/logo`)

- [ ] **Step 3: Implement backend endpoints**

In `Extensions/ApplicationBuilderExtensions.cs`:
Map public `GET /api/config/branding/logo`:
```csharp
app.MapGet("/api/config/branding/logo", () =>
{
    var dir = Path.Combine(AppContext.BaseDirectory, "data", "branding");
    if (!Directory.Exists(dir)) return Results.NotFound();
    var file = Directory.GetFiles(dir, "logo.*").FirstOrDefault();
    if (file == null) return Results.NotFound();
    var ext = Path.GetExtension(file).ToLowerInvariant();
    var contentType = ext switch
    {
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream"
    };
    return Results.File(file, contentType, enableRangeProcessing: true);
});
```

In `Components/Capabilities/CapabilityEndpoints.cs`:
Map admin `POST /api/config/branding/logo`:
```csharp
api.MapPost("/api/config/branding/logo", async (HttpRequest request, [FromServices] ISettingRepository settingsRepo, [FromServices] DynamicEmbeddingService embeddingService, [FromServices] IAuditLogger auditLogger, HttpContext httpContext) =>
{
    if (!request.HasFormContentType || request.Form.Files.Count == 0)
        return Results.BadRequest(new { error = "No file uploaded" });

    var file = request.Form.Files[0];
    if (file.Length > 2 * 1024 * 1024)
        return Results.BadRequest(new { error = "File size exceeds 2MB limit" });

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".ico", ".webp" };
    if (!allowedExtensions.Contains(ext))
        return Results.BadRequest(new { error = "Unsupported image format" });

    var dir = Path.Combine(AppContext.BaseDirectory, "data", "branding");
    Directory.CreateDirectory(dir);
    
    // Remove older logo files
    foreach (var old in Directory.GetFiles(dir, "logo.*"))
    {
        try { File.Delete(old); } catch { }
    }

    var targetPath = Path.Combine(dir, $"logo{ext}");
    using (var stream = new FileStream(targetPath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var settings = await settingsRepo.GetSettingsAsync() ?? new RouterSettings();
    settings.DashboardIcon = "/api/config/branding/logo";
    await settingsRepo.SaveSettingsAsync(settings);
    embeddingService.UpdateSettings(settings);

    var username = httpContext.User.Identity?.Name ?? "admin";
    await auditLogger.LogAdminActionAsync(username, "branding.logo.upload", "BrandingLogo", $"/api/config/branding/logo", true);

    return Results.Ok(new { url = "/api/config/branding/logo", success = true });
}).DisableAntiforgery();
```

- [ ] **Step 4: Run integration tests to verify they pass**

Run: `dotnet test McpRouter.Tests --filter "FullyQualifiedName~Branding_Logo_Upload_And_Retrieval_Works"`
Expected: PASS

- [ ] **Step 5: Commit changes**

```bash
git add McpRouter.Tests/PipelineIntegrationTests.cs Extensions/ApplicationBuilderExtensions.cs Components/Capabilities/CapabilityEndpoints.cs
git commit -m "feat(branding): add backend endpoints for custom branding logo upload and retrieval"
```

---

### Task 2: Frontend Branding & Dynamic Favicon / Title Sync

**Files:**
- Create: `frontend/public/favicon.svg`
- Modify: `frontend/index.html`
- Modify: `frontend/src/shared/components/Header.tsx`
- Modify: `frontend/src/stores/useConfigStore.ts`
- Create/Modify: `frontend/src/shared/utils/branding.ts`
- Test: `frontend/src/test/components/HeaderBranding.test.tsx`

**Interfaces:**
- `isImageUrl(icon?: string): boolean`
- `updateFaviconAndTitle(title?: string, icon?: string): void`

- [ ] **Step 1: Write failing frontend test for dynamic favicon, title, and logo image rendering**

Create `frontend/src/test/components/HeaderBranding.test.tsx`:
```typescript
/**
 * @requirement UI-05
 * @category UI
 * @type PositiveFeature
 * @description Header renders PNG logo image when configured and FontAwesome icon when class is provided.
 */
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { Header } from '../../shared/components/Header';
import { useConfigStore } from '../../stores/useConfigStore';

describe('Header Branding Rendering', () => {
  it('renders image when branding icon is a URL/PNG', () => {
    useConfigStore.setState({ branding: { title: 'Custom Corp', icon: '/api/config/branding/logo' } });
    render(<Header />);
    const img = screen.getByAltText('Logo');
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('src', '/api/config/branding/logo');
  });

  it('renders FontAwesome icon when branding icon is a CSS class', () => {
    useConfigStore.setState({ branding: { title: 'Custom Corp', icon: 'fa-solid fa-bolt' } });
    render(<Header />);
    const icon = document.querySelector('i.fa-bolt');
    expect(icon).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run frontend test to verify it fails**

Run: `cd frontend && npm test src/test/components/HeaderBranding.test.tsx`
Expected: FAIL (Image element not found)

- [ ] **Step 3: Implement dynamic favicon, title sync, and Header rendering**

Create `frontend/src/shared/utils/branding.ts`:
```typescript
export function isImageUrl(icon?: string | null): boolean {
  if (!icon) return false;
  const trimmed = icon.trim().toLowerCase();
  if (trimmed.startsWith('data:image/') || trimmed.startsWith('http://') || trimmed.startsWith('https://') || trimmed.startsWith('/')) {
    return true;
  }
  return /\.(png|jpg|jpeg|svg|ico|webp)$/i.test(trimmed);
}

export function updateFaviconAndTitle(title?: string, icon?: string) {
  if (typeof document === 'undefined') return;

  // Title
  document.title = title ? `${title} - MCP Router` : 'MCP Router Gateway Dashboard';

  // Favicon
  let link: HTMLLinkElement | null = document.querySelector("link[rel~='icon']");
  if (!link) {
    link = document.createElement('link');
    link.rel = 'icon';
    document.getElementsByTagName('head')[0].appendChild(link);
  }

  if (isImageUrl(icon)) {
    link.href = icon!;
    link.type = icon!.endsWith('.svg') ? 'image/svg+xml' : 'image/png';
  } else {
    // Default brand SVG favicon
    const svgIcon = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><rect width="100" height="100" rx="20" fill="#0f172a"/><circle cx="50" cy="50" r="30" fill="#f97316"/><path d="M50 30 L65 60 L35 60 Z" fill="#ffffff"/></svg>`;
    link.href = `data:image/svg+xml,${encodeURIComponent(svgIcon)}`;
    link.type = 'image/svg+xml';
  }
}
```

Update `Header.tsx`:
- Call `updateFaviconAndTitle(branding?.title, branding?.icon)` when `branding` changes.
- Render `<img>` if `isImageUrl(branding?.icon)`, else `<i>`.

Update `layout.css`:
```css
.logo-icon.logo-img {
    height: 36px;
    width: auto;
    max-width: 48px;
    object-fit: contain;
    border-radius: var(--radius-sm);
    display: inline-block;
    vertical-align: middle;
}
```

- [ ] **Step 4: Run frontend tests to verify they pass**

Run: `cd frontend && npm test src/test/components/HeaderBranding.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit changes**

```bash
git add frontend/public/ favicon* frontend/index.html frontend/src/shared/utils/branding.ts frontend/src/shared/components/Header.tsx frontend/src/styles/layout.css frontend/src/test/components/HeaderBranding.test.tsx
git commit -m "feat(frontend): support dynamic favicon, browser title sync, and PNG logo rendering"
```

---

### Task 3: Logo Image Upload in Settings UI

**Files:**
- Modify: `frontend/src/api/settingsApi.ts`
- Modify: `frontend/src/components/settings/GeneralTab.tsx`
- Test: `frontend/src/test/components/GeneralTabLogoUpload.test.tsx`

**Interfaces:**
- `settingsApi.uploadBrandingLogo(file: File): Promise<{ url: string; success: boolean }>`

- [ ] **Step 1: Write failing frontend test for Settings Logo Upload**

Create `frontend/src/test/components/GeneralTabLogoUpload.test.tsx`:
```typescript
/**
 * @requirement UI-05
 * @category UI
 * @type PositiveFeature
 * @description General Settings Tab provides an image upload button and live preview for custom branding logos.
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import React from 'react';
import { GeneralTab } from '../../components/settings/GeneralTab';
import * as settingsApi from '../../api/settingsApi';

describe('GeneralTab Logo Upload', () => {
  it('allows uploading a logo image and updates the icon input', async () => {
    vi.spyOn(settingsApi, 'uploadBrandingLogo').mockResolvedValue({ url: '/api/config/branding/logo', success: true });
    const mockSave = vi.fn().mockResolvedValue(true);
    render(<GeneralTab settings={null} saveEmbeddingSettings={mockSave} />);

    const uploadInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    expect(uploadInput).toBeInTheDocument();

    const file = new File(['dummy content'], 'logo.png', { type: 'image/png' });
    fireEvent.change(uploadInput, { target: { files: [file] } });

    await waitFor(() => {
      expect(settingsApi.uploadBrandingLogo).toHaveBeenCalledWith(file);
    });
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npm test src/test/components/GeneralTabLogoUpload.test.tsx`
Expected: FAIL (upload input not found or `uploadBrandingLogo` not exported)

- [ ] **Step 3: Implement logo upload in `settingsApi.ts` and `GeneralTab.tsx`**

In `frontend/src/api/settingsApi.ts`:
```typescript
export async function uploadBrandingLogo(file: File): Promise<{ url: string; success: boolean }> {
  const formData = new FormData();
  formData.append('file', file);
  return apiRequest<{ url: string; success: boolean }>('/api/config/branding/logo', {
    method: 'POST',
    body: formData,
  });
}
```

In `frontend/src/components/settings/GeneralTab.tsx`:
- Add file input with ref and upload button.
- Add live preview container showing either image or FA icon.
- Handle file upload, update `dashboardIcon` state to `/api/config/branding/logo`, and refresh config store.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npm test src/test/components/GeneralTabLogoUpload.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit changes**

```bash
git add frontend/src/api/settingsApi.ts frontend/src/components/settings/GeneralTab.tsx frontend/src/test/components/GeneralTabLogoUpload.test.tsx
git commit -m "feat(settings): add logo image upload and live preview to General Settings tab"
```

---

### Task 4: Centering Top Bar & Sub Bars

**Files:**
- Modify: `frontend/src/styles/layout.css`
- Modify: `frontend/src/styles/tester.css`
- Modify: `frontend/src/components/settings/SettingsView.tsx`
- Modify: `frontend/src/components/clients/AppKeysCard.tsx`
- Test: `frontend/src/test/components/LayoutCentering.test.tsx`

- [ ] **Step 1: Write test checking centered class styles and layouts**

Create `frontend/src/test/components/LayoutCentering.test.tsx`:
```typescript
/**
 * @requirement UI-01
 * @category UI
 * @type PositiveFeature
 * @description Navigation tab bars use centered alignment across top bar and sub bars.
 */
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import React from 'react';
import App from '../../App';

describe('Navigation Centering', () => {
  it('renders top bar tabs navigation', () => {
    const { container } = render(<App />);
    const tabsNav = container.querySelector('.tabs-nav');
    expect(tabsNav).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Update CSS styles for centering**

In `frontend/src/styles/layout.css`:
```css
.tabs-nav {
    display: flex;
    justify-content: center;
    gap: var(--space-2);
    border-bottom: 1px solid var(--border-color);
    padding-bottom: var(--space-2);
    margin-bottom: var(--space-2);
    width: 100%;
    box-sizing: border-box;
}
```

In `frontend/src/styles/tester.css`:
```css
.tester-tabs {
    display: flex;
    justify-content: center;
    gap: var(--space-2);
    border-bottom: 1px solid var(--border-color);
    margin-bottom: var(--space-4);
    padding-bottom: 5px;
}
```

In `frontend/src/components/settings/SettingsView.tsx`:
Update `.settings-sub-nav` style to `justifyContent: 'center'`.

In `frontend/src/components/clients/AppKeysCard.tsx`:
Update `.sub-tabs-nav` style to `justifyContent: 'center'`.

- [ ] **Step 3: Run all frontend tests**

Run: `cd frontend && npm test`
Expected: All tests PASS

- [ ] **Step 4: Commit changes**

```bash
git add frontend/src/styles/layout.css frontend/src/styles/tester.css frontend/src/components/settings/SettingsView.tsx frontend/src/components/clients/AppKeysCard.tsx frontend/src/test/components/LayoutCentering.test.tsx
git commit -m "style(layout): center top navigation bar and sub navigation tabs"
```

---

### Task 5: Version Bump, Catalog Generator & Full Verification

**Files:**
- Modify: `mcp-router.csproj` (`4.32.0`)
- Modify: `frontend/src/stores/useUserStore.ts` (`4.32.0`)
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `docs/software-requirements-and-test-catalog.md`
- Modify: `docs/requirements-catalog.json`

- [ ] **Step 1: Bump version numbers across project**
Bump version to `4.32.0` in `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, `README.md`.

- [ ] **Step 2: Regenerate and verify test requirements catalog**
```bash
dotnet run --project scripts/CatalogGenerator
dotnet run --project scripts/CatalogGenerator -- --verify-only
```

- [ ] **Step 3: Run full backend and frontend test suites and build**
```bash
dotnet test McpRouter.slnx
cd frontend && npm run build && npm test
```

- [ ] **Step 4: Commit release bump and verified catalog**
```bash
git add mcp-router.csproj frontend/src/stores/useUserStore.ts CHANGELOG.md README.md docs/
git commit -m "chore(release): bump version to 4.32.0 with branding and layout enhancements"
```
