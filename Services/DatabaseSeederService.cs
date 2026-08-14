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
            
            try
            {
                logger.LogInformation("Initializing database via Dapper...");
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

                // Create Servers table
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

                // Ensure the Settings table exists and has a default row
                try
                {
                    conn.Execute(
                        "CREATE TABLE IF NOT EXISTS Settings (" +
                        "Id TEXT PRIMARY KEY, " +
                        "EmbeddingProvider TEXT, " +
                        "EmbeddingApiUrl TEXT, " +
                        "EmbeddingApiKey TEXT, " +
                        "EmbeddingApiModel TEXT, " +
                        "EmbeddingModelDir TEXT, " +
                        "RequireManualApproval INTEGER DEFAULT 0, " +
                        "GlobalMaxKeys INTEGER DEFAULT 100, " +
                        "UserMaxKeys INTEGER DEFAULT 5)");

                    try
                    {
                        conn.Execute("ALTER TABLE Settings ADD COLUMN RequireManualApproval INTEGER DEFAULT 0");
                    }
                    catch (Exception exReqApproval) { logger.LogDebug(exReqApproval, "Settings.RequireManualApproval DDL notice: {Message}", exReqApproval.Message); }

                    try
                    {
                        conn.Execute("ALTER TABLE Settings ADD COLUMN GlobalMaxKeys INTEGER DEFAULT 100");
                    }
                    catch (Exception exGlobalMax) { logger.LogDebug(exGlobalMax, "Settings.GlobalMaxKeys DDL notice: {Message}", exGlobalMax.Message); }

                    try
                    {
                        conn.Execute("ALTER TABLE Settings ADD COLUMN UserMaxKeys INTEGER DEFAULT 5");
                    }
                    catch (Exception exUserMax) { logger.LogDebug(exUserMax, "Settings.UserMaxKeys DDL notice: {Message}", exUserMax.Message); }

                    try
                    {
                        conn.Execute(
                            "CREATE TABLE IF NOT EXISTS AppKeys (" +
                            "Id TEXT PRIMARY KEY, " +
                            "Name TEXT, " +
                            "Username TEXT, " +
                            "OwnerSid TEXT DEFAULT '', " +
                            "KeyPrefix TEXT, " +
                            "EncryptedKey TEXT, " +
                            "ScopesJson TEXT DEFAULT '[]', " +
                            "ExpiresAt TEXT, " +
                            "CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP)");
                    }
                    catch (Exception exAppKeys)
                    {
                        logger.LogWarning(exAppKeys, "AppKeys table init warning");
                    }

                    // Idempotent migration: attribute app-key calls to their owner's SID on pre-existing databases.
                    try { conn.Execute("ALTER TABLE AppKeys ADD COLUMN OwnerSid TEXT DEFAULT ''"); }
                    catch (Exception exOwnerSid) { logger.LogDebug(exOwnerSid, "AppKeys.OwnerSid DDL notice: {Message}", exOwnerSid.Message); }

                    // Idempotent migration: unique index on KeyPrefix
                    try { conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_AppKeys_KeyPrefix ON AppKeys (KeyPrefix);"); }
                    catch (Exception exIndex) { logger.LogDebug(exIndex, "AppKeys.KeyPrefix index notice: {Message}", exIndex.Message); }

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
                            logger.LogInformation("Successfully added column {Column} to Servers table.", colName);
                        }
                        catch (Exception exAlter)
                        {
                            logger.LogInformation("Column {Column} DDL notice: {Message}", colName, exAlter.Message);
                        }
                    }

                    try
                    {
                        conn.Execute(
                            "UPDATE Servers SET AuthShape = 'bearer' " +
                            "WHERE AuthShape IS NULL OR AuthShape = '';");
                    }
                    catch (Exception exAuthShapeBackfill) { logger.LogDebug(exAuthShapeBackfill, "Servers.AuthShape backfill notice: {Message}", exAuthShapeBackfill.Message); }

                    // Create AccessPolicies table for SQLite
                    try
                    {
                        conn.Execute(
                            "CREATE TABLE IF NOT EXISTS AccessPolicies (" +
                            "Id TEXT PRIMARY KEY, " +
                            "TargetId TEXT, " +
                            "RequiredGroup TEXT, " +
                            "IsAllowed INTEGER DEFAULT 1)");
                    }
                    catch (Exception exPolicies)
                    {
                        logger.LogWarning(exPolicies, "AccessPolicies table init warning");
                    }

                    // Create AuditLogs and AdminAuditLogs tables for SQLite
                    try
                    {
                        conn.Execute(
                            "CREATE TABLE IF NOT EXISTS AuditLogs (" +
                            "RequestId TEXT PRIMARY KEY, " +
                            "UserPrincipalName TEXT, " +
                            "UserSid TEXT, " +
                            "ServerCodeName TEXT, " +
                            "ItemName TEXT, " +
                            "RequestMethod TEXT, " +
                            "ExecutionTimeMs INTEGER, " +
                            "StatusCode INTEGER, " +
                            "RequestPayload TEXT, " +
                            "ResponsePayload TEXT, " +
                            "ErrorMessage TEXT, " +
                            "Timestamp TEXT DEFAULT CURRENT_TIMESTAMP)");

                        conn.Execute(
                            "CREATE TABLE IF NOT EXISTS AdminAuditLogs (" +
                            "Id TEXT PRIMARY KEY, " +
                            "Username TEXT, " +
                            "Action TEXT, " +
                            "Target TEXT, " +
                            "Details TEXT, " +
                            "Success INTEGER, " +
                            "ErrorMessage TEXT, " +
                            "Timestamp TEXT DEFAULT CURRENT_TIMESTAMP)");
                    }
                    catch (Exception exAudit)
                    {
                        logger.LogWarning(exAudit, "Audit tables init warning");
                    }

                    // Create GroupMappings table for SQLite
                    try
                    {
                        conn.Execute(
                            "CREATE TABLE IF NOT EXISTS GroupMappings (" +
                            "Id TEXT PRIMARY KEY, " +
                            "ExternalId TEXT, " +
                            "InternalGroup TEXT)");
                    }
                    catch (Exception exMappings)
                    {
                        logger.LogWarning(exMappings, "GroupMappings table init warning");
                    }

                    // Create SecretProviders and AuthProviderConfigs tables for SQLite
                    try
                    {
                        conn.Execute(
                            "CREATE TABLE IF NOT EXISTS SecretProviders (" +
                            "ProviderName TEXT PRIMARY KEY, " +
                            "DisplayName TEXT, " +
                            "ConfigJson TEXT, " +
                            "IsEnabled INTEGER DEFAULT 1)");

                        conn.Execute(
                            "INSERT OR IGNORE INTO SecretProviders (ProviderName, DisplayName, IsEnabled) VALUES " +
                            "('Vault', 'HashiCorp Vault (KV v2)', 1), " +
                            "('WindowsRegistry', 'Windows Registry (DPAPI)', 1), " +
                            "('Environment', 'Container Environment', 1);");

                        conn.Execute(
                            "CREATE TABLE IF NOT EXISTS AuthProviderConfigs (" +
                            "ProviderName TEXT PRIMARY KEY, " +
                            "DisplayName TEXT, " +
                            "UserHeader TEXT, " +
                            "GroupsHeader TEXT, " +
                            "ConfigJson TEXT, " +
                            "IsEnabled INTEGER DEFAULT 1)");

                        try
                        {
                            conn.Execute("ALTER TABLE AuthProviderConfigs ADD COLUMN ConfigJson TEXT NULL");
                        }
                        catch {}

                        try
                        {
                            conn.Execute("ALTER TABLE SecretProviders ADD COLUMN ConfigJson TEXT NULL");
                        }
                        catch {}

                        conn.Execute(
                            "INSERT OR IGNORE INTO AuthProviderConfigs (ProviderName, DisplayName, UserHeader, GroupsHeader, IsEnabled) VALUES " +
                            "('ActiveDirectory', 'Active Directory', 'Remote-User', 'Remote-Groups', 1), " +
                            "('HeaderAuth', 'Configurable Reverse Proxy Header Auth', 'Remote-User', 'Remote-Groups', 1);");
                    }
                    catch (Exception exSecret)
                    {
                        logger.LogWarning(exSecret, "Secret/Auth provider table init warning");
                    }

                    var countSettings = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Settings");
                    if (countSettings == 0)
                    {
                        conn.Execute("INSERT INTO Settings (Id, RequireManualApproval, GlobalMaxKeys, UserMaxKeys) VALUES ('default', 0, 100, 5)");
                    }

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
                    logger.LogError(ex, "Failed to create or seed Settings table");
                }
                
                CatalogDatabaseSeeder.SeedCatalogServers(dbFactory, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize database.");
            }
        }
    }
}
