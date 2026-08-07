using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using McpRouter.Models;
using McpRouter.Services;
using McpRouter.Core.Identity;
using McpRouter.Core.Database;
using Dapper;

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
        private readonly IEmbeddingService _embeddingService;

        private readonly Core.Routing.ToolRoutingManager _toolRoutingManager = new();
        private readonly Core.Routing.ResourceRoutingManager _resourceRoutingManager = new();
        private readonly Core.Routing.PromptRoutingManager _promptRoutingManager = new();

        public bool IsMetaMode { get; set; } = false;
        private Task? _initializeTask = null;
        public readonly object _initLock = new();
        private readonly CancellationTokenSource _cts = new();
        private string _lastInitializeRequest = string.Empty;
        private readonly List<Task> _backendInitTasks = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeRequestCancellationTokens = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonRpcResponse>> _clientPendingRequests = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonRpcMessageConverter() }
        };

        private readonly SessionManager? _sessionManager;
 
        public ClientSession(string sessionId, HttpResponse clientResponse, List<McpServer> servers, HttpClient httpClient, IEmbeddingService embeddingService, SessionManager? sessionManager, Microsoft.Extensions.Logging.ILogger logger)
        {
            _sessionId = sessionId;
            _clientResponse = clientResponse;
            _servers = servers;
            _httpClient = httpClient;
            _embeddingService = embeddingService;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public ClientSession(string sessionId, HttpResponse clientResponse, List<McpServer> servers, HttpClient httpClient, IEmbeddingService embeddingService, Microsoft.Extensions.Logging.ILogger logger)
            : this(sessionId, clientResponse, servers, httpClient, embeddingService, null, logger)
        {
        }

        public async Task<UserIdentityContext> ResolveUserIdentityAsync()
        {
            if (_clientResponse?.HttpContext?.RequestServices == null)
            {
                return new UserIdentityContext("system", "System", new List<string>());
            }
            try
            {
                // AppKey authentication bypass for custom claims mapping
                var httpContext = _clientResponse.HttpContext;
                if (httpContext.User?.Identity?.IsAuthenticated == true && httpContext.User.Identity.AuthenticationType == "AppKey")
                {
                    var username = httpContext.User.Identity.Name ?? "anonymous";
                    return new UserIdentityContext(username, "AppKey", new List<string>());
                }

                var compositeProvider = _clientResponse.HttpContext.RequestServices.GetService<CompositeIdentityProvider>();
                if (compositeProvider != null)
                {
                    return await compositeProvider.ResolveIdentityAsync(_clientResponse.HttpContext);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve user identity via CompositeIdentityProvider");
            }
            return new UserIdentityContext("anonymous", "None", new List<string>());
        }

        public async Task<bool> IsUserAuthorizedAsync(string requestMethod, string targetId, string? category = null)
        {
            // If authenticated via AppKey, check key-level scopes first
            if (_clientResponse?.HttpContext?.Items.TryGetValue("AppKeyUsed", out var appKeyUsedObj) == true && appKeyUsedObj is bool appKeyUsed && appKeyUsed)
            {
                if (_clientResponse.HttpContext.Items.TryGetValue("AppKeyScopes", out var scopesObj) == true && scopesObj is string scopesJson)
                {
                    bool scopeAllowed = false;
                    try
                    {
                        var scopes = JsonSerializer.Deserialize<List<string>>(scopesJson);
                        if (scopes != null)
                        {
                            var serverId = targetId.Contains("__") ? targetId.Split("__", 2)[0] : targetId;
                            foreach (var s in scopes)
                            {
                                var cleanScope = s.Trim().ToLowerInvariant();
                                if (cleanScope == "all" || cleanScope == "mcp_client" || cleanScope == "*")
                                {
                                    scopeAllowed = true;
                                    break;
                                }
                                if (cleanScope == $"server:{serverId}".ToLowerInvariant())
                                {
                                    scopeAllowed = true;
                                    break;
                                }
                                if (cleanScope == $"tool:{targetId}".ToLowerInvariant() ||
                                    cleanScope == $"prompt:{targetId}".ToLowerInvariant() ||
                                    cleanScope == $"resource:{targetId}".ToLowerInvariant() ||
                                    cleanScope == $"server:{targetId}".ToLowerInvariant() ||
                                    cleanScope == targetId.ToLowerInvariant())
                                {
                                    scopeAllowed = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception exScopes)
                    {
                        _logger.LogWarning(exScopes, "Failed to parse AppKey scopes JSON: {ScopesJson}", scopesJson);
                    }

                    if (!scopeAllowed)
                    {
                        _logger.LogWarning("AppKey rejected: requested target '{TargetId}' is outside the key's allowed scopes '{ScopesJson}'", targetId, scopesJson);
                        return false;
                    }
                }
            }

            var identity = await ResolveUserIdentityAsync();

            // 1. System/Admin bypass (e.g. Username is admin, or is in "Administrators" group, or username is "system")
            if (identity.Username == "admin" || identity.Username == "system" || identity.GroupNames.Contains("Administrators") || identity.GroupNames.Contains("full_admin"))
            {
                return true;
            }

            if (_clientResponse?.HttpContext?.RequestServices == null)
            {
                // If there's no HttpContext, we default to false (fail closed)
                return false;
            }

            try
            {
                var dbFactory = _clientResponse.HttpContext.RequestServices.GetService<IDbConnectionFactory>();
                if (dbFactory == null)
                {
                    return false;
                }

                using var conn = dbFactory.CreateConnection();

                var targetKeys = new List<string> { $"tool:{targetId}", $"prompt:{targetId}", $"resource:{targetId}" };
                string serverId;
                if (targetId.StartsWith("mcp://"))
                {
                    serverId = Uri.TryCreate(targetId, UriKind.Absolute, out var parsedUri) ? parsedUri.Host : targetId;
                }
                else
                {
                    serverId = targetId.Contains("__") ? targetId.Split("__", 2)[0] : targetId;
                }
                targetKeys.Add($"server:{serverId}");
                if (!string.IsNullOrEmpty(category))
                {
                    targetKeys.Add($"category:{category}");
                }

                var externalIds = identity.GroupNames.ToList();
                if (!string.IsNullOrEmpty(identity.Sid)) externalIds.Add(identity.Sid);
                if (!string.IsNullOrEmpty(identity.Username)) externalIds.Add(identity.Username);

                var mappedGroups = new List<string>();
                try
                {
                    const string mapSql = "SELECT InternalGroup FROM GroupMappings WHERE ExternalId IN @ExternalIds;";
                    mappedGroups = (await conn.QueryAsync<string>(mapSql, new { ExternalIds = externalIds })).ToList();
                }
                catch (Exception exMap)
                {
                    _logger.LogWarning(exMap, "Failed to query GroupMappings, assuming empty");
                }

                var allUserGroups = identity.GroupNames.Concat(mappedGroups).Where(g => !string.IsNullOrEmpty(g)).Distinct().ToList();

                if (dbFactory.ProviderName == "sqlite")
                {
                    // Check if there are any policies for the targets first to default-allow (inverted to fail closed)
                    const string countSql = "SELECT COUNT(*) FROM AccessPolicies WHERE TargetId IN @TargetIds;";
                    int policyCount = await conn.ExecuteScalarAsync<int>(countSql, new { TargetIds = targetKeys });
                    if (policyCount == 0)
                    {
                        return false;
                    }

                    // Check if there's an explicit deny for any of the user's groups
                    const string denySql = "SELECT COUNT(*) FROM AccessPolicies WHERE TargetId IN @TargetIds AND RequiredGroup IN @GroupNames AND IsAllowed = 0;";
                    int denyCount = await conn.ExecuteScalarAsync<int>(denySql, new { TargetIds = targetKeys, GroupNames = allUserGroups });
                    if (denyCount > 0)
                    {
                        return false;
                    }

                    // Check if there's an allow for any of the user's groups
                    const string allowSql = "SELECT COUNT(*) FROM AccessPolicies WHERE TargetId IN @TargetIds AND RequiredGroup IN @GroupNames AND IsAllowed = 1;";
                    int allowCount = await conn.ExecuteScalarAsync<int>(allowSql, new { TargetIds = targetKeys, GroupNames = allUserGroups });
                    return allowCount > 0;
                }
                else
                {
                    // Call stored procedure with mapped groups!
                    var groupNamesCsv = string.Join(",", allUserGroups);
                    var parameters = new {
                        GroupNames = groupNamesCsv,
                        ItemName = targetId,
                        RequestMethod = requestMethod
                    };
                    int isAllowed = await conn.ExecuteScalarAsync<int>(
                        "sp_EvaluateUserAccess",
                        parameters,
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    return isAllowed == 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user authorization for target '{TargetId}'", targetId);
                return false; // Fail closed fallback
            }
        }

        public async Task WriteMessageAsync(object message)
        {
            await _writeLock.WaitAsync();
            try
            {
                if (_clientResponse.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    return;
                }
                var json = JsonSerializer.Serialize(message, _jsonOptions);
                _logger.LogInformation("[JSON-RPC Gateway -> Client] {Payload}", json);
                _sessionManager?.AddPerformanceMetrics(0, json.Length / 4, 0);
                await _clientResponse.WriteAsync($"event: message\ndata: {json}\n\n");
                await _clientResponse.Body.FlushAsync();
            }
            catch (ObjectDisposedException)
            {
                // Client connection closed cleanly
            }
            catch (OperationCanceledException)
            {
                // Request cancelled
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Notice writing to client SSE stream: {Message}", ex.Message);
            }
            finally
            {
                _writeLock.Release();
            }
        }


        public async Task EnsureBackendsInitializedAsync()
        {
            if (_initializeTask == null)
            {
                lock (_initLock)
                {
                    if (_initializeTask == null)
                    {
                        var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"auto-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpRouterGatewayAuto\",\"version\":\"0.4.0\"}}}";
                        _initializeTask = Task.Run(async () => await InitializeBackendsAsync(initReq));
                    }
                }
            }
            await _initializeTask;

            List<Task> pending;
            lock (_backendInitTasks)
            {
                pending = _backendInitTasks.ToList();
            }
            if (pending.Count > 0)
            {
                await Task.WhenAll(pending);
            }
        }
 
        public void StartInitialization(string initializeRequest)
        {
            lock (_initLock)
            {
                if (_initializeTask == null)
                {
                    var finalRequest = initializeRequest;
                    if (initializeRequest.Contains("server/discover"))
                    {
                        finalRequest = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"auto-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpRouterGatewayAuto\",\"version\":\"0.4.0\"}}}";
                    }
                    _initializeTask = Task.Run(async () => await InitializeBackendsAsync(finalRequest));
                }
            }
        }
 
        public async Task InitializeBackendsAsync(string initializeRequest)
        {
            _lastInitializeRequest = initializeRequest;
            var tasks = new List<Task>();
            foreach (var server in _servers.Where(s => s.Enabled && s.Type != "custom"))
            {
                var task = Task.Run(async () => await ConnectAndInitializeBackendAsync(server));
                tasks.Add(task);
            }
            lock (_backendInitTasks)
            {
                _backendInitTasks.AddRange(tasks);
            }

            // We do NOT block on backend initialization, but we trigger a background tools cache population
            _ = Task.Run(async () =>
            {
                // Wait a couple seconds for some backends to finish initial connection
                await Task.Delay(3000);
                try
                {
                    await _toolRoutingManager.PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"init-list\"}", _backendConnections, _logger, _servers, _sessionManager);
                    _logger.LogInformation("Completed initial background tools cache population.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run initial background tools cache population.");
                }
            });

            await Task.CompletedTask;
        }

        private async Task ConnectAndInitializeBackendAsync(McpServer server)
        {
            int maxAttempts = 2;
            int attempt = 0;
            while (!_cts.Token.IsCancellationRequested && attempt < maxAttempts)
            {
                attempt++;
                try
                {
                    _logger.LogInformation("Attempting to connect to backend {ServerId} (attempt {Attempt}/{MaxAttempts}) at {Url}...", server.Id, attempt, maxAttempts, server.Url);
                    _sessionManager?.UpdateBackendStatus(server.Id, "Connecting", attempt, "");

                    var retriever = _clientResponse?.HttpContext?.RequestServices?.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                    var conn = new BackendConnection(server, _httpClient, _logger, retriever);
                    if (server.Type != "http" && server.Type != "streamable")
                    {
                        using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await conn.ConnectAsync().WaitAsync(ctsTimeout.Token);
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

                        if (message is JsonRpcNotification notification)
                        {
                            if (notification.Method == "notifications/tools/list_changed")
                            {
                                _logger.LogInformation("Received notifications/tools/list_changed from server {ServerId}. Invalidating tools cache.", server.Id);
                                _sessionManager?.RemoveServerToolsCache(server.Id);
                                _toolRoutingManager.InvalidateCache();
                            }

                            else if (notification.Method == "notifications/resources/list_changed")
                            {
                                _logger.LogInformation("Received notifications/resources/list_changed from server {ServerId}. Invalidating resources cache.", server.Id);
                                _sessionManager?.RemoveServerResourcesCache(server.Id);
                            }
                            else if (notification.Method == "notifications/prompts/list_changed")
                            {
                                _logger.LogInformation("Received notifications/prompts/list_changed from server {ServerId}. Invalidating prompts cache.", server.Id);
                                _sessionManager?.RemoveServerPromptsCache(server.Id);
                            }
                        }
                        
                        // Otherwise, it is a notification (e.g. logMessage, resourceUpdated) - forward to client
                        var serialized = JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
                        using var doc = JsonDocument.Parse(serialized);
                        await WriteMessageAsync(doc.RootElement.Clone());
                    });

                    // Send initialize request to this backend
                    using (var ctsInit = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        var initReq = string.IsNullOrEmpty(_lastInitializeRequest)
                            ? "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"auto-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpRouterGatewayAuto\",\"version\":\"0.4.0\"}}}"
                            : _lastInitializeRequest;
                        var resp = await conn.SendRequestAsync("initialize", initReq).WaitAsync(ctsInit.Token);
                        if (resp.Error != null)
                        {
                            throw new Exception($"Initialize failed: {resp.Error.Message}");
                        }
                    }
                    
                    // Send initialized notification to this backend
                    await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");

                    _backendConnections[server.Id] = conn;
                    _logger.LogInformation("Successfully connected and initialized backend server: {ServerId}", server.Id);
                    _sessionManager?.UpdateBackendStatus(server.Id, "Connected", attempt, "");
                    return; // Success, exit method
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to connect to backend {ServerId} at {Url} (attempt {Attempt}/{MaxAttempts}). Error: {Error}", 
                        server.Id, server.Url, attempt, maxAttempts, ex.Message);
                    
                    _sessionManager?.UpdateBackendStatus(server.Id, attempt >= maxAttempts ? "Failed" : "Retrying", attempt, ex.Message);

                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError("Stopped retrying connection to backend {ServerId} after {MaxAttempts} failed attempts.", server.Id, maxAttempts);
                        break;
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3), _cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        public void StartInitializationForBackend(string serverId)
        {
            var server = _servers.FirstOrDefault(s => s.Id == serverId);
            if (server != null && server.Enabled && server.Type != "custom")
            {
                if (_backendConnections.TryRemove(serverId, out var oldConn))
                {
                    oldConn.Dispose();
                }
                _ = Task.Run(async () => await ConnectAndInitializeBackendAsync(server));
            }
        }

        public SessionManager? GetSessionManager() => _sessionManager;

        public async Task<List<object>> ListToolsAsync(string body)
        {
            var tools = await _toolRoutingManager.ListToolsAsync(body, IsMetaMode, _backendConnections, _logger, EnsureBackendsInitializedAsync, _servers, _sessionManager);
            return tools;
        }

        public async Task<object> CallToolAsync(string toolName, string body, McpRouter.Models.RouterDbContext db)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int statusCode = 200;
            string? errorMessage = null;
            string? responsePayload = null;

            try
            {
                // Namespace validation
                var activeServerIds = _servers.Where(s => s.Enabled).Select(s => s.Id).ToList();
                if (!McpRouter.Core.Security.SecurityValidationHelper.ValidateToolOrPromptName(toolName, activeServerIds))
                {
                    statusCode = 403;
                    errorMessage = $"Security Error: Invalid or spoofed namespaced identifier: '{toolName}'.";
                    var errResult = new {
                        isError = true,
                        content = new[] {
                            new {
                                type = "text",
                                text = errorMessage
                            }
                        }
                    };
                    responsePayload = JsonSerializer.Serialize(errResult);
                    return errResult;
                }

                // RBAC Check
                var isAuth = await IsUserAuthorizedAsync("tools/call", toolName);
                if (!isAuth)
                {
                    var identity = await ResolveUserIdentityAsync();
                    statusCode = 403;
                    errorMessage = $"Security Error: User '{identity.Username}' does not have permission to execute tool '{toolName}'.";
                    var errResult = new {
                        isError = true,
                        content = new[] {
                            new {
                                type = "text",
                                text = errorMessage
                            }
                        }
                    };
                    responsePayload = JsonSerializer.Serialize(errResult);
                    return errResult;
                }

                string? requestId = null;
                using (var cts = new CancellationTokenSource())
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("id", out var idProp))
                        {
                            requestId = idProp.GetString() ?? idProp.GetRawText();
                            _activeRequestCancellationTokens[requestId] = cts;
                        }
                    }
                    catch { }

                    try
                    {
                        var res = await _toolRoutingManager.CallToolAsync(toolName, body, db, _backendConnections, _servers, _logger, _httpClient, _embeddingService, EnsureBackendsInitializedAsync, RewriteRequestJson, cts.Token, _sessionManager);
                        responsePayload = res != null ? JsonSerializer.Serialize(res) : null;
                        return res;
                    }
                    catch (Exception exCall)
                    {
                        statusCode = 500;
                        errorMessage = exCall.Message;
                        throw;
                    }
                    finally
                    {
                        if (requestId != null)
                        {
                            _activeRequestCancellationTokens.TryRemove(requestId, out _);
                        }
                    }
                }
            }
            catch (Exception exOuter)
            {
                if (statusCode == 200)
                {
                    statusCode = 500;
                    errorMessage = exOuter.Message;
                }
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await AuditInvocationAsync("tools/call", toolName, body, statusCode, stopwatch.ElapsedMilliseconds, responsePayload, errorMessage);
            }
        }

        public void CancelRequest(string requestId)
        {
            if (_activeRequestCancellationTokens.TryRemove(requestId, out var cts))
            {
                try
                {
                    cts.Cancel();
                    _logger.LogInformation("Cancelled active request: {RequestId}", requestId);
                }
                catch (ObjectDisposedException) { }
            }
        }

        public bool TryHandleClientResponse(string requestId, string responseJson)
        {
            if (_clientPendingRequests.TryRemove(requestId, out var tcs))
            {
                try
                {
                    var response = JsonSerializer.Deserialize<JsonRpcResponse>(responseJson, _jsonOptions);
                    if (response != null)
                    {
                        tcs.SetResult(response);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse client response for ID {RequestId}", requestId);
                }
                tcs.TrySetResult(new JsonRpcResponse { Id = requestId, Error = new JsonRpcError { Code = -32603, Message = "Failed to parse response payload." } });
                return true;
            }
            return false;
        }

        public async Task<JsonRpcResponse> ForwardRequestToClientAsync(JsonRpcRequest request)
        {
            var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestId = request.Id?.ToString() ?? Guid.NewGuid().ToString("N");
            
            _clientPendingRequests[requestId] = tcs;

            var clientRequest = new {
                jsonrpc = "2.0",
                method = request.Method,
                id = requestId,
                @params = request.Params
            };
            
            await WriteMessageAsync(clientRequest);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var reg = cts.Token.Register(() => tcs.TrySetCanceled());
            try
            {
                return await tcs.Task;
            }
            catch (TaskCanceledException)
            {
                return new JsonRpcResponse {
                    Id = requestId,
                    Error = new JsonRpcError { Code = -32000, Message = "Sampling request timed out or cancelled by client." }
                };
            }
            finally
            {
                _clientPendingRequests.TryRemove(requestId, out _);
            }
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

        public async Task<List<object>> ListResourcesAsync(string body)
        {
            var resources = await _resourceRoutingManager.ListResourcesAsync(body, _backendConnections, _logger, EnsureBackendsInitializedAsync, _sessionManager);
            return resources;
        }

        public async Task<object?> ReadResourceAsync(string resourceUri, string body)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int statusCode = 200;
            string? errorMessage = null;
            string? responsePayload = null;

            try
            {
                // Namespace validation
                var activeServerIds = _servers.Where(s => s.Enabled).Select(s => s.Id).ToList();
                if (!McpRouter.Core.Security.SecurityValidationHelper.ValidateResourceUri(resourceUri, activeServerIds))
                {
                    statusCode = 403;
                    errorMessage = $"Security Error: Invalid or spoofed resource URI namespace: '{resourceUri}'.";
                    throw new UnauthorizedAccessException(errorMessage);
                }

                var isAuth = await IsUserAuthorizedAsync("resources/read", resourceUri);
                if (!isAuth)
                {
                    var identity = await ResolveUserIdentityAsync();
                    statusCode = 403;
                    errorMessage = $"Security Error: User '{identity.Username}' does not have permission to read resource '{resourceUri}'.";
                    throw new UnauthorizedAccessException(errorMessage);
                }

                var res = await _resourceRoutingManager.ReadResourceAsync(resourceUri, body, _backendConnections, EnsureBackendsInitializedAsync, RewriteRequestJson, _sessionManager);
                responsePayload = res != null ? JsonSerializer.Serialize(res) : null;
                return res;
            }
            catch (Exception ex)
            {
                if (statusCode == 200)
                {
                    statusCode = 500;
                    errorMessage = ex.Message;
                }
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await AuditInvocationAsync("resources/read", resourceUri, body, statusCode, stopwatch.ElapsedMilliseconds, responsePayload, errorMessage);
            }
        }

        public async Task<List<object>> ListResourceTemplatesAsync(string body)
        {
            var templates = await _resourceRoutingManager.ListResourceTemplatesAsync(body, _backendConnections, _logger, EnsureBackendsInitializedAsync, _sessionManager);
            return templates;
        }

        public async Task<object> CompleteAsync(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (!root.TryGetProperty("params", out var paramsProp))
                {
                    return new { completion = new { values = Array.Empty<string>(), hasMore = false } };
                }

                if (paramsProp.TryGetProperty("ref", out var refProp))
                {
                    if (refProp.TryGetProperty("type", out var typeProp))
                    {
                        var refType = typeProp.GetString();
                        if (refType == "ref/resource")
                        {
                            if (refProp.TryGetProperty("uriTemplate", out var templateProp))
                            {
                                var uriTemplate = templateProp.GetString() ?? string.Empty;
                                if (uriTemplate == "logs://{server_name}/today")
                                {
                                    var argVal = string.Empty;
                                    if (paramsProp.TryGetProperty("value", out var valProp))
                                    {
                                        argVal = valProp.GetString() ?? string.Empty;
                                    }
                                    var serverIds = _servers.Select(s => s.Id).ToList();
                                    var matching = serverIds
                                        .Where(id => id.StartsWith(argVal, StringComparison.OrdinalIgnoreCase))
                                        .Take(10)
                                        .ToList();
                                    return new { completion = new { values = matching, hasMore = false } };
                                }
                                
                                if (uriTemplate.StartsWith("mcp://"))
                                {
                                    var parts = uriTemplate.Substring("mcp://".Length).Split('/', 2);
                                    if (parts.Length == 2)
                                    {
                                        var serverId = parts[0];
                                        var backendTemplate = parts[1];
                                        if (_backendConnections.TryGetValue(serverId, out var conn))
                                        {
                                            var rewrittenBody = RewriteRequestJson(body, "uriTemplate", backendTemplate);
                                            var resp = await conn.SendRequestAsync("completion/complete", rewrittenBody);
                                            if (resp.Result != null) return resp.Result.Value;
                                        }
                                    }
                                }
                            }
                        }
                        else if (refType == "ref/prompt")
                        {
                            if (refProp.TryGetProperty("name", out var nameProp))
                            {
                                var promptName = nameProp.GetString() ?? string.Empty;
                                var parts = promptName.Split("__", 2);
                                if (parts.Length == 2)
                                {
                                    var serverId = parts[0];
                                    var rawName = parts[1];
                                    if (_backendConnections.TryGetValue(serverId, out var conn))
                                    {
                                        var rewrittenBody = RewriteRequestJson(body, "name", rawName);
                                        var resp = await conn.SendRequestAsync("completion/complete", rewrittenBody);
                                        if (resp.Result != null) return resp.Result.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling completion/complete request");
            }

            return new { completion = new { values = Array.Empty<string>(), hasMore = false } };
        }

        public async Task<List<object>> ListPromptsAsync(string body)
        {
            var prompts = await _promptRoutingManager.ListPromptsAsync(body, _backendConnections, _logger, EnsureBackendsInitializedAsync, _sessionManager);
            return prompts;
        }

        public async Task<object?> GetPromptAsync(string promptName, string body)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int statusCode = 200;
            string? errorMessage = null;
            string? responsePayload = null;

            try
            {
                // Namespace validation
                var activeServerIds = _servers.Where(s => s.Enabled).Select(s => s.Id).ToList();
                if (!McpRouter.Core.Security.SecurityValidationHelper.ValidateToolOrPromptName(promptName, activeServerIds))
                {
                    statusCode = 403;
                    errorMessage = $"Security Error: Invalid or spoofed prompt namespace: '{promptName}'.";
                    throw new UnauthorizedAccessException(errorMessage);
                }

                var isAuth = await IsUserAuthorizedAsync("prompts/get", promptName);
                if (!isAuth)
                {
                    var identity = await ResolveUserIdentityAsync();
                    statusCode = 403;
                    errorMessage = $"Security Error: User '{identity.Username}' does not have permission to access prompt '{promptName}'.";
                    throw new UnauthorizedAccessException(errorMessage);
                }

                var res = await _promptRoutingManager.GetPromptAsync(promptName, body, _backendConnections, EnsureBackendsInitializedAsync, RewriteRequestJson);
                responsePayload = res != null ? JsonSerializer.Serialize(res) : null;
                return res;
            }
            catch (Exception ex)
            {
                if (statusCode == 200)
                {
                    statusCode = 500;
                    errorMessage = ex.Message;
                }
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await AuditInvocationAsync("prompts/get", promptName, body, statusCode, stopwatch.ElapsedMilliseconds, responsePayload, errorMessage);
            }
        }

        private async Task AuditInvocationAsync(
            string requestMethod,
            string itemName,
            string? payload,
            int statusCode,
            long executionTimeMs,
            string? responsePayload,
            string? errorMessage)
        {
            var auditLogger = _clientResponse?.HttpContext?.RequestServices?.GetService<McpRouter.Core.Logging.IAuditLogger>();
            if (auditLogger == null) return;

            try
            {
                var identity = await ResolveUserIdentityAsync();
                
                string serverId;
                if (itemName.StartsWith("mcp://"))
                {
                    serverId = Uri.TryCreate(itemName, UriKind.Absolute, out var parsedUri) ? parsedUri.Host : itemName;
                }
                else
                {
                    serverId = itemName.Contains("__") ? itemName.Split("__", 2)[0] : itemName;
                }

                // Try to extract requestId from request payload
                string? requestId = null;
                if (!string.IsNullOrEmpty(payload))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(payload);
                        if (doc.RootElement.TryGetProperty("id", out var idProp))
                        {
                            requestId = idProp.GetString() ?? idProp.GetRawText();
                        }
                    }
                    catch {}
                }
                requestId ??= Guid.NewGuid().ToString("N");

                await auditLogger.LogInvocationAsync(
                    requestId,
                    identity.Username,
                    identity.Sid ?? "",
                    serverId,
                    itemName,
                    requestMethod,
                    (int)executionTimeMs,
                    statusCode,
                    payload,
                    responsePayload,
                    errorMessage
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write invocation audit log");
            }
        }
    }
}
