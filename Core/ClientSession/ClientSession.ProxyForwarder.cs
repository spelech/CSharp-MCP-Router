using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpRouter.Models;

namespace McpRouter
{
    public partial class ClientSession
    {
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
    }
}
