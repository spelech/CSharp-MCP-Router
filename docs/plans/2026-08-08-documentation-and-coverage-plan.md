# Documentation, Code Coverage, and Architecture Class Diagrams Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add detailed Code Coverage reporting to `README.md` and `docs/coverage-report.md`, configure C# XML documentation generation with IntelliSense docstrings, and add Mermaid class diagrams to `ARCHITECTURE.md`.

**Architecture:**
- **Task 1**: Create `docs/coverage-report.md` and add Code Coverage section to `README.md`.
- **Task 2**: Update `mcp-router.csproj` to enable XML doc generation and add XML docstrings across domain interfaces and services.
- **Task 3**: Add 3 comprehensive Mermaid `classDiagram` blocks to `ARCHITECTURE.md`.

**Tech Stack:** Markdown, Mermaid.js, C# (.NET 10), xUnit.

## Global Constraints

- Retain 100% test pass rate (277/277 tests passing).
- Ensure all relative file links in markdown are valid.
- Maintain atomic commit workflow via `./commit.sh`.

---

### Task 1: Create Code Coverage Report & Update README

**Files:**
- Create: `docs/coverage-report.md`
- Modify: `README.md`

- [ ] **Step 1: Create `docs/coverage-report.md`**

Write detailed coverage breakdown tables for Core Session, Routing Engine, Controllers, Security & Providers.

- [ ] **Step 2: Add Code Coverage Section to `README.md`**

Add a **🧪 Code Coverage & Quality Gate** section in `README.md` with summary table and link to `docs/coverage-report.md`.

- [ ] **Step 3: Stage and commit**

```bash
git add docs/coverage-report.md README.md
./commit.sh "docs(coverage): add comprehensive code coverage report and README section"
```

---

### Task 2: Enable C# XML Documentation & Add XML Comments

**Files:**
- Modify: `mcp-router.csproj`
- Modify: `Core/ClientSession.cs` & partials
- Modify: `Core/Routing/ToolRoutingManager.cs`, `ResourceRoutingManager.cs`, `ResourceCatalogManager.cs`
- Modify: `Services/SemanticSearchService.cs`, `BackendHealthCheckService.cs`
- Modify: `Core/Identity/IIdentityProvider.cs`, `Core/Secrets/ISecretRetriever.cs`

- [ ] **Step 1: Update `mcp-router.csproj`**

Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and `<NoWarn>$(NoWarn);1591</NoWarn>` to `<PropertyGroup>`.

- [ ] **Step 2: Add XML docstrings to core interfaces and services**

Add `/// <summary>`, `/// <param>`, and `/// <returns>` annotations.

- [ ] **Step 3: Verify build and tests**

Run: `dotnet test McpRouter.slnx`
Expected: PASS (0 build errors, 277 tests pass)

- [ ] **Step 4: Stage and commit**

```bash
git add mcp-router.csproj Core/ Services/
./commit.sh "docs(xml): enable C# XML documentation generation and annotate core services"
```

---

### Task 3: Add Architecture Class Diagrams to `ARCHITECTURE.md`

**Files:**
- Modify: `ARCHITECTURE.md`

- [ ] **Step 1: Add Mermaid Class Diagrams to `ARCHITECTURE.md`**

Add 3 Mermaid `classDiagram` blocks:
1. Client Session & Modular Partial Class Architecture
2. Pluggable Strategy Interfaces & Security Providers
3. Routing Engine Architecture & Helper Separation

- [ ] **Step 2: Stage and commit**

```bash
git add ARCHITECTURE.md
./commit.sh "docs(architecture): add Mermaid class diagrams for session, interfaces, and routing engine"
```
