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

                    var hasSettings = db.Settings.Any();
                    if (!hasSettings)
                    {
                        db.Settings.Add(new RouterSettings());
                        db.SaveChanges();
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
                            Category = "homecontrol",
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
                            Category = "financial",
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
                            Category = "financial",
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
                            Category = "media",
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
                            Category = "unifi",
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
                            Category = "media",
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
                        Category = "media",
                        DisplayName = "Arr Services (HD)",
                        Url = "http://mcp-arr-hd:3000/mcp",
                        Enabled = true,
                        Hidden = false,
                        Type = "http"
                    });
                    db.Servers.Add(new McpServer
                    {
                        Id = "mcp-arr-4k",
                        Category = "media4k",
                        DisplayName = "Arr Services (4K)",
                        Url = "http://mcp-arr-4k:3000/mcp",
                        Enabled = true,
                        Hidden = false,
                        Type = "http"
                    });
                    db.Servers.Add(new McpServer
                    {
                        Id = "docker",
                        Category = "infrastructure",
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
                                    existing.Category = server.Category;
                                    existing.Enabled = server.Enabled;
                                    existing.Hidden = server.Hidden;
                                    existing.ApiKey = server.ApiKey;
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
