# Model Context Gateway (MCG): Operations Runbook

Production deployment, reverse proxy configuration, database backup/recovery, observability, health checks, and disaster recovery procedures for **Model Context Gateway (MCG)**.

---

## 🚀 Production Deployment Guidelines

### 1. Docker Compose Deployment (Recommended)

Below is the standard production `docker-compose.yaml` configuration with persistent volume mounts, security hardening, and resource limits:

```yaml
version: "3.8"

services:
  mcg:
    image: ghcr.io/spelech/model-context-gateway:latest
    container_name: mcg
    restart: unless-stopped
    security_opt:
      - no-new-privileges:true
    networks:
      - net_cloud
      - net_smarthome
      - net_media
    ports:
      - "8026:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - Database__Provider=SQLite
      - Database__ConnectionString=Data Source=/data/mcg.db
      - MCG_MASTER_KEY=${MCG_MASTER_KEY}
      - CORS_ALLOWED_ORIGINS=https://mcp.yourdomain.com,http://10.0.0.10:8026
      - EMBEDDING_MODEL_DIR=/data/models
    volumes:
      - /containers/mcp/router/data:/data
      - /containers/mcp/router/models:/data/models
    deploy:
      resources:
        limits:
          cpus: "2.0"
          memory: 1024M
        reservations:
          cpus: "0.2"
          memory: 256M
    labels:
      - caddy=mcp.yourdomain.com
      - caddy.import_1=cloudflare
      - caddy.import_2=tinyauth
      - caddy.reverse_proxy={{upstreams 8080}}
      - kuma.mcg.http.name=Model Context Gateway
      - kuma.mcg.http.url=http://mcg:8080/health
      - kuma.mcg.http.group=Infrastructure

networks:
  net_cloud:
    external: true
  net_smarthome:
    external: true
  net_media:
    external: true
```

---

### 2. Systemd Service Deployment (Linux Bare-Metal / VM)

For bare-metal Linux deployments:

Create `/etc/systemd/system/mcg.service`:
```ini
[Unit]
Description=Model Context Gateway (MCG)
After=network.target network-online.target
Wants=network-online.target

[Service]
Type=simple
User=mcg
Group=mcg
WorkingDirectory=/opt/mcg
ExecStart=/usr/bin/dotnet /opt/mcg/mcg.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mcg

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:8026
Environment=Database__Provider=SQLite
Environment=Database__ConnectionString=Data Source=/var/lib/mcg/mcg.db
Environment=MCG_MASTER_KEY=your_64_char_hex_master_key_here
Environment=EMBEDDING_MODEL_DIR=/var/lib/mcg/models

# Security sandbox
ProtectSystem=full
ProtectHome=true
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
```

Enable and start the service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable --now mcg
sudo systemctl status mcg
```

---

### 3. Windows Server Deployment (IIS In-Process & Windows Service)

For Windows Server hosting and validation, the repository provides automation scripts and operational documentation:

- **Comprehensive Guide**: [**Windows Deployment, Enterprise Hosting & Validation Guide (`docs/windows-deployment-and-validation-guide.md`)**](windows-deployment-and-validation-guide.md)
- **IIS In-Process Automation**: `scripts/windows/Deploy-IIS.ps1` (configures `No Managed Code`, `AlwaysRunning`, unbuffered SSE streaming with `responseBufferLimit="0"`, and Windows Authentication).
- **Windows Service Automation**: `scripts/windows/Setup-WindowsService.ps1` (registers SCM auto-restart recovery triggers and service lifecycle).
- **Secret Management**: `scripts/windows/Set-RegistrySecrets.ps1` (DPAPI machine encryption for registry keys).
- **Diagnostic Runner**: `scripts/windows/Test-WindowsEnvironment.ps1` (end-to-end environment validation).

#### Quick IIS Deployment (PowerShell as Administrator):
```powershell
# Deploy to IIS with Windows Authentication on Port 8080:
.\scripts\windows\Deploy-IIS.ps1 -SiteName "ModelContextGateway" -Port 8080 -EnableWindowsAuth
```

#### Quick Windows Service Lifecycle (PowerShell as Administrator):
```powershell
# Install and start Windows Service with auto-recovery on Port 8080:
.\scripts\windows\Setup-WindowsService.ps1 -Action Install -Port 8080

