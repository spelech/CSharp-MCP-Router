using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace McpRouter
{
    public partial class ClientSession
    {
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
                _logger.LogDebug("[JSON-RPC Gateway -> Client] {Payload}", McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(json));
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

        public async Task BroadcastNotificationAsync(string method, string body)
        {
            var tasks = new List<Task>();
            foreach (var conn in _backendConnections.Values)
            {
                tasks.Add(conn.SendNotificationAsync(method, body));
            }
            await Task.WhenAll(tasks);
        }
    }
}
