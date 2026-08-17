# 05. Interactive Test Bench

The **Interactive Test Bench** (`Test Bench` tab) is a developer and operator playground for interactively testing backend MCP tools, reading virtual resources, evaluating prompt templates, simulating semantic vector search, and inspecting real-time diagnostic logs.

---

## 🎛️ Test Bench Overview & Layout

The Test Bench aggregates five specialized diagnostic tools in an interactive multi-panel layout:

```
+---------------------------------------------------------------------------------------------+
| 🧪 Interactive Test Bench                                                                   |
+---------------------------------------------------------------------------------------------+
|  [ 🛠️ Tool Tester ]   [ 📄 Resource Tester ]   [ 💬 Prompt Tester ]   [ 🧠 Semantic Router ] |
+---------------------------------------------------------------------------------------------+
|                                                                                             |
|  [ Active Tester Panel: Dynamic Forms, Arguments, & Execution Controls ]                    |
|                                                                                             |
+---------------------------------------------------------------------------------------------+
| 📟 Live Diagnostic Logs & Gateway Terminal                                                   |
+---------------------------------------------------------------------------------------------+
```

---

## 🛠️ 1. Tool Tester (`ToolTesterCard`)

The Tool Tester allows manual invocation of any discovered or custom namespaced tool across connected servers without needing an external AI client.

```
+-------------------------------------------------------------------------------+
| 🛠️ Tool Execution Tester                                                      |
+-------------------------------------------------------------------------------+
| Target Server: [ docker (Docker Infrastructure Daemon) ▾ ]                    |
| Tool Name:     [ docker__restart_container ▾             ]                    |
|                                                                               |
| Parameters (Generated from JSON Schema):                                      |
|   Container ID / Name (*): [ homewebservice                                 ] |
|   Timeout Seconds:         [ 30                                             ] |
|                                                                               |
| [ ▶ Execute Tool ]                                                            |
+-------------------------------------------------------------------------------+
| Result (200 OK - 42ms):                                                       |
| {                                                                             |
|   "content": [                                                                |
|     { "type": "text", "text": "Container homewebservice restarted successfully" }|
|   ]                                                                           |
| }                                                                             |
+-------------------------------------------------------------------------------+
```

### Key Capabilities
1. **Dynamic JSON Schema Form Builder**: Inspects the tool's JSON Schema parameter definition and automatically generates typed form inputs (strings, numbers, booleans, arrays, nested JSON objects).
2. **Schema Validation**: Enforces required fields and type constraints before sending requests.
3. **Execution Metrics**: Displays response status code (`200 OK`, `403 Forbidden`, `500 Internal Error`) and execution latency in milliseconds.
4. **Syntax-Highlighted Output**: Pretty-printed JSON payload formatting with one-click copy.

---

## 📄 2. Virtual Resource Tester (`ResourceTesterCard`)

Inspect and read virtual MCP resources exposed by backend servers:

```
+-------------------------------------------------------------------------------+
| 📄 Virtual Resource Tester                                                    |
+-------------------------------------------------------------------------------+
| Resource URI: [ mcp://docker/containers/status                             ]  |
|                                                                               |
| [ 📖 Read Resource ]                                                          |
+-------------------------------------------------------------------------------+
| MIME Type: application/json | Size: 1.4 KB                                    |
| {                                                                             |
|   "containers": [                                                             |
|     { "name": "caddy", "status": "running", "uptime": "14d 2h" },             |
|     { "name": "vault", "status": "running", "uptime": "30d 6h" }              |
|   ]                                                                           |
| }                                                                             |
+-------------------------------------------------------------------------------+
```

### Key Capabilities
* **Virtual URI Resolution**: Resolves URIs following the `mcp://{serverId}/{resourcePath}` convention.
* **MIME Type Inspection**: Displays content type (e.g. `application/json`, `text/markdown`, `text/plain`).
* **Content Formatter**: Auto-formats structured text and renders clean previews.

---

