using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpRouter.Models;

namespace McpRouter
{
    public class BackendConnection : IDisposable
    {
        private readonly McpServer _server;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts = new();
        
        private string? _messageUrl;
        private string _sessionId = Guid.NewGuid().ToString("N");
        private Task? _readerTask;
        private Task? _pingTask;

        public ConcurrentDictionary<string, TaskCompletionSource<JsonRpcResponse>> PendingRequests { get; } = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonRpcMessageConverter() }
        };

        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public BackendConnection(McpServer server, HttpClient httpClient, ILogger logger)
        {
            _server = server;
            _httpClient = httpClient;
            _logger = logger;
            if (server.Type == "streamable")
            {
                _sessionId = string.Empty;
            }
        }

        private void ConfigureRequest(HttpRequestMessage request, string targetUrl)
        {
            request.Headers.Host = "localhost";
        }

        private void ApplyAuthAndCustomHeaders(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(_server.ApiKey))
            {
                if (_server.Id == "ha")
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation("X-API-Key", _server.ApiKey);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
                }
            }

            if (!string.IsNullOrEmpty(_server.HeadersJson))
            {
                try
                {
                    var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(_server.HeadersJson);
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
            ConfigureRequest(request, _server.Url);
            request.Headers.Add("Mcp-Session-Id", _sessionId);
            ApplyAuthAndCustomHeaders(request);

            _logger.LogInformation("Connecting to backend {ServerId} SSE stream at {Url}...", _server.Id, _server.Url);
        }

        public void StartReader(Func<JsonRpcMessage, Task> onMessageReceived)
        {
            if (_server.Type == "http" || _server.Type == "custom" || _server.Type == "streamable")
            {
                _logger.LogInformation("Server {ServerId} is HTTP/Custom/Streamable type; skipping background SSE reader.", _server.Id);
                return;
            }

            _readerTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, _server.Url);
                        ConfigureRequest(request, _server.Url);
                        request.Headers.Accept.Clear();
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                        request.Headers.Add("Mcp-Session-Id", _sessionId);
                        
                        ApplyAuthAndCustomHeaders(request);
                        
                        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                        response.EnsureSuccessStatusCode();

                        foreach (var h in response.Headers)
                        {
                            _logger.LogInformation("Response Header for {ServerId}: {Key} = {Val}", _server.Id, h.Key, string.Join(", ", h.Value));
                        }
                        foreach (var h in response.Content.Headers)
                        {
                            _logger.LogInformation("Response Content Header for {ServerId}: {Key} = {Val}", _server.Id, h.Key, string.Join(", ", h.Value));
                        }

                        // Check for Mcp-Session-Id header (for Streamable HTTP transport)
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
                            _messageUrl = _server.Url;
                            _logger.LogInformation("Captured Mcp-Session-Id for {ServerId}: {_sessionId}. Using same URL for POST requests.", _server.Id, _sessionId);
                        }
                        else if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
                        {
                            _messageUrl = _server.Url;
                            _logger.LogInformation("Response is text/event-stream for {ServerId}. Defaulting message URL to same URL.", _server.Id);
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
                                    // Resolve post message URL
                                    // Can be relative or absolute
                                    if (Uri.IsWellFormedUriString(data, UriKind.Absolute))
                                    {
                                        _messageUrl = data;
                                    }
                                    else
                                    {
                                        var baseUri = new Uri(_server.Url);
                                        _messageUrl = new Uri(baseUri, data).ToString();
                                    }
                                    _logger.LogInformation("Resolved Message URL for {ServerId}: {_messageUrl}", _server.Id, _messageUrl);
                                }
                                else if (currentEvent == "message")
                                {
                                    try
                                    {
                                        var responseObj = JsonSerializer.Deserialize<JsonRpcMessage>(data, _jsonOptions);
                                        if (responseObj != null) await onMessageReceived(responseObj);
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
                        var pending = PendingRequests.Values.ToList();
                        PendingRequests.Clear();
                        foreach (var tcs in pending)
                        {
                            tcs.TrySetException(new HttpRequestException($"Backend disconnected: {ex.Message}", ex));
                        }
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

        private async Task<JsonRpcResponse> SendDirectPostAsync(string bodyJson)
        {
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var req = new HttpRequestMessage(HttpMethod.Post, _server.Url) { Content = content };
            ConfigureRequest(req, _server.Url);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (!string.IsNullOrEmpty(_sessionId))
                req.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);

            ApplyAuthAndCustomHeaders(req);

            using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ctsTimeout.Token);

            var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            resp.EnsureSuccessStatusCode();

            // Capture session ID from response headers
            if (resp.Headers.TryGetValues("Mcp-Session-Id", out var sVals))
                _sessionId = sVals.FirstOrDefault() ?? _sessionId;
            else if (resp.Content.Headers.TryGetValues("Mcp-Session-Id", out var scVals))
                _sessionId = scVals.FirstOrDefault() ?? _sessionId;

            var responseBody = await resp.Content.ReadAsStringAsync(linked.Token);

            // Handle SSE-wrapped responses (event: message\ndata: {...})
            if (responseBody.TrimStart().StartsWith("event:") || responseBody.TrimStart().StartsWith("data:"))
            {
                using var sr = new StringReader(responseBody);
                string? currentEvent = null;
                string? dataValue = null;
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("event:")) currentEvent = line.Substring(6).Trim();
                    else if (line.StartsWith("data:")) dataValue = line.Substring(5).Trim();
                }
                if (!string.IsNullOrEmpty(dataValue)) responseBody = dataValue;
            }

            var responseObj = JsonSerializer.Deserialize<JsonRpcResponse>(responseBody);
            return responseObj ?? new JsonRpcResponse { Error = new JsonRpcError { Code = -32603, Message = "Failed to deserialize POST response" } };
        }

        public async Task<JsonRpcResponse> SendRequestAsync(string method, string bodyJson)
        {
            if (_server.Type == "http" || _server.Type == "custom" || _server.Type == "streamable")
            {
                return await SendDirectPostAsync(bodyJson);
            }

            if (_messageUrl == null)
            {
                _logger.LogWarning("Cannot send request {Method} to {ServerId}: No message URL available (SSE endpoint not received).", method, _server.Id);
                return new JsonRpcResponse { Error = new JsonRpcError { Code = -32001, Message = "Not connected" } };
            }

            // Extract client JSON-RPC request ID
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
                    // Inject our own ID if missing
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyJson) ?? new();
                    dict["id"] = requestId;
                    modifiedBody = JsonSerializer.Serialize(dict);
                }
            } catch { }

            var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            PendingRequests[requestId] = tcs;

            try
            {
                var content = new StringContent(modifiedBody, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                
                // Build post message
                using var req = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content };
                ConfigureRequest(req, _messageUrl);
                req.Headers.Accept.Clear();
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    req.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
                }

                ApplyAuthAndCustomHeaders(req);

                using var res = await _httpClient.SendAsync(req, _cts.Token);
                res.EnsureSuccessStatusCode();

                return await tcs.Task.WaitAsync(RequestTimeout, _cts.Token);
            }
            finally
            {
                PendingRequests.TryRemove(requestId, out _);
            }
        }

        public async Task<JsonRpcResponse> CallMethodAsync(string method, object parameters, string? overrideId = null)
        {
            var bodyObj = new { jsonrpc = "2.0", method = method, @params = parameters, id = overrideId ?? Guid.NewGuid().ToString("N") };
            var bodyJson = JsonSerializer.Serialize(bodyObj);

            if (_server.Type == "http" || _server.Type == "custom" || _server.Type == "streamable")
            {
                return await SendDirectPostAsync(bodyJson);
            }

            // Wait up to 5 seconds for message URL to resolve
            int attempts = 0;
            while (_messageUrl == null && attempts < 50)
            {
                await Task.Delay(100);
                attempts++;
            }

            if (_messageUrl == null)
            {
                throw new InvalidOperationException($"Backend {_server.Id} has not sent its endpoint event yet.");
            }

            // Extract client JSON-RPC request ID
            string requestId = bodyObj.id;
            
            var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            PendingRequests[requestId] = tcs;

            try
            {
                var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                
                // Build post message
                using var postReq = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content };
                ConfigureRequest(postReq, _messageUrl);
                postReq.Headers.Accept.Clear();
                postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    postReq.Headers.Add("Mcp-Session-Id", _sessionId);
                }
                
                ApplyAuthAndCustomHeaders(postReq);

                var postResp = await _httpClient.SendAsync(postReq, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                postResp.EnsureSuccessStatusCode();

                // Capture session ID from POST response headers if not already set
                if (string.IsNullOrEmpty(_sessionId))
                {
                    IEnumerable<string>? postSessionValues = null;
                    if (postResp.Headers.TryGetValues("Mcp-Session-Id", out var phVals))
                    {
                        postSessionValues = phVals;
                    }
                    else if (postResp.Content.Headers.TryGetValues("Mcp-Session-Id", out var pcVals))
                    {
                        postSessionValues = pcVals;
                    }

                    if (postSessionValues != null)
                    {
                        _sessionId = postSessionValues.FirstOrDefault() ?? string.Empty;
                        _logger.LogInformation("Captured Mcp-Session-Id from POST response for {ServerId}: {_sessionId}", _server.Id, _sessionId);
                    }
                }

                // Await the response from the SSE stream reader (max 10s timeout)
                using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ctsTimeout.Token);
                
                linkedCts.Token.Register(() => tcs.TrySetCanceled());

                try
                {
                    return await tcs.Task;
                }
                catch (OperationCanceledException)
                {
                    if (ctsTimeout.IsCancellationRequested)
                    {
                        throw new TimeoutException($"Request to backend {_server.Id} timed out after 30 seconds.");
                    }
                    throw;
                }
            }
            finally
            {
                PendingRequests.TryRemove(requestId, out _);
            }
        }

        public async Task SendNotificationAsync(string method, string bodyJson)
        {
            if (_server.Type == "streamable")
            {
                // Fire-and-forget for streamable — notifications don't need responses
                try { await SendDirectPostAsync(bodyJson); } catch { /* ignore */ }
                return;
            }

            if (_server.Type == "http" || _server.Type == "custom")
            {
                var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using var postReq = new HttpRequestMessage(HttpMethod.Post, _server.Url) { Content = content };
                ConfigureRequest(postReq, _server.Url);
                postReq.Headers.Accept.Clear();
                postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                
                ApplyAuthAndCustomHeaders(postReq);

                await _httpClient.SendAsync(postReq, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                return;
            }

            int attempts = 0;
            while (_messageUrl == null && attempts < 50)
            {
                await Task.Delay(100);
                attempts++;
            }

            if (_messageUrl == null) return;

            var content2 = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            content2.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var postReq2 = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content2 };
            ConfigureRequest(postReq2, _messageUrl);
            postReq2.Headers.Accept.Clear();
            postReq2.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            postReq2.Headers.Add("Mcp-Session-Id", _sessionId);
            
            ApplyAuthAndCustomHeaders(postReq2);

            await _httpClient.SendAsync(postReq2, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
