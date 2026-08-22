# 🚀 Enterprise Production Deployment & Database Migration Guide

This guide outlines requirements, configurations, and migration paths for deploying the **Model Context Protocol (MCP) Router Gateway** into production environments.

---

## 🔒 Required Production Configuration

Production environments must be locked down to prevent spoofing, unauthorized access, and credential leakage. Below are the mandatory configuration parameters.

### 🚨 Critical Parameters

| Configuration Key | Environment Variable Equivalent | Type | Description / Behavior |
| :--- | :--- | :--- | :--- |
| **`ROUTER_MASTER_KEY`** | `ROUTER_MASTER_KEY` | **Mandatory** | A high-entropy Base64/hex-encoded 256-bit key. Used to encrypt downstream server credentials, API tokens, and configurations in the database. **Fatal error on startup if missing!** |
| **`DB_PROVIDER`** | `DB_PROVIDER` | Optional | Supported: `sqlite`, `mssql`, `mysql`. Defaults silently to `sqlite` if missing or unconfigured. |
| **`ConnectionStrings:DefaultConnection`** | `ConnectionStrings__DefaultConnection` | **Mandatory** | Connection string for the chosen database provider. |
| **`CORS_ALLOWED_ORIGINS`** | `CORS_ALLOWED_ORIGINS` | **Mandatory** | Comma/semicolon/whitespace-separated list of allowed origins. Unconfigured/empty locks out browser access in production. |
| **`OpenIddict:CertificatePath`** | `OpenIddict__CertificatePath` | **Mandatory** | Filepath to the PFX/PKCS#12 certificate used to sign OAuth tokens in production. Unconfigured fallbacks reset on application restart, invalidating active client sessions. |
| **`Oidc:TrustedProxies`** | `Oidc__TrustedProxies` | **Highly Recommended** | List of upstream reverse proxy IP addresses trusted by the gateway. Defaults strictly to loopback-only (`127.0.0.1` / `::1`) if unconfigured. |
| **`Admin:GroupSid`** | `Admin__GroupSid` | Optional | Active Directory Group SID designated for Administrators. Defaults to `S-1-5-32-544` (Local Administrators). |

---

## ⚠️ Deploy-Time Behavior Change

In previous releases, the trusted-proxy fallback trusted IPs in standard container subnets (`10.0.0.0/8`, `172.16.0.0/12`).
**Starting in version 4.5.5, the unconfigured default is loopback-only (`127.0.0.1` and `::1`).**

> 💡 **Important Deployment Action:** If your reverse proxy (e.g., Caddy, Nginx, Traefik, IIS) runs on a bridge network, you **MUST** configure `Oidc:TrustedProxies` with the proxy's IP address. If left unset, proxy-passed SSO headers from remote hosts will be stripped, degrading authentication to guest access.

---

## 🟢 Quick Start — SQLite (Default Provider)

SQLite requires **no manual schema steps**: on first start the gateway creates tables and applies migrations automatically via the built-in seeder. This is the recommended path for single-node deployments.

### Prerequisites
* **.NET 10 runtime** on the host (or use the container image).
* Active Directory **SID** resolution is fully cross-platform via LDAP. Native `WindowsIdentity` is automatically used as a fast-path on Windows hosts. Header-based (reverse-proxy) identity also works cross-platform.
* A reverse proxy (IIS/Nginx/Caddy/Traefik) terminating TLS and injecting identity headers, if using header auth.

### Step 1 — Generate the master key
`ROUTER_MASTER_KEY` encrypts downstream credentials at rest (AES-GCM). Generate a 256-bit key:
```bash
openssl rand -base64 32
```

### Step 2 — Generate the OpenIddict signing certificate
Production fails closed if no certificate is configured (tokens would otherwise reset every restart):
```bash
openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 3650 -nodes -subj "/CN=mcp-router"
openssl pkcs12 -export -out openiddict.pfx -inkey key.pem -in cert.pem -passout pass:
```
Point `OpenIddict:CertificatePath` at the resulting `openiddict.pfx`.

