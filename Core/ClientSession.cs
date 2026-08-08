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
using Microsoft.Extensions.Configuration;
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


        public SessionManager? GetSessionManager() => _sessionManager;

        public async Task<List<object>> ListToolsAsync(string body, HttpContext? httpContext = null)
        {
            var tools = await _toolRoutingManager.ListToolsAsync(body, IsMetaMode, _backendConnections, _logger, EnsureBackendsInitializedAsync, _servers, _sessionManager);
            return await FilterAuthorizedAsync(tools, "tools/list", "name", httpContext);
        }

        public async Task<object> CallToolAsync(string toolName, string body, McpRouter.Models.RouterDbContext db, HttpContext? httpContext = null)
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

                // RBAC Check — use the live per-request HttpContext when provided
                var isAuth = await IsUserAuthorizedAsync("tools/call", toolName, null, httpContext);
                if (!isAuth)
                {
                    var identity = await ResolveUserIdentityAsync(httpContext);
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
                await AuditInvocationAsync("tools/call", toolName, body, statusCode, stopwatch.ElapsedMilliseconds, responsePayload, errorMessage, httpContext);
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


        public void Close()
        {
            foreach (var conn in _backendConnections.Values)
            {
                conn.Dispose();
            }
            _backendConnections.Clear();
        }


        public async Task<List<object>> ListResourcesAsync(string body, HttpContext? httpContext = null)
        {
            var resources = await _resourceRoutingManager.ListResourcesAsync(body, _backendConnections, _logger, EnsureBackendsInitializedAsync, _sessionManager);
            return await FilterAuthorizedAsync(resources, "resources/list", "uri", httpContext);
        }

        public async Task<object?> ReadResourceAsync(string resourceUri, string body, HttpContext? httpContext = null)
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

                // RBAC Check — use the live per-request HttpContext when provided
                var isAuth = await IsUserAuthorizedAsync("resources/read", resourceUri, null, httpContext);
                if (!isAuth)
                {
                    var identity = await ResolveUserIdentityAsync(httpContext);
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
                await AuditInvocationAsync("resources/read", resourceUri, body, statusCode, stopwatch.ElapsedMilliseconds, responsePayload, errorMessage, httpContext);
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

        public async Task<List<object>> ListPromptsAsync(string body, HttpContext? httpContext = null)
        {
            var prompts = await _promptRoutingManager.ListPromptsAsync(body, _backendConnections, _logger, EnsureBackendsInitializedAsync, _sessionManager);
            return await FilterAuthorizedAsync(prompts, "prompts/list", "name", httpContext);
        }

        public async Task<object?> GetPromptAsync(string promptName, string body, HttpContext? httpContext = null)
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

                // RBAC Check — use the live per-request HttpContext when provided
                var isAuth = await IsUserAuthorizedAsync("prompts/get", promptName, null, httpContext);
                if (!isAuth)
                {
                    var identity = await ResolveUserIdentityAsync(httpContext);
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
                await AuditInvocationAsync("prompts/get", promptName, body, statusCode, stopwatch.ElapsedMilliseconds, responsePayload, errorMessage, httpContext);
            }
        }

    }
}
