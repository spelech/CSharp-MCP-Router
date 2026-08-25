# C# Formatting Standardization & Housekeeping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Standardize C# code formatting and brace rules via `.editorconfig`, integrate `dotnet format` verification into CI, eliminate dead code warnings, and verify full requirements catalog integrity.

**Architecture:** Configure root `.editorconfig` with Roslyn style and diagnostic rules, update GitHub Actions CI to enforce format verification on `.slnx`, execute a solution-wide format and dead-code sweep, and run tests/catalog generators to ensure zero regressions.

**Tech Stack:** .NET 10 / C# 13, Roslyn Analyzers, GitHub Actions CI, xUnit, Dapper, OpenIddict.

## Global Constraints
- Target Framework: net10.0
- Mandatory curly braces for all control flow statements (`csharp_prefer_braces = true:warning`)
- Preserve all MCP dual-spec and protocol version compatibility logic
- Preserve all database schema migration logic in `Infrastructure/Persistence/`
- Every test requirement must remain fully annotated and cataloged with zero drift (`scripts/CatalogGenerator`)
- Release version bump from 4.34.3 to 4.34.4 across `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`

---

### Task 1: Update `.editorconfig` with Comprehensive Formatting and Roslyn Diagnostic Rules

**Files:**
- Modify: `.editorconfig`

**Interfaces:**
- Consumes: Design spec from `docs/superpowers/specs/2026-08-24-csharp-formatting-and-housekeeping-design.md`
- Produces: Root `.editorconfig` containing brace rules, spacing rules, using sorting, and Roslyn dead-code diagnostic severities

- [ ] **Step 1: Write updated `.editorconfig`**

Update `.editorconfig` with:
```ini
root = true

# All files
[*]
indent_style = space
indent_size = 2
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# files
[*.cs]
indent_size = 4

# C# Control flow & Braces
csharp_prefer_braces = true:warning
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true

# C# Spacing & Layout
csharp_space_after_cast = false
csharp_space_around_binary_operators = before_and_after
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_declaration_statements = false

# Import directives
csharp_using_directive_placement = outside_namespace:warning
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Dead code & Roslyn Diagnostics
dotnet_diagnostic.IDE0005.severity = warning
dotnet_diagnostic.IDE0051.severity = warning
dotnet_diagnostic.IDE0052.severity = warning
dotnet_diagnostic.IDE0059.severity = warning
dotnet_diagnostic.CS0168.severity = warning
dotnet_diagnostic.CS0219.severity = warning

# Style conventions
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# Shell scripts
[*.sh]
end_of_line = lf
```

- [ ] **Step 2: Commit `.editorconfig` changes**

```bash
git add .editorconfig
git commit -m "chore: configure csharp editorconfig with brace rules and dead code diagnostics"
```

---

### Task 2: Add `dotnet format` Verification Gate to GitHub Actions CI

**Files:**
- Modify: `.github/workflows/ci.yml:32-60`

**Interfaces:**
- Consumes: Task 1 `.editorconfig`
- Produces: Backend CI job step running `dotnet format McpRouter.slnx --verify-no-changes`

- [ ] **Step 1: Update `.github/workflows/ci.yml`**

Add the formatting verification step in the `backend` job right after dependency restore:
```yaml
      - name: Restore dependencies
        run: dotnet restore McpRouter.slnx

      - name: Verify C# code formatting & style
        run: dotnet format McpRouter.slnx --verify-no-changes
```

- [ ] **Step 2: Commit CI workflow change**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add dotnet format verification gate to backend CI"
```

---

### Task 3: Solution-Wide Automated Formatting & Dead Code Remediation

**Files:**
- Modify: Solution C# files across `mcp-router`, `McpRouter.Tests`, `scripts/CatalogGenerator`

**Interfaces:**
- Consumes: Task 1 rules and Task 2 CI gate
- Produces: C# codebase adhering 100% to `.editorconfig` formatting, curly braces on control flow, and zero dead code compiler warnings

- [ ] **Step 1: Execute `dotnet format` on the solution**

Run:
```bash
dotnet format McpRouter.slnx
```

- [ ] **Step 2: Inspect and fix compiler warnings & dead code**

Run `dotnet build McpRouter.slnx` and resolve any warnings (unused variables, unread private fields, redundant assignments).

- [ ] **Step 3: Verify formatting passes cleanly**

Run:
```bash
dotnet format McpRouter.slnx --verify-no-changes
```
Expected: Exits with code 0 (no unformatted files).

- [ ] **Step 4: Commit formatted codebase and dead code fixes**

```bash
git add -A
git commit -m "style: apply dotnet format and resolve dead code warnings"
```

---

### Task 4: Comprehensive Test Suite & Requirements Catalog Verification

**Files:**
- Modify: `docs/software-requirements-and-test-catalog.md` (if regenerated)
- Modify: `docs/requirements-catalog.json` (if regenerated)

**Interfaces:**
- Consumes: Formatted code from Task 3
- Produces: 100% passing tests and verified Requirements Catalog

- [ ] **Step 1: Run solution tests**

Run:
```bash
dotnet test McpRouter.slnx --configuration Release
```
Expected: All tests pass with 0 failures.

- [ ] **Step 2: Regenerate and verify requirements catalog**

Run:
```bash
dotnet run --project scripts/CatalogGenerator
dotnet run --project scripts/CatalogGenerator -- --verify-only
```
Expected: Verification succeeds with zero drift.

- [ ] **Step 3: Commit catalog updates (if any)**

```bash
git add docs/
git commit -m "docs: sync test requirements catalog"
```

---

### Task 5: Version Bump (4.34.4) & Release Documentation

**Files:**
- Modify: `mcp-router.csproj`
- Modify: `frontend/src/stores/useUserStore.ts`
- Modify: `CHANGELOG.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: Completed formatting, CI gate, and verified test suite
- Produces: Synchronized release metadata for v4.34.4

- [ ] **Step 1: Update version in `mcp-router.csproj`**

Set `<Version>4.34.4</Version>`, `<AssemblyVersion>4.34.4.0</AssemblyVersion>`, `<FileVersion>4.34.4.0</FileVersion>`.

- [ ] **Step 2: Update version in `frontend/src/stores/useUserStore.ts`**

Update fallback version string to `'4.34.4'`.

- [ ] **Step 3: Update `CHANGELOG.md` and `README.md`**

Add `4.34.4` release entry describing the `.editorconfig` formatting standardization, CI quality gate, and housekeeping. Update top 5 table in `README.md`.

- [ ] **Step 4: Run release verification script**

Run:
```bash
python3 scripts/verify_release.py --skip-tests
```
Expected: PASS with all version checks and markdown links verified.

- [ ] **Step 5: Commit release synchronization**

```bash
git add mcp-router.csproj frontend/src/stores/useUserStore.ts CHANGELOG.md README.md
git commit -m "chore: bump version to 4.34.4 and update changelog"
```