## 💬 3. Prompt Template Tester (`PromptTesterCard`)

Evaluate parameterized prompt templates exposed by backend servers or custom file specifications:

```
+-------------------------------------------------------------------------------+
| 💬 Prompt Template Tester                                                     |
+-------------------------------------------------------------------------------+
| Target Server:   [ notes-rag (SilverBullet / Notes MCP) ▾ ]                   |
| Prompt Template: [ summarize_architecture ▾               ]                   |
|                                                                               |
| Arguments:                                                                    |
|   Topic (*):     [ CSharp-MCP-Router Security Hardening                     ] |
|   Max Length:    [ 500                                                      ] |
|                                                                               |
| [ 📑 Render Prompt ]                                                          |
+-------------------------------------------------------------------------------+
| Rendered Messages:                                                            |
| [System]: "You are an expert systems architect reviewing homelab security..." |
| [User]:   "Summarize the architecture for CSharp-MCP-Router Security..."      |
+-------------------------------------------------------------------------------+
```

---

## 🧠 4. Semantic Router Simulator (`SemanticRouterCard`)

Simulate how the router's vector embedding engine scores and ranks tools when an AI agent calls `search_tools`:

```
+-------------------------------------------------------------------------------+
| 🧠 Semantic Search Simulator (Meta-Mode Test)                                 |
+-------------------------------------------------------------------------------+
| Natural Language Query: [ restart web proxy container                       ] |
| Search Limit:           [ 5 ▾ ]                                               |
|                                                                               |
| [ 🔍 Simulate Semantic Search ]                                               |
+-------------------------------------------------------------------------------+
| Search Results (Embedding Latency: 12ms):                                     |
|                                                                               |
| 1. docker__restart_container  [Score: 0.942] 🟢 High Match                    |
|    "Restart a running Docker container by name or container ID."              |
|                                                                               |
| 2. docker__stop_container     [Score: 0.815] 🟡 Moderate                      |
|    "Stop a running Docker container."                                         |
|                                                                               |
| 3. caddy__reload_config       [Score: 0.748] 🟡 Moderate                      |
|    "Triggers an in-process reload of the Caddy web reverse proxy config."     |
+-------------------------------------------------------------------------------+
```

### Key Capabilities
* **Intent Ranking Evaluation**: Verifies that natural language user prompts map to the expected tool names.
* **Vector Cosine Scoring**: Displays normalized similarity scores (`0.00` to `1.00`).
* **Engine Verification**: Validates whether the Local ONNX model or OpenAI API provider is functioning correctly.

---

## 📟 5. Live Diagnostic Logs Terminal (`LogsTerminalCard`)

Positioned at the bottom of the Test Bench view, the Live Logs Terminal provides real-time visibility into internal router operations:

```
+-------------------------------------------------------------------------------+
| 📟 Live Diagnostic Logs & Gateway Activity             [ Clear ] [ Auto-Scroll ]|
| Filter: [ ALL ▾ ] [ INFO ▾ ] [ WARN ▾ ] [ ERROR ▾ ]                           |
+-------------------------------------------------------------------------------+
| [13:45:02.112] [INF] [McpSession:c8b4] Client authenticated as 'admin' via OIDC
| [13:45:02.115] [INF] [McpSession:c8b4] Initialized Meta-Mode stream (2 tools)
| [13:45:04.220] [INF] [ToolExecution] docker__restart_container invoked by admin
| [13:45:04.262] [INF] [ToolExecution] docker__restart_container completed in 42ms
| [13:45:10.512] [WRN] [HealthCheck] Backend 'plex' responded slowly (1250ms)
+-------------------------------------------------------------------------------+
```

### Key Capabilities
* **Thread-Safe In-Memory Stream**: Streams live gateway logs without disk I/O bottlenecks.
* **Severity Filtering**: Filter logs by `INFO`, `WARN`, or `ERROR` levels.
* **Auto-Scroll & Freeze**: Pin terminal scroll position while diagnosing active issues.
