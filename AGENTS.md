# MCP Router Agents Guide

This file provides context and rules for AI coding agents modifying the `CSharp-MCP-Router` repository.

## Architecture

This project is a high-performance C# ASP.NET Core gateway router for the Model Context Protocol (MCP). It proxies requests from clients (IDEs, LLMs) to multiple backend MCP servers.

- **Meta-Mode:** By default (`/sse`), the router hides all backend tools and exposes only `search_tools` and `execute_tool`. This prevents context window bloat.
- **Proxying:** Target-specific proxying is supported via `/{targetServerId}`.
- **Serialization:** We use `System.Text.Json`. We have a custom `JsonRpcMessageConverter` to handle JSON-RPC schemas safely. Do not use this converter globally or during recursive serialization.
- **Dependency Injection:** Handled in `Program.cs`. `SessionManager` tracks active `ClientSession`s. `BackendConnection` handles individual upstream server lifecycles.

## Code Organization

- `/Core/`: Contains the core routing logic (`ClientSession`, `BackendConnection`, `SessionManager`, `CustomTools`).
- `/Models/`: Contains data transfer objects and protocol models.
- `/Controllers/`, `/Middleware/`, `/Extensions/`: Standard ASP.NET Core components.
- `/wwwroot/`: The frontend UI for the router dashboard.

## Tests

Integration and unit tests are located in `/McpRouter.Tests`.
Run tests via `dotnet test McpRouter.slnx`.

## Rules

- **MANDATORY VERSIONING RULE**: **EVERY COMMIT OR MERGE TO `main` MUST BUMP THE VERSION NUMBER.**
  - Patch Bumps (e.g. `2.7.0` -> `2.7.1`): For bug fixes, performance optimizations, log refactoring, or minor UI tweaks.
  - Minor Bumps (e.g. `2.7.0` -> `2.8.0`): For new features, API endpoints, schema changes, or architectural additions.
  - **Files That MUST Be Updated Simultaneously**:
    1. `mcp-router.csproj` (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`)
    2. `wwwroot/index.html` (`<span class="badge badge-primary" id="version-badge">vX.Y.Z</span>`)
    3. `README.md` (Add release entry to the Release Changelog table)
- **MANDATORY DOCUMENTATION & ATOMIC COMMIT RULE**: **EVERY FEATURE OR CHANGE MUST INCLUDE UP-TO-DATE DOCUMENTATION.**
  - When working on a feature branch, AI agents **MUST** ensure relevant guides (`README.md`, `ARCHITECTURE.md`, `docs/features-guide.md`, etc.) are updated to reflect the design or functionality changes.
  - Agents **MUST** use logical, atomic commits for changes. Commit code and documentation separately or in cleanly grouped atomic commits.
- Do not use string manipulation (`string.Replace`) for JSON payloads. Use `JsonNode` (see `ClientSession.RewriteRequestJson`).
- Do not commit mockups to `docs/assets/`. Use actual UI screenshots.
- Ensure that you use atomic commits for logical changes.