### Step 3 — Minimal configuration
Copy `.env.example` to `.env` (or use `appsettings.Production.json.example`) and set at minimum:
```bash
ASPNETCORE_ENVIRONMENT=Production
ROUTER_MASTER_KEY=<from step 1>
DB_PROVIDER=sqlite                                   # default; may be omitted
ConnectionStrings__DefaultConnection=Data Source=/data/mcprouter.db
CORS_ALLOWED_ORIGINS=https://mcp.internal.example.com
OpenIddict__CertificatePath=/data/openiddict.pfx
Oidc__TrustedProxies=10.20.30.40                     # your reverse proxy IP (loopback-only if unset)
Admin__GroupSid=S-1-5-21-...                         # your AD admins group SID
```

### Step 4 — Persist the database file
The SQLite `.db` file (and the PFX) **must live on persistent storage**. In containers, mount a volume at the directory referenced by `Data Source` (e.g. `-v mcprouter-data:/data`). Losing the file loses all servers, keys, policies, and audit history.

### Step 5 — Run
```bash
# from a published build
dotnet mcp-router.dll
# or the container image, with the data volume and env file
docker run --env-file .env -v mcprouter-data:/data -p 8080:8080 <image>
```

### Step 6 — Verify
* The gateway logs `Initializing database via Dapper...` and creates the schema on first boot.
* Liveness: `GET /health` returns healthy (note: this is a static liveness probe — a true readiness probe is on the observability backlog).
* Confirm audit capture by making an authenticated call and reading it back via `GET /api/audit`.

> ⚠️ **Scope note:** Only the SQLite path is exercised in the current CI/test suite (159 tests, SQLite). The SQL Server / MySQL provider paths, real AD/LDAP, and Vault integration are implemented but have **not** been validated end-to-end in this repo — verify those in your own environment before relying on them.

---

## 📂 Database Providers & Schema Initialization

The MCP Router supports SQL Server, MySQL/MariaDB, and SQLite. For comprehensive dialect specifications, envelope encryption details, fail-closed validation contracts, and Docker Compose configurations, see the [**Database Provider Support & Deployment Matrix Guide**](database-providers.md).

When configuring MS SQL Server or MySQL, execute the database scripts in the exact sequence described below to initialize the database and stored procedures.

### Schema Initialization Order

1. **`01_tables.sql`**: Renders database tables, constraints, and indexes.
2. **`02_procedures.sql`**: Renders model access evaluation, audit logging, and JIT secret retrieval stored procedures.

### DB Script Directories
* **MS SQL Server**: `scripts/db/mssql/`
* **MySQL / MariaDB**: `scripts/db/mysql/`

---

## 🔄 Upgrading Existing Database Schemas (Migrations)

When upgrading existing deployments, you must apply versioned delta migration scripts sequentially.

### Initial Migration (Version 003)
The migration `003_add_appkeys_ownersid.sql` introduces an optional `OwnerSid` field to the `AppKeys` table and updates stored procedures (`sp_SaveAppKey` and `sp_GetAppKeys`) to track app key owners dynamically.

#### Upgrade Execution Procedure:
Run the corresponding delta migration script against your database using standard CLI tools.

* **MS SQL Server**:
  ```bash
  sqlcmd -S localhost -U sa -P Password123! -i scripts/db/mssql/migrations/003_add_appkeys_ownersid.sql
  ```
* **MySQL / MariaDB**:
  ```bash
  mysql -u root -p McpEnterpriseDb < scripts/db/mysql/migrations/003_add_appkeys_ownersid.sql
  ```

---

## 📦 Deployment Configuration Templates

For quick integration, copy `.env.example` to `.env` or copy `appsettings.Production.json.example` to `appsettings.Production.json` directly into your container/server configuration.
## 🧩 Support Matrix

For detailed information on supported combinations of hosting environments, authentication providers, and downstream delegation methods (e.g., Docker vs IIS, OIDC vs AppKey), please refer to the [**Deployment & Authentication Support Matrix**](support-matrix.md).
