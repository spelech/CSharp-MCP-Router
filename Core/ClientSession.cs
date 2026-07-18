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
    public partial class ClientSession
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

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonRpcMessageConverter() }
        };

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
                var defaultInitRequest = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"auto-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpRouterGatewayAuto\",\"version\":\"0.4.0\"}}}";
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
                        if (message is JsonRpcResponse response && response.Id != null)
                        {
                            var idStr = response.Id.ToString();
                            if (idStr != null && conn.PendingRequests.TryRemove(idStr, out var tcs))
                            {
                                tcs.SetResult(response);
                                return;
                            }
                        }
                        
                        // Otherwise, it is a notification (e.g. logMessage, resourceUpdated) - forward to client
                        var serialized = JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
                        using var doc = JsonDocument.Parse(serialized);
                        await WriteMessageAsync(doc.RootElement.Clone());
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
                        if (resp.Result != null && resp.Result.Value.TryGetProperty("tools", out var toolsList))
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
                            
                            var exposedName = item.ServerId + "__" + rawToolName;
                            
                            _toolRoutingTable[exposedName] = item.ServerId;
                            
                            // Deserialize and rewrite name if needed
                            var toolDict = JsonSerializer.Deserialize<Dictionary<string, object>>(tool.GetRawText());
                            if (toolDict != null)
                            {
                                toolDict["name"] = exposedName;
                                if (toolDict.TryGetValue("description", out var desc))
                                    toolDict["description"] = $"[{item.ServerId}] " + desc;
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
                
                // Restore the original tool name by stripping the serverId__ prefix
                string routingBody = body;
                var prefix = serverId + "__";
                if (toolName.StartsWith(prefix))
                {
                    var realToolName = toolName.Substring(prefix.Length);
                    routingBody = RewriteRequestJson(body, "name", realToolName);
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
                        return (serverId, response.Result ?? default(JsonElement));
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

        public async Task BroadcastNotificationAsync(string method, string body)
        {
            var tasks = new List<Task>();
            foreach (var conn in _backendConnections.Values)
            {
                tasks.Add(conn.SendNotificationAsync(method, body));
            }
            await Task.WhenAll(tasks);
        }

        public void Close()
        {
            foreach (var conn in _backendConnections.Values)
            {
                conn.Dispose();
            }
            _backendConnections.Clear();
        }

        private string RewriteRequestJson(string body, string paramKey, string newValue)
        {
            try
            {
                var docOptions = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };
                var node = System.Text.Json.Nodes.JsonNode.Parse(body, null, docOptions);
                if (node == null) return body;

                if (node is System.Text.Json.Nodes.JsonObject obj)
                {
                    RewriteObject(obj, paramKey, newValue);
                }
                else if (node is System.Text.Json.Nodes.JsonArray array)
                {
                    foreach (var item in array)
                    {
                        if (item is System.Text.Json.Nodes.JsonObject itemObj)
                        {
                            RewriteObject(itemObj, paramKey, newValue);
                        }
                    }
                }
                return node.ToJsonString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse and rewrite JSON body for key '{ParamKey}' to '{NewValue}'", paramKey, newValue);
                return body;
            }
        }

        private static void RewriteObject(System.Text.Json.Nodes.JsonObject obj, string paramKey, string newValue)
        {
            if (obj.TryGetPropertyValue("params", out var paramsNode) && paramsNode is System.Text.Json.Nodes.JsonObject paramsObj)
            {
                paramsObj[paramKey] = newValue;
            }
        }

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

        public async Task<object?> ReadResourceAsync(string resourceUri, string body)
        {
            await EnsureBackendsInitializedAsync();

            if (_resourceRoutingTable.TryGetValue(resourceUri, out var serverId) && _backendConnections.TryGetValue(serverId, out var conn))
            {
                var prefix = $"mcp://{serverId}/";
                var rawUri = Uri.UnescapeDataString(resourceUri.Substring(prefix.Length));
                string routingBody = RewriteRequestJson(body, "uri", rawUri);
                var resp = await conn.SendRequestAsync("resources/read", routingBody);
                return resp.Result;
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

        public async Task<object?> GetPromptAsync(string promptName, string body)
        {
            await EnsureBackendsInitializedAsync();

            if (_promptRoutingTable.TryGetValue(promptName, out var serverId) && _backendConnections.TryGetValue(serverId, out var conn))
            {
                var prefix = serverId + "__";
                var rawName = promptName.Substring(prefix.Length);
                string routingBody = RewriteRequestJson(body, "name", rawName);
                var resp = await conn.SendRequestAsync("prompts/get", routingBody);
                return resp.Result;
            }
            throw new KeyNotFoundException($"Prompt {promptName} not found in routing table.");
        }
    }
}
