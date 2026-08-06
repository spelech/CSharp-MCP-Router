using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpRouter.Models;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Transports
{
    public class SseTransport : ITransport
    {
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);
        private readonly McpServer _server;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly JsonRpcStateManager _stateManager;
        private readonly CancellationTokenSource _cts = new();
        private readonly McpRouter.Core.Secrets.CompositeSecretRetriever? _secretRetriever;
        
        private string? _messageUrl;
        private readonly TaskCompletionSource<string> _messageUrlTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private string _sessionId = Guid.NewGuid().ToString("N");
        private Task? _readerTask;
        private Task? _pingTask;

        private void SetMessageUrl(string url)
        {
            _messageUrl = url;
            _messageUrlTcs.TrySetResult(url);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonRpcMessageConverter() }
        };

        public SseTransport(McpServer server, HttpClient httpClient, ILogger logger, JsonRpcStateManager stateManager, McpRouter.Core.Secrets.CompositeSecretRetriever? secretRetriever = null)
        {
            _server = server;
            _httpClient = httpClient;
            _logger = logger;
            _stateManager = stateManager;
            _secretRetriever = secretRetriever;
        }

        private async Task<string?> ResolveTokenAsync()
        {
            var provider = _server.SecretProvider ?? "None";
            if (provider != "None" && !string.IsNullOrEmpty(provider) && _secretRetriever != null)
            {
                var secretPath = _server.Url;
                var keyName = _server.SecretItemKey ?? "ApiKey";
                return await _secretRetriever.GetSecretAsync(secretPath, keyName);
            }
            return !string.IsNullOrEmpty(_server.ApiKey) ? _server.ApiKey : null;
        }

        private void ApplyAuthAndCustomHeaders(HttpRequestMessage request)
        {
            ApplyAuthAndCustomHeadersAsync(request).GetAwaiter().GetResult();
        }

        private async Task ApplyAuthAndCustomHeadersAsync(HttpRequestMessage request)
        {
            var token = await ResolveTokenAsync();
            var authShape = (_server.AuthShape ?? "bearer").ToLowerInvariant();

            if (!string.IsNullOrEmpty(token))
            {
                switch (authShape)
                {
                    case "bearer":
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        break;
                    case "basic":
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
                        break;
                    case "raw":
                        request.Headers.TryAddWithoutValidation("Authorization", token);
                        break;
                    case "x-api-key":
                        request.Headers.TryAddWithoutValidation("X-API-Key", token);
                        break;
                    case "custom-header":
                        var headerName = !string.IsNullOrWhiteSpace(_server.CustomHeaderName) ? _server.CustomHeaderName : "X-Auth-Token";
                        request.Headers.TryAddWithoutValidation(headerName, token);
                        break;
                    case "query":
                        var paramName = !string.IsNullOrWhiteSpace(_server.CustomHeaderName) ? _server.CustomHeaderName : "token";
                        var uriBuilder = new UriBuilder(request.RequestUri!);
                        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                        query[paramName] = token;
                        uriBuilder.Query = query.ToString();
                        request.RequestUri = uriBuilder.Uri;
                        break;
                    default:
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(_server.HeadersJson))
            {
                try
                {
                    var customHeaders = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(_server.HeadersJson);
                    if (customHeaders != null)
                    {
                        foreach (var kvp in customHeaders)
                        {
                            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse custom headers for server {ServerId}", _server.Id);
                }
            }
        }

        public async Task ConnectAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _server.Url);
            request.Headers.Host = "localhost";
            request.Headers.Add("Mcp-Session-Id", _sessionId);
            await ApplyAuthAndCustomHeadersAsync(request);

            _logger.LogInformation("Connecting to backend {ServerId} SSE stream at {Url}...", _server.Id, _server.Url);
        }

        public void StartReader(Func<JsonRpcMessage, Task> onMessageReceived)
        {
            _readerTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, _server.Url);
                        request.Headers.Host = "localhost";
                        request.Headers.Accept.Clear();
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                        request.Headers.Add("Mcp-Session-Id", _sessionId);
                        
                        await ApplyAuthAndCustomHeadersAsync(request);
                        
                        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                        response.EnsureSuccessStatusCode();

                        IEnumerable<string>? sessionValues = null;
                        if (response.Headers.TryGetValues("Mcp-Session-Id", out var hVals))
                        {
                            sessionValues = hVals;
                        }
                        else if (response.Content.Headers.TryGetValues("Mcp-Session-Id", out var cVals))
                        {
                            sessionValues = cVals;
                        }

                        if (sessionValues != null)
                        {
                            _sessionId = sessionValues.FirstOrDefault() ?? string.Empty;
                            _ = Task.Delay(1500, _cts.Token).ContinueWith(t => {
                                if (!t.IsCanceled && _messageUrl == null) {
                                    SetMessageUrl(_server.Url);
                                }
                            });
                        }
                        else if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
                        {
                            _ = Task.Delay(1500, _cts.Token).ContinueWith(t => {
                                if (!t.IsCanceled && _messageUrl == null) {
                                    SetMessageUrl(_server.Url);
                                }
                            });
                        }
                        
                        using var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
                        using var reader = new StreamReader(stream);
                        
                        string? currentEvent = null;
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            var line = await reader.ReadLineAsync(_cts.Token);
                            if (line == null) break;
                            
                            if (line.StartsWith("event:"))
                            {
                                currentEvent = line.Substring(6).Trim();
                            }
                            else if (line.StartsWith("data:"))
                            {
                                var data = line.Substring(5).Trim();
                                if (currentEvent == "endpoint")
                                {
                                    string resolvedUrl;
                                    if (Uri.IsWellFormedUriString(data, UriKind.Absolute))
                                    {
                                        resolvedUrl = data;
                                    }
                                    else
                                    {
                                        var baseUri = new Uri(_server.Url);
                                        resolvedUrl = new Uri(baseUri, data).ToString();
                                    }
                                    SetMessageUrl(resolvedUrl);
                                }
                                else if (currentEvent == "message")
                                {
                                    try
                                    {
                                        var responseObj = JsonSerializer.Deserialize<JsonRpcMessage>(data, _jsonOptions);
                                        if (responseObj != null)
                                        {
                                            if (responseObj is not JsonRpcResponse)
                                            {
                                                _logger.LogInformation("[JSON-RPC Backend {ServerId} -> Gateway] {Payload}", _server.Id, data);
                                            }
                                            await onMessageReceived(responseObj);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to parse SSE message data: {Data}", data);
                                    }
                                }
                            }
                            else if (string.IsNullOrEmpty(line))
                            {
                                currentEvent = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Disconnected from backend {ServerId}. Reconnecting in 5s... Error: {Msg}", _server.Id, ex.Message);
                        _stateManager.CancelAll();
                        await Task.Delay(5000, _cts.Token);
                    }
                }
            });

            _pingTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token);
                    try
                    {
                        var resp = await CallMethodAsync("ping", new { });
                        if (resp.Error != null)
                        {
                            _logger.LogWarning("Ping failed for backend {ServerId}: {Code} {Message}", _server.Id, resp.Error.Code, resp.Error.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ping exception for backend {ServerId}", _server.Id);
                    }
                }
            });
        }

        public async Task<JsonRpcResponse> SendRequestAsync(string method, string bodyJson)
        {
            if (_messageUrl == null)
            {
                try
                {
                    await _messageUrlTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), _cts.Token);
                }
                catch (TimeoutException)
                {
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (_messageUrl == null)
            {
                return new JsonRpcResponse { Error = new JsonRpcError { Code = -32001, Message = "Not connected" } };
            }

            string requestId = Guid.NewGuid().ToString("N");
            string modifiedBody = bodyJson;
            
            try 
            {
                using var doc = JsonDocument.Parse(bodyJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("id", out var idProp))
                {
                    requestId = idProp.ToString();
                }
                else
                {
                    var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(bodyJson) ?? new();
                    dict["id"] = requestId;
                    modifiedBody = JsonSerializer.Serialize(dict);
                }
            } catch { }

            var tcs = _stateManager.CreateRequest(requestId);

            try
            {
                var content = new StringContent(modifiedBody, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                
                using var req = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content };
                req.Headers.Host = "localhost";
                req.Headers.Accept.Clear();
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    req.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
                }

                await ApplyAuthAndCustomHeadersAsync(req);

                _logger.LogInformation("[JSON-RPC Gateway -> Backend {ServerId}] {Payload}", _server.Id, modifiedBody);

                using var res = await _httpClient.SendAsync(req, _cts.Token);
                res.EnsureSuccessStatusCode();

                var response = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15), _cts.Token);
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                _logger.LogInformation("[JSON-RPC Backend {ServerId} -> Gateway] {Payload}", _server.Id, responseJson);
                return response;
            }
            finally
            {
                // Ensure it is removed if not already completed by the reader
                _stateManager.TryCompleteRequest(requestId, null!);
            }
        }

        public async Task<JsonRpcResponse> CallMethodAsync(string method, object parameters, string? overrideId = null)
        {
            var bodyObj = new { jsonrpc = "2.0", method = method, @params = parameters, id = overrideId ?? Guid.NewGuid().ToString("N") };
            var bodyJson = JsonSerializer.Serialize(bodyObj);

            if (_messageUrl == null)
            {
                try
                {
                    await _messageUrlTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), _cts.Token);
                }
                catch (TimeoutException)
                {
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (_messageUrl == null)
            {
                throw new InvalidOperationException($"Backend {_server.Id} has not sent its endpoint event yet.");
            }

            string requestId = bodyObj.id;
            var tcs = _stateManager.CreateRequest(requestId);

            try
            {
                var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                
                using var postReq = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content };
                postReq.Headers.Host = "localhost";
                postReq.Headers.Accept.Clear();
                postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    postReq.Headers.Add("Mcp-Session-Id", _sessionId);
                }
                
                await ApplyAuthAndCustomHeadersAsync(postReq);

                using var res = await _httpClient.SendAsync(postReq, _cts.Token);
                res.EnsureSuccessStatusCode();

                return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15), _cts.Token);
            }
            finally
            {
                _stateManager.TryCompleteRequest(requestId, null!);
            }
        }

        public async Task SendNotificationAsync(string method, string bodyJson)
        {
            if (_messageUrl == null) return;
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var req = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content };
            req.Headers.Host = "localhost";
            await ApplyAuthAndCustomHeadersAsync(req);
            if (!string.IsNullOrEmpty(_sessionId))
                req.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
            
            using var res = await _httpClient.SendAsync(req, _cts.Token);
        }

        public async Task SendResponseAsync(string responseJson)
        {
            if (_messageUrl == null) return;
            var content = new StringContent(responseJson, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var req = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content };
            req.Headers.Host = "localhost";
            await ApplyAuthAndCustomHeadersAsync(req);
            if (!string.IsNullOrEmpty(_sessionId))
                req.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
            
            using var res = await _httpClient.SendAsync(req, _cts.Token);
            res.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
