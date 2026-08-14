# Extract Release Changelog to CHANGELOG.md Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract the 110+ row release changelog table from `README.md` into a dedicated `CHANGELOG.md` file, update `README.md` to display a compact 5-release preview with a link to `CHANGELOG.md`, and update `scripts/bump_version.py` automation to keep both files synchronized.

**Architecture:** Create `CHANGELOG.md` in repository root containing all historical releases. Update `README.md` to show only the 5 most recent releases with a link to `CHANGELOG.md`. Modify `scripts/bump_version.py` so that future version bumps automatically update `CHANGELOG.md` and the 5-row preview table in `README.md`. Update repository agent documentation rules.

**Tech Stack:** Markdown, Python 3 (`scripts/bump_version.py`), Git, Bash.

## Global Constraints

- `CHANGELOG.md` must contain 100% of historical releases (`v4.2.15` through `v2.0.0`).
- `README.md` preview table must contain exactly the 5 most recent releases (`v4.2.15` through `v4.2.11`).
- `scripts/bump_version.py` must stage `CHANGELOG.md` alongside `mcp-router.csproj`, `README.md`, and `frontend/src/stores/useUserStore.ts`.
- All solution unit tests (`dotnet test McpRouter.slnx`) must pass with 0 errors.

---

### Task 1: Create CHANGELOG.md and Update README.md Preview Table

**Files:**
- Create: `CHANGELOG.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: Existing release changelog table in `README.md`.
- Produces: `CHANGELOG.md` with full history, `README.md` with 5-row preview table and link to `CHANGELOG.md`.

- [ ] **Step 1: Extract full changelog to CHANGELOG.md**

Extract all release entries from `README.md` into `CHANGELOG.md` under header `# 📜 Release Changelog`.

- [ ] **Step 2: Update README.md Release Changelog section**

Replace the 110+ row table in `README.md` with a concise header, a link `For complete release history, see [CHANGELOG.md](CHANGELOG.md)`, and a table containing only the top 5 releases (`v4.2.15` through `v4.2.11`).

- [ ] **Step 3: Verify markdown rendering and file links**

Verify `CHANGELOG.md` exists and `README.md` links cleanly to `CHANGELOG.md`.

- [ ] **Step 4: Commit changes**

```bash
git add CHANGELOG.md README.md
git commit -m "docs(changelog): extract full release history to CHANGELOG.md and trim README preview to 5 releases"
```

---

### Task 2: Update Version Bump Automation Script (`scripts/bump_version.py`)

**Files:**
- Modify: `scripts/bump_version.py`

**Interfaces:**
- Consumes: Commit message string argument.
- Produces: Automatic version bump updating `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md` (top 5 rows), followed by `git add`.

- [ ] **Step 1: Update scripts/bump_version.py logic**

Modify `scripts/bump_version.py` to:
1. Define `changelog_path = os.path.join(repo_root, "CHANGELOG.md")`.
2. Insert new release row into `CHANGELOG.md` after header table separator.
3. Update `README.md` preview table to insert the new release row and maintain exactly 5 release rows.
4. Add `changelog_path` to `paths_to_stage` for `git add`.

- [ ] **Step 2: Test bump_version.py with a dry-run / test message**

Run `python3 scripts/bump_version.py "test(changelog): test automated changelog update"` and verify:
- `CHANGELOG.md` has the new entry at the top of the table.
- `README.md` has the new entry and contains exactly 5 rows in the preview table.

- [ ] **Step 3: Revert test version bump commit or commit script changes**

```bash
git checkout -- mcp-router.csproj frontend/src/stores/useUserStore.ts README.md CHANGELOG.md
git add scripts/bump_version.py
git commit -m "feat(automation): update bump_version.py to manage CHANGELOG.md and top-5 README preview"
```

---

### Task 3: Update Repository Rules and Execute Final Verification

**Files:**
- Modify: `AGENTS.md`
- Modify: `.agents/GEMINI.md`

**Interfaces:**
- Consumes: Repository rules guidelines.
- Produces: Updated documentation rules requiring `CHANGELOG.md` updates.

- [ ] **Step 1: Update AGENTS.md and .agents/GEMINI.md**

Update the Mandatory Versioning Rule sections in `AGENTS.md` and `.agents/GEMINI.md` to explicitly state `CHANGELOG.md` as one of the files that MUST be updated simultaneously during version bumps.

- [ ] **Step 2: Run full test suite and commit script**

Run `./commit.sh "docs(rules): update agent rules for CHANGELOG.md extraction"` to verify the whole versioning and build workflow succeeds.

