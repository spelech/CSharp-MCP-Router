using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpRouter.Models;

namespace McpRouter.Services
{
    public class DockerAutoDiscoveryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DockerAutoDiscoveryService> _logger;
        private readonly string _socketPath = "/var/run/docker.sock";

        public DockerAutoDiscoveryService(IServiceProvider serviceProvider, ILogger<DockerAutoDiscoveryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Docker Auto-Discovery Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!File.Exists(_socketPath))
                {
                    _logger.LogTrace("Docker socket not found at {SocketPath}. Skipping auto-discovery scan.", _socketPath);
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                try
                {
                    await ScanContainersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Docker auto-discovery scan.");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }

        private async Task ScanContainersAsync(CancellationToken stoppingToken)
        {
            var udsEndPoint = new UnixDomainSocketEndPoint(_socketPath);
            using var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    try
                    {
                        await socket.ConnectAsync(udsEndPoint, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            using var httpClient = new HttpClient(handler);
            var response = await httpClient.GetStringAsync("http://localhost/containers/json", stoppingToken);
            
            using var doc = JsonDocument.Parse(response);
            var containers = doc.RootElement.EnumerateArray();

            var discoveredServers = new List<McpServer>();

            foreach (var container in containers)
            {
                if (!container.TryGetProperty("Labels", out var labelsProp) || labelsProp.ValueKind != JsonValueKind.Object)
                    continue;

                // Check if mcp.enabled is true (or check for mcp.id as a fallback indicator)
                bool mcpEnabled = false;
                if (labelsProp.TryGetProperty("mcp.enabled", out var enabledProp) && 
                    enabledProp.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                {
                    mcpEnabled = true;
                }

                if (!mcpEnabled)
                    continue;

                // Parse ID
                if (!labelsProp.TryGetProperty("mcp.id", out var idProp) || string.IsNullOrWhiteSpace(idProp.GetString()))
                    continue;

                var id = idProp.GetString()!.Trim();

                // Parse Port
                if (!labelsProp.TryGetProperty("mcp.port", out var portProp) || string.IsNullOrWhiteSpace(portProp.GetString()))
                    continue;

                var port = portProp.GetString()!.Trim();

                // Resolve Container Name (strip leading slash)
                string containerName = "";
                if (container.TryGetProperty("Names", out var namesProp) && namesProp.ValueKind == JsonValueKind.Array && namesProp.GetArrayLength() > 0)
                {
                    var name = namesProp[0].GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        containerName = name.TrimStart('/');
                    }
                }

                if (string.IsNullOrEmpty(containerName))
                    continue;

                // Optional Display Name
                string displayName = id;
                if (labelsProp.TryGetProperty("mcp.displayName", out var dnProp) && !string.IsNullOrWhiteSpace(dnProp.GetString()))
                {
                    displayName = dnProp.GetString()!.Trim();
                }

                // Optional Type
                string type = "sse";
                if (labelsProp.TryGetProperty("mcp.type", out var typeProp) && !string.IsNullOrWhiteSpace(typeProp.GetString()))
                {
                    type = typeProp.GetString()!.Trim().ToLowerInvariant();
                }

                // Optional Path
                string path = type == "http" ? "/mcp" : "/sse";
                if (labelsProp.TryGetProperty("mcp.path", out var pathProp) && !string.IsNullOrWhiteSpace(pathProp.GetString()))
                {
                    path = pathProp.GetString()!.Trim();
                    if (!path.StartsWith("/")) path = "/" + path;
                }

                // Optional Categories
                var categories = new List<string>();
                if (labelsProp.TryGetProperty("mcp.categories", out var catProp) && !string.IsNullOrWhiteSpace(catProp.GetString()))
                {
                    categories = catProp.GetString()!.Split(',')
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                }
                if (categories.Count == 0)
                {
                    categories.Add("default");
                }

                discoveredServers.Add(new McpServer
                {
                    Id = id,
                    DisplayName = displayName,
                    Url = $"http://{containerName}:{port}{path}",
                    Type = type,
                    Enabled = true,
                    Hidden = false,
                    Categories = categories,
                    AutoDiscovered = true
                });
            }

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
            var sessionManager = scope.ServiceProvider.GetRequiredService<SessionManager>();

            bool changed = false;

            // 1. Upsert discovered servers
            foreach (var discovered in discoveredServers)
            {
                var existing = db.Servers.FirstOrDefault(s => s.Id == discovered.Id);
                if (existing == null)
                {
                    _logger.LogInformation("Auto-discovered new MCP server: '{DisplayName}' ({Id}) at {Url}", discovered.DisplayName, discovered.Id, discovered.Url);
                    db.Servers.Add(discovered);
                    changed = true;
                }
                else
                {
                    // Update connection/mapping info if it has changed
                    bool updated = false;
                    if (existing.Url != discovered.Url) { existing.Url = discovered.Url; updated = true; }
                    if (existing.Type != discovered.Type) { existing.Type = discovered.Type; updated = true; }
                    if (existing.DisplayName != discovered.DisplayName) { existing.DisplayName = discovered.DisplayName; updated = true; }
                    if (!existing.Enabled) { existing.Enabled = true; updated = true; } // Reactivate if it was disabled
                    
                    // Compare Categories
                    var catMatch = existing.Categories.Count == discovered.Categories.Count && 
                                   existing.Categories.All(c => discovered.Categories.Contains(c));
                    if (!catMatch)
                    {
                        existing.Categories = discovered.Categories;
                        updated = true;
                    }

                    if (updated)
                    {
                        _logger.LogInformation("Updating auto-discovered MCP server: '{DisplayName}' ({Id})", discovered.DisplayName, discovered.Id);
                        existing.AutoDiscovered = true;
                        changed = true;
                    }
                }
            }

            // 2. Disable previously auto-discovered servers that are no longer running
            var activeIds = discoveredServers.Select(s => s.Id).ToHashSet();
            var dbAutoServers = db.Servers.Where(s => s.AutoDiscovered).ToList();

            foreach (var dbServer in dbAutoServers)
            {
                if (!activeIds.Contains(dbServer.Id) && dbServer.Enabled)
                {
                    _logger.LogInformation("Auto-discovered MCP server container stopped/removed. Disabling: '{DisplayName}' ({Id})", dbServer.DisplayName, dbServer.Id);
                    dbServer.Enabled = false;
                    changed = true;
                }
            }

            if (changed)
            {
                db.SaveChanges();
                sessionManager.ResetAll();
            }
        }
    }
}
