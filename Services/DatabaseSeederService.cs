using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpRouter.Core.Database;
using McpRouter.Core.Secrets;
using McpRouter.Models;
using McpRouter.Services.DatabaseSeeders;
using Dapper;

namespace McpRouter.Services
{
    public class JsonListTypeHandler : SqlMapper.TypeHandler<List<string>>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, List<string>? value)
        {
            parameter.Value = System.Text.Json.JsonSerializer.Serialize(value ?? new List<string>());
        }

        public override List<string> Parse(object value)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(str) ?? new List<string>(); }
                catch { }
            }
            return new List<string>();
        }
    }

    public static class DatabaseSeederService
    {
        public static void SeedDatabase(this WebApplication app)
        {
            SeedDatabase(app.Services, app.Configuration);
        }

        public static void SeedDatabase(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var provider = dbFactory.ProviderName.ToLower();
            
            try
            {
                logger.LogInformation("Initializing database via Dapper ({Provider})...", provider);
                SqlMapper.AddTypeHandler(new JsonListTypeHandler());

                var encryptionKey = configuration["DB_ENCRYPTION_KEY"];
                if (string.IsNullOrEmpty(encryptionKey))
                {
                    logger.LogInformation("No DB_ENCRYPTION_KEY provided in configuration. A unique key has been resolved.");
                }
                else if (encryptionKey.Length < 16)
                {
                    logger.LogCritical("SECURITY WARNING: The configured DB_ENCRYPTION_KEY is too short (< 16 characters).");
                }

                using var conn = dbFactory.CreateConnection();

                if (provider == "sqlite")
                {
                    // Create SQLite tables (standard SQLite SQL syntax)
                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS Servers (
                            Id TEXT PRIMARY KEY,
                            DisplayName TEXT,
                            Url TEXT,
                            Enabled INTEGER DEFAULT 1,
                            Hidden INTEGER DEFAULT 0,
                            Type TEXT DEFAULT 'sse',
                            SecretProvider TEXT DEFAULT 'None',
                            SecretItemKey TEXT,
                            SecretMount TEXT,
                            SecretPath TEXT,
                            SecretField TEXT,
                            AuthShape TEXT DEFAULT 'bearer',
                            CustomHeaderName TEXT,
                            Categories TEXT DEFAULT '[]',
                            ApiKey TEXT,
                            HeadersJson TEXT,
                            AutoDiscovered INTEGER DEFAULT 0
                        );
                    ");

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS Settings (
                            Id TEXT PRIMARY KEY,
                            EmbeddingProvider TEXT,
                            EmbeddingApiUrl TEXT,
                            EmbeddingApiKey TEXT,
                            EmbeddingApiModel TEXT,
                            EmbeddingModelDir TEXT,
                            RequireManualApproval INTEGER DEFAULT 0,
                            GlobalMaxKeys INTEGER DEFAULT 100,
                            UserMaxKeys INTEGER DEFAULT 5
                        );
                    ");

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS AppKeys (
                            Id TEXT PRIMARY KEY,
                            Name TEXT,
                            Username TEXT,
                            OwnerSid TEXT DEFAULT '',
                            KeyPrefix TEXT,
                            EncryptedKey TEXT,
                            ScopesJson TEXT DEFAULT '[]',
                            ExpiresAt TEXT,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                        );
                    ");

                    try { conn.Execute("ALTER TABLE Settings ADD COLUMN RequireManualApproval INTEGER DEFAULT 0"); } catch {}
                    try { conn.Execute("ALTER TABLE Settings ADD COLUMN GlobalMaxKeys INTEGER DEFAULT 100"); } catch {}
                    try { conn.Execute("ALTER TABLE Settings ADD COLUMN UserMaxKeys INTEGER DEFAULT 5"); } catch {}
                    try { conn.Execute("ALTER TABLE AppKeys ADD COLUMN OwnerSid TEXT DEFAULT ''"); } catch {}

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS AccessPolicies (
                            Id TEXT PRIMARY KEY,
                            TargetId TEXT,
                            RequiredGroup TEXT,
                            IsAllowed INTEGER DEFAULT 1
                        );
                    ");

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS GroupMappings (
                            Id TEXT PRIMARY KEY,
                            ExternalId TEXT,
                            InternalGroup TEXT
                        );
                    ");

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS AuditLogs (
                            RequestId TEXT PRIMARY KEY,
                            UserPrincipalName TEXT,
                            UserSid TEXT,
                            ServerCodeName TEXT,
                            ItemName TEXT,
                            RequestMethod TEXT,
                            ExecutionTimeMs INTEGER,
                            StatusCode INTEGER,
                            RequestPayload TEXT,
                            ResponsePayload TEXT,
                            ErrorMessage TEXT,
                            Timestamp TEXT DEFAULT CURRENT_TIMESTAMP
                        );
                    ");

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS AdminAuditLogs (
                            Id TEXT PRIMARY KEY,
                            Username TEXT,
                            Action TEXT,
                            Target TEXT,
                            Details TEXT,
                            Success INTEGER,
                            ErrorMessage TEXT,
                            Timestamp TEXT DEFAULT CURRENT_TIMESTAMP
                        );
                    ");

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS SecretProviders (
                            ProviderName TEXT PRIMARY KEY,
                            DisplayName TEXT,
                            EncryptedConfigJson TEXT,
                            IsEnabled INTEGER DEFAULT 1
                        );
                    ");

                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS AuthProviderConfigs (
                            ProviderName TEXT PRIMARY KEY,
                            DisplayName TEXT,
                            UserHeader TEXT,
                            GroupsHeader TEXT,
                            ConfigJson TEXT,
                            IsEnabled INTEGER DEFAULT 1
                        );
                    ");

                    // Safe alters for legacy columns
                    var colDefs = new (string Name, string Ddl)[]
                    {
                        ("SecretProvider", "ALTER TABLE Servers ADD COLUMN SecretProvider TEXT DEFAULT 'None'"),
                        ("SecretItemKey", "ALTER TABLE Servers ADD COLUMN SecretItemKey TEXT NULL"),
                        ("SecretMount", "ALTER TABLE Servers ADD COLUMN SecretMount TEXT NULL"),
                        ("SecretPath", "ALTER TABLE Servers ADD COLUMN SecretPath TEXT NULL"),
                        ("SecretField", "ALTER TABLE Servers ADD COLUMN SecretField TEXT NULL"),
                        ("AuthShape", "ALTER TABLE Servers ADD COLUMN AuthShape TEXT DEFAULT 'bearer'"),
                        ("CustomHeaderName", "ALTER TABLE Servers ADD COLUMN CustomHeaderName TEXT NULL")
                    };

                    foreach (var (colName, ddl) in colDefs)
                    {
                        try
                        {
                            conn.Execute(ddl);
                        }
                        catch {}
                    }
                }
                else if (provider == "mssql")
                {
                    // Create SQL Server tables if they do not exist
                    conn.Execute(@"
                        IF OBJECT_ID('dbo.Servers', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[Servers] (
                                [Id]                VARCHAR(100) PRIMARY KEY,
                                [DisplayName]       NVARCHAR(200) NOT NULL,
                                [Url]               VARCHAR(500) NOT NULL,
                                [Enabled]           BIT NOT NULL DEFAULT 1,
                                [Hidden]            BIT NOT NULL DEFAULT 0,
                                [Type]              VARCHAR(20) NOT NULL DEFAULT 'sse',
                                [SecretProvider]    VARCHAR(50) NOT NULL DEFAULT 'None',
                                [SecretItemKey]     VARCHAR(100) NULL,
                                [SecretMount]       VARCHAR(100) NULL,
                                [SecretPath]        VARCHAR(250) NULL,
                                [SecretField]       VARCHAR(100) NULL,
                                [AuthShape]         VARCHAR(20) NOT NULL DEFAULT 'bearer',
                                [CustomHeaderName]  VARCHAR(100) NULL,
                                [Categories]        NVARCHAR(MAX) NOT NULL DEFAULT '[]',
                                [ApiKey]            NVARCHAR(MAX) NULL,
                                [HeadersJson]       NVARCHAR(MAX) NULL,
                                [AutoDiscovered]    BIT NOT NULL DEFAULT 0
                            );
                        END;

                        IF OBJECT_ID('dbo.Settings', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[Settings] (
                                [Id]                      VARCHAR(50) PRIMARY KEY,
                                [EmbeddingProvider]       VARCHAR(50) NULL,
                                [EmbeddingApiUrl]         VARCHAR(500) NULL,
                                [EmbeddingApiKey]         NVARCHAR(MAX) NULL,
                                [EmbeddingApiModel]       VARCHAR(100) NULL,
                                [EmbeddingModelDir]       VARCHAR(500) NULL,
                                [RequireManualApproval]   BIT NOT NULL DEFAULT 0,
                                [GlobalMaxKeys]           INT NOT NULL DEFAULT 100,
                                [UserMaxKeys]             INT NOT NULL DEFAULT 5
                            );
                        END;

                        IF OBJECT_ID('dbo.AppKeys', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[AppKeys] (
                                [Id]           VARCHAR(100) PRIMARY KEY,
                                [Name]         NVARCHAR(200) NOT NULL,
                                [Username]     NVARCHAR(256) NOT NULL,
                                [OwnerSid]     NVARCHAR(200) NOT NULL DEFAULT '',
                                [KeyPrefix]    VARCHAR(50) NOT NULL,
                                [EncryptedKey] NVARCHAR(MAX) NOT NULL,
                                [ScopesJson]   NVARCHAR(MAX) NOT NULL DEFAULT '[]',
                                [ExpiresAt]    DATETIME2 NULL,
                                [CreatedAt]    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                            );
                        END;

                        IF OBJECT_ID('dbo.AccessPolicies', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[AccessPolicies] (
                                [Id]            VARCHAR(100) PRIMARY KEY,
                                [TargetId]      VARCHAR(250) NOT NULL,
                                [RequiredGroup] NVARCHAR(256) NOT NULL,
                                [IsAllowed]     BIT NOT NULL DEFAULT 1,
                                [CreatedAt]     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                            );
                        END;

                        IF OBJECT_ID('dbo.GroupMappings', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[GroupMappings] (
                                [Id]             VARCHAR(100) PRIMARY KEY,
                                [ExternalId]     VARCHAR(256) NOT NULL,
                                [InternalGroup]  NVARCHAR(256) NOT NULL,
                                [CreatedAt]      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                            );
                        END;

                        IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[AuditLogs] (
                                [AuditId]           BIGINT IDENTITY(1,1) PRIMARY KEY,
                                [RequestId]         VARCHAR(64) NOT NULL,
                                [UserPrincipalName] NVARCHAR(256) NOT NULL,
                                [UserSid]           VARCHAR(180) NOT NULL,
                                [ServerCodeName]    VARCHAR(100) NOT NULL,
                                [ItemName]          VARCHAR(150) NULL,
                                [RequestMethod]     VARCHAR(50) NOT NULL,
                                [ExecutionTimeMs]   INT NOT NULL,
                                [StatusCode]        INT NOT NULL,
                                [RequestPayload]    NVARCHAR(MAX) NULL,
                                [ResponsePayload]   NVARCHAR(MAX) NULL,
                                [ErrorMessage]      NVARCHAR(MAX) NULL,
                                [Timestamp]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                            );
                        END;

                        IF OBJECT_ID('dbo.AdminAuditLogs', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[AdminAuditLogs] (
                                [Id]           VARCHAR(50) NOT NULL PRIMARY KEY,
                                [Username]     NVARCHAR(256) NOT NULL,
                                [Action]       VARCHAR(100) NOT NULL,
                                [Target]       VARCHAR(256) NOT NULL,
                                [Details]      NVARCHAR(MAX) NULL,
                                [Success]      BIT NOT NULL,
                                [ErrorMessage] NVARCHAR(MAX) NULL,
                                [Timestamp]    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                            );
                        END;

                        IF OBJECT_ID('dbo.SecretProviders', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[SecretProviders] (
                                [ProviderId]          INT IDENTITY(1,1) PRIMARY KEY,
                                [ProviderName]        VARCHAR(50) NOT NULL UNIQUE,
                                [DisplayName]         NVARCHAR(100) NOT NULL,
                                [EncryptedConfigJson] NVARCHAR(MAX) NULL,
                                [IsEnabled]           BIT NOT NULL DEFAULT 1,
                                [UpdatedAt]           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                            );
                        END;

                        IF OBJECT_ID('dbo.AuthProviderConfigs', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[AuthProviderConfigs] (
                                [AuthId]              INT IDENTITY(1,1) PRIMARY KEY,
                                [ProviderName]        VARCHAR(50) NOT NULL UNIQUE,
                                [DisplayName]         NVARCHAR(100) NOT NULL,
                                [UserHeader]          VARCHAR(100) NULL DEFAULT 'Remote-User',
                                [GroupsHeader]        VARCHAR(100) NULL DEFAULT 'Remote-Groups',
                                [ConfigJson]          NVARCHAR(MAX) NULL,
                                [IsEnabled]           BIT NOT NULL DEFAULT 1,
                                [UpdatedAt]           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                            );
                        END;
                    ");
                }
                else if (provider == "mysql")
                {
                    // Create MySQL tables if they do not exist
                    conn.Execute(@"
                        CREATE TABLE IF NOT EXISTS `Servers` (
                            `Id`                VARCHAR(100) PRIMARY KEY,
                            `DisplayName`       VARCHAR(200) NOT NULL,
                            `Url`               VARCHAR(500) NOT NULL,
                            `Enabled`           TINYINT(1) NOT NULL DEFAULT 1,
                            `Hidden`            TINYINT(1) NOT NULL DEFAULT 0,
                            `Type`              VARCHAR(20) NOT NULL DEFAULT 'sse',
                            `SecretProvider`    VARCHAR(50) NOT NULL DEFAULT 'None',
                            `SecretItemKey`     VARCHAR(100) NULL,
                            `SecretMount`       VARCHAR(100) NULL,
                            `SecretPath`        VARCHAR(250) NULL,
                            `SecretField`       VARCHAR(100) NULL,
                            `AuthShape`         VARCHAR(20) NOT NULL DEFAULT 'bearer',
                            `CustomHeaderName`  VARCHAR(100) NULL,
                            `Categories`        LONGTEXT NOT NULL,
                            `ApiKey`            LONGTEXT NULL,
                            `HeadersJson`       LONGTEXT NULL,
                            `AutoDiscovered`    TINYINT(1) NOT NULL DEFAULT 0
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `Settings` (
                            `Id`                      VARCHAR(50) PRIMARY KEY,
                            `EmbeddingProvider`       VARCHAR(50) NULL,
                            `EmbeddingApiUrl`         VARCHAR(500) NULL,
                            `EmbeddingApiKey`         LONGTEXT NULL,
                            `EmbeddingApiModel`       VARCHAR(100) NULL,
                            `EmbeddingModelDir`       VARCHAR(500) NULL,
                            `RequireManualApproval`   TINYINT(1) NOT NULL DEFAULT 0,
                            `GlobalMaxKeys`           INT NOT NULL DEFAULT 100,
                            `UserMaxKeys`             INT NOT NULL DEFAULT 5
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `AppKeys` (
                            `Id`           VARCHAR(100) PRIMARY KEY,
                            `Name`         VARCHAR(200) NOT NULL,
                            `Username`     VARCHAR(256) NOT NULL,
                            `OwnerSid`     VARCHAR(200) NOT NULL DEFAULT '',
                            `KeyPrefix`    VARCHAR(50) NOT NULL,
                            `EncryptedKey` LONGTEXT NOT NULL,
                            `ScopesJson`   LONGTEXT NOT NULL,
                            `ExpiresAt`    DATETIME NULL,
                            `CreatedAt`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `AccessPolicies` (
                            `Id`            VARCHAR(100) PRIMARY KEY,
                            `TargetId`      VARCHAR(250) NOT NULL,
                            `RequiredGroup` VARCHAR(256) NOT NULL,
                            `IsAllowed`     TINYINT(1) NOT NULL DEFAULT 1,
                            `CreatedAt`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `GroupMappings` (
                            `Id`             VARCHAR(100) PRIMARY KEY,
                            `ExternalId`     VARCHAR(256) NOT NULL,
                            `InternalGroup`  VARCHAR(256) NOT NULL,
                            `CreatedAt`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `AuditLogs` (
                            `AuditId`           BIGINT AUTO_INCREMENT PRIMARY KEY,
                            `RequestId`         VARCHAR(64) NOT NULL,
                            `UserPrincipalName` VARCHAR(256) NOT NULL,
                            `UserSid`           VARCHAR(180) NOT NULL,
                            `ServerCodeName`    VARCHAR(100) NOT NULL,
                            `ItemName`          VARCHAR(150) NULL,
                            `RequestMethod`     VARCHAR(50) NOT NULL,
                            `ExecutionTimeMs`   INT NOT NULL,
                            `StatusCode`        INT NOT NULL,
                            `RequestPayload`    LONGTEXT NULL,
                            `ResponsePayload`   LONGTEXT NULL,
                            `ErrorMessage`      LONGTEXT NULL,
                            `Timestamp`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `AdminAuditLogs` (
                            `Id`           VARCHAR(50) NOT NULL PRIMARY KEY,
                            `Username`     VARCHAR(256) NOT NULL,
                            `Action`       VARCHAR(100) NOT NULL,
                            `Target`       VARCHAR(256) NOT NULL,
                            `Details`      LONGTEXT NULL,
                            `Success`      TINYINT(1) NOT NULL,
                            `ErrorMessage` LONGTEXT NULL,
                            `Timestamp`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `SecretProviders` (
                            `ProviderId`          INT AUTO_INCREMENT PRIMARY KEY,
                            `ProviderName`        VARCHAR(50) NOT NULL UNIQUE,
                            `DisplayName`         VARCHAR(100) NOT NULL,
                            `EncryptedConfigJson` LONGTEXT NULL,
                            `IsEnabled`           TINYINT(1) NOT NULL DEFAULT 1,
                            `UpdatedAt`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `AuthProviderConfigs` (
                            `AuthId`              INT AUTO_INCREMENT PRIMARY KEY,
                            `ProviderName`        VARCHAR(50) NOT NULL UNIQUE,
                            `DisplayName`         VARCHAR(100) NOT NULL,
                            `UserHeader`          VARCHAR(100) NULL DEFAULT 'Remote-User',
                            `GroupsHeader`        VARCHAR(100) NULL DEFAULT 'Remote-Groups',
                            `ConfigJson`          LONGTEXT NULL,
                            `IsEnabled`           TINYINT(1) NOT NULL DEFAULT 1,
                            `UpdatedAt`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                    ");
                }

                // Check settings and default rows
                var countSettings = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Settings;");
                if (countSettings == 0)
                {
                    conn.Execute("INSERT INTO Settings (Id, RequireManualApproval, GlobalMaxKeys, UserMaxKeys) VALUES ('default', 0, 100, 5);");
                }

                // Populate default SecretProviders safely and provider-agnostically
                var secretProviders = new[]
                {
                    new { ProviderName = "Vault", DisplayName = "HashiCorp Vault (KV v2)", IsEnabled = 1 },
                    new { ProviderName = "WindowsRegistry", DisplayName = "Windows Registry (DPAPI)", IsEnabled = 1 },
                    new { ProviderName = "Environment", DisplayName = "Container Environment", IsEnabled = 1 }
                };

                foreach (var sp in secretProviders)
                {
                    var exists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM SecretProviders WHERE ProviderName = @ProviderName", new { sp.ProviderName });
                    if (exists == 0)
                    {
                        conn.Execute("INSERT INTO SecretProviders (ProviderName, DisplayName, IsEnabled) VALUES (@ProviderName, @DisplayName, @IsEnabled)", sp);
                    }
                }

                // Populate default AuthProviderConfigs safely and provider-agnostically
                var authProviders = new[]
                {
                    new { ProviderName = "ActiveDirectory", DisplayName = "Active Directory", UserHeader = "Remote-User", GroupsHeader = "Remote-Groups", IsEnabled = 1 },
                    new { ProviderName = "HeaderAuth", DisplayName = "Configurable Reverse Proxy Header Auth", UserHeader = "Remote-User", GroupsHeader = "Remote-Groups", IsEnabled = 1 }
                };

                foreach (var ap in authProviders)
                {
                    var exists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM AuthProviderConfigs WHERE ProviderName = @ProviderName", new { ap.ProviderName });
                    if (exists == 0)
                    {
                        conn.Execute("INSERT INTO AuthProviderConfigs (ProviderName, DisplayName, UserHeader, GroupsHeader, IsEnabled) VALUES (@ProviderName, @DisplayName, @UserHeader, @GroupsHeader, @IsEnabled)", ap);
                    }
                }

                // Call validation check to ensure 100% schema compliance
                ValidateSchemaCompatibility(conn, provider, logger);

                ClientAppKeySeeder.SeedDefaultClientsAndKeys(dbFactory, logger, configuration);

                try
                {
                    var embeddingSvc = scope.ServiceProvider.GetRequiredService<DynamicEmbeddingService>();
                    Task.Run(async () => await embeddingSvc.PreWarmAsync());
                }
                catch (Exception exPrewarm)
                {
                    logger.LogWarning(exPrewarm, "Pre-warm background trigger warning");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize or migrate database.");
            }

            try
            {
                CatalogDatabaseSeeder.SeedCatalogServers(dbFactory, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed catalog servers.");
            }
        }

        private static void ValidateSchemaCompatibility(System.Data.IDbConnection conn, string provider, ILogger logger)
        {
            logger.LogInformation("Running database schema compatibility validation pass...");
            var tablesToCheck = new Dictionary<string, string>
            {
                { "Servers", "SELECT Id, DisplayName, Url, Enabled, Hidden, Type, SecretProvider, SecretItemKey, SecretMount, SecretPath, SecretField, AuthShape, CustomHeaderName, Categories, ApiKey, HeadersJson, AutoDiscovered FROM Servers WHERE 1=0" },
                { "Settings", "SELECT Id, EmbeddingProvider, EmbeddingApiUrl, EmbeddingApiKey, EmbeddingApiModel, EmbeddingModelDir, RequireManualApproval, GlobalMaxKeys, UserMaxKeys FROM Settings WHERE 1=0" },
                { "AppKeys", "SELECT Id, Name, Username, OwnerSid, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt FROM AppKeys WHERE 1=0" },
                { "AccessPolicies", "SELECT Id, TargetId, RequiredGroup, IsAllowed FROM AccessPolicies WHERE 1=0" },
                { "GroupMappings", "SELECT Id, ExternalId, InternalGroup FROM GroupMappings WHERE 1=0" },
                { "AuditLogs", "SELECT RequestId, UserPrincipalName, UserSid, ServerCodeName, ItemName, RequestMethod, ExecutionTimeMs, StatusCode, RequestPayload, ResponsePayload, ErrorMessage, Timestamp FROM AuditLogs WHERE 1=0" },
                { "AdminAuditLogs", "SELECT Id, Username, Action, Target, Details, Success, ErrorMessage, Timestamp FROM AdminAuditLogs WHERE 1=0" },
                { "SecretProviders", "SELECT ProviderName, DisplayName, EncryptedConfigJson, IsEnabled FROM SecretProviders WHERE 1=0" },
                { "AuthProviderConfigs", "SELECT ProviderName, DisplayName, UserHeader, GroupsHeader, ConfigJson, IsEnabled FROM AuthProviderConfigs WHERE 1=0" }
            };

            foreach (var table in tablesToCheck)
            {
                try
                {
                    conn.Execute(table.Value);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Database schema compatibility check failed on table '{table.Key}'. Required column(s) may be missing, mismatched, or the table is defined using a legacy schema (e.g. McpServers instead of Servers). Please apply the migration scripts located in 'scripts/db/{provider}/' or recreate the table. Error detail: {ex.Message}";
                    logger.LogCritical(ex, "{ErrorMessage}", errorMsg);
                    throw new InvalidOperationException(errorMsg, ex);
                }
            }
            logger.LogInformation("Database schema compatibility check passed successfully.");
        }
    }
}
