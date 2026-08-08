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
   - **Scopes**: Select `read:tools`, `execute:tools`, `admin:full`.
   - **Expiration**: Select 30 days, 90 days, 1 year, or Never.
4. Click **Create Key**. Copy the generated secret key (it will only be displayed once).

---

## 🛠️ Integration Snippets & Guides

Click **`Client Setup`** on the Dashboard toolbar to view ready-to-copy client configurations:

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
