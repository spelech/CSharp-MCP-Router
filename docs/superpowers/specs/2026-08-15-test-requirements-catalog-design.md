# Automated Software Requirements & Test Verification Catalog Design

**Date:** 2026-08-15  
**Version Target:** v4.15.0  
**Status:** Approved  

---

## 1. Overview & Problem Statement

As the test suite across backend (.NET xUnit), frontend (Vitest / React 19), and End-to-End (Playwright) suites grows, the repository achieves high test coverage and confidence. However, the exact functional specifications—and specifically the distinction between **what the application does** (positive capabilities) and **what the application does NOT do** (guardrails, security boundaries, and fail-closed behaviors)—are buried in individual test bodies.

This feature introduces an automated **Software Requirements Specification (SRS) & Test Verification Catalog** extraction and documentation system. Test cases across C# xUnit, Vitest, and Playwright are annotated with structured requirement metadata, and a high-performance Roslyn + TypeScript CLI tool parses, aggregates, and emits both human-readable Markdown and machine-readable JSON catalogs.

---

## 2. Requirements Taxonomy & ID Schema

### 2.1 ID Structure
Requirement identifiers use the structured format `{CATEGORY}-{NUMBER}`:

| Category Prefix | Subsystem Domain | Description | Example IDs |
| :--- | :--- | :--- | :--- |
| **`AUTH`** | Authentication & RBAC | SIDs, AppKeys, OIDC, SSO headers, group mappings | `AUTH-01`, `AUTH-02` |
| **`MCP`** | Core MCP Protocol Engine | Tool namespacing, schema validation, meta-mode, JSON-RPC | `MCP-01`, `MCP-02` |
| **`TRANS`** | Transport Channels | SSE streams, HTTP direct/stateless, STDIO subprocesses, Proxy | `TRANS-01`, `TRANS-02` |
| **`SEC`** | Secrets & Credential Management | HashiCorp Vault (Token & AppRole), Env vars, Windows DPAPI | `SEC-01`, `SEC-02` |
| **`DB`** | Persistence & Data Migrations | SQLite, MSSQL, MySQL dialect support and schema auto-updates | `DB-01`, `DB-02` |
| **`UI`** | Frontend Dashboard & Test Bench | React components, forms, logs terminal, server inspector | `UI-01`, `UI-02` |
| **`GUARD`** | Universal Safety & Guardrails | Fail-closed rules, rate limits, anti-tamper, negative bounds | `GUARD-01`, `GUARD-02` |

### 2.2 Requirement Types
1. **`Positive` ("What the Application DOES")**:
   - Explicit functional capabilities, workflow behaviors, and expected success paths.
2. **`Negative` / `Guardrail` ("What the Application DOES NOT DO")**:
   - Explicit boundaries, fail-closed handling on corrupt or missing data, permission denials, and security invariants.

---

## 3. Test Annotation Standards

### 3.1 Backend C# xUnit Tests (`McpRouter.Tests`)
Backend tests use a custom attribute `[Requirement]` that implements xUnit's `ITraitAttribute`, coupled with XML summary docstrings:

```csharp
namespace McpRouter.Tests
{
    public enum RequirementType
    {
        Positive,
        Negative
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirementAttribute : Attribute, Xunit.Sdk.ITraitAttribute
    {
        public string Id { get; }
        public string Description { get; }
        public RequirementType Type { get; set; } = RequirementType.Positive;
        public string? Category { get; set; }

        public RequirementAttribute(string id, string description)
        {
            Id = id;
            Description = description;
        }
    }
}
```

#### Usage in C# Test Files:
```csharp
/// <summary>
/// Verifies that users with Admin SID (S-1-5-32-544) bypass explicit deny policies.
/// </summary>
[Fact]
[Requirement("AUTH-01", "Admin SID bypasses all explicit deny policies", Type = RequirementType.Positive, Category = "AUTH")]
public async Task AdminSid_BypassesExplicitDenyPolicy()
{
    // Test logic...
}

/// <summary>
/// Ensures expired or malformed AppKey scopes fail closed and never permit downstream execution.
/// </summary>
[Fact]
[Requirement("GUARD-01", "Expired AppKeys must fail closed with 401 Unauthorized", Type = RequirementType.Negative, Category = "GUARD")]
public async Task ExpiredAppKey_FailsClosed_RejectsExecution()
{
    // Test logic...
}
```

Because `RequirementAttribute` implements `ITraitAttribute`, standard `dotnet test` trait filtering works seamlessly:
```bash
dotnet test --filter "Category=AUTH"
dotnet test --filter "Id=AUTH-01"
```

---

### 3.2 Frontend Vitest & Playwright Tests (`frontend/src/test`, `frontend/e2e`)
Frontend tests use standardized JSDoc block comments directly above test blocks or describe suites:

