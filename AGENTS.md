# Model Context Gateway (MCG) Agents Guide

This file provides context and rules for AI coding agents modifying the `ModelContextGateway` repository.

## Architecture

This project is a high-performance C# ASP.NET Core gateway for the Model Context Protocol (MCP). It proxies requests from clients (IDEs, LLMs) to multiple backend MCP servers.

- **Meta-Mode:** By default (`/sse`), the gateway hides all backend tools and exposes only `search_tools` and `execute_tool`. This prevents context window bloat.
- **Proxying:** Target-specific proxying is supported via `/{targetServerId}`.
- **Serialization:** We use `System.Text.Json`. We have a custom `JsonRpcMessageConverter` to handle JSON-RPC schemas safely. Do not use this converter globally or during recursive serialization.
- **Dependency Injection:** Handled in `Program.cs`. `SessionManager` tracks active `ClientSession`s. `BackendConnection` handles individual upstream server lifecycles.

## Code Organization

- `/Core/`: Contains the core routing logic (`ClientSession`, `BackendConnection`, `SessionManager`, `ToolRoutingManager`, `CustomToolRegistry`).
- `/Models/`: Contains data transfer objects and protocol models.
- `/Controllers/`, `/Middleware/`, `/Extensions/`: Standard ASP.NET Core components.
- `/frontend/`: The frontend UI source code for the gateway dashboard.

## Tests

Integration and unit tests are located in `/ModelContextGateway.Tests`.
Run tests via `dotnet test ModelContextGateway.slnx`.

## Rules

- **MANDATORY VERSIONING RULE**: **EVERY CODE CHANGE COMMIT OR MERGE TO `main` MUST BUMP THE VERSION NUMBER.**
  - **Exception**: Documentation-only changes (e.g. updating markdown files under `docs/`, `README.md`, or architecture guides without code/schema changes) **DO NOT** require a version bump, release tagging, or container rebuild.
  - Patch Bumps (e.g. `5.0.0` -> `5.0.1`): For bug fixes, performance optimizations, log refactoring, or minor UI tweaks.
  - Minor Bumps (e.g. `5.0.0` -> `5.1.0`): For new features, API endpoints, schema changes, or architectural additions.
  - **Files That MUST Be Updated Simultaneously (When Bumping Version)**:
    1. `ModelContextGateway.csproj` (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`)
    2. `frontend/src/stores/useUserStore.ts` (React fallback version)
    3. `CHANGELOG.md` (Add release entry to the Release Changelog table)
    4. `README.md` (Update top-5 release preview table)
- **MANDATORY DOCUMENTATION & ATOMIC COMMIT RULE**: **EVERY FEATURE OR CHANGE MUST INCLUDE UP-TO-DATE DOCUMENTATION.**
  - When working on a feature branch, AI agents **MUST** ensure relevant guides (`README.md`, `ARCHITECTURE.md`, `docs/features-guide.md`, etc.) are updated to reflect the design or functionality changes.
  - Agents **MUST** use logical, atomic commits for changes. Commit code and documentation separately or in cleanly grouped atomic commits.
- **MANDATORY TEST REQUIREMENT ANNOTATIONS RULE**: **ALL NEW AND MODIFIED TESTS MUST BE ANNOTATED WITH REQUIREMENTS.**
  - Every test proof in C# (`ModelContextGateway.Tests`), Vitest (`frontend/src/test`), and Playwright (`frontend/e2e`) **MUST** include requirement metadata:
    - **C#**: `[Requirement("AUTH-01", "AUTH", RequirementType.Positive, "Description")]` (or with named properties `[Requirement("AUTH-01", "Description", Type = RequirementType.Positive, Category = "AUTH")]`)
    - **TypeScript / JSDoc**:
      ```typescript
      /**
       * @requirement AUTH-01
       * @category AUTH
       * @type PositiveFeature | FailClosedGuardrail
       * @description Detailed specification statement.
       */
      ```
    - **IMPORTANT**: Requirement IDs must **NEVER** use `REQ-` prefixes. Always use the standard category taxonomy (e.g. `AUTH-01`, `DB-01`, `GUARD-01`, `MCP-01`, `SEC-01`, `TRANS-01`, `UI-01`).
  - When tests are added, updated, or deleted, agents **MUST** regenerate and verify the requirements catalog:
    ```bash
    dotnet run --project scripts/CatalogGenerator
    dotnet run --project scripts/CatalogGenerator -- --verify-only
    ```
  - For full taxonomy, category codes (`AUTH`, `DB`, `GUARD`, `MCP`, `SEC`, `TRANS`, `UI`), and conventions, refer to [`docs/test-catalog-guide.md`](docs/test-catalog-guide.md).
- **MANDATORY RELEASE TAGGING & DOCKER PUBLISHING RULE**: **EVERY MERGE TO `main` CUTTING A NEW RELEASE MUST CREATE AND PUSH A GIT TAG.**
  - Once PR quality gates pass and the PR is merged into `main`, agents **MUST** immediately create and push the release tag:
    ```bash
    git tag v<version>
    git push origin v<version>
    ```
  - This triggers the automated `Build and Push Docker Image` GitHub Actions workflow to build and publish the container images (`ghcr.io/spelech/model-context-gateway:latest`, `:<version>`, `:latest-full`, `:<version>-full`).
- **MANDATORY POST-RELEASE MCP STACK REFRESH RULE**: **PULL LATEST IMAGE IN THE MCP STACK AFTER RELEASE.**
  - After confirming the release workflow has published the new image, agents **MUST** update the running gateway in the MCP stack:
    ```bash
    docker compose -f /containers/mcp/docker-compose.yaml pull mcg
    docker compose -f /containers/mcp/docker-compose.yaml up -d mcg
    ```
  - Verify live health: `curl -s http://localhost:8026/health` should return `{"status":"healthy","service":"ModelContextGateway","version":"<version>"}`.
- Do not use string manipulation (`string.Replace`) for JSON payloads. Use `JsonNode` (see `ClientSession.RewriteRequestJson`).
- Do not commit mockups to `docs/assets/`. Use actual UI screenshots.
- Ensure that you use atomic commits for logical changes.
