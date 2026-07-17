using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace McpRouter
{
    public partial class ClientSession
    {
        private readonly Dictionary<string, string> _resourceRoutingTable = new();
        private readonly Dictionary<string, string> _promptRoutingTable = new();

        public async Task<List<object>> ListResourcesAsync(string body)
        {
            var allResources = new List<object>();
            var tasks = new List<Task<(string ServerId, JsonElement Resources)>>();
            
            await EnsureBackendsInitializedAsync();

            foreach (var entry in _backendConnections)
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
                        _logger.LogError(ex, "Error listing resources on server {ServerId}", serverId);
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

        public async Task<object> ReadResourceAsync(string resourceUri, string body)
        {
            await EnsureBackendsInitializedAsync();

            if (_resourceRoutingTable.TryGetValue(resourceUri, out var serverId) && _backendConnections.TryGetValue(serverId, out var conn))
            {
                var prefix = $"mcp://{serverId}/";
                var rawUri = Uri.UnescapeDataString(resourceUri.Substring(prefix.Length));
                var routingBody = body.Replace($"\"uri\":\"{resourceUri}\"", $"\"uri\":\"{rawUri}\"");
                
                var resp = await conn.SendRequestAsync("resources/read", routingBody);
                return resp.Result ?? default(object);
            }
            throw new KeyNotFoundException($"Resource {resourceUri} not found in routing table.");
        }

        public async Task<List<object>> ListPromptsAsync(string body)
        {
            var allPrompts = new List<object>();
            var tasks = new List<Task<(string ServerId, JsonElement Prompts)>>();
            
            await EnsureBackendsInitializedAsync();

            foreach (var entry in _backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var resp = await conn.SendRequestAsync("prompts/list", body);
                        if (resp.Result != null && resp.Result.Value.TryGetProperty("prompts", out var promptsList))
                        {
                            return (serverId, promptsList);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error listing prompts on server {ServerId}", serverId);
                    }
                    return (serverId, default(JsonElement));
                }));
            }

            var completed = await Task.WhenAll(tasks);
            foreach (var item in completed)
            {
                if (item.Prompts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var prompt in item.Prompts.EnumerateArray())
                    {
                        if (prompt.TryGetProperty("name", out var nameProp))
                        {
                            var rawName = nameProp.GetString() ?? string.Empty;
                            var exposedName = item.ServerId + "__" + rawName;
                            
                            _promptRoutingTable[exposedName] = item.ServerId;
                            
                            var promptDict = JsonSerializer.Deserialize<Dictionary<string, object>>(prompt.GetRawText());
                            if (promptDict != null)
                            {
                                promptDict["name"] = exposedName;
                                if (promptDict.TryGetValue("description", out var descVal))
                                    promptDict["description"] = $"[{item.ServerId}] {descVal}";
                                allPrompts.Add(promptDict);
                            }
                        }
                    }
                }
            }
            return allPrompts;
        }

        public async Task<object> GetPromptAsync(string promptName, string body)
        {
            await EnsureBackendsInitializedAsync();

            if (_promptRoutingTable.TryGetValue(promptName, out var serverId) && _backendConnections.TryGetValue(serverId, out var conn))
            {
                var prefix = serverId + "__";
                var rawName = promptName.Substring(prefix.Length);
                var routingBody = body.Replace($"\"name\":\"{promptName}\"", $"\"name\":\"{rawName}\"");
                
                var resp = await conn.SendRequestAsync("prompts/get", routingBody);
                return resp.Result ?? default(object);
            }
            throw new KeyNotFoundException($"Prompt {promptName} not found in routing table.");
        }
    }
}
