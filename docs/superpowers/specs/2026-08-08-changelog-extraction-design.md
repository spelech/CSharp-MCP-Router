# Design Spec: Extract Release Changelog to CHANGELOG.md

**Date**: 2026-08-08  
**Status**: Approved  

---

## 1. Overview
The release changelog table in `README.md` has grown to over 110 rows (`v4.2.15` down to `v2.0.0`), significantly bloating `README.md` and adding unnecessary maintenance overhead during atomic commits. This spec outlines extracting the full release history into a standalone `CHANGELOG.md` file while preserving a top-5 recent releases preview table in `README.md`.

---

## 2. Structural & File Changes

### 2.1 New `CHANGELOG.md`
- Created in repository root (`/containers/dev/csharp-mcp-router/CHANGELOG.md`).
- Formatted with standard header and full Markdown table containing all historical versions (`v4.2.15` through `v2.0.0`).

### 2.2 Updated `README.md`
- Replace the 110+ row table under `## 📜 Release Changelog` with:
  - A short intro linking to `CHANGELOG.md`.
  - A compact table containing only the **5 most recent releases**.

### 2.3 Updated `scripts/bump_version.py`
- Modify `bump_version.py` to:
  1. Read and prepend new release rows to `CHANGELOG.md`.
  2. Update the top 5 recent releases preview table in `README.md`.
  3. Include `CHANGELOG.md` in `paths_to_stage` for `git add`.

### 2.4 Updated Repository Documentation Rules
- Update `AGENTS.md` and `.agents/GEMINI.md` to list `CHANGELOG.md` alongside `mcp-router.csproj` and `frontend/src/stores/useUserStore.ts` as mandatory versioning files.

---

## 3. Verification Criteria
- `python3 scripts/bump_version.py "test(changelog): test version bump"` correctly updates `CHANGELOG.md` and top 5 rows of `README.md`.
- `CHANGELOG.md` contains 100% of historical releases.
- All unit tests (`dotnet test McpRouter.slnx`) pass cleanly.
