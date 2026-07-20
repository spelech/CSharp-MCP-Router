# MCP Router AI Coding Agent Guidelines (GEMINI.md)

This document dictates rules, practices, and guidelines that all AI coding assistants (such as Antigravity / AGY / Gemini) MUST follow when working on the `CSharp-MCP-Router` repository.

---

## 📡 1. Model Context Protocol (MCP) Standards

The gateway acts as a high-fidelity proxy/aggregator. Always adhere strictly to the Model Context Protocol specifications:
* **Tool Calling (`tools/list`, `tools/call`)**: 
  * Backend tools must be namespaced using `<serverId>__<toolName>` (e.g. `docker__list_containers`).
  * Incoming client calls must be correctly resolved, un-namespaced, and routed to the respective backend.
  * Payloads must be verified against their schemas.
* **Resources (`resources/list`, `resources/read`)**:
  * URIs must be virtualized to prevent collision (e.g., mapping backends to `mcp://{serverId}/{uri}`).
* **Prompts (`prompts/list`, `prompts/get`)**:
  * Namespacing and resolution must match the tool-calling convention.
* **Notifications**:
  * Handlers must forward messages asynchronously to all connected client sessions.
  * Empty response bodies (`202 Accepted`) returned by stateless backends for client-to-server notifications must be handled gracefully without JSON parsing crashes.

---

## 🎨 2. UI & Frontend Guidelines

The dashboard is built using a dark-mode glassmorphic design. When modifying the frontend UI:
* **Design Consistency**:
  * Colors must use centralized CSS variables in [`variables.css`](file:///containers/mcp/router/wwwroot/css/variables.css). Do not hardcode HEX or RGB values.
  * Main accent theme uses a vibrant Orange (`#f97316` / primary) and Yellow (`#eab308` / secondary) combination.
* **Layout Shifts**:
  * **Vertical Jumping**: The `body` must align items using `align-items: flex-start` in [`layout.css`](file:///containers/mcp/router/wwwroot/css/layout.css) to ensure the container always renders aligned from the top, avoiding layout jumps when switching tabs of varying heights.
  * **Horizontal Shifting**: Prevent horizontal layout jumps due to scrollbar visibility by keeping `scrollbar-gutter: stable` on the `html` element.

---

## 📸 3. Real Screenshots Standard

**AI-generated images or placeholder assets are strictly prohibited** in the documentation. All screenshots under `docs/assets/` must be captured from the actual, live-running application using the automated screenshot tool.

### How to capture actual screenshots:
A Puppeteer script, [`take_screenshots.js`](file:///containers/mcp/router/take_screenshots.js), is included in the repository. Run it via Docker to easily capture actual screenshots:

```bash
docker run --rm --init --cap-add=SYS_ADMIN -u root \
  -e PUPPETEER_CACHE_DIR=/home/pptruser/.cache/puppeteer \
  -e DASHBOARD_URL=http://mcp-router:8080/ \
  -e SSO_USER=steve \
  -e SSO_GROUPS=full_admin,house_member \
  -e SSO_NAME="Steve Pelech" \
  --network net_cloud \
  -v $(pwd)/take_screenshots.js:/home/pptruser/app/take_screenshots.js \
  -v $(pwd)/docs/assets:/home/pptruser/app/screenshots \
  ghcr.io/puppeteer/puppeteer:latest \
  node /home/pptruser/app/take_screenshots.js
```

Ensure `mcp-router` is rebuilt and running (`docker compose up -d mcp-router`) on the `net_cloud` network before running the screenshot tool.

---

## 💻 4. Coding & Serialization Standards

* **JSON Payloads**:
  * Never use string manipulation (`Replace`, `Substring`) for rewrites or modifications of JSON-RPC payloads. Always use C# `JsonNode`, `JsonObject`, or `JsonDocument` parsing.
* **JsonRpcMessageConverter**:
  * This custom converter handles JSON-RPC polymorphic message serialization. Do NOT register it globally or invoke it recursively in nested objects to prevent stack overflow errors.
* **Dependency Injection**:
  * Follow clean separation of concerns. Maintain strategy interfaces (`ITransport`) for backend connections, shielding core routing classes from transport layers.
  * Handlers, connection lifecycles, and caches must be kept thread-safe using single-execution locks and thread-safe collections (`ConcurrentDictionary`).

---

## 🏷️ 5. Versioning

* When releasing core fixes, always perform a matching version bump across the codebase:
  * In the C# project file: [`mcp-router.csproj`](file:///containers/mcp/router/mcp-router.csproj) (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`).
  * In the UI dashboard header: [`index.html`](file:///containers/mcp/router/wwwroot/index.html) (`<span class="badge">vX.Y.Z</span>`).
