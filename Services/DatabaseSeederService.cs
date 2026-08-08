using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpRouter.Core.Secrets;
using McpRouter.Models;
using McpRouter.Services.DatabaseSeeders;

namespace McpRouter.Services
{
    public static class DatabaseSeederService
    {
        public static void SeedDatabase(this WebApplication app)
        {
            SeedDatabase(app.Services, app.Configuration);
        }

        public static void SeedDatabase(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("Initializing database...");

                var encryptionKey = configuration["DB_ENCRYPTION_KEY"];
                if (string.IsNullOrEmpty(encryptionKey))
                {
                    logger.LogInformation("No DB_ENCRYPTION_KEY provided in configuration. A unique, cryptographically secure key has been generated and persisted in the data directory.");
                }
                else if (encryptionKey.Length < 16)
                {
                    logger.LogCritical("SECURITY WARNING: The configured DB_ENCRYPTION_KEY is too short (< 16 characters). Please set a strong, high-entropy DB_ENCRYPTION_KEY environment variable to secure your deployment!");
                }

                db.Database.EnsureCreated();

                // Ensure the Settings table exists and has a default row
                try
                {
                    db.Database.ExecuteSqlRaw(
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
                        db.Database.ExecuteSqlRaw("ALTER TABLE Settings ADD COLUMN RequireManualApproval INTEGER DEFAULT 0");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Settings ADD COLUMN GlobalMaxKeys INTEGER DEFAULT 100");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Settings ADD COLUMN UserMaxKeys INTEGER DEFAULT 5");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw(
                            "CREATE TABLE IF NOT EXISTS AppKeys (" +
                            "Id TEXT PRIMARY KEY, " +
                            "Name TEXT, " +
                            "Username TEXT, " +
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

                    string[] serverColumns = new[]
                    {
                        "ALTER TABLE Servers ADD COLUMN SecretProvider TEXT DEFAULT 'None'",
                        "ALTER TABLE Servers ADD COLUMN SecretItemKey TEXT NULL",
                        "ALTER TABLE Servers ADD COLUMN SecretMount TEXT NULL",
                        "ALTER TABLE Servers ADD COLUMN SecretPath TEXT NULL",
                        "ALTER TABLE Servers ADD COLUMN SecretField TEXT NULL",
                        "ALTER TABLE Servers ADD COLUMN AuthShape TEXT DEFAULT 'bearer'",
                        "ALTER TABLE Servers ADD COLUMN CustomHeaderName TEXT NULL"
                    };

                    foreach (var ddl in serverColumns)
                    {
                        try
                        {
                            db.Database.ExecuteSqlRaw(ddl);
                        }
                        catch (Exception exAlter)
                        {
                            if (!exAlter.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogWarning(exAlter, "Failed DDL: {Sql}", ddl);
                            }
                        }
                    }

                    // (SecretProvider backfill removed — all dialects default 'None' via DDL; Vault/Registry/Env are opt-in per server.)
                    try
                    {
                        var misconfigured = db.Servers
                            .Where(s => s.SecretProvider != "None" && string.IsNullOrEmpty(s.SecretPath) && string.IsNullOrEmpty(s.SecretMount))
                            .Select(s => s.Id).ToList();
                        if (misconfigured.Count > 0)
                            logger.LogWarning("Servers [{Ids}] set SecretProvider != 'None' but have no SecretPath/SecretMount; they FAIL CLOSED until a provider path is configured.", string.Join(", ", misconfigured));
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw(
                            "UPDATE Servers SET AuthShape = 'bearer' " +
                            "WHERE AuthShape IS NULL OR AuthShape = '';");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Tools ADD COLUMN SecretProvider TEXT DEFAULT 'None'");
                    }
                    catch { }

                    // Create AccessPolicies table for SQLite
                    try
                    {
                        db.Database.ExecuteSqlRaw(
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
                        db.Database.ExecuteSqlRaw(
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

                        db.Database.ExecuteSqlRaw(
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
                        db.Database.ExecuteSqlRaw(
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
                        db.Database.ExecuteSqlRaw(
                            "CREATE TABLE IF NOT EXISTS SecretProviders (" +
                            "ProviderName TEXT PRIMARY KEY, " +
                            "DisplayName TEXT, " +
                            "ConfigJson TEXT, " +
                            "IsEnabled INTEGER DEFAULT 1)");

                        db.Database.ExecuteSqlRaw(
                            "INSERT OR IGNORE INTO SecretProviders (ProviderName, DisplayName, IsEnabled) VALUES " +
                            "('Vault', 'HashiCorp Vault (KV v2)', 1), " +
                            "('WindowsRegistry', 'Windows Registry (DPAPI)', 1), " +
                            "('Environment', 'Container Environment', 1);");

                        db.Database.ExecuteSqlRaw(
                            "CREATE TABLE IF NOT EXISTS AuthProviderConfigs (" +
                            "ProviderName TEXT PRIMARY KEY, " +
                            "DisplayName TEXT, " +
                            "UserHeader TEXT, " +
                            "GroupsHeader TEXT, " +
                            "ConfigJson TEXT, " +
                            "IsEnabled INTEGER DEFAULT 1)");

                        try
                        {
                            db.Database.ExecuteSqlRaw("ALTER TABLE AuthProviderConfigs ADD COLUMN ConfigJson TEXT NULL");
                        }
                        catch {}

                        try
                        {
                            db.Database.ExecuteSqlRaw("ALTER TABLE SecretProviders ADD COLUMN ConfigJson TEXT NULL");
                        }
                        catch {}

                        db.Database.ExecuteSqlRaw(
                            "INSERT OR IGNORE INTO AuthProviderConfigs (ProviderName, DisplayName, UserHeader, GroupsHeader, IsEnabled) VALUES " +
                            "('ActiveDirectory', 'Active Directory', 'Remote-User', 'Remote-Groups', 1), " +
                            "('PocketID_TinyAuth', 'PocketID / TinyAuth OIDC', 'Remote-User', 'Remote-Groups', 1);");
                    }
                    catch (Exception exSecret)
                    {
                        logger.LogWarning(exSecret, "Secret/Auth provider table init warning");
                    }

                    var hasSettings = db.Settings.Any();
                    if (!hasSettings)
                    {
                        db.Settings.Add(new RouterSettings());
                        db.SaveChanges();
                    }

                    ClientAppKeySeeder.SeedDefaultClientsAndKeys(db, logger, configuration);

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
                
                CatalogDatabaseSeeder.SeedCatalogServers(db, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize database.");
            }
        }
    }
}
