using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpRouter.Models;

using Microsoft.EntityFrameworkCore;

namespace McpRouter.Services
{
    public static class DatabaseSeederService
    {
        public static void SeedDatabase(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("Initializing database...");

                var encryptionKey = app.Configuration["DB_ENCRYPTION_KEY"];
                if (string.IsNullOrEmpty(encryptionKey) || encryptionKey == "DefaultSecureKey123!")
                {
                    logger.LogCritical("SECURITY WARNING: The database is running with a default or weak DB_ENCRYPTION_KEY. Please set a strong DB_ENCRYPTION_KEY environment variable to secure your deployment!");
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
                        "RequireManualApproval INTEGER DEFAULT 0)");

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Settings ADD COLUMN RequireManualApproval INTEGER DEFAULT 0");
                    }
                    catch
                    {
                        // Ignore if column already exists
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
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN AuthShape TEXT DEFAULT 'bearer'");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Servers ADD COLUMN CustomHeaderName TEXT NULL");
                    }
                    catch { }

                    // Backfill existing servers in SQLite database
                    try
                    {
                        db.Database.ExecuteSqlRaw(
                            "UPDATE Servers SET SecretProvider = 'None' " +
                            "WHERE (SecretProvider IS NULL OR SecretProvider = '' OR SecretProvider = 'Vault') " +
                            "AND (ApiKey IS NOT NULL AND ApiKey != '');");

                        db.Database.ExecuteSqlRaw(
                            "UPDATE Servers SET AuthShape = 'bearer' " +
                            "WHERE AuthShape IS NULL OR AuthShape = '';");
                    }
                    catch { }

                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Tools ADD COLUMN SecretProvider TEXT DEFAULT 'Vault'");
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
                            foreach (var server in customServers)
                            {
                                var existing = db.Servers.FirstOrDefault(s => s.Id == server.Id);
                                if (existing == null)
                                {
                                    logger.LogInformation($"Registering custom server '{server.DisplayName}' ({server.Id}) from config...");
                                    db.Servers.Add(server);
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
    }
}
