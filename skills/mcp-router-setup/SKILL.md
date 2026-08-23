---
name: mcp-router-setup
description: Use when installing, configuring, deploying, or bootstrapping the CSharp-MCP-Router gateway in a new or existing environment (Docker, Docker Compose, or Windows IIS) for personal home-lab or enterprise use.
---

# Universal MCP Router Setup (`mcp-router-setup`)

## Overview

**CSharp-MCP-Router** is a high-performance ASP.NET Core gateway and proxy for the Model Context Protocol (MCP). It aggregates multiple backend MCP servers, exposes a token-efficient **Meta-Mode** (`/sse`) with `search_tools` and `execute_tool` to prevent context window bloat, and provides an encrypted database-backed **Admin MCP Server** (`/admin`) and Web UI for hot-reloading configurations at runtime.

This skill provides an autonomous 6-phase decision and bootstrapping engine to install and configure the router in any environment without needing to clone or compile source code.

---

## When to Use

```
                         [Start Setup]
                               │
                ┌──────────────┴──────────────┐
                ▼                             ▼
       [Docker / Containers]          [Windows Server IIS]
                │                             │
        Phase 1 & 2: Probe            Phase 1 & 2: Probe
                │                             │
                └──────────────┬──────────────┘
                               ▼
               Phase 3: Configuration Paradigm
                ┌──────────────┴──────────────┐
                ▼                             ▼
       [Environment Variables]       [Web UI & Database]
       (Static / 12-Factor)          (Dynamic Hot-Reload)
                │                             │
                └──────────────┬──────────────┘
                               ▼
               Phase 4: Identity & Network Mode
                ┌──────────────┴──────────────┐
                ▼                             ▼
       [Personal / Standalone]        [Enterprise AD / OIDC]
       (Loopback / LAN CIDR)          (SSO / Forward-Auth)
                │                             │
                └──────────────┬──────────────┘
                               ▼
               Phase 5: Generate Artifacts
               (256-bit Key, Compose / IIS)
                               ▼
               Phase 6: Health & Client Setup
               (Claude, Cursor, Cline, Windsurf)
```

### Trigger Conditions & Use Cases
- User wants to install, deploy, or configure `CSharp-MCP-Router` from scratch.
- Deploying the router via Docker Compose, Docker CLI, or Windows IIS.
- Connecting AI IDEs (Claude Desktop, Cursor, Cline, Windsurf, Antigravity) to an aggregated MCP gateway.
- Setting up standalone home-lab routing or enterprise SSO/AD authentication.
- Troubleshooting missing `ROUTER_MASTER_KEY`, 403 network access, or client connection errors.

### When NOT to Use
- Connecting to an individual, standalone MCP server directly without an aggregator or router.
- Modifying router source code or writing C# unit tests (refer to repository developer guides instead).

---

## Phase 1: Automated Environment Probing

Before prompting the user, inspect the host environment to determine defaults and available capabilities.

### 1.1 Probing Commands

Run the appropriate detection commands for the platform:

```bash
# 1. OS & Architecture Detection
uname -s -m 2>/dev/null || echo "$OS"

# 2. Docker Daemon Availability
docker info >/dev/null 2>&1 && echo "DOCKER_AVAILABLE=true" || echo "DOCKER_AVAILABLE=false"
test -e /var/run/docker.sock && echo "DOCKER_SOCK_FOUND=true" || echo "DOCKER_SOCK_FOUND=false"

# 3. Vault & Secret Store Detection
if [ -n "$VAULT_ADDR" ]; then echo "VAULT_DETECTED=$VAULT_ADDR"; fi

# 4. Windows Active Directory Context (PowerShell / Windows)
# In PowerShell:
# if ($env:USERDNSDOMAIN) { Write-Output "AD_DOMAIN=$env:USERDNSDOMAIN" }
```

### 1.2 Probe Interpretation
- **Docker socket present & daemon running**: Recommend Docker Compose (Phase 2).
- **Windows host with `USERDNSDOMAIN` or IIS**: Recommend Windows IIS / Service track with Active Directory authentication.
- **`VAULT_ADDR` present**: Pre-populate HashiCorp Vault secret provider options.

---

## Phase 2: Hosting Platform Selection

Present the user with the two primary hosting platforms:

| Platform | Recommended When | Requirements |
| :--- | :--- | :--- |
| **Docker / Compose** *(Recommended)* | Linux, macOS, WSL2, Home-Lab servers, Kubernetes | Docker Engine 20.10+, Docker Compose v2+ |
| **Windows IIS / Service** | Enterprise Windows Server, native Windows Auth / DPAPI | Windows Server 2019+, IIS with ASP.NET Core Module v2 (in-process) |