# Query service status and health:
.\scripts\windows\Setup-WindowsService.ps1 -Action Status
```

---

## 🔒 Reverse Proxy & SSL/TLS Configuration

Because the gateway uses Server-Sent Events (`SSE`) for streaming JSON-RPC responses, reverse proxies must disable response buffering and preserve long-lived HTTP streams.

### 1. Caddy Reverse Proxy (`Caddyfile`)

Caddy handles SSE streams out of the box. Forward identity headers from your authentication middleware:

```caddy
mcp.yourdomain.com {
    import cloudflare
    import forward_auth

    # Pass forward-auth user and group claims
    reverse_proxy mcg:8080 {
        header_up Host {host}
        header_up X-Real-IP {remote_host}
        header_up X-Forwarded-For {remote_host}
        header_up X-Forwarded-Proto {scheme}
    }
}
```

*Note: Always format and validate Caddy configs before reloading:*
```bash
docker compose exec caddy caddy fmt --overwrite /etc/caddy/Caddyfile
docker compose exec caddy caddy validate --config /etc/caddy/Caddyfile
```

---

### 2. NGINX / SWAG Configuration

In NGINX, explicitly disable buffering and increase read timeouts for SSE endpoints:

```nginx
server {
    listen 443 ssl http2;
    server_name mcp.example.com;

    ssl_certificate /etc/letsencrypt/live/mcp.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/mcp.example.com/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:8026;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Forward-Auth Headers
        proxy_set_header Remote-User $remote_user;
        proxy_set_header Remote-Groups $http_remote_groups;

        # SSE Streaming Settings
        proxy_http_version 1.1;
        proxy_set_header Connection '';
        proxy_buffering off;
        proxy_cache off;
        chunked_transfer_encoding on;
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }
}
```

---

## 🗄️ Database Operations: Backup, Restore & Maintenance

The gateway persistence tier manages 12 core tables across SQLite, MS SQL Server, and MySQL. For schema contracts and the complete Entity-Relationship Diagram, see [**Database Provider Support & Deployment Matrix (`docs/database-providers.md`)**](database-providers.md#unified-database-entity-relationship-diagram-erd).

### 1. SQLite Provider (`Data Source=/data/mcg.db`)

#### Safe Online Backup (Zero Downtime)
SQLite locks during active transactions. Use the SQLite CLI online backup API to take a consistent snapshot:

```bash
# Execute online backup without stopping the container
docker compose exec mcg sqlite3 /data/mcg.db ".backup '/data/mcg-backup-$(date +%Y%m%d_%H%M%S).db'"
```

#### Restoring SQLite Database
```bash
# 1. Stop the gateway container
docker compose stop mcg

# 2. Restore backup file
cp /data/backups/mcg-backup-20260825.db /data/mcg.db

# 3. Start container
docker compose start mcg
```

---

### 2. Microsoft SQL Server Provider

#### Taking a Database Backup
```sql
BACKUP DATABASE [ModelContextGateway]
TO DISK = N'/var/opt/mssql/backup/ModelContextGateway_Full.bak'
WITH FORMAT, INIT, COMPRESSION, STATS = 10;
```

#### Restoring Database
```sql
USE [master];
ALTER DATABASE [ModelContextGateway] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [ModelContextGateway]
FROM DISK = N'/var/opt/mssql/backup/ModelContextGateway_Full.bak'
WITH REPLACE;
ALTER DATABASE [ModelContextGateway] SET MULTI_USER;
```

---

### 3. MySQL Provider

#### Taking a Backup via `mysqldump`
```bash
mysqldump -u mcp_user -p --single-transaction --routines --triggers --databases mcpmaster > /backups/mcpmaster_$(date +%F).sql
```

#### Restoring Backup
```bash
mysql -u mcp_user -p mcpmaster < /backups/mcpmaster_2026-08-25.sql
```

---

## 📊 Observability, Health Checks, & Logs

### 1. Health Probe (`GET /health`)
The `/health` endpoint provides structured status information for orchestrators and uptime monitors (e.g. Uptime Kuma):

```bash
curl -s http://10.0.0.10:8026/health | jq .
```

Example JSON Response:
```json
{
  "status": "healthy",
  "service": "ModelContextGateway",
  "version": "5.0.0",
  "database": {
    "provider": "SQLite",
    "connected": true
  },
  "servers": {
    "total": 14,
    "healthy": 14
  },
  "sessions": {
    "active": 3
  },
  "memoryBytes": 47185920
}
```

---

### 2. Prometheus Metrics (`GET /metrics`)
Scrape Prometheus metrics for Grafana dashboards:
* `mcg_active_sessions_total`: Current number of open client sessions.
* `mcg_tool_execution_duration_seconds`: Histogram of tool execution latency.
* `mcg_tool_executions_total{status="200"}`: Total tool execution count by status code.
* `mcg_semantic_search_duration_seconds`: Latency of ONNX vector scoring.

---

### 3. Log Inspection with Dozzle
View live streaming container logs via Dozzle or Docker CLI:
```bash
# Follow logs in real-time
docker compose logs -f --tail=100 mcg

# Check for warnings or errors
docker compose logs mcg | grep -E "WARN|FAIL|ERR"
```

*Note: The gateway's built-in `PiiSanitizer` automatically scrubs Bearer tokens, passwords, and API keys before logging.*

---

## 🔄 Disaster Recovery & Secret Rotation

### 1. AES-256-GCM Master Key Rotation
If the master encryption key (`MCG_MASTER_KEY`) must be rotated:
1. Export unencrypted backup or use the internal migration utility:
   ```bash
   dotnet run --project ModelContextGateway.csproj -- re-encrypt-master-key --old-key <OLD_HEX> --new-key <NEW_HEX>
   ```
2. Update the `MCG_MASTER_KEY` environment variable in `docker-compose.yaml`.
3. Restart the container: `docker compose up -d mcg`.

### 2. HashiCorp Vault AppRole Credential Rotation
1. Generate new `secret-id` in Vault:
   ```bash
   vault write -f auth/approle/role/mcg/secret-id
   ```
2. Navigate to **`Settings`** -> **`Secret Providers`** tab in the Model Context Gateway UI.
3. Paste the new `Secret ID` and click **`Save Provider Settings`**.
4. The gateway dynamically invalidates existing cached tokens and authenticates with the new credentials without dropping active client connections.

### 3. Emergency Container Rollback
If a newly deployed container version encounters issues:
```bash
# 1. Update tag in docker-compose.yaml to previous stable release
sed -i 's/v5.0.0/v4.35.0/g' docker-compose.yaml

# 2. Re-create container
docker compose up -d --force-recreate mcg

# 3. Verify health
curl -f http://localhost:8026/health
```
