# 01. Dashboard & Navigation Interface

The **MCP Gateway Router Dashboard** provides a centralized control plane for monitoring, configuring, and testing all connected backend Model Context Protocol (MCP) servers.

---

## 🖥️ Layout & Header Navigation

The top navigation header provides one-click access across all primary views:

1. **Dashboard View (`Dashboard`)**: Main control plane displaying server health, stats cards, filtering toolbars, registered clients, and approval notifications.
2. **Test Bench View (`Test Bench`)**: Interactive console for executing tools, inspecting resource URIs, rendering prompts, and testing vector semantic routing.
3. **Settings View (`Settings`)**: System settings, vector embedding engine parameters, security policies, and authentication configuration.
4. **Active Identity & Status Badge**: Displays current identity (`Remote-User` or AppKey principal) along with current router version (`v4.0.25`).

---

## 📊 Dashboard Cards & Indicators

### 1. Statistics Summary Card
Located at the top of the dashboard, providing quick aggregate metrics:
- **Total Backend Servers**: Count of active/registered MCP servers.
- **Healthy Connections**: Count of backend servers responding green to health probes.
- **Total Tools & Resources**: Total cataloged tools and virtual resource URIs across all connected servers.
- **Pending Approvals**: Badge showing pending manual tool execution approval requests.

### 2. Server Status Cards
Each registered MCP server is rendered in a dedicated card displaying:
- **Status Indicator**:
  - 🟢 **Green**: Connected & healthy.
  - 🟡 **Yellow**: Initializing / warning.
  - 🔴 **Red**: Unreachable / failing health probe.
- **Server Name & ID**: Unique identifier (e.g. `docker`, `homeassistant`, `actual_budget`).
- **Transport Type**: `HTTP`, `SSE`, or `STDIO`.
- **Capabilities Badges**: Badges indicating supported MCP capabilities (`Tools`, `Resources`, `Prompts`).
- **Secret Provider Badge**: `None`, `Env`, `Vault`, or `Registry`.
- **Action Buttons**:
  - 👁️ **Inspect**: View tool definitions, schemas, resource URIs, and prompt templates.
  - ✏️ **Edit**: Modify connection URL, secret provider settings, or headers.
  - 🛡️ **Policy**: Configure fine-grained RBAC group access policies.
  - 🗑️ **Delete**: Remove server registration.

---

## 🔍 Searching & Filtering Servers

- **Search Bar**: Type any text to dynamically filter servers by name, ID, category, or transport type.
- **Category Filter Tabs**:
  - `All`: Display all registered servers.
  - `Smart Home`: Home Assistant, Zigbee, UniFi.
  - `Media`: Radarr, Sonarr, Plex, Seerr.
  - `Cloud`: Nextcloud, Vaultwarden, Homebox.
  - `Infrastructure`: Docker, Monitoring, System logs.
- **Pagination Toolbar**: Navigate through paginated server cards (10 per page by default).
