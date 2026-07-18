using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
                        var resp = await conn.SendRequestAsync("resources/list", body);
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
            return allResources;
        }

        public async Task<object?> ReadResourceAsync(string resourceUri, string body, ConcurrentDictionary<string, BackendConnection> backendConnections, Func<Task> ensureBackendsInitializedAsync, Func<string, string, string, string> rewriteRequestJson)
        {
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
    }
}
