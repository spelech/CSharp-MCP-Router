using System.Text.Json;
using Dapper;

namespace ModelContextGateway.Infrastructure.Persistence.DatabaseSeeders
{
    public static class CatalogDatabaseSeeder
    {
        public static void SeedCatalogServers(IDbConnectionFactory dbFactory, ILogger logger)
        {
            using var conn = dbFactory.CreateConnection();
            var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Servers");

            if (count == 0)
            {
                logger.LogInformation("Database empty. Performing initial migration from environment variables...");

                var haToken = Environment.GetEnvironmentVariable("HOMEASSISTANT_TOKEN");
                if (!string.IsNullOrEmpty(haToken))
                {
                    InsertServer(conn, "ha", "Home Assistant", "http://ha-mcp:8086/mcp", "http", new List<string> { "homecontrol" }, haToken);
                }

                var actualPass = Environment.GetEnvironmentVariable("ACTUAL_PASSWORD");
                if (!string.IsNullOrEmpty(actualPass))
                {
                    InsertServer(conn, "actual", "Actual Budget", "http://actual-mcp:3000/sse", "sse", new List<string> { "financial" }, Environment.GetEnvironmentVariable("ACTUAL_BEARER_TOKEN"));
                }

                var rwKey = Environment.GetEnvironmentVariable("RECEIPTWRANGLER_API_KEY");
                if (!string.IsNullOrEmpty(rwKey) && rwKey != "YOUR_RECEIPTWRANGLER_API_KEY_HERE")
                {
                    InsertServer(conn, "receiptwrangler", "Receipt Wrangler", "http://receiptwrangler-mcp:3000/mcp", "sse", new List<string> { "financial" }, rwKey);
                }

                var seerrKey = Environment.GetEnvironmentVariable("SEERR_API_KEY");
                if (!string.IsNullOrEmpty(seerrKey))
                {
                    InsertServer(conn, "seerr", "Overseerr requests", "http://seerr-mcp:8000/sse", "sse", new List<string> { "media" }, seerrKey);
                }

                var unifiUser = Environment.GetEnvironmentVariable("UNIFI_USERNAME");
                if (!string.IsNullOrEmpty(unifiUser))
                {
                    InsertServer(conn, "unifi", "UniFi Controller", "http://unifi-mcp:3000/mcp", "http", new List<string> { "unifi" });
                }

                var plexToken = Environment.GetEnvironmentVariable("PLEX_TOKEN");
                if (!string.IsNullOrEmpty(plexToken))
                {
                    InsertServer(conn, "plex", "Plex Media Server", "http://plex-mcp:8000/sse", "sse", new List<string> { "media" }, plexToken);
                }

                InsertServer(conn, "mcp-arr-hd", "Arr Services (HD)", "http://mcp-arr-hd:3000/mcp", "http", new List<string> { "media" });
                InsertServer(conn, "mcp-arr-4k", "Arr Services (4K)", "http://mcp-arr-4k:3000/mcp", "http", new List<string> { "media4k" });
                InsertServer(conn, "docker", "Docker Containers", "http://docker-mcp:8000/sse", "sse", new List<string> { "infrastructure" });
                InsertServer(conn, "media", "Media (Plex & Overseerr)", "http://media-mcp:8080/sse", "sse", new List<string> { "media" });

                logger.LogInformation("Database migration completed successfully.");
            }

            // Auto-fix server types for ha, unifi, and arr backends to http
            try
            {
                conn.Execute("UPDATE Servers SET Type = 'http' WHERE Id IN ('ha', 'unifi', 'mcp-arr-hd', 'mcp-arr-4k') AND Type != 'http'");
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
                            var catJson = JsonSerializer.Serialize(server.Categories ?? new());
                            var exists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Servers WHERE Id = @Id", new { server.Id });
                            if (exists == 0)
                            {
                                conn.Execute(@"INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, Categories, SecretProvider, AuthShape, ApiKey) VALUES (@Id, @DisplayName, @Url, 1, 0, @Type, @Categories, 'None', 'bearer', @ApiKey)",
                                    new { server.Id, server.DisplayName, server.Url, server.Type, Categories = catJson, server.ApiKey });
                            }
                            else
                            {
                                conn.Execute(@"UPDATE Servers SET DisplayName = @DisplayName, Url = @Url, Type = @Type, Categories = @Categories, Enabled = @Enabled, Hidden = @Hidden WHERE Id = @Id",
                                    new { server.Id, server.DisplayName, server.Url, server.Type, Categories = catJson, server.Enabled, server.Hidden });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load custom servers from JSON.");
                }
            }
        }

        private static void InsertServer(System.Data.IDbConnection conn, string id, string displayName, string url, string type, List<string> categories, string? apiKey = null)
        {
            var catJson = JsonSerializer.Serialize(categories);
            conn.Execute(@"INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, Categories, SecretProvider, AuthShape, ApiKey) VALUES (@Id, @DisplayName, @Url, 1, 0, @Type, @Categories, 'None', 'bearer', @ApiKey)",
                new { Id = id, DisplayName = displayName, Url = url, Type = type, Categories = catJson, ApiKey = apiKey });
        }
    }
}


