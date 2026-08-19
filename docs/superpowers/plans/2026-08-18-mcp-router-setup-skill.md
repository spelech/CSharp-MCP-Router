# Universal MCP Router Setup Skill (`mcp-router-setup`) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a universal, self-contained AI agent setup skill (`mcp-router-setup`) in `skills/mcp-router-setup/` and `.agents/skills/mcp-router-setup/` that guides users and AI agents through deploying the MCP Router on Docker or Windows IIS, comparing Env vs. UI configuration modes, probing for existing auth/secret infrastructure, and generating production-ready scaffolds with 0 source cloning required.

**Architecture:** A standardized [AgentSkills.io](https://agentskills.io/specification) skill (`SKILL.md`) structured into a 6-phase decision engine (Probing $\rightarrow$ Platform Selection $\rightarrow$ Config Paradigm $\rightarrow$ Identity & Network Topology $\rightarrow$ Artifact Generation $\rightarrow$ Verification & Client Setup) with bundled configuration templates (`docker-compose.yml`, `web.config`, `.env.example`, `appsettings.Production.json.example`).

**Tech Stack:** Markdown / AgentSkills YAML frontmatter, Docker Compose, Windows IIS (`AspNetCoreModuleV2`), C# xUnit requirement proofs, living catalog generator.

## Global Constraints

- **Spec File:** `docs/superpowers/specs/2026-08-18-mcp-router-setup-skill-design.md`
- **Skill Locations:** Must be installed in both `skills/mcp-router-setup/` and `.agents/skills/mcp-router-setup/`.
- **Frontmatter Spec:** Max 1024 characters total frontmatter; `name: mcp-router-setup`; `description` begins with "Use when..." and describes triggering conditions without summarizing internal workflow.
- **Mandatory Versioning Rule:** Bump version across `mcp-router.csproj`, `frontend/src/shared/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`.
- **Test Requirements Annotations:** Every test in `McpRouter.Tests` must include `[Requirement("REQ-ID", "Category", RequirementType.Positive, "Description")]`.
- **Living Catalog Verification:** Must pass `dotnet run --project scripts/CatalogGenerator -- --verify-only` with zero drift.

---

### Task 1: Author Universal `mcp-router-setup` Skill (`SKILL.md`)

**Files:**
- Create: `skills/mcp-router-setup/SKILL.md`
- Create: `.agents/skills/mcp-router-setup/SKILL.md`

**Interfaces:**
- Consumes: Design specification in `docs/superpowers/specs/2026-08-18-mcp-router-setup-skill-design.md`.
- Produces: Complete, actionable `SKILL.md` implementing the 6-phase decision interview (Probing, Platform, Env vs UI, Identity, Artifacts, Verification).

- [ ] **Step 1: Write `skills/mcp-router-setup/SKILL.md`**
  Include full YAML frontmatter, phase-by-phase interview guide, environment probing commands for Docker, Vault, and Active Directory, trade-off comparisons for Env vs. UI configuration, standalone vs. enterprise identity modes, 256-bit `ROUTER_MASTER_KEY` generation recipes, and client connection snippets for Claude Desktop, Cursor, Cline, and Windsurf.

- [ ] **Step 2: Mirror `SKILL.md` into `.agents/skills/mcp-router-setup/SKILL.md`**
  Ensure project-local discovery by agents visiting the repository.

- [ ] **Step 3: Commit**
  ```bash
  git add skills/mcp-router-setup/SKILL.md .agents/skills/mcp-router-setup/SKILL.md
  git commit -m "feat(skills): create universal mcp-router-setup skill"
  ```

---

### Task 2: Create Bundled Scaffold Templates

**Files:**
- Create: `skills/mcp-router-setup/templates/docker-compose.yml`
- Create: `skills/mcp-router-setup/templates/web.config`
- Create: `skills/mcp-router-setup/templates/.env.example`
- Create: `skills/mcp-router-setup/templates/appsettings.Production.json.example`
- Mirror to: `.agents/skills/mcp-router-setup/templates/*`

**Interfaces:**
- Consumes: Production templates from root directory and IIS guides.
- Produces: Standalone, portable template files ready for agents to copy or adapt into any workspace without needing the full repository.

- [ ] **Step 1: Write `docker-compose.yml` template**
  Configured with `ghcr.io/spelech/mcp-router:latest`, `/var/run/docker.sock` volume mount, `./data:/app/data`, `ROUTER_MASTER_KEY`, and health checks.

- [ ] **Step 2: Write `web.config` template**
  Configured with `AspNetCoreModuleV2`, `hostingModel="inprocess"`, `responseBufferLimit="0"`, and 50MB request limit.

- [ ] **Step 3: Write `.env.example` & `appsettings.Production.json.example` templates**
  Complete with Standalone allowed network defaults (`127.0.0.1`, `::1`, `10.0.0.0/8`, `0.0.0.0/0`), AD SID (`S-1-5-32-544`), and OIDC header settings.

- [ ] **Step 4: Mirror templates to `.agents/skills/mcp-router-setup/templates/`**

- [ ] **Step 5: Commit**
  ```bash
  git add skills/mcp-router-setup/templates/ .agents/skills/mcp-router-setup/templates/
  git commit -m "feat(skills): add scaffold templates for docker and iis setup"
  ```

---

### Task 3: Unit Tests & Requirement Proofs for Setup Skill

**Files:**
- Create: `McpRouter.Tests/SetupSkillTests.cs`

**Interfaces:**
- Consumes: `skills/mcp-router-setup/SKILL.md` and templates.
- Produces: Automated xUnit test suite validating YAML frontmatter schema compliance, required sections, template syntax validity, and requirement traceability.

- [ ] **Step 1: Write `McpRouter.Tests/SetupSkillTests.cs`**
  Add unit tests annotated with `[Requirement]` attributes:
  - `Skill_Frontmatter_IsValidAndWithinCharacterLimit` (`DOC-SETUP-SKILL-FRONTMATTER`)
  - `Skill_ContainsAllRequiredPhasesAndComparisons` (`DOC-SETUP-SKILL-WORKFLOW`)
  - `Templates_AreValidAndContainRequiredDirectives` (`DOC-SETUP-SKILL-TEMPLATES`)
  - `Skill_MirroredInAgentsDirectory` (`DOC-SETUP-SKILL-MIRROR`)

- [ ] **Step 2: Run tests to verify they pass**
  Run: `dotnet test --filter "FullyQualifiedName~SetupSkillTests"`
  Expected: PASS (4/4 tests)

- [ ] **Step 3: Commit**
  ```bash
  git add McpRouter.Tests/SetupSkillTests.cs
  git commit -m "test(skills): add automated verification proofs for mcp-router-setup skill"
  ```

---

### Task 4: Release Bump, Documentation & Living Catalog Sync

**Files:**
- Modify: `mcp-router.csproj`
- Modify: `frontend/src/shared/stores/useUserStore.ts`
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `docs/features-guide.md`
- Update: `docs/requirements-catalog.json`
- Update: `docs/software-requirements-and-test-catalog.md`

**Interfaces:**
- Consumes: New test proofs from Task 3.
- Produces: Synchronized release metadata (`v4.19.1` or `v4.20.0`), updated user guides with one-liner skill invocation instructions, and verified requirements catalog.

- [ ] **Step 1: Bump version numbers to `4.19.1` (or `4.20.0`)**
  Update `mcp-router.csproj`, `frontend/src/shared/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`.

- [ ] **Step 2: Document skill usage in `README.md` and `docs/features-guide.md`**
  Add zero-clone installation instructions:
  ```bash
  curl -fsSL https://raw.githubusercontent.com/spelech/csharp-mcp-router/main/skills/mcp-router-setup/SKILL.md -o .agents/skills/mcp-router-setup/SKILL.md
  ```

- [ ] **Step 3: Regenerate and verify Living Requirements Catalog**
  ```bash
  dotnet run --project scripts/CatalogGenerator
  dotnet run --project scripts/CatalogGenerator -- --verify-only
  ```

- [ ] **Step 4: Run full solution test suite**
  ```bash
  dotnet test McpRouter.slnx
  ```

- [ ] **Step 5: Commit**
  ```bash
  git add -A
  git commit -m "chore(release): bump version to 4.19.1, document setup skill, and regenerate catalog"
  ```
