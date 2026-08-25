# Design Specification: C# Formatting Standardization & Housekeeping

**Date:** 2026-08-24  
**Status:** Approved  
**Scope:** Repository-wide C# codebase (`mcp-router`, `McpRouter.Tests`, `scripts/CatalogGenerator`), CI workflows, and documentation.

---

## 1. Objectives

1. **Establish C# Formatting Parity with Frontend (ESLint):**
   - Provide an authoritative root `.editorconfig` enforcing standard C# 13 / .NET 10 code style, Allman bracing, sorting of imports, whitespace, and mandatory curly braces on all control flow statements (`if`, `else`, `for`, `foreach`, `while`, `do`).
2. **Automate & Lock In Formatting in CI:**
   - Add `dotnet format McpRouter.slnx --verify-no-changes` to the GitHub Actions CI backend quality gate in `.github/workflows/ci.yml`.
3. **Dead Code Elimination & Housekeeping:**
   - Configure Roslyn analyzer diagnostics for dead code (`IDE0051`, `IDE0052`, `IDE0059`, `CS0168`, `CS0219`, `IDE0005`) with `warning` severity.
   - Scan for and eliminate unused private members, redundant assignments, and dead helper code.
4. **Preserve Compatibility & Requirements Integrity:**
   - Retain all Model Context Protocol (MCP) dual-spec and protocol version compatibility logic.
   - Retain all database migration scripts and schema verifications in `Infrastructure/Persistence/`.
   - Maintain 100% test passing rate and regenerate the Requirements Catalog with zero drift.

---

## 2. `.editorconfig` Specification

The root `.editorconfig` will define:

### 2.1 General Formatting
```ini
root = true

[*]
indent_style = space
indent_size = 2
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
indent_size = 4
```

### 2.2 C# Bracing & Layout Rules
```ini
[*.cs]
# Control flow & Braces
csharp_prefer_braces = true:warning
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true

# Spacing
csharp_space_after_cast = false
csharp_space_around_binary_operators = before_and_after
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_declaration_statements = false
```

### 2.3 Import Directives & Organization
```ini
[*.cs]
csharp_using_directive_placement = outside_namespace:warning
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
```

### 2.4 Code Style & Dead Code Roslyn Diagnostics
```ini
[*.cs]
# Dead code diagnostics
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
```

---

## 3. CI Quality Gate Specification

Update `.github/workflows/ci.yml` in the `backend` job:

```yaml
  backend:
    name: Backend Build & Test (.NET 10)
    runs-on: ubuntu-latest
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Set up .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore McpRouter.slnx

      - name: Verify C# code formatting & style
        run: dotnet format McpRouter.slnx --verify-no-changes

      - name: Build backend (Release)
        run: dotnet build McpRouter.slnx --configuration Release --no-restore
```

---

## 4. Execution Plan & Quality Verification

1. **Apply `.editorconfig`**: Update the root file with the full specification.
2. **Execute Solution Formatting**: Run `dotnet format McpRouter.slnx` across the entire solution.
3. **Dead Code Remediation**:
   - Inspect build warnings (`IDE0051`, `IDE0052`, `IDE0059`, `CS0168`, `CS0219`, `IDE0005`).
   - Clean up unreferenced private members and dead variables.
4. **Validation Gates**:
   - Verify `dotnet format McpRouter.slnx --verify-no-changes` exits cleanly with code 0.
   - Run `dotnet test McpRouter.slnx` ensuring all unit and integration test proofs pass.
   - Run `dotnet run --project scripts/CatalogGenerator` and verify zero drift with `--verify-only`.
5. **Version Bump & Documentation**:
   - Bump version (patch release) across `mcp-router.csproj`, `frontend/src/stores/useUserStore.ts`, `CHANGELOG.md`, and `README.md`.
