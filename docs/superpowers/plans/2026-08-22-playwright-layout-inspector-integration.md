# Playwright Layout Inspector Integration & Layout Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve layout and accessibility issues identified by `playwright-layout-inspector` (background DOM collisions, overflow bleed, touch targets, focus rings) and integrate `playwright-layout-inspector` into the repository test suite and CI.

**Architecture:** Modernize background styling into CSS pseudo-elements to remove DOM collisions, enforce WCAG touch targets and focus rings, add `playwright-layout-inspector` to `frontend/package.json`, create Playwright E2E layout audit specs, and wire into CI quality gates.

**Tech Stack:** TypeScript, React 19, CSS3, Playwright, `playwright-layout-inspector`, GitHub Actions.

## Global Constraints
- Never use `string.Replace` on JSON payloads.
- All test proofs must be annotated with requirement metadata (e.g. `@requirement UI-01` or `@requirement UI-07`, never use `REQ-` prefixes).
- Version bumps must update `mcp-router.csproj`, `frontend/src/shared/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`.
- Regenerate requirements catalog via `dotnet run --project scripts/CatalogGenerator -- --verify-only`.

---

### Task 1: Background Decoration, Touch Target & Focus Ring Remediation

**Files:**
- Modify: `frontend/src/styles/layout.css`
- Modify: `frontend/src/styles/styles.css`
- Modify: `frontend/src/styles/components.css`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/pages/ConsentView.tsx`
- Modify: `frontend/src/components/clients/ClientSetupGuide.tsx`
- Test: `frontend/src/test/components/LayoutCentering.test.tsx`

**Steps:**
- [ ] **Step 1: Write failing test verifying touch target and focus styles**
- [ ] **Step 2: Update `layout.css`, `App.tsx`, and `ConsentView.tsx`**
  - Replace `.background-decor` with pure CSS `body::before` / `body::after` ambient glow effects (`pointer-events: none; contain: paint; z-index: -1; filter: blur(120px);`).
  - Remove unnecessary `<div className="background-decor">` elements from `App.tsx` and `ConsentView.tsx`.
- [ ] **Step 3: Update `ClientSetupGuide.tsx` for touch targets**
  - Ensure `#meta-mode-toggle` and interactive labels have minimum 24×24px interactive touch areas.
- [ ] **Step 4: Update `styles.css` / `components.css` for focus rings**
  - Add `:focus-visible` ring indicators across buttons, tab items, inputs, and links (`outline: 2px solid var(--primary); outline-offset: 2px;`).
- [ ] **Step 5: Run tests and commit**
  - `git add frontend/ && git commit -m "fix(layout): eliminate background DOM collisions, enhance touch targets and focus rings"`

---

### Task 2: Playwright Layout Inspector Integration & E2E Specs

**Files:**
- Modify: `frontend/package.json`
- Create: `frontend/e2e/layout-inspector.spec.ts`
- Modify: `frontend/playwright.config.ts` (if needed)

**Steps:**
- [ ] **Step 1: Add dependency and npm script in `package.json`**
  - Add `"playwright-layout-inspector": "github:spelech/playwright-layout-inspector"` to `devDependencies`.
  - Add `"test:layout": "playwright-layout-inspector audit http://localhost:3000 --device 'Desktop 1080p' --min-score 85"`.
- [ ] **Step 2: Create `frontend/e2e/layout-inspector.spec.ts`**
  - Annotated with structured JSDoc `@requirement UI-07`, `@category UI`, `@type PositiveFeature`.
  - Audits Desktop 1080p and Mobile (Samsung Galaxy S25+).
  - Asserts composite UX score ≥ 85 (Grade A) and zero overflow bleeds.
- [ ] **Step 3: Run layout audit to verify high score (Grade A)**
- [ ] **Step 4: Commit**
  - `git add frontend/ && git commit -m "feat(e2e): integrate playwright-layout-inspector and add layout audit tests"`

---

### Task 3: CI Quality Gates Integration

**Files:**
- Modify: `.github/workflows/ci.yml`

**Steps:**
- [ ] **Step 1: Update `.github/workflows/ci.yml`**
  - Add conditional Playwright layout audit step when frontend files are changed.
- [ ] **Step 2: Commit**
  - `git add .github/workflows/ci.yml && git commit -m "ci: add conditional playwright layout audit step"`

---

### Task 4: Version Bump, Catalog Generator & Full Verification

**Files:**
- Modify: `mcp-router.csproj` (Bump to `4.33.0`)
- Modify: `frontend/src/shared/stores/useUserStore.ts` (Bump to `4.33.0`)
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Update: `docs/software-requirements-and-test-catalog.md` & `docs/requirements-catalog.json`

**Steps:**
- [ ] **Step 1: Bump version numbers to 4.33.0**
- [ ] **Step 2: Regenerate and verify test requirements catalog**
  - `dotnet run --project scripts/CatalogGenerator`
  - `dotnet run --project scripts/CatalogGenerator -- --verify-only`
- [ ] **Step 3: Run full backend and frontend test suites and build**
  - `dotnet test McpRouter.slnx`
  - `cd frontend && npm test && npm run build`
- [ ] **Step 4: Commit release bump**
  - `git add mcp-router.csproj frontend/src/shared/stores/useUserStore.ts CHANGELOG.md README.md docs/ && git commit -m "chore(release): bump version to 4.33.0 with layout remediation and layout inspector integration"`
