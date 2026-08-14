# 04. Client Setup & App Key Management

The MCP Gateway Router allows IDEs, AI coding assistants, and autonomous agents to connect seamlessly via standard Model Context Protocol (MCP) clients.

---

## 🔑 Managing App Keys

To connect external clients securely without passing raw SSO headers:

1. Click **`Clients`** in the dashboard header.
2. Click **`+ Generate App Key`**.
3. Fill in:
   - **Key Name**: Descriptive name (e.g. `Cursor IDE Laptop` or `OpenClaw Agent`).
   - **Assigned User**: Principal UPN (e.g. `steve`).
   - **Scopes**: Select global (`*`), server-level (`server:docker`), category-level (`category:smarthome`), or granular capabilities (`tool:docker__ps`). See the [**AppKey Scopes & Authorization Guide**](../appkey-scopes.md) for full scope syntax rules.
   - **Expiration**: Select 30 days, 90 days, 1 year, or Never.
4. Click **Create Key**. Copy the generated secret key (it will only be displayed once).

> [!TIP]
> For comprehensive details on scope syntax grammar, dynamic category evaluation, token hashing, and multi-stage RBAC evaluation rules, consult the [**AppKey Scopes & Authorization Guide**](../appkey-scopes.md).

---

## 🛠️ Dynamic Integration Snippets & Setup Generator

The **MCP Client Setup Guide** card on the dashboard features an interactive multi-target configuration generator:

- **Target Route / Server Selector**: Choose between **Unified Meta-Mode Gateway** (`/sse?meta=true`), an **Individual Backend Server** (`/docker`, `/ha`, `/actual`, `/plex`, `/excel`, etc.), or a **Server Category** (`/media`, `/smarthome`, `/infrastructure`).
- **Client Application**: Select **Claude Desktop**, **Cursor IDE**, **VS Code / Cline / Roo Code**, or **TypeScript SDK**.
- **Host Origin Override**: Customize the host domain or IP (defaulting to current browser origin).
- **App Key Toggle**: Check **Include `X-App-Key`** to automatically inject secret authorization headers into generated configurations.

---

### 1. Cursor IDE Integration (`.cursor/mcp.json`)
Add the following to your project's `.cursor/mcp.json` or global Cursor configuration:
```json
{
  "mcpServers": {
    "mcp-router": {
      "url": "http://10.0.0.10:8026/sse",
      "headers": {
        "X-App-Key": "mcp_app_key_your_generated_secret_key_here"
      }
    }
  }
}
```

### 2. Claude Desktop Integration (`claude_desktop_config.json`)
Add to `~/.config/Claude/claude_desktop_config.json` (Linux) or `%APPDATA%\Claude\claude_desktop_config.json` (Windows):
```json
{
  "mcpServers": {
    "mcp-router": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/inspector",
        "http://10.0.0.10:8026/sse"
      ],
      "env": {
        "X_APP_KEY": "mcp_app_key_your_generated_secret_key_here"
      }
    }
  }
}
```

### 3. OpenClaw Agent / Custom Python/TypeScript Clients
Connect directly to the SSE stream or Meta-Mode endpoint:
```typescript
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { SSEClientTransport } from "@modelcontextprotocol/sdk/client/sse.js";

const transport = new SSEClientTransport(
  new URL("http://10.0.0.10:8026/sse"),
  {
    requestInit: {
      headers: {
        "X-App-Key": "mcp_app_key_your_generated_secret_key_here"
      }
    }
  }
);

const client = new Client({ name: "my-agent", version: "1.0.0" }, { capabilities: {} });
await client.connect(transport);
```