---

## Phase 3: Configuration Paradigm (Env vs. UI & Database)

Explain the trade-offs between static environment variable configuration and dynamic database configuration:

| Dimension | Environment Variables (`.env`) | Web UI & Database (Dynamic) |
| :--- | :--- | :--- |
| **Management** | Static declarative files (`.env`, `docker-compose.yml`) | Browser Web UI or `/admin` MCP Server |
| **Updates** | Requires container/service restart | Zero-downtime dynamic hot-reloading |
| **Secret Storage** | Plaintext in environment / compose files | AES-256-GCM / SQLCipher encrypted at rest |
| **AI Agent Admin** | Manual human editing of files | Autonomous agent self-configuration via Admin MCP |
| **Multi-Tenancy** | Single shared gateway configuration | Role-Based Access Control & audit logging |
| **Best For** | CI/CD pipelines, GitOps, minimal static setups | Active multi-server gateways, teams, dynamic tools |

---

## Phase 4: Identity & Network Topology

Select the identity and network access tier based on deployment scope:

### Option A: Personal / Home-Lab (Standalone Mode)
- **Database**: SQLite (`./data/mcp_router.db`).
- **Network Restriction**: Restrict admin UI and admin tools to local loopback or local LAN CIDR subnets using `Admin:StandaloneAllowedNetworks`.
- **Authentication**: Admin AppKey (`Authorization: Bearer mcp-...` or `X-App-Key` header) for external agent access, local IP network trust for Web UI.

### Option B: Enterprise Mode
- **Database**: Microsoft SQL Server (`mssql`), MySQL (`mysql`), or SQLite (`sqlite`).
- **Authentication Providers**:
  - **Active Directory (Windows Auth / LDAP)**: Domain SID mapping (e.g., `S-1-5-32-544` for local Administrators).
  - **OIDC / Reverse Proxy Forward-Auth**: Authentik, Authelia, PocketID, Keycloak with forward-auth headers (`Remote-User`, `Remote-Groups`).
- **Secret Providers**: HashiCorp Vault KV v2 (`VAULT_ADDR`, `VAULT_TOKEN`) or Windows DPAPI.

---

## Phase 5: Artifact Generation & Secrets Scaffolding

### 5.1 Master Key Options

The router encrypts database credentials using a 256-bit key. You have three flexible options:
1. **Auto-Generated Persistent Keyfile** *(Default)*: Omit `ROUTER_MASTER_KEY` and the gateway will auto-generate and store `./data/.master.key` on first boot.
2. **File Secret / Docker Secrets**: Set `ROUTER_MASTER_KEY_FILE=/run/secrets/router_master_key`.
3. **Explicit Environment Variable**: Generate a 256-bit Base64 key:

```bash
# Linux / macOS / Bash
openssl rand -base64 32

# Python Fallback
python3 -c "import secrets, base64; print(base64.b64encode(secrets.token_bytes(32)).decode())"

# Windows PowerShell (CSPRNG)
$bytes = New-Object byte[] 32; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes); [Convert]::ToBase64String($bytes)
```

### 5.2 Docker Compose Artifacts

#### `docker-compose.yml`
```yaml
services:
  mcp-router:
    image: ghcr.io/spelech/mcp-router:latest
    container_name: mcp-router
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - DB_PROVIDER=${DB_PROVIDER:-sqlite}
      # Optional: Auto-generated to ./data/.master.key if omitted
      # - ROUTER_MASTER_KEY=${ROUTER_MASTER_KEY}
      - CORS_ALLOWED_ORIGINS=${CORS_ALLOWED_ORIGINS:-http://localhost:3000,http://localhost:8080}
      - Admin__StandaloneAllowedNetworks__0=${STANDALONE_ALLOWED_NETWORK:-127.0.0.1}
      - Admin__StandaloneAllowedNetworks__1=::1
      # Uncomment for LAN subnet access:
      # - Admin__StandaloneAllowedNetworks__2=192.168.1.0/24
    volumes:
      - ./data:/app/data
      - /var/run/docker.sock:/var/run/docker.sock
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 3
      start_period: 5s
```

#### `.env`
```ini
# Core Secrets (Generated 256-bit Key)
ROUTER_MASTER_KEY=REPLACE_WITH_GENERATED_BASE64_KEY

# Database Configuration
DB_PROVIDER=sqlite

# Network & Admin Access (Loopback or CIDR)
STANDALONE_ALLOWED_NETWORK=127.0.0.1
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:8080
```

