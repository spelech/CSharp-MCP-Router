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
using McpRouter.Core.Secrets;
using McpRouter.Models;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Transports
{
    public class HttpTransport : ITransport
    {
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);
        private readonly McpServer _server;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly ISecretRetriever? _secretRetriever;
        private readonly CancellationTokenSource _cts = new();
        private string _sessionId = string.Empty;

        public HttpTransport(McpServer server, HttpClient httpClient, ILogger logger, ISecretRetriever? secretRetriever = null)
        {
            _server = server;
            _httpClient = httpClient;
            _logger = logger;
            _secretRetriever = secretRetriever;
        }

        public async Task<string?> ResolveTokenAsync(ISecretRetriever? secretRetriever = null)
        {
            var provider = _server.SecretProvider ?? "None";
            if (provider.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(_server.ApiKey) ? _server.ApiKey : null;
            }

            var retriever = secretRetriever ?? _secretRetriever;
            if (retriever == null)
            {
                throw new InvalidOperationException($"SecretProvider is configured to '{provider}' for server '{_server.Id}', but no secret retriever is registered.");
            }

            string path = !string.IsNullOrWhiteSpace(_server.SecretPath) ? _server.SecretPath : _server.Url;
            if (!string.IsNullOrWhiteSpace(_server.SecretMount))
            {
                path = $"{_server.SecretMount}:{path}";
            }
            string field = !string.IsNullOrWhiteSpace(_server.SecretField)
                ? _server.SecretField
                : (!string.IsNullOrWhiteSpace(_server.SecretItemKey) ? _server.SecretItemKey : "ApiKey");

            string? secret = null;
            if (retriever is CompositeSecretRetriever composite)
            {
                secret = await composite.GetSecretForProviderAsync(provider, path, field);
            }
            else
            {
                secret = await retriever.GetSecretAsync(path, field);
            }

            if (string.IsNullOrEmpty(secret))
            {
                throw new System.Security.SecurityException($"Failed to resolve secret from provider '{provider}' for server '{_server.Id}' (path: '{path}', field: '{field}'). Plaintext ApiKey fallback is disabled.");
            }

            return secret;
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

        public Task ConnectAsync()
        {
            // HTTP transport does not need persistent connection
            return Task.CompletedTask;
        }

        public void StartReader(Func<JsonRpcMessage, Task> onMessageReceived)
        {
            // HTTP transport has no background reader
        }

        public async Task<JsonRpcResponse> SendRequestAsync(string method, string bodyJson)
        {
            _logger.LogInformation("[JSON-RPC Gateway -> Backend {ServerId}] {Payload}", _server.Id, bodyJson);
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var req = new HttpRequestMessage(HttpMethod.Post, _server.Url) { Content = content };
            req.Headers.Host = "localhost";
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (!string.IsNullOrEmpty(_sessionId))
                req.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);

            await ApplyAuthAndCustomHeadersAsync(req);

            using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ctsTimeout.Token);

            var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            resp.EnsureSuccessStatusCode();

            if (resp.Headers.TryGetValues("Mcp-Session-Id", out var sVals))
                _sessionId = sVals.FirstOrDefault() ?? _sessionId;
            else if (resp.Content.Headers.TryGetValues("Mcp-Session-Id", out var scVals))
                _sessionId = scVals.FirstOrDefault() ?? _sessionId;

            string responseBody = string.Empty;
            using (var stream = await resp.Content.ReadAsStreamAsync(linked.Token))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync(linked.Token)) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("data:"))
                    {
                        responseBody = trimmed.Substring(5).Trim();
                        break;
                    }
                    else if (trimmed.StartsWith("{"))
                    {
                        responseBody = trimmed;
                        break;
                    }
                }
            }

            _logger.LogInformation("[JSON-RPC Backend {ServerId} -> Gateway] {Payload}", _server.Id, responseBody);
            _logger.LogInformation("[HttpTransport DEBUG] Server {ServerId} responded with status {StatusCode}. Body: '{Body}'", _server.Id, resp.StatusCode, responseBody);

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new JsonRpcResponse();
            }

            var responseObj = JsonSerializer.Deserialize<JsonRpcResponse>(responseBody);
            return responseObj ?? new JsonRpcResponse { Error = new JsonRpcError { Code = -32603, Message = "Failed to deserialize POST response" } };
        }

        public async Task<JsonRpcResponse> CallMethodAsync(string method, object parameters, string? overrideId = null)
        {
            var bodyObj = new { jsonrpc = "2.0", method = method, @params = parameters, id = overrideId ?? Guid.NewGuid().ToString("N") };
            var bodyJson = JsonSerializer.Serialize(bodyObj);
            return await SendRequestAsync(method, bodyJson);
        }

        public async Task SendNotificationAsync(string method, string bodyJson)
        {
            await SendRequestAsync(method, bodyJson);
        }

        public Task SendResponseAsync(string responseJson)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
