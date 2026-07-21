using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Services;

namespace McpRouter.Core.Routing
{
    public class ToolRoutingManager
    {
        private readonly ConcurrentDictionary<string, string> _toolRoutingTable = new();
        private readonly List<object> _cachedTools = new();
        private readonly object _cacheLock = new();
        private bool _isCachePopulated = false;

        public async Task<List<object>> ListToolsAsync(string body, bool isMetaMode, IEnumerable<KeyValuePair<string, BackendConnection>> backendConnections, ILogger logger, Func<Task> ensureBackendsInitializedAsync, IEnumerable<McpServer> servers, SessionManager? sessionManager = null)
        {
            if (isMetaMode)
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

            foreach (var customTool in McpRouter.CustomTools.CustomToolRegistry.GetAll())
            {
                bool includeTool = false;
                if (customTool.Name.StartsWith("seerr_") && servers.Any(s => s.Id == "seerr"))
                    includeTool = true;
                else if (customTool.Name.StartsWith("plex_") && servers.Any(s => s.Id == "plex"))
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
                                    toolDict["description"] = $"[{item.ServerId}] " + desc;
                                serverTools.Add(toolDict);
                                allTools.Add(toolDict);
                            }
                        }
                    }
                    if (sessionManager != null)
                    {
                        sessionManager.SetServerToolsCache(item.ServerId, serverTools);
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

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _isCachePopulated = false;
                _cachedTools.Clear();
            }
        }


        public async Task<object> CallToolAsync(
            string toolName, 
            string body, 
            McpRouter.Models.RouterDbContext db,
            ConcurrentDictionary<string, BackendConnection> backendConnections,
            IEnumerable<McpServer> servers,
            ILogger logger,
            HttpClient httpClient,
            IEmbeddingService embeddingService,
            Func<Task> ensureBackendsInitializedAsync,
            Func<string, string, string, string> rewriteRequestJson,
            CancellationToken cancellationToken = default,
            SessionManager? sessionManager = null)
        {
            try
            {
                var task = CallToolInternalAsync(toolName, body, db, backendConnections, servers, logger, httpClient, embeddingService, ensureBackendsInitializedAsync, rewriteRequestJson, sessionManager);
                return await task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Execution of tool '{ToolName}' was cancelled.", toolName);
                return new {
                    isError = true,
                    content = new[] {
                        new {
                            type = "text",
                            text = "Error: request was cancelled by the client."
                        }
                    }
                };
            }
        }

        private async Task<object> CallToolInternalAsync(
            string toolName, 
            string body, 
            McpRouter.Models.RouterDbContext db,
            ConcurrentDictionary<string, BackendConnection> backendConnections,
            IEnumerable<McpServer> servers,
            ILogger logger,
            HttpClient httpClient,
            IEmbeddingService embeddingService,
            Func<Task> ensureBackendsInitializedAsync,
            Func<string, string, string, string> rewriteRequestJson,
            SessionManager? sessionManager)
        {
            await ensureBackendsInitializedAsync();

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

                var tools = new List<object>();
                lock (_cacheLock)
                {
                    tools.AddRange(_cachedTools);
                }

                var results = await SemanticSearchService.SearchToolsSemanticAsync(query, tools, embeddingService);
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
                    var result = await ExecuteTargetToolAsync(targetName, targetBody, db, backendConnections, servers, logger, httpClient, ensureBackendsInitializedAsync, rewriteRequestJson, sessionManager);
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

            return await ExecuteTargetToolAsync(toolName, body, db, backendConnections, servers, logger, httpClient, ensureBackendsInitializedAsync, rewriteRequestJson, sessionManager);
        }

        private async Task<object> ExecuteTargetToolAsync(
            string toolName, 
            string body, 
            McpRouter.Models.RouterDbContext db,
            ConcurrentDictionary<string, BackendConnection> backendConnections,
            IEnumerable<McpServer> servers,
            ILogger logger,
            HttpClient httpClient,
            Func<Task> ensureBackendsInitializedAsync,
            Func<string, string, string, string> rewriteRequestJson,
            SessionManager? sessionManager)
        {
            var customTool = McpRouter.CustomTools.CustomToolRegistry.Get(toolName);
            if (customTool != null)
            {
                logger.LogInformation("Executing custom native C# tool '{ToolName}'", toolName);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var parameters = root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("arguments", out var argsProp) 
                    ? argsProp 
                    : JsonDocument.Parse("{}").RootElement;
                    
                try
                {
                    var result = await customTool.ExecuteAsync(parameters, httpClient, db);
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
                    logger.LogError(ex, "Error executing custom tool {ToolName}", toolName);
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

            if (!_toolRoutingTable.ContainsKey(toolName))
            {
                logger.LogInformation("Tool '{ToolName}' not found in routing table. Refreshing tools cache...", toolName);
                try
                {
                    await PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"refresh-list\"}", backendConnections, logger, servers);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to refresh tools cache during CallToolAsync for '{ToolName}'", toolName);
                }
            }

            if (_toolRoutingTable.TryGetValue(toolName, out var serverId) && backendConnections.TryGetValue(serverId, out var conn))
            {
                logger.LogInformation("Routing tool call '{ToolName}' to server '{ServerId}'", toolName, serverId);

                var settings = db.Settings.FirstOrDefault();
                if (settings != null && settings.RequireManualApproval && IsSensitiveTool(toolName))
                {
                    var approved = await RequestManualApprovalAsync(toolName, body, sessionManager, serverId, logger);
                    if (!approved)
                    {
                        return new {
                            isError = true,
                            content = new[] {
                                new {
                                    type = "text",
                                    text = "Error: tool execution denied by security policy (Requires Manual Approval)."
                                }
                            }
                        };
                    }
                }
                
                string routingBody = body;
                var prefix = serverId + "__";
                if (toolName.StartsWith(prefix))
                {
                    var realToolName = toolName.Substring(prefix.Length);
                    routingBody = rewriteRequestJson(body, "name", realToolName);
                }
                
                try
                {
                    var resp = await conn.SendRequestAsync("tools/call", routingBody);
                    if (resp.Error != null)
                    {
                        var transformed = TransformError(resp.Error, toolName, serverId);
                        return new {
                            isError = true,
                            content = new[] {
                                new {
                                    type = "text",
                                    text = transformed
                                }
                            }
                        };
                    }
                    return resp;
                }
                catch (Exception ex)
                {
                    var transformed = TransformException(ex, toolName, serverId);
                    return new {
                        isError = true,
                        content = new[] {
                            new {
                                type = "text",
                                text = transformed
                            }
                        }
                    };
                }
            }
            
            throw new KeyNotFoundException($"Tool {toolName} not found in routing table.");
        }

        private string TransformError(JsonRpcError error, string toolName, string serverId)
        {
            var suggestion = GetActionableSuggestion(error.Message, toolName, serverId);
            var errObj = new {
                error = error.Message,
                code = error.Code,
                suggestion = suggestion,
                remediation = $"Check the logs using resources path: logs://{serverId}/today to diagnose connectivity issues."
            };
            return JsonSerializer.Serialize(errObj, new JsonSerializerOptions { WriteIndented = true });
        }

        private string TransformException(Exception ex, string toolName, string serverId)
        {
            var suggestion = GetActionableSuggestion(ex.Message, toolName, serverId);
            var errObj = new {
                error = ex.Message,
                code = -32603,
                suggestion = suggestion,
                remediation = $"Check the logs using resources path: logs://{serverId}/today to diagnose connectivity issues."
            };
            return JsonSerializer.Serialize(errObj, new JsonSerializerOptions { WriteIndented = true });
        }

        private string GetActionableSuggestion(string message, string toolName, string serverId)
        {
            var msg = message.ToLowerInvariant();
            if (msg.Contains("auth") || msg.Contains("unauthorized") || msg.Contains("forbidden") || msg.Contains("key") || msg.Contains("token"))
            {
                return $"Authentication/Authorization failure on backend '{serverId}'. Please verify your credentials or api keys in settings.";
            }
            if (msg.Contains("timeout") || msg.Contains("deadline") || msg.Contains("timed out"))
            {
                return $"Request to '{serverId}' timed out. Please check if the service is running, responsive, or under heavy load.";
            }
            if (msg.Contains("conn") || msg.Contains("refused") || msg.Contains("socket") || msg.Contains("unreachable"))
            {
                return $"Network connection refused by backend '{serverId}'. Ensure that the container is running and host/port routes are open.";
            }
            if (msg.Contains("argument") || msg.Contains("parameter") || msg.Contains("invalid value") || msg.Contains("required"))
            {
                return $"Invalid arguments passed to tool '{toolName}'. Please call search_tools to audit the parameters schema.";
            }
            return $"An unexpected error occurred while executing '{toolName}' on backend '{serverId}'. Review the backend logs.";
        }

        private bool IsSensitiveTool(string toolName)
        {
            var name = toolName.ToLowerInvariant();
            return name.Contains("docker") || 
                   name.Contains("actual") || 
                   name.Contains("write") || 
                   name.Contains("delete") || 
                   name.Contains("update") || 
                   name.Contains("remove") || 
                   name.Contains("restart") || 
                   name.Contains("stop") || 
                   name.Contains("start") || 
                   name.Contains("ha__") || 
                   name.Contains("ha-mcp__") || 
                   name.Contains("unifi");
        }

        private async Task<bool> RequestManualApprovalAsync(string toolName, string body, SessionManager? sessionManager, string serverId, ILogger logger)
        {
            if (sessionManager == null) return true;
            
            string argumentsText = "{}";
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("arguments", out var argsProp))
                {
                    argumentsText = JsonSerializer.Serialize(argsProp, new JsonSerializerOptions { WriteIndented = true });
                }
            }
            catch { }

            var approval = new PendingApproval
            {
                ToolName = toolName,
                Arguments = argumentsText,
                SessionId = serverId
            };

            sessionManager.PendingApprovals[approval.Id] = approval;
            logger.LogWarning("Tool call '{ToolName}' is pending user approval. Approval ID: {ApprovalId}", toolName, approval.Id);

            return await approval.Tcs.Task;
        }

        public List<object> GetCachedTools()
        {
            lock (_cacheLock)
            {
                return new List<object>(_cachedTools);
            }
        }
    }
}