### 5.3 Windows IIS Artifacts

#### `web.config`
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\mcp-router.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <environmentVariable name="responseBufferLimit" value="0" />
      </environmentVariables>
    </aspNetCore>
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="52428800" />
      </requestFiltering>
    </security>
  </system.webServer>
</configuration>
```

#### `appsettings.Production.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DB_PROVIDER": "sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=data/mcp_router.db"
  },
  "Admin": {
    "StandaloneAllowedNetworks": [
      "127.0.0.1",
      "::1"
    ]
  }
}
```

---

## Phase 6: Health Verification & Client Setup

### 6.1 Service Launch & Verification

Start the router service and verify health endpoints:

```bash
# Docker Compose Launch
docker compose up -d

# Verify Gateway Health
curl -s -f http://localhost:8080/health || echo "Health check failed"

# Verify SSE Meta-Mode Stream
curl -i -N -H "Accept: text/event-stream" http://localhost:8080/sse
```

Expected Health Check Output:
```json
{"status":"Healthy","database":"Connected","timestamp":"..."}
```

---

### 6.2 Client Configuration Snippets

Provide ready-to-use configuration JSON for the user's AI assistants:

#### Claude Desktop (`claude_desktop_config.json`)
```json
{
  "mcpServers": {
    "mcp-router": {
      "command": "npx",
      "args": ["-y", "mcp-proxy", "http://localhost:8080/sse"]
    }
  }
}
```

#### Cursor (`.cursor/mcp.json`)
```json
{
  "mcpServers": {
    "mcp-router": {
      "url": "http://localhost:8080/sse"
    }
  }
}
```

#### Cline (`cline_mcp_settings.json` / VS Code)
```json
{
  "mcpServers": {
    "mcp-router": {
      "url": "http://localhost:8080/sse",
      "transport": "sse"
    }
  }
}
```

#### Windsurf (`mcp_config.json`)
```json
{
  "mcpServers": {
    "mcp-router": {
      "serverUrl": "http://localhost:8080/sse"
    }
  }
}
```

#### Autonomous Agent Router Administration (`Admin MCP Server`)
To allow an AI agent to manage, add, edit, or delete backend MCP servers, auth providers, secret stores, and access policies dynamically:
```json
{
  "mcpServers": {
    "mcp-router-admin": {
      "url": "http://localhost:8080/admin/sse",
      "headers": {
        "Authorization": "Bearer mcp-global-admin-default-cli-key-99"
      }
    }
  }
}
```

> [!TIP]
> **Next Step: Automated Provider Provisioning**
> Once the gateway is deployed and running, use the **`mcp-router-admin`** skill (`.agents/skills/mcp-router-admin/SKILL.md`) to autonomously configure Authentik, Keycloak, Microsoft Entra ID, Active Directory, HashiCorp Vault, semantic search embeddings, access policies, and backend servers.

---

## Common Mistakes & Troubleshooting

| Symptom / Error | Root Cause | Solution |
| :--- | :--- | :--- |
| **Container restarts immediately / Key error** | Missing or malformed `ROUTER_MASTER_KEY` | Generate a 256-bit base64 key with `openssl rand -base64 32` and set it in `.env`. |
| **`403 Forbidden` on Web UI or Admin endpoints** | Client IP not allowed in Standalone Mode | Add client IP or CIDR (e.g. `192.168.1.0/24` or `0.0.0.0/0`) to `Admin__StandaloneAllowedNetworks__*`. |
| **Docker MCP servers fail to spawn** | Router container cannot access Docker daemon | Ensure `- /var/run/docker.sock:/var/run/docker.sock` volume is mounted and permissions allow read/write. |
| **SSE streams disconnect or buffer indefinitely in IIS** | IIS response buffering delays text/event-stream chunks | Ensure `<environmentVariable name="responseBufferLimit" value="0" />` is present in `web.config`. |
| **OIDC / Reverse Proxy returns unauthorized** | Missing or stripped forward-auth headers | Verify proxy passes `Remote-User` and `Remote-Groups` headers and upstream IP is in `Oidc:TrustedProxies`. |
| **Database file permission errors on Linux** | SQLite volume `./data` owned by root | Run `mkdir -p data && chmod 777 data` before starting container. |

---

## Quick Reference Commands

```bash
# Generate 256-bit Key
KEY=$(openssl rand -base64 32) && echo "ROUTER_MASTER_KEY=$KEY"

# Start Gateway
docker compose up -d

# Check Logs
docker compose logs -f mcp-router

# Probe Meta-Mode Tools List
curl -s http://localhost:8080/health
```
