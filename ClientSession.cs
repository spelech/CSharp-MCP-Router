using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using McpRouter.Models;

namespace McpRouter
{
    public class ClientSession
    {
        private readonly string _sessionId;
        private readonly HttpResponse _clientResponse;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly List<McpServer> _servers;
        private readonly ConcurrentDictionary<string, BackendConnection> _backendConnections = new();
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        // Map toolName -> serverId
        private readonly ConcurrentDictionary<string, string> _toolRoutingTable = new();
        private readonly List<object> _cachedTools = new();
        private readonly object _cacheLock = new();
        private bool _isCachePopulated = false;
        public bool IsMetaMode { get; set; } = false;
        private Task? _initializeTask = null;
        public readonly object _initLock = new();

        public ClientSession(string sessionId, HttpResponse clientResponse, List<McpServer> servers, HttpClient httpClient, Microsoft.Extensions.Logging.ILogger logger)
        {
            _sessionId = sessionId;
            _clientResponse = clientResponse;
            _servers = servers;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task WriteMessageAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            await _writeLock.WaitAsync();
            try
            {
                await _clientResponse.WriteAsync($"event: message\ndata: {json}\n\n");
                await _clientResponse.Body.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public Task EnsureBackendsInitializedAsync()
        {
            lock (_initLock)
            {
                if (_initializeTask != null)
                {
                    return _initializeTask;
                }

                _logger.LogInformation("Backends not initialized yet. Auto-initializing with default payload for SessionId: {SessionId}", _sessionId);
                var defaultInitRequest = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"auto-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpRouterGatewayAuto\",\"version\":\"0.1.0\"}}}";
                _initializeTask = InitializeBackendsAsync(defaultInitRequest);
                return _initializeTask;
            }
        }

        public void StartInitialization(string initializeRequest)
        {
            lock (_initLock)
            {
                if (_initializeTask == null)
                {
                    _initializeTask = Task.Run(async () => await InitializeBackendsAsync(initializeRequest));
                }
            }
        }

        public async Task InitializeBackendsAsync(string initializeRequest)
        {
            var tasks = _servers.Where(s => s.Enabled && s.Type != "custom").Select(async server =>
            {
                try
                {
                    var conn = new BackendConnection(server, _httpClient, _logger);
                    if (server.Type != "http" && server.Type != "streamable")
                    {
                        await conn.ConnectAsync();
                    }
                    
                    // Start background reader
                    conn.StartReader(async (message) =>
                    {
                        // If message is a response, complete the TaskCompletionSource
                        if (message.TryGetProperty("id", out var idProp))
                        {
                            var idStr = idProp.ToString();
                            if (conn.PendingRequests.TryRemove(idStr, out var tcs))
                            {
                                tcs.SetResult(message.Clone());
                                return;
                            }
                        }
                        
                        // Otherwise, it is a notification (e.g. logMessage, resourceUpdated) - forward to client
                        await WriteMessageAsync(message);
                    });

                    // Send initialize request to this backend
                    var resp = await conn.SendRequestAsync("initialize", initializeRequest);
                    
                    // Send initialized notification to this backend
                    await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");

                    _backendConnections[server.Id] = conn;
                    _logger.LogInformation("Initialized backend server connection: {ServerId}", server.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to backend: {ServerId} at {Url}", server.Id, server.Url);
                }
            });

            await Task.WhenAll(tasks);

            // Pre-populate tools cache and routing table
            try
            {
                await PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"init-list\"}");
                _logger.LogInformation("Pre-populated tools cache and routing table (total {Count} tools).", _cachedTools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pre-populate tools cache during backend connection initialization.");
            }
        }

        private async Task PopulateToolsCacheAsync(string body)
        {
            var allTools = new List<object>();

            // Add custom native C# tools (Plex and Overseerr)
            foreach (var customTool in McpRouter.CustomTools.CustomToolRegistry.GetAll())
            {
                bool includeTool = false;
                if (customTool.Name.StartsWith("seerr_") && _servers.Any(s => s.Id == "seerr"))
                    includeTool = true;
                else if (customTool.Name.StartsWith("plex_") && _servers.Any(s => s.Id == "plex"))
                    includeTool = true;

                if (includeTool)
                {
                    allTools.Add(new
                    {
                        name = customTool.Name,
                        description = customTool.Description,
                        inputSchema = customTool.InputSchema
                    });
                }
            }

            var tasks = new List<Task<(string ServerId, JsonElement Tools)>>();

            foreach (var entry in _backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;
                
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var resp = await conn.SendRequestAsync("tools/list", body);
                        if (resp.TryGetProperty("result", out var result) && result.TryGetProperty("tools", out var toolsList))
                        {
                            return (serverId, toolsList);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error listing tools on server {ServerId}", serverId);
                    }
                    return (serverId, default(JsonElement));
                }));
            }

            var completed = await Task.WhenAll(tasks);
            foreach (var item in completed)
            {
                if (item.Tools.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tool in item.Tools.EnumerateArray())
                    {
                        if (tool.TryGetProperty("name", out var nameProp))
                        {
                            var rawToolName = nameProp.GetString() ?? string.Empty;
                            
                            // Suffix tools from 4K arr instance to avoid name collisions with HD
                            var exposedName = item.ServerId == "mcp-arr-4k"
                                ? rawToolName + "_4k"
                                : rawToolName;
                            
                            _toolRoutingTable[exposedName] = item.ServerId;
                            
                            // Deserialize and rewrite name if needed
                            var toolDict = JsonSerializer.Deserialize<Dictionary<string, object>>(tool.GetRawText());
                            if (toolDict != null)
                            {
                                if (item.ServerId == "mcp-arr-4k")
                                {
                                    toolDict["name"] = exposedName;
                                    // Prefix description so users know it's the 4K instance
                                    if (toolDict.TryGetValue("description", out var desc))
                                        toolDict["description"] = "[4K] " + desc;
                                }
                                allTools.Add(toolDict);
                            }
                        }
                    }
                }
            }