```typescript
/**
 * @id UI-04
 * @category UI
 * @type positive
 * @description Dynamic form generation correctly validates and casts boolean, integer, and JSON schema types
 */
it('renders form inputs according to schema and casts types on change', () => {
  // ...
});

/**
 * @id GUARD-03
 * @category GUARD
 * @type negative
 * @description Unauthenticated or denied users are never shown server configuration secrets or admin action buttons
 */
test('should hide admin settings and edit buttons for guest context', async ({ page }) => {
  // ...
});
```

---

## 4. CLI Generator Tool Architecture (`scripts/CatalogGenerator`)

The generator is implemented as a lightweight .NET 10 console project located at `/scripts/CatalogGenerator/`.

```
scripts/
└── CatalogGenerator/
    ├── CatalogGenerator.csproj       # .NET 10 project with Microsoft.CodeAnalysis.CSharp
    ├── Program.cs                    # CLI entrypoint, argument parsing (--verify, --output, --json)
    ├── Models/
    │   ├── RequirementItem.cs        # Primary Requirement entity
    │   ├── TestCaseProof.cs          # Test verification reference (file, line, method name, suite)
    │   ├── RequirementType.cs        # Positive vs Negative enum
    │   └── CatalogIndex.cs           # Aggregated index model and statistics
    ├── Parsers/
    │   ├── RoslynCSharpParser.cs     # Exact Roslyn CSharpSyntaxTree parser for xUnit tests
    │   └── TypeScriptTestParser.cs   # AST / JSDoc parser for Vitest and Playwright specs
    └── Emitters/
        ├── MarkdownEmitter.cs        # Generates docs/software-requirements-and-test-catalog.md
        └── JsonEmitter.cs            # Generates docs/requirements-catalog.json
```

### 4.1 Processing Pipeline
1. **Source Discovery**:
   - Scans `McpRouter.Tests/**/*.cs` for `[Requirement]` attributes and XML comments.
   - Scans `frontend/src/test/**/*.{ts,tsx}` and `frontend/e2e/**/*.spec.ts` for JSDoc blocks.
2. **AST Extraction**:
   - `RoslynCSharpParser` analyzes syntax trees, extracting attribute arguments, method declarations, file paths, line numbers, and doc comments.
   - `TypeScriptTestParser` extracts JSDoc tags (`@id`, `@category`, `@type`, `@description`) and test descriptions.
3. **Aggregation & Normalization**:
   - Groups multi-suite proofs under the matching Requirement ID (e.g. unit + E2E both proving `AUTH-01`).
   - Validates for missing descriptions, unknown categories, or orphaned IDs.
4. **Emission**:
   - Emits `docs/software-requirements-and-test-catalog.md`.
   - Emits `docs/requirements-catalog.json`.
5. **Verification Mode (`--verify-only`)**:
   - Returns exit code `0` if all files and documentation are up to date and valid.
   - Returns exit code `1` with clear error messages if unannotated tests exist or docs are out of sync (used in CI quality gates).

---

## 5. Generated Documentation Layout

The generated `docs/software-requirements-and-test-catalog.md` includes:
1. **Executive Summary & Category Breakdown Table**: Total requirements, positive features, guardrails, and proof count per subsystem.
2. **Functional Requirements ("What the Application DOES")**: Grouped by category with requirement descriptions, acceptance criteria, and clickable links to source code test files and line numbers.
3. **Boundary & Guardrail Rules ("What the Application DOES NOT DO")**: Highlighted with GitHub alert boxes (`> [!IMPORTANT] Guardrail Invariant: ...`), detailing fail-closed behaviors and security boundaries.
4. **Traceability Matrix**: Complete table mapping ID $\leftrightarrow$ Subsystem $\leftrightarrow$ Type $\leftrightarrow$ Suite $\leftrightarrow$ Test Method $\leftrightarrow$ File Link.

---

## 6. Developer Guidelines & Agent Rules Updates

### 6.1 `AGENTS.md` and `.agents/GEMINI.md` Rule Addition
A mandatory rule will be added to agent guidelines:
- **Mandatory Test Requirement Annotations Rule**:
  - Any new or modified test in `McpRouter.Tests`, `frontend/src/test`, or `frontend/e2e` MUST include requirement annotations (`[Requirement(...)]` in C# or `@id` JSDoc in TypeScript).
  - Agents must run `dotnet run --project scripts/CatalogGenerator` to regenerate the requirements catalog after updating tests.

### 6.2 Documentation & Guide
- Create `docs/test-catalog-guide.md` providing step-by-step instructions for developers and agents.
- Update `README.md` and `docs/features-guide.md` to reference the catalog.
- Add `npm run docs:catalog` script to `frontend/package.json` / root workflow.

---

## 7. Versioning & Release
- **Bump Version to `v4.15.0`**:
  - `mcp-router.csproj` (`<Version>4.15.0</Version>`, `<AssemblyVersion>`, `<FileVersion>`)
  - `frontend/package.json` (`"version": "4.15.0"`)
  - `frontend/src/stores/useUserStore.ts` (`version: '4.15.0'`)
  - `CHANGELOG.md` (Add v4.15.0 release entry)
  - `README.md` (Update top-5 release preview table)
