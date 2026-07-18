# MCP Router Agents Guide

This file provides context for AI coding agents modifying the `CSharp-MCP-Router` repository.

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

- Do not use string manipulation (`string.Replace`) for JSON payloads. Use `JsonNode` (see `ClientSession.RewriteRequestJson`).
- Do not commit mockups to `docs/assets/`. Use actual UI screenshots.
- Ensure that you use atomic commits for logical changes.
