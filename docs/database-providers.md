# 🗄️ Database Provider Support & Deployment Matrix

This document provides the definitive architectural specification, schema contracts, encryption model, and deployment configurations for all database engines supported by the **Model Context Protocol (MCP) Router Gateway**.

---

## 📊 Database Engine Support Matrix

The MCP Router employs **Dapper** with specialized dialect handlers and native ADO.NET providers to deliver high-throughput, low-latency persistence across embedded, enterprise on-premises, and cloud environments.

| Feature / Dimension | 🪶 SQLite (Default) | 🏢 Microsoft SQL Server | 🐬 MySQL / MariaDB |
| :--- | :--- | :--- | :--- |
| **Provider Identifier (`DB_PROVIDER`)** | `sqlite` | `mssql` | `mysql` |
| **Underlying ADO.NET Driver** | [`Microsoft.Data.Sqlite`](https://www.nuget.org/packages/Microsoft.Data.Sqlite) | [`Microsoft.Data.SqlClient`](https://www.nuget.org/packages/Microsoft.Data.SqlClient) | [`MySqlConnector`](https://www.nuget.org/packages/MySqlConnector) |
| **Target Versions** | 3.35+ (WAL Mode Enabled) | 2016, 2019, 2022, Azure SQL | MySQL 8.0+, MariaDB 10.5+ |
| **Execution Paradigm** | Direct SQL & In-Process DDL | T-SQL Stored Procedures (`sp_*`) | Stored Procedures (`sp_*`) |
| **Upsert Mechanism** | `ON CONFLICT(Id) DO UPDATE` | Stored Proc `IF EXISTS ... UPDATE` | `ON DUPLICATE KEY UPDATE` |
| **Parameter Prefix Convention** | Named parameters (`@Param`) | Named T-SQL parameters (`@Param`) | Strict `p_` prefix (`p_Param`) |
| **Timestamp Generation** | `CURRENT_TIMESTAMP` (UTC ISO-8601) | `SYSUTCDATETIME()` (DATETIME2) | `CURRENT_TIMESTAMP` / `NOW()` |
| **Schema Migration Mode** | Automatic in-process migrations | Scripted DDL (`scripts/db/mssql/`) | Scripted DDL (`scripts/db/mysql/`) |
| **Startup Validation** | Table, column & pragma checks | Table, column, FK & proc checks | Table, column, FK & proc checks |
| **Recommended Deployment** | Single-node homelab & edge agents | Corporate Windows/AD enterprise | Cloud-native, Linux & Kubernetes |

---

## 🔍 Dialect Specifications & Schema Contracts

### 1. SQLite Engine Dialect

SQLite is the zero-configuration embedded engine designed for single-node instances, developer workstations, and edge agent deployments.

#### Engine Characteristics & Concurrency
* **Embedded Storage**: Database resides in a single binary file on disk (default: `data/mcp_router.db`) or in-memory for ephemeral test runs (`Data Source=:memory:`).
* **Write-Ahead Logging (WAL)**: SQLite operates with Write-Ahead Logging to support concurrent read operations while writes execute, preventing database lock contention.
* **Type Handlers**: JSON arrays and complex collections (such as `Categories` and `ScopesJson`) are stored as serialized UTF-8 `TEXT` and mapped via custom Dapper `JsonListTypeHandler` instances.

#### In-Process Data-Preserving Upgrades (`DatabaseSeederService.cs`)
On startup, `DatabaseSeederService.ApplySqliteMigrations` inspects `sqlite_master` and SQLite table pragmas (`pragma_table_info`) to apply additive, non-destructive schema migrations automatically:
1. **Legacy Table Migration**: Automatically renames legacy `McpServers` tables to `Servers` while preserving all server configurations.
2. **Column Expansion**: Dynamically adds missing columns (`EncryptedConfigJson` on `SecretProviders` and `AuthProviderConfigs`, `OwnerSid` on `AppKeys`, vector embedding settings on `Settings`, and dynamic auth fields on `Servers`).
3. **Data Integrity**: Migrates legacy unencrypted `ConfigJson` payloads directly into `EncryptedConfigJson` without loss.

```sql
-- SQLite Upsert Contract Example (Servers)
INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, SecretProvider, SecretItemKey, SecretMount, SecretPath, SecretField, AuthShape, CustomHeaderName, Categories, ApiKey, HeadersJson, AutoDiscovered)
VALUES (@Id, @DisplayName, @Url, @Enabled, @Hidden, @Type, @SecretProvider, @SecretItemKey, @SecretMount, @SecretPath, @SecretField, @AuthShape, @CustomHeaderName, @Categories, @ApiKey, @HeadersJson, @AutoDiscovered)
ON CONFLICT(Id) DO UPDATE SET
    DisplayName = @DisplayName, Url = @Url, Enabled = @Enabled, Hidden = @Hidden, Type = @Type,
    SecretProvider = @SecretProvider, SecretItemKey = @SecretItemKey, SecretMount = @SecretMount,
    SecretPath = @SecretPath, SecretField = @SecretField, AuthShape = @AuthShape,
    CustomHeaderName = @CustomHeaderName, Categories = @Categories, ApiKey = @ApiKey,
    HeadersJson = @HeadersJson, AutoDiscovered = @AutoDiscovered;
```

---

### 2. Microsoft SQL Server Engine Dialect

Microsoft SQL Server provides enterprise-grade reliability, strict security isolation, high availability (Always On availability groups), and centralized DBA management.

#### Schema Initialization & Migration Scripts
DDL scripts are located in `scripts/db/mssql/`:
* `01_tables.sql`: Tables, primary keys, nonclustered indexes (`UQ_AppKeys_KeyPrefix`), and foreign key constraints.
* `02_procedures.sql`: Complete suite of 10 T-SQL stored procedures.
* `migrations/`: Incremental upgrade scripts (`003_add_appkeys_ownersid.sql`, `004_align_runtime_persistence.sql`).

#### Complete Stored Procedure Suite
All persistence operations in MSSQL execute via pre-compiled stored procedures:

| Stored Procedure | Purpose | Key Parameters |
| :--- | :--- | :--- |
| `sp_EvaluateUserAccess` | Evaluates group authorization for tools, servers, prompts, and resources | `@GroupNames`, `@ItemName`, `@RequestMethod` |
| `sp_GetAllowedItemsForGroups` | Returns distinct allowed tools and secret paths for active user groups | `@GroupNames` |
| `sp_GetServerSecrets` | Fetches downstream secret paths and provider bindings for a server | `@ServerCodeName` |
| `sp_SaveSecretProvider` | Upserts secret provider configuration with encrypted credentials | `@ProviderName`, `@DisplayName`, `@EncryptedConfigJson`, `@IsEnabled` |
| `sp_SaveAuthProvider` | Upserts identity/auth provider configuration and header bindings | `@ProviderName`, `@DisplayName`, `@UserHeader`, `@GroupsHeader`, `@EncryptedConfigJson`, `@IsEnabled` |
| `sp_InsertAuditLog` | Records telemetry and sanitized payloads with UTC timestamps | `@RequestId`, `@UserPrincipalName`, `@UserSid`, `@ServerCodeName`, `@ItemName`, `@RequestMethod`, `@ExecutionTimeMs`, `@StatusCode`, `@RequestPayload`, `@ResponsePayload`, `@ErrorMessage` |
| `sp_SaveAppKey` | Upserts client application API keys with owner SID tracking | `@Id`, `@Name`, `@Username`, `@KeyPrefix`, `@EncryptedKey`, `@ScopesJson`, `@OwnerSid`, `@ExpiresAt` |
| `sp_DeleteAppKey` | Permanently revokes an application API key by ID | `@Id` |
| `sp_GetAppKeys` | Queries active API keys (filtered by username or all for admins) | `@Username` |
| `sp_InsertAdminAuditLog` | Records administrative mutations (server additions, policy edits) | `@Id`, `@Username`, `@Action`, `@Target`, `@Details`, `@Success`, `@ErrorMessage` |

#### Parameter Mapping Contract (`sp_SaveAppKey` & `@CreatedAt`)
> [!IMPORTANT]
> **Strict Dapper Parameter Mapping Rule**: The `sp_SaveAppKey` stored procedure generates creation timestamps server-side using `SYSUTCDATETIME()`. It does **NOT** accept an `@CreatedAt` parameter. Dapper parameter objects must supply `@Id`, `@Name`, `@Username`, `@KeyPrefix`, `@EncryptedKey`, `@ScopesJson`, `@OwnerSid`, and `@ExpiresAt`. Passing `@CreatedAt` will fail schema compatibility validation.

```sql
-- MS SQL Server Stored Procedure Contract (sp_SaveAppKey)
CREATE OR ALTER PROCEDURE [dbo].[sp_SaveAppKey]
    @Id VARCHAR(100),
    @Name NVARCHAR(200),
    @Username NVARCHAR(256),
    @KeyPrefix VARCHAR(50),
    @EncryptedKey NVARCHAR(MAX),
    @ScopesJson NVARCHAR(MAX),
    @OwnerSid NVARCHAR(200) = '',
    @ExpiresAt DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM [dbo].[AppKeys] WHERE [Id] = @Id)
    BEGIN
        UPDATE [dbo].[AppKeys]
        SET [Name] = @Name, [Username] = @Username, [OwnerSid] = @OwnerSid,
            [KeyPrefix] = @KeyPrefix, [EncryptedKey] = @EncryptedKey,
            [ScopesJson] = @ScopesJson, [ExpiresAt] = @ExpiresAt
        WHERE [Id] = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[AppKeys] ([Id], [Name], [Username], [OwnerSid], [KeyPrefix], [EncryptedKey], [ScopesJson], [ExpiresAt], [CreatedAt])
        VALUES (@Id, @Name, @Username, @OwnerSid, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, SYSUTCDATETIME());
    END
END;
```

---

### 3. MySQL / MariaDB Engine Dialect

MySQL and MariaDB support Linux-based infrastructure, cloud container platforms (Amazon ECS, EKS, Azure Container Apps), and managed database services (Amazon RDS, Google Cloud SQL, Azure Database for MySQL).

#### Schema Initialization & Migration Scripts
DDL scripts are located in `scripts/db/mysql/`:
* `01_tables.sql`: InnoDB tables with `utf8mb4` charset and foreign key cascades.
* `02_procedures.sql`: MySQL stored procedures using `DELIMITER //` definitions.
* `migrations/`: Incremental upgrade scripts (`003_add_appkeys_ownersid.sql`, `004_align_runtime_persistence.sql`).

#### Strict `p_` Parameter Binding Convention
> [!CAUTION]
> **MySQL Parameter Scoping**: In MySQL stored procedures, parameter names that match column names (e.g. `WHERE Username = Username`) resolve to column references, creating tautologies. To prevent variable shadowing and silent query bugs, **all MySQL stored procedure parameters MUST use the `p_` prefix** (e.g., `p_Id`, `p_Name`, `p_Username`, `p_KeyPrefix`, `p_EncryptedKey`, `p_ScopesJson`, `p_OwnerSid`, `p_ExpiresAt`).

In the C# repository layer ([`Repositories.cs`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-53/Infrastructure/Persistence/Repositories.cs)), MySQL procedure calls explicitly bind parameters with this prefix:

```csharp
// MySQL Dapper Parameter Invocation in Repositories.cs
await conn.ExecuteAsync(
    "sp_SaveAppKey",
    new
    {
        p_Id = key.Id,
        p_Name = key.Name,
        p_Username = key.Username,
        p_KeyPrefix = key.KeyPrefix,
        p_EncryptedKey = key.EncryptedKey,
        p_ScopesJson = key.ScopesJson,
        p_OwnerSid = key.OwnerSid ?? "",
        p_ExpiresAt = key.ExpiresAt
    },
    commandType: CommandType.StoredProcedure
);
```

```sql
-- MySQL Stored Procedure Contract (sp_SaveAppKey)
DELIMITER //
DROP PROCEDURE IF EXISTS `sp_SaveAppKey` //
CREATE PROCEDURE `sp_SaveAppKey`(
    IN p_Id VARCHAR(100),
    IN p_Name VARCHAR(200),
    IN p_Username VARCHAR(256),
    IN p_KeyPrefix VARCHAR(50),
    IN p_EncryptedKey LONGTEXT,
    IN p_ScopesJson LONGTEXT,
    IN p_OwnerSid VARCHAR(200),
    IN p_ExpiresAt DATETIME
)
BEGIN
    INSERT INTO `AppKeys` (`Id`, `Name`, `Username`, `OwnerSid`, `KeyPrefix`, `EncryptedKey`, `ScopesJson`, `ExpiresAt`, `CreatedAt`)
    VALUES (p_Id, p_Name, p_Username, IFNULL(p_OwnerSid, ''), p_KeyPrefix, p_EncryptedKey, p_ScopesJson, p_ExpiresAt, NOW())
    ON DUPLICATE KEY UPDATE
        `Name` = p_Name,
        `Username` = p_Username,
        `OwnerSid` = IFNULL(p_OwnerSid, ''),
        `KeyPrefix` = p_KeyPrefix,
        `EncryptedKey` = p_EncryptedKey,
        `ScopesJson` = p_ScopesJson,
        `ExpiresAt` = p_ExpiresAt;
END //
DELIMITER ;
```

---

## 🔐 Database Encryption & Secrets Architecture

The MCP Router implements authenticated envelope encryption for all sensitive secrets, tokens, and third-party configuration payloads persisted in the database.

```mermaid
flowchart TD
    ConfigEnv["Environment Variable<br><code>ROUTER_MASTER_KEY</code> / <code>DB_ENCRYPTION_KEY</code>"] --> DbKeyHelper["DbKeyHelper.ResolveDbEncryptionKey()"]
    DbKeyHelper --> PBKDF2["PBKDF2 Key Derivation<br>SHA256, 600,000 Iterations<br>Salt: {Secret}_McpRouter_Salt_v2"]
    PBKDF2 --> DerivedKey["256-bit AES-GCM Key"]
    
    PlainText["Plaintext JSON Configuration<br>(Vault Tokens, Client Secrets, LDAP Bind Pwd)"] --> Encrypt["SymmetricEncryptionHelper.Encrypt()"]
    DerivedKey --> Encrypt
    Nonce["12-byte Cryptographic Nonce"] --> Encrypt
    
    Encrypt --> Packed["Packed Binary Payload<br>[ 12B Nonce | 16B Auth Tag | Ciphertext ]"]
    Packed --> Base64["Base64 Encrypted String"]
    Base64 --> DB[("Database Storage<br><code>EncryptedConfigJson</code>")]
    
    DB --> Decrypt["SymmetricEncryptionHelper.Decrypt()"]
    DerivedKey --> Decrypt
    Decrypt --> PlainTextOut["Decrypted In-Memory DTO<br><code>ConfigJson</code>"]
```

### 1. Key Resolution (`DbKeyHelper.cs`)
Encryption keys are resolved during bootstrap via [`DbKeyHelper.ResolveDbEncryptionKey(configuration)`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-53/Infrastructure/Secrets/DbKeyHelper.cs):
1. **Lookup Hierarchy**: Inspects `ROUTER_MASTER_KEY` first, falling back to `DB_ENCRYPTION_KEY`.
2. **Fail-Closed Security**: If both keys are missing or blank, startup terminates with a fatal `InvalidOperationException`. Self-generating ephemeral fallback keys is strictly disabled in production to prevent silent data loss upon container restart.
3. **Thread-Safe Caching**: Key resolution uses double-checked locking to cache the resolved string in memory, minimizing configuration lookups.

### 2. Symmetric Encryption Engine (`SymmetricEncryptionHelper.cs`)
* **Key Derivation**: Derives a 256-bit symmetric encryption key using `Rfc2898DeriveBytes.Pbkdf2` with **SHA-256**, **600,000 iterations**, and a domain-isolated salt (`{secretString}_McpRouter_Salt_v2`).
* **Authenticated Encryption**: Uses **AES-256-GCM** (`System.Security.Cryptography.AesGcm`) providing confidentiality, integrity, and authenticity.
* **Payload Structure**: Packed binary payload containing `[ 12-byte Nonce | 16-byte Auth Tag | N-byte Ciphertext ]`, encoded as Base64.
* **Transparent Decryption & Key Fallback**: If decryption with the primary master key encounters an authentication tag mismatch, the engine automatically attempts fallback decryption using the legacy `DB_ENCRYPTION_KEY` before failing gracefully.

### 3. Protected Columns (`EncryptedConfigJson`)
Sensitive provider settings are stored in dedicated encrypted columns:
* `SecretProviders.EncryptedConfigJson`: Stores HashiCorp Vault access tokens, namespace headers, and registry paths.
* `AuthProviderConfigs.EncryptedConfigJson`: Stores Active Directory service account passwords, OIDC client secrets, and token endpoint credentials.
* **Automatic Decryption on Read**: Data repositories automatically decrypt `EncryptedConfigJson` when populating `SecretProviderDto.ConfigJson` and `AuthProviderDto.ConfigJson`, ensuring application code operates on clean in-memory representations.

---

## 🛡️ Startup Schema Validation & Fail-Closed Integrity Checks

To prevent runtime data corruption or silent failures caused by misconfigured schemas, the gateway executes a comprehensive validation pass on every startup ([`DatabaseSeederService.ValidateSchemaCompatibility`](file:///containers/dev/csharp-mcp-router/.worktrees/issue-53/Infrastructure/Persistence/DatabaseSeederService.cs)):

```mermaid
sequenceDiagram
    autonumber
    participant App as MCP Router Startup
    participant Seeder as DatabaseSeederService
    participant DB as Target Database Engine

    App->>Seeder: SeedDatabase(services, config)
    Seeder->>DB: ApplyUpgradeMigrations() (SQLite/MSSQL/MySQL)
    Seeder->>DB: EnsureBaselineTables()
    Seeder->>DB: EnsureDefaultRows() (Settings, Providers)
    Seeder->>DB: ValidateSchemaCompatibility()
    
    rect rgb(240, 248, 255)
        Note over Seeder,DB: 1. Zero-row column validation across all 9 tables
        Seeder->>DB: SELECT [AllColumns] FROM Servers/Settings/AppKeys... WHERE 1=0
    end
    
    rect rgb(255, 250, 240)
        Note over Seeder,DB: 2. Engine-Specific Schema & Contract Validation
        alt SQLite
            Seeder->>DB: PRAGMA table_info checks (EncryptedConfigJson, OwnerSid)
        else MSSQL
            Seeder->>DB: Verify Tools.ServerId is VARCHAR(100)
            Seeder->>DB: Check 10 stored procedures in sys.procedures
            Seeder->>DB: Verify sp_SaveAppKey does NOT accept @CreatedAt
        else MySQL
            Seeder->>DB: Verify Tools.ServerId is VARCHAR(100)
            Seeder->>DB: Check 10 stored procedures in information_schema.routines
            Seeder->>DB: Verify p_ parameter prefix in information_schema.parameters
        end
    end

    alt Validation Passes
        Seeder-->>App: ✅ Schema validation passed, proceed with startup
    else Validation Fails
        Seeder-->>App: ❌ Throw InvalidOperationException (Fail-Closed)
    end
```

### Fail-Closed Error Handling
If any column, stored procedure, data type, or parameter convention is missing or invalid:
1. The gateway writes a `Critical` log entry detailing the exact failure and required migration script.
2. An `InvalidOperationException` is thrown, halting server startup immediately.
3. Traffic is not accepted until the underlying database schema is brought into full compliance.

---

## 🚀 Deployment & Configuration Matrix

### 1. SQLite Deployment (Default / Embedded)

#### Connection String Format
```ini
ConnectionStrings__DefaultConnection=Data Source=/app/data/mcp_router.db;
```

#### Docker Compose Template (`docker-compose.sqlite.yml`)
```yaml
version: '3.8'

services:
  mcp-router:
    image: ghcr.io/spelech/csharp-mcp-router:latest
    container_name: mcp-router-sqlite
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_PROVIDER=sqlite
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/mcp_router.db;
      - ROUTER_MASTER_KEY=base64_256bit_master_key_here_must_be_configured_at_rest==
      - CORS_ALLOWED_ORIGINS=https://mcp.yourdomain.com
      - Oidc__TrustedProxies=10.0.0.10,127.0.0.1
    volumes:
      - mcp_sqlite_data:/app/data
      - ./certs/oauth_signing.pfx:/app/certs/oauth_signing.pfx:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 3

volumes:
  mcp_sqlite_data:
    driver: local
```

---

### 2. Microsoft SQL Server Deployment (Enterprise)

#### Connection String Formats
```ini
# Standard SQL Authentication with TLS Encryption
ConnectionStrings__DefaultConnection=Server=tcp:sqlserver.internal,1433;Database=McpEnterpriseDb;User ID=mcp_app;Password=YourComplexPassword123!;Encrypt=True;TrustServerCertificate=False;

# Self-Signed Certificate / Internal Test Cluster
ConnectionStrings__DefaultConnection=Server=tcp:sqlserver.internal,1433;Database=McpEnterpriseDb;User ID=sa;Password=YourComplexPassword123!;TrustServerCertificate=True;
```

#### Schema Initialization Command
Execute the initialization scripts against your MSSQL instance prior to launching the router:
```bash
# 1. Initialize Tables & Baseline Indexes
sqlcmd -S sqlserver.internal -U sa -P "YourComplexPassword123!" -i scripts/db/mssql/01_tables.sql

# 2. Deploy Stored Procedures Suite
sqlcmd -S sqlserver.internal -U sa -P "YourComplexPassword123!" -i scripts/db/mssql/02_procedures.sql
```

#### Docker Compose Template (`docker-compose.mssql.yml`)
```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: mcp-sqlserver
    restart: unless-stopped
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourComplexPassword123!
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - mssql_data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourComplexPassword123!' -C -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5

  mcp-router:
    image: ghcr.io/spelech/csharp-mcp-router:latest
    container_name: mcp-router-mssql
    restart: unless-stopped
    depends_on:
      sqlserver:
        condition: service_healthy
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_PROVIDER=mssql
      - ConnectionStrings__DefaultConnection=Server=tcp:sqlserver,1433;Database=McpEnterpriseDb;User ID=sa;Password=YourComplexPassword123!;TrustServerCertificate=True;
      - ROUTER_MASTER_KEY=base64_256bit_master_key_here_must_be_configured_at_rest==
      - CORS_ALLOWED_ORIGINS=https://mcp.yourdomain.com
      - Oidc__TrustedProxies=10.0.0.10,127.0.0.1
    volumes:
      - ./certs/oauth_signing.pfx:/app/certs/oauth_signing.pfx:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 3

volumes:
  mssql_data:
    driver: local
```

---

### 3. MySQL / MariaDB Deployment (Cloud / Linux Stack)

#### Connection String Format
```ini
ConnectionStrings__DefaultConnection=Server=mysql.internal;Port=3306;Database=McpEnterpriseDb;Uid=mcp_app;Pwd=YourComplexPassword123!;AllowUserVariables=True;
```

#### Schema Initialization Command
Execute the initialization scripts against your MySQL / MariaDB instance:
```bash
# 1. Initialize Tables & Baseline Indexes
mysql -h mysql.internal -u root -p"YourComplexPassword123!" < scripts/db/mysql/01_tables.sql

# 2. Deploy Stored Procedures Suite
mysql -h mysql.internal -u root -p"YourComplexPassword123!" < scripts/db/mysql/02_procedures.sql
```

#### Docker Compose Template (`docker-compose.mysql.yml`)
```yaml
version: '3.8'

services:
  mysql:
    image: mysql:8.4
    container_name: mcp-mysql
    restart: unless-stopped
    command: --default-authentication-plugin=mysql_native_password
    environment:
      - MYSQL_ROOT_PASSWORD=YourComplexPassword123!
      - MYSQL_DATABASE=McpEnterpriseDb
      - MYSQL_USER=mcp_app
      - MYSQL_PASSWORD=YourComplexPassword123!
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql
      - ./scripts/db/mysql/01_tables.sql:/docker-entrypoint-initdb.d/01_tables.sql:ro
      - ./scripts/db/mysql/02_procedures.sql:/docker-entrypoint-initdb.d/02_procedures.sql:ro
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u", "root", "-pYourComplexPassword123!"]
      interval: 10s
      timeout: 5s
      retries: 5

  mcp-router:
    image: ghcr.io/spelech/csharp-mcp-router:latest
    container_name: mcp-router-mysql
    restart: unless-stopped
    depends_on:
      mysql:
        condition: service_healthy
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_PROVIDER=mysql
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=McpEnterpriseDb;Uid=mcp_app;Pwd=YourComplexPassword123!;AllowUserVariables=True;
      - ROUTER_MASTER_KEY=base64_256bit_master_key_here_must_be_configured_at_rest==
      - CORS_ALLOWED_ORIGINS=https://mcp.yourdomain.com
      - Oidc__TrustedProxies=10.0.0.10,127.0.0.1
    volumes:
      - ./certs/oauth_signing.pfx:/app/certs/oauth_signing.pfx:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 3

volumes:
  mysql_data:
    driver: local
```

---

## 🔗 Related Documentation & References

* [Production Deployment & Database Migration Guide](../DEPLOY.md)
* [System Architecture & Dependency Injection](../ARCHITECTURE.md)
* [Pairwise Testing Matrix & Integration Suites](testing-matrix.md)
* [Pluggable Secret Providers & Server Management](user-guide/02-server-management-and-secrets.md)
* [Features & Connection Guidelines](features-guide.md)
