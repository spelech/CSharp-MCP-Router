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

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN SecretProvider TEXT DEFAULT 'None'");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN SecretItemKey TEXT NULL");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN SecretMount TEXT NULL");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN SecretPath TEXT NULL");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN SecretField TEXT NULL");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN AuthShape TEXT DEFAULT 'bearer'");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN CustomHeaderName TEXT NULL");
                    }
                    catch { }

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

                    // AppKey Hashing Migration: migrate legacy AES-CBC encrypted AppKeys to SHA-256 hashes (gated by RUN_KEY_MIGRATION flag)
                    try
                    {
                        var runKeyMigration = configuration["KeyMigration:Enabled"] ?? configuration["RUN_KEY_MIGRATION"] ?? Environment.GetEnvironmentVariable("RUN_KEY_MIGRATION");
                        if (string.Equals(runKeyMigration, "true", StringComparison.OrdinalIgnoreCase))
                        {
                            var appKeys = db.AppKeys.ToList();
                            bool keysUpdated = false;
                            foreach (var key in appKeys)
                            {
                                if (string.IsNullOrEmpty(key.EncryptedKey)) continue;

                                bool isHashed = key.EncryptedKey.Length == 64
                                    && key.EncryptedKey.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

                                if (!isHashed)
                                {
                                    var decrypted = DecryptLegacyAppKey(key.EncryptedKey, configuration);
                                    if (string.IsNullOrEmpty(decrypted))
                                    {
                                        logger.LogError($"AppKey Hashing Migration: Failed to decrypt legacy AppKey '{key.Name}' (Id: {key.Id}). Skipping migration for this key to prevent corruption.");
                                        continue;
                                    }

                                    using var sha256 = SHA256.Create();
                                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(decrypted));
                                    key.EncryptedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();
                                    keysUpdated = true;
                                }
                            }

                            if (keysUpdated)
                            {
                                logger.LogInformation("Migrated legacy AppKeys to SHA-256 hashes.");
                                db.SaveChanges();
                            }
                        }
                        else
                        {
                            logger.LogInformation("AppKey legacy-key migration skipped. Set RUN_KEY_MIGRATION=true for a one-time migration.");
                        }
                    }
                    catch (Exception exKeyMig)
                    {
                        logger.LogWarning(exKeyMig, "AppKey hashing migration warning");
                    }

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
                
                // Migration script: if empty, import from environment
                if (!db.Servers.Any())
                {
                    logger.LogInformation("Database empty. Performing initial migration from environment variables...");

                    // 1. Home Assistant MCP
                    var haUrl = Environment.GetEnvironmentVariable("HOMEASSISTANT_URL") ?? "http://10.0.0.10:8123";
                    var haToken = Environment.GetEnvironmentVariable("HOMEASSISTANT_TOKEN");
                    if (!string.IsNullOrEmpty(haToken))
                    {
                        db.Servers.Add(new McpServer
                        {
                            Id = "ha",
                            Categories = new List<string> { "homecontrol" },
                            DisplayName = "Home Assistant",
                            Url = "http://ha-mcp:8086/mcp",
                            Enabled = true,
                            Hidden = false,
                            Type = "http",
                            ApiKey = haToken
                        });
                        logger.LogInformation("Imported HA MCP config.");
                    }

                    // 2. Actual Budget MCP
                    var actualPass = Environment.GetEnvironmentVariable("ACTUAL_PASSWORD");
                    if (!string.IsNullOrEmpty(actualPass))
                    {
                        db.Servers.Add(new McpServer
                        {
                            Id = "actual",
                            Categories = new List<string> { "financial" },
                            DisplayName = "Actual Budget",
                            Url = "http://actual-mcp:3000/sse",
                            Enabled = true,
                            Hidden = false,
                            Type = "sse",
                            ApiKey = Environment.GetEnvironmentVariable("ACTUAL_BEARER_TOKEN")
                        });
                        logger.LogInformation("Imported Actual Budget MCP config.");
                    }

                    // 3. Receipt Wrangler MCP
                    var rwKey = Environment.GetEnvironmentVariable("RECEIPTWRANGLER_API_KEY");
                    if (!string.IsNullOrEmpty(rwKey) && rwKey != "YOUR_RECEIPTWRANGLER_API_KEY_HERE")
                    {
                        db.Servers.Add(new McpServer
                        {
                            Id = "receiptwrangler",
                            Categories = new List<string> { "financial" },
                            DisplayName = "Receipt Wrangler",
                            Url = "http://receiptwrangler-mcp:3000/mcp",
                            Enabled = true,
                            Hidden = false,
                            Type = "sse",
                            ApiKey = rwKey
                        });
                        logger.LogInformation("Imported Receipt Wrangler MCP config.");
                    }

                    // 5. Overseerr/Seerr MCP
                    var seerrKey = Environment.GetEnvironmentVariable("SEERR_API_KEY");
                    if (!string.IsNullOrEmpty(seerrKey))
                    {
                        db.Servers.Add(new McpServer
                        {
                            Id = "seerr",
                            Categories = new List<string> { "media" },
                            DisplayName = "Overseerr requests",
                            Url = "http://seerr-mcp:8000/sse",
                            Enabled = true,
                            Hidden = false,
                            Type = "sse",
                            ApiKey = seerrKey
                        });
                        logger.LogInformation("Imported Overseerr config.");
                    }

                    // 6. UniFi MCP
                    var unifiUser = Environment.GetEnvironmentVariable("UNIFI_USERNAME");
                    if (!string.IsNullOrEmpty(unifiUser))
                    {
                        db.Servers.Add(new McpServer
                        {
                            Id = "unifi",
                            Categories = new List<string> { "unifi" },
                            DisplayName = "UniFi Controller",
                            Url = "http://unifi-mcp:3000/mcp",
                            Enabled = true,
                            Hidden = false,
                            Type = "http"
                        });
                        logger.LogInformation("Imported UniFi MCP config.");
                    }

                    // 7. Plex Media Server
                    var plexToken = Environment.GetEnvironmentVariable("PLEX_TOKEN");
                    if (!string.IsNullOrEmpty(plexToken))
                    {
                        db.Servers.Add(new McpServer
                        {
                            Id = "plex",
                            Categories = new List<string> { "media" },
                            DisplayName = "Plex Media Server",
                            Url = "http://plex-mcp:8000/sse",
                            Enabled = true,
                            Hidden = false,
                            Type = "sse",
                            ApiKey = plexToken
                        });
                        logger.LogInformation("Imported Plex config.");
                    }

                    // 7. Arr HD / 4K MCP
                    db.Servers.Add(new McpServer
                    {
                        Id = "mcp-arr-hd",
                        Categories = new List<string> { "media" },
                        DisplayName = "Arr Services (HD)",
                        Url = "http://mcp-arr-hd:3000/mcp",
                        Enabled = true,
                        Hidden = false,
                        Type = "http"
                    });
                    db.Servers.Add(new McpServer
                    {
                        Id = "mcp-arr-4k",
                        Categories = new List<string> { "media4k" },
                        DisplayName = "Arr Services (4K)",
                        Url = "http://mcp-arr-4k:3000/mcp",
                        Enabled = true,
                        Hidden = false,
                        Type = "http"
                    });
                    db.Servers.Add(new McpServer
                    {
                        Id = "docker",
                        Categories = new List<string> { "infrastructure" },
                        DisplayName = "Docker Containers",
                        Url = "http://docker-mcp:8000/sse",
                        Enabled = true,
                        Hidden = false,
                        Type = "sse"
                    });
                    logger.LogInformation("Imported Docker MCP config.");
                    logger.LogInformation("Imported Arr MCP configurations.");

                    db.SaveChanges();
                    logger.LogInformation("Database migration completed successfully.");
                }

                // Auto-fix server types for ha, unifi, and arr backends to http (stateless Streamable HTTP)
                try
                {
                    bool changed = false;
                    var ha = db.Servers.FirstOrDefault(s => s.Id == "ha");
                    if (ha != null && ha.Type != "http")
                    {
                        ha.Type = "http";
                        changed = true;
                    }
                    var unifi = db.Servers.FirstOrDefault(s => s.Id == "unifi");
                    if (unifi != null && unifi.Type != "http")
                    {
                        unifi.Type = "http";
                        changed = true;
                    }
                    var arrHd = db.Servers.FirstOrDefault(s => s.Id == "mcp-arr-hd");
                    if (arrHd != null && arrHd.Type != "http")
                    {
                        arrHd.Type = "http";
                        changed = true;
                    }
                    var arr4k = db.Servers.FirstOrDefault(s => s.Id == "mcp-arr-4k");
                    if (arr4k != null && arr4k.Type != "http")
                    {
                        arr4k.Type = "http";
                        changed = true;
                    }
                    
                    if (changed)
                    {
                        logger.LogInformation("Applying database type fixes for ha, unifi, and arr backends to http...");
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to update server types in database.");
                }

                // Load custom servers from configuration JSON if it exists
                var customServersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "custom_servers.json");
                if (File.Exists(customServersPath))
                {
                    try
                    {
                        logger.LogInformation("Found custom_servers.json. Processing configuration...");
                        var jsonContent = File.ReadAllText(customServersPath);
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var customServers = JsonSerializer.Deserialize<List<McpServer>>(jsonContent, options);
                        if (customServers != null)
                        {
                            var existingServersDict = db.Servers.ToDictionary(s => s.Id);
                            foreach (var server in customServers)
                            {
                                if (!existingServersDict.TryGetValue(server.Id, out var existing))
                                {
                                    logger.LogInformation($"Registering custom server '{server.DisplayName}' ({server.Id}) from config...");
                                    db.Servers.Add(server);
                                    existingServersDict[server.Id] = server;
                                }
                                else
                                {
                                    logger.LogInformation($"Updating custom server '{server.DisplayName}' ({server.Id}) from config...");
                                    existing.DisplayName = server.DisplayName;
                                    existing.Url = server.Url;
                                    existing.Type = server.Type;
                                    existing.Categories = server.Categories;
                                    existing.Enabled = server.Enabled;
                                    existing.Hidden = server.Hidden;
                                    if (!string.IsNullOrEmpty(server.ApiKey))
                                    {
                                        existing.ApiKey = server.ApiKey;
                                    }
                                    existing.SecretProvider = !string.IsNullOrEmpty(server.SecretProvider) ? server.SecretProvider : "None";
                                    existing.SecretItemKey = server.SecretItemKey;
                                    existing.AuthShape = !string.IsNullOrEmpty(server.AuthShape) ? server.AuthShape : "bearer";
                                    existing.CustomHeaderName = server.CustomHeaderName;
                                    existing.HeadersJson = server.HeadersJson;
                                }
                            }
                            db.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to load custom servers from JSON.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize database.");
            }
        }

        private static string DecryptLegacyAppKey(string ciphertext, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(ciphertext)) return string.Empty;

            try
            {
                var fullCipher = Convert.FromBase64String(ciphertext);
                if (fullCipher.Length < 16) return string.Empty;

                var secretString = configuration["ROUTER_SECRET"]
                    ?? configuration["ROUTER_MASTER_KEY"]
                    ?? McpRouter.Core.Secrets.DbKeyHelper.ResolveDbEncryptionKey(configuration);

                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretString));
                }

                using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.Key = keyBytes;

                    var iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
