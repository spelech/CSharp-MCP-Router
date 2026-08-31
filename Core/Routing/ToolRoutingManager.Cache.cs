using System.Text.Json;

namespace ModelContextGateway.Core.Routing
{
    public partial class ToolRoutingManager
    {
        /// <summary>
        /// Asynchronously lists available tools from connected backends or returns bootstrap Meta-Mode tools.
        /// </summary>
        public async Task<List<object>> ListToolsAsync(string body, bool isMetaMode, IEnumerable<KeyValuePair<string, BackendConnection>> backendConnections, ILogger logger, Func<Task> ensureBackendsInitializedAsync, IEnumerable<McpServer> servers, SessionManager? sessionManager = null)
        {
            if (isMetaMode)
            {
                return GetMetaModeTools();
            }

            await ensureBackendsInitializedAsync();

            lock (_cacheLock)
            {
                if (_isCachePopulated)
                {
                    return new List<object>(_cachedTools);
                }
            }

            await PopulateToolsCacheAsync(body, backendConnections, logger, servers, sessionManager);
            lock (_cacheLock)
            {
                return new List<object>(_cachedTools);
            }
        }

        public async Task PopulateToolsCacheAsync(string body, IEnumerable<KeyValuePair<string, BackendConnection>> backendConnections, ILogger logger, IEnumerable<McpServer> servers, SessionManager? sessionManager = null)
        {
            var allTools = new List<object>();

            var tasks = new List<Task<(string ServerId, JsonElement Tools)>>();

            foreach (var entry in backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var reqBody = "{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"refresh-list\"}";
                        var resp = await conn.SendRequestAsync("tools/list", reqBody);
                        if (resp.Result != null && resp.Result.Value.TryGetProperty("tools", out var toolsList))
                        {
                            return (serverId, toolsList);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error listing tools on server {ServerId}", serverId);
                    }
                    return (serverId, default(JsonElement));
                }));
            }

            var completed = await Task.WhenAll(tasks);
            foreach (var item in completed)
            {
                if (item.Tools.ValueKind == JsonValueKind.Array)
                {
                    var serverTools = new List<object>();
                    foreach (var tool in item.Tools.EnumerateArray())
                    {
                        if (tool.TryGetProperty("name", out var nameProp))
                        {
                            var rawToolName = nameProp.GetString() ?? string.Empty;
                            var exposedName = item.ServerId + "__" + rawToolName;

                            _toolRoutingTable[exposedName] = item.ServerId;

                            var toolDict = JsonSerializer.Deserialize<Dictionary<string, object>>(tool.GetRawText());
                            if (toolDict != null)
                            {
                                toolDict["name"] = exposedName;
                                if (toolDict.TryGetValue("description", out var desc))
                                {
                                    toolDict["description"] = $"[{item.ServerId}] " + desc;
                                }

                                var srv = servers.FirstOrDefault(s => s.Id == item.ServerId);
                                if (srv != null && (srv.AllowPassThroughAuth || !string.IsNullOrEmpty(srv.DynamicAuthPrompt)))
                                {
                                    var authPrompt = !string.IsNullOrEmpty(srv.DynamicAuthPrompt) ? srv.DynamicAuthPrompt : "This tool requires a target authentication token. Call with target_auth_token parameter.";
                                    toolDict["description"] = $"{toolDict["description"]}\n\nAUTH REQUIRED: {authPrompt}";
                                }

                                serverTools.Add(toolDict);
                                allTools.Add(toolDict);
                            }
                        }
                    }
                    if (sessionManager != null)
                    {
                        serverTools = serverTools.OrderBy(t => GetToolName(t), StringComparer.Ordinal).ToList();
                        sessionManager.SetServerToolsCache(item.ServerId, serverTools);
                    }
                }
            }

            allTools = allTools.OrderBy(t => GetToolName(t), StringComparer.Ordinal).ToList();

            lock (_cacheLock)
            {
                _cachedTools.Clear();
                _cachedTools.AddRange(allTools);
                _isCachePopulated = true;
            }
        }
    }
}