            lock (_cacheLock)
            {
                _cachedTools.Clear();
                _cachedTools.AddRange(allTools);
                _isCachePopulated = true;
            }
        }

        public async Task<List<object>> ListToolsAsync(string body)
        {
            if (IsMetaMode)
            {
                return new List<object>
                {
                    new
                    {
                        name = "search_tools",
                        description = "Semantically search across all registered internal MCP tools (Excel, Docker, Plex, Home Assistant, etc.) using keywords. Returns the matching tool names, descriptions, and input schemas. Use this first to discover what tools are available.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new
                            {
                                query = new { type = "string", description = "The natural language query describing what you want to do (e.g. 'read Excel file data', 'restart Docker container')." }
                            },
                            required = new[] { "query" }
                        }
                    },
                    new
                    {
                        name = "execute_tool",
                        description = "Execute a specific internal MCP tool by name with arguments. Obtain the correct tool name and arguments schema by calling search_tools first.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new
                            {
                                name = new { type = "string", description = "The exact name of the tool to execute (e.g., 'docker/list_containers')." },
                                arguments = new { type = "object", description = "The arguments JSON object expected by the target tool." }
                            },
                            required = new[] { "name", "arguments" }
                        }
                    }
                };
            }

            await EnsureBackendsInitializedAsync();

            lock (_cacheLock)
            {
                if (_isCachePopulated)
                {
                    return new List<object>(_cachedTools);
                }
            }

            // Fallback: populate synchronously if cache isn't ready
            await PopulateToolsCacheAsync(body);
            lock (_cacheLock)
            {
                return new List<object>(_cachedTools);
            }
        }

        public async Task<object> CallToolAsync(string toolName, string body, RouterDbContext db)
        {
            await EnsureBackendsInitializedAsync();

            if (toolName == "search_tools")
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                
                string query = "";
                if (root.TryGetProperty("params", out var paramsProp) && 
                    paramsProp.TryGetProperty("arguments", out var argsProp) && 
                    argsProp.TryGetProperty("query", out var queryProp))
                {
                    query = queryProp.GetString() ?? "";
                }

                var results = await SearchToolsInternalAsync(query);
                return new {
                    content = new[] {
                        new {
                            type = "text",
                            text = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })
                        }
                    }
                };
            }
            else if (toolName == "execute_tool")
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                
                string targetName = "";
                JsonElement targetArgs = default;
                
                if (root.TryGetProperty("params", out var paramsProp) && 
                    paramsProp.TryGetProperty("arguments", out var argsProp))
                {
                    if (argsProp.TryGetProperty("name", out var nameProp))
                    {
                        targetName = nameProp.GetString() ?? "";
                    }
                    if (argsProp.TryGetProperty("arguments", out var targetArgsProp))
                    {
                        targetArgs = targetArgsProp.Clone();
                    }
                }

                if (string.IsNullOrEmpty(targetName))
                {
                    return new {
                        isError = true,
                        content = new[] {
                            new {
                                type = "text",
                                text = "Error: target tool name is required."
                            }
                        }
                    };
                }

                // Reconstruct a standard tools/call payload for the target tool
                var targetPayload = new
                {
                    jsonrpc = "2.0",
                    method = "tools/call",
                    @params = new
                    {
                        name = targetName,
                        arguments = targetArgs.ValueKind == JsonValueKind.Undefined ? (object)new Dictionary<string, object>() : targetArgs
                    }
                };
                var targetBody = JsonSerializer.Serialize(targetPayload);

                try
                {
                    var result = await ExecuteTargetToolAsync(targetName, targetBody, db);
                    return result;
                }
                catch (Exception ex)
                {
                    return new {
                        isError = true,
                        content = new[] {
                            new {
                                type = "text",
                                text = $"Error executing target tool {targetName}: {ex.Message}"
                            }
                        }
                    };
                }
            }

            return await ExecuteTargetToolAsync(toolName, body, db);
        }

        public async Task<object> ExecuteTargetToolAsync(string toolName, string body, RouterDbContext db)
        {
            // Try custom native C# tool first
            var customTool = McpRouter.CustomTools.CustomToolRegistry.Get(toolName);
            if (customTool != null)
            {
                _logger.LogInformation("Executing custom native C# tool '{ToolName}'", toolName);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var parameters = root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("arguments", out var argsProp) 
                    ? argsProp 
                    : JsonDocument.Parse("{}").RootElement;
                    
                try
                {
                    var result = await customTool.ExecuteAsync(parameters, _httpClient, db);
                    return new {
                        content = new[] {
                            new {
                                type = "text",
                                text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing custom tool {ToolName}", toolName);
                    return new {
                        isError = true,
                        content = new[] {
                            new {
                                type = "text",
                                text = $"Error executing custom tool {toolName}: {ex.Message}"
                            }
                        }
                    };
                }
            }

            // If not in routing table, try to refresh the cache once in case a new tool was registered
            if (!_toolRoutingTable.ContainsKey(toolName))
            {
                _logger.LogInformation("Tool '{ToolName}' not found in routing table. Refreshing tools cache...", toolName);
                try
                {
                    await PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"refresh-list\"}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh tools cache during CallToolAsync for '{ToolName}'", toolName);
                }
            }

            if (_toolRoutingTable.TryGetValue(toolName, out var serverId) && _backendConnections.TryGetValue(serverId, out var conn))
            {
                _logger.LogInformation("Routing tool call '{ToolName}' to server '{ServerId}'", toolName, serverId);
                
                // If the tool was exposed with a _4k suffix, rewrite the body to use the real tool name
                string routingBody = body;
                if (toolName.EndsWith("_4k") && serverId == "mcp-arr-4k")
                {
                    var realToolName = toolName.Substring(0, toolName.Length - 3); // strip "_4k"
                    routingBody = body.Replace($"\"name\":\"{toolName}\"", $"\"name\":\"{realToolName}\"");
                }
                
                return await conn.SendRequestAsync("tools/call", routingBody);
            }
            
            throw new KeyNotFoundException($"Tool {toolName} not found in routing table.");
        }

        private async Task<List<object>> SearchToolsInternalAsync(string query)
        {
            var tools = new List<object>();
            lock (_cacheLock)
            {
                tools.AddRange(_cachedTools);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return tools;
            }

            var queryWords = query.ToLower()
                .Split(new[] { ' ', ',', '.', ';', ':', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            if (queryWords.Count == 0)
            {
                queryWords = query.ToLower()
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            var scoredTools = new List<(object Tool, double Score)>();

            foreach (var tool in tools)
            {
                string name = "";
                string description = "";

                if (tool is JsonElement je)
                {
                    name = je.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    description = je.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                }
                else if (tool is System.Collections.IDictionary dict)
                {
                    name = dict.Contains("name") ? dict["name"]?.ToString() ?? "" : "";
                    description = dict.Contains("description") ? dict["description"]?.ToString() ?? "" : "";
                }
                else
                {
                    var type = tool.GetType();
                    name = type.GetProperty("name")?.GetValue(tool)?.ToString() ?? 
                           type.GetProperty("Name")?.GetValue(tool)?.ToString() ?? "";
                    description = type.GetProperty("description")?.GetValue(tool)?.ToString() ?? 
                                  type.GetProperty("Description")?.GetValue(tool)?.ToString() ?? "";
                }

                var fullText = (name + " " + description).ToLower();
                double score = 0;
                int matches = 0;

                if (fullText.Contains(query.ToLower()))
                {
                    score += 10.0;
                }

                if (name.ToLower().Contains(query.ToLower()))
                {
                    score += 5.0;
                }

                foreach (var word in queryWords)
                {
                    if (name.ToLower().Contains(word))
                    {
                        score += 3.0;
                        matches++;
                    }
                    else if (description.ToLower().Contains(word))
                    {
                        score += 1.0;
                        matches++;
                    }
                }

                if (matches > 1)
                {
                    score += matches * 2.0;
                }

                if (score > 0)
                {
                    scoredTools.Add((tool, score));
                }
            }

            var results = scoredTools
                .OrderByDescending(x => x.Score)
                .Select(x => x.Tool)
                .Take(15)
                .ToList();

            if (results.Count == 0)
            {
                results = tools.Take(10).ToList();
            }

            return results;
        }

        public async Task<Dictionary<string, JsonElement>> BroadcastRequestAsync(string body)
        {
            var results = new Dictionary<string, JsonElement>();
            var tasks = new List<Task<(string ServerId, JsonElement Result)>>();

            foreach (var entry in _backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var response = await conn.SendRequestAsync("unknown", body);
                        return (serverId, response);
                    }
                    catch
                    {
                        return (serverId, default(JsonElement));
                    }
                }));
            }

            var completed = await Task.WhenAll(tasks);
            foreach (var item in completed)
            {
                if (item.Result.ValueKind != JsonValueKind.Undefined)
                {
                    results[item.ServerId] = item.Result;
                }
            }
            return results;
        }

        public void Close()
        {
            foreach (var conn in _backendConnections.Values)
            {
                conn.Dispose();
            }
            _backendConnections.Clear();
        }
    }
}
