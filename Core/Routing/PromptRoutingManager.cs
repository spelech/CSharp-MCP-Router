using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Routing
{
    public class PromptRoutingManager
    {
        private readonly Dictionary<string, string> _promptRoutingTable = new();

        public async Task<List<object>> ListPromptsAsync(string body, IEnumerable<KeyValuePair<string, BackendConnection>> backendConnections, ILogger logger, Func<Task> ensureBackendsInitializedAsync)
        {
            var allPrompts = new List<object>();
            var tasks = new List<Task<(string ServerId, JsonElement Prompts)>>();
            
            await ensureBackendsInitializedAsync();

            foreach (var entry in backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var reqBody = "{\"jsonrpc\":\"2.0\",\"method\":\"prompts/list\",\"id\":\"refresh-prompt-list\"}";
                        var resp = await conn.SendRequestAsync("prompts/list", reqBody);
                        if (resp.Result != null && resp.Result.Value.TryGetProperty("prompts", out var promptsList))
                        {
                            return (serverId, promptsList);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error listing prompts on server {ServerId}", serverId);
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

        public async Task<object?> GetPromptAsync(string promptName, string body, ConcurrentDictionary<string, BackendConnection> backendConnections, Func<Task> ensureBackendsInitializedAsync, Func<string, string, string, string> rewriteRequestJson)
        {
            await ensureBackendsInitializedAsync();

            if (_promptRoutingTable.TryGetValue(promptName, out var serverId) && backendConnections.TryGetValue(serverId, out var conn))
            {
                var prefix = serverId + "__";
                var rawName = promptName.Substring(prefix.Length);
                string routingBody = rewriteRequestJson(body, "name", rawName);
                var resp = await conn.SendRequestAsync("prompts/get", routingBody);
                return resp.Result;
            }
            throw new KeyNotFoundException($"Prompt {promptName} not found in routing table.");
        }
    }
}
