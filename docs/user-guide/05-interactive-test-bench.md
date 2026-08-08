# 05. Interactive Test Bench

The **Test Bench View** (`Test Bench` in header) is an interactive developer playground for testing backend MCP tools, inspecting virtual resources, rendering prompt templates, testing semantic vector search, and monitoring live SSE diagnostic logs.

---

## 🛠️ 1. Tool Tester Card

The Tool Tester allows invoking any namespaced tool across connected servers:

1. Select a **Server** from the dropdown menu (e.g. `docker`).
2. Select a **Tool** (e.g. `docker__list_containers`).
3. The router dynamically generates the input form fields based on the tool's JSON schema.
4. Fill in parameter values and click **Execute Tool**.
5. View execution status (`200 OK`), execution duration in milliseconds, and formatted JSON output payload in the Output Console.

---

## 📄 2. Resource Tester Card

Inspect virtualized MCP resources across backend servers:

1. Select a backend server or type a virtual resource URI (e.g. `mcp://docker/containers/status` or `mcp://homeassistant/states`).
2. Click **Read Resource**.
3. View the resource payload, MIME type (e.g. `application/json`, `text/plain`), and text contents.

---

## 💬 3. Prompt Tester Card

Render MCP prompt templates for LLM interaction:

1. Select a server exposing prompts (e.g. `notes-rag`).
2. Select a **Prompt** (e.g. `summarize_notes`).
3. Fill in prompt arguments.
4. Click **Get Prompt** to view rendered system and user messages.

---

## 🧠 4. Semantic Router Card (`search_tools`)

Test Meta-Mode vector similarity search:

1. Enter a natural language goal into the query input (e.g. `"restart web container"`, `"check account balance"`, `"turn off lights"`).
2. Click **Search Tools**.
3. The router runs vector embedding cosine similarity against all registered tool descriptions and displays the top matching namespaced tools with similarity scores (e.g. `0.92 - docker__restart_container`).

---

## 📟 5. Live Logs & Terminal Card

Monitor real-time gateway activity:
- Stream live SSE diagnostic events, HTTP requests, authentication challenges, and backend routing execution times directly in the embedded terminal console.
- Filter logs by severity (`INFO`, `WARN`, `ERROR`).
