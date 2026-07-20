using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpRouter.Services;

namespace McpRouter.Core.Routing
{
    public class ResourceRoutingManager
    {
        private readonly Dictionary<string, string> _resourceRoutingTable = new();

        public async Task<List<object>> ListResourcesAsync(string body, IEnumerable<KeyValuePair<string, BackendConnection>> backendConnections, ILogger logger, Func<Task> ensureBackendsInitializedAsync)
        {
            var allResources = new List<object>();
            var tasks = new List<Task<(string ServerId, JsonElement Resources)>>();
            
            await ensureBackendsInitializedAsync();

            foreach (var entry in backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var reqBody = "{\"jsonrpc\":\"2.0\",\"method\":\"resources/list\",\"id\":\"refresh-res-list\"}";
                        var resp = await conn.SendRequestAsync("resources/list", reqBody);
                        if (resp.Result != null && resp.Result.Value.TryGetProperty("resources", out var resourcesList))
                        {
                            return (serverId, resourcesList);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error listing resources on server {ServerId}", serverId);
                    }
                    return (serverId, default(JsonElement));
                }));
            }

            var completed = await Task.WhenAll(tasks);
            foreach (var item in completed)
            {
                if (item.Resources.ValueKind == JsonValueKind.Array)
                {
                    foreach (var resource in item.Resources.EnumerateArray())
                    {
                        if (resource.TryGetProperty("uri", out var uriProp))
                        {
                            var rawUri = uriProp.GetString() ?? string.Empty;
                            var exposedUri = $"mcp://{item.ServerId}/{Uri.EscapeDataString(rawUri)}";
                            
                            _resourceRoutingTable[exposedUri] = item.ServerId;
                            
                            var resourceDict = JsonSerializer.Deserialize<Dictionary<string, object>>(resource.GetRawText());
                            if (resourceDict != null)
                            {
                                resourceDict["uri"] = exposedUri;
                                if (resourceDict.TryGetValue("name", out var nameVal))
                                    resourceDict["name"] = $"[{item.ServerId}] {nameVal}";
                                allResources.Add(resourceDict);
                            }
                        }
                    }
                }
            }

            // Append built-in router resources at the end to keep backend resources at index 0 for unit tests
            allResources.Add(new Dictionary<string, object> {
                { "uri", "router://status" },
                { "name", "Router Connection Status" },
                { "mimeType", "application/json" },
                { "description", "Real-time connection status of the MCP gateway and active sessions." }
            });
            allResources.Add(new Dictionary<string, object> {
                { "uri", "router://active-servers" },
                { "name", "Active Backend Servers" },
                { "mimeType", "application/json" },
                { "description", "Details about all registered backend servers and their connectivity status." }
            });
            allResources.Add(new Dictionary<string, object> {
                { "uri", "router://metrics" },
                { "name", "Gateway Operational Metrics" },
                { "mimeType", "application/json" },
                { "description", "Operational metrics, including tool counts, session counts, and system telemetry." }
            });

            return allResources;
        }

        public async Task<List<object>> ListResourceTemplatesAsync(string body, IEnumerable<KeyValuePair<string, BackendConnection>> backendConnections, ILogger logger, Func<Task> ensureBackendsInitializedAsync)
        {
            var allTemplates = new List<object>();
            
            // Add built-in templates
            allTemplates.Add(new Dictionary<string, object> {
                { "uriTemplate", "logs://{server_name}/today" },
                { "name", "Backend Server Log" },
                { "description", "Fetch today's real-time logs for a specific backend server." },
                { "parameters", new Dictionary<string, object> {
                    { "server_name", new Dictionary<string, object> {
                        { "description", "The unique identifier of the backend server (e.g., ha, unifi, docker)" }
                    } }
                } }
            });

            await ensureBackendsInitializedAsync();

            var tasks = new List<Task<(string ServerId, JsonElement Templates)>>();
            foreach (var entry in backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var reqBody = "{\"jsonrpc\":\"2.0\",\"method\":\"resources/templates/list\",\"id\":\"refresh-temp-list\"}";
                        var resp = await conn.SendRequestAsync("resources/templates/list", reqBody);
                        if (resp.Result != null && resp.Result.Value.TryGetProperty("templates", out var templatesList))
                        {
                            return (serverId, templatesList);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error listing templates on server {ServerId}", serverId);
                    }
                    return (serverId, default(JsonElement));
                }));
            }

            var completed = await Task.WhenAll(tasks);
            foreach (var item in completed)
            {
                if (item.Templates.ValueKind == JsonValueKind.Array)
                {
                    foreach (var template in item.Templates.EnumerateArray())
                    {
                        if (template.TryGetProperty("uriTemplate", out var uriTemplateProp))
                        {
                            var rawTemplate = uriTemplateProp.GetString() ?? string.Empty;
                            var exposedTemplate = $"mcp://{item.ServerId}/{rawTemplate}";
                            
                            var templateDict = JsonSerializer.Deserialize<Dictionary<string, object>>(template.GetRawText());
                            if (templateDict != null)
                            {
                                templateDict["uriTemplate"] = exposedTemplate;
                                if (templateDict.TryGetValue("name", out var nameVal))
                                    templateDict["name"] = $"[{item.ServerId}] {nameVal}";
                                allTemplates.Add(templateDict);
                            }
                        }
                    }
                }
            }
            return allTemplates;
        }

        public async Task<object?> ReadResourceAsync(string resourceUri, string body, ConcurrentDictionary<string, BackendConnection> backendConnections, Func<Task> ensureBackendsInitializedAsync, Func<string, string, string, string> rewriteRequestJson, SessionManager? sessionManager = null)
        {
            if (resourceUri.StartsWith("router://"))
            {
                return ResolveLocalResource(resourceUri, backendConnections, sessionManager);
            }
            if (resourceUri.StartsWith("logs://"))
            {
                return ResolveLocalLogResource(resourceUri);
            }

            await ensureBackendsInitializedAsync();

            if (_resourceRoutingTable.TryGetValue(resourceUri, out var serverId) && backendConnections.TryGetValue(serverId, out var conn))
            {
                var prefix = $"mcp://{serverId}/";
                var rawUri = Uri.UnescapeDataString(resourceUri.Substring(prefix.Length));
                string routingBody = rewriteRequestJson(body, "uri", rawUri);
                var resp = await conn.SendRequestAsync("resources/read", routingBody);
                return resp.Result;
            }
            throw new KeyNotFoundException($"Resource {resourceUri} not found in routing table.");
        }

        private object ResolveLocalResource(string uri, ConcurrentDictionary<string, BackendConnection> backendConnections, SessionManager? sessionManager)
        {
            string jsonText = "{}";
            if (uri == "router://status")
            {
                var statusObj = new {
                    status = "online",
                    activeSessions = sessionManager?.ActiveSessionsCount ?? 0,
                    backendCount = backendConnections.Count,
                    timestamp = DateTime.UtcNow
                };
                jsonText = JsonSerializer.Serialize(statusObj);
            }
            else if (uri == "router://active-servers")
            {
                var serversList = new List<object>();
                var statuses = sessionManager?.BackendStatuses;
                if (statuses != null)
                {
                    foreach (var entry in statuses)
                    {
                        serversList.Add(new {
                            id = entry.Key,
                            status = entry.Value.Status,
                            attempts = entry.Value.Attempts,
                            error = entry.Value.Error
                        });
                    }
                }
                jsonText = JsonSerializer.Serialize(serversList);
            }
            else if (uri == "router://metrics")
            {
                var metricsObj = new {
                    totalRequests = sessionManager?.TotalRequests ?? 0,
                    activeConnections = sessionManager?.ActiveSessionsCount ?? 0,
                    memoryUsageBytes = GC.GetTotalMemory(false),
                    upTimeSeconds = (DateTime.UtcNow - (sessionManager?.StartTime ?? DateTime.UtcNow)).TotalSeconds
                };
                jsonText = JsonSerializer.Serialize(metricsObj);
            }

            return new {
                contents = new[] {
                    new {
                        uri = uri,
                        mimeType = "application/json",
                        text = jsonText
                    }
                }
            };
        }

        private object ResolveLocalLogResource(string uri)
        {
            var match = System.Text.RegularExpressions.Regex.Match(uri, @"^logs://([^/]+)/today$");
            string logText = "No logs found for this backend server.";
            if (match.Success)
            {
                var serverId = match.Groups[1].Value;
                var filteredLogs = LogBuffer.GetLogs()
                    .Where(l => l.Message.Contains($"backend {serverId}", StringComparison.OrdinalIgnoreCase) || 
                                l.Message.Contains($"backend server: {serverId}", StringComparison.OrdinalIgnoreCase) || 
                                l.Message.Contains($"connect to backend {serverId}", StringComparison.OrdinalIgnoreCase) || 
                                l.Message.Contains(serverId, StringComparison.OrdinalIgnoreCase))
                    .Take(100) // limit to 100 log lines to avoid payload bloat
                    .Select(l => $"[{l.Timestamp:yyyy-MM-dd HH:mm:ss}] [{l.Level}] {l.Message}")
                    .ToList();
                if (filteredLogs.Count > 0)
                {
                    logText = string.Join("\n", filteredLogs);
                }
            }

            return new {
                contents = new[] {
                    new {
                        uri = uri,
                        mimeType = "text/plain",
                        text = logText
                    }
                }
            };
        }
    }
}
