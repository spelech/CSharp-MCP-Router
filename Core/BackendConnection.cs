using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Core.Transports;

namespace McpRouter
{
    public class BackendConnection : IDisposable
    {
        private readonly McpServer _server;
        private readonly ITransport _transport;
        private readonly JsonRpcStateManager _stateManager;
        
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonRpcMessageConverter() }
        };
        
        public ConcurrentDictionary<string, TaskCompletionSource<JsonRpcResponse>> PendingRequests => _stateManager.PendingRequests;
        public TimeSpan RequestTimeout { get => _transport.RequestTimeout; set => _transport.RequestTimeout = value; }

        public BackendConnection(McpServer server, HttpClient httpClient, ILogger logger)
        {
            _server = server;
            _stateManager = new JsonRpcStateManager();

            if (server.Type == "http" || server.Type == "custom" || server.Type == "streamable")
            {
                _transport = new HttpTransport(server, httpClient, logger);
            }
            else
            {
                _transport = new SseTransport(server, httpClient, logger, _stateManager);
            }
        }

        public async Task ConnectAsync()
        {
            await _transport.ConnectAsync();
        }

        public void StartReader(Func<JsonRpcMessage, Task> onMessageReceived)
        {
            _transport.StartReader(async (message) => 
            {
                if (message is JsonRpcResponse response && response.Id != null)
                {
                    var idStr = response.Id.ToString();
                    if (idStr != null)
                    {
                        // Fallback hook for old way, even though stateManager handles it inside SseTransport
                        if (PendingRequests.TryRemove(idStr, out var oldTcs))
                        {
                            oldTcs.SetResult(response);
                        }
                        
                        if (_stateManager.TryCompleteRequest(idStr, response))
                        {
                            return;
                        }
                    }
                }
                
                await onMessageReceived(message);
            });
        }

        public async Task<JsonRpcResponse> SendRequestAsync(string method, string bodyJson)
        {
            return await _transport.SendRequestAsync(method, bodyJson);
        }

        public async Task<JsonRpcResponse> CallMethodAsync(string method, object parameters, string? overrideId = null)
        {
            if (_transport is SseTransport sse)
                return await sse.CallMethodAsync(method, parameters, overrideId);
            else if (_transport is HttpTransport http)
                return await http.CallMethodAsync(method, parameters, overrideId);
                
            throw new NotSupportedException();
        }

        public async Task SendNotificationAsync(string method, string bodyJson)
        {
            await _transport.SendNotificationAsync(method, bodyJson);
        }

        public async Task SendResponseAsync(JsonRpcResponse response)
        {
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            await _transport.SendResponseAsync(json);
        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}
