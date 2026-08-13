using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using McpRouter.Models;

namespace McpRouter.Core.Transports
{
    public class PendingRequestTcs : TaskCompletionSource<JsonRpcResponse>
    {
        public object? OriginalId { get; }
        public string UpstreamId { get; }
        public string? SessionId { get; }
        public CancellationToken CancellationToken { get; }
        public DateTime Expiry { get; }

        public PendingRequestTcs(
            object? originalId,
            string upstreamId,
            string? sessionId,
            CancellationToken cancellationToken,
            TimeSpan timeout)
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
            OriginalId = originalId;
            UpstreamId = upstreamId;
            SessionId = sessionId;
            CancellationToken = cancellationToken;
            Expiry = DateTime.UtcNow.Add(timeout);
        }
    }

    public class JsonRpcStateManager
    {
        public ConcurrentDictionary<string, TaskCompletionSource<JsonRpcResponse>> PendingRequests { get; } = new();
        private readonly object _lock = new();

        public TaskCompletionSource<JsonRpcResponse> CreateRequest(string id)
        {
            lock (_lock)
            {
                var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!PendingRequests.TryAdd(id, tcs))
                {
                    throw new InvalidOperationException($"Duplicate request ID '{id}' detected. Silent overwrite prevented.");
                }
                return tcs;
            }
        }

        public PendingRequestTcs CreateTrackedRequest(
            string upstreamId,
            object? originalId,
            string? sessionId,
            CancellationToken cancellationToken,
            TimeSpan timeout)
        {
            lock (_lock)
            {
                var tcs = new PendingRequestTcs(originalId, upstreamId, sessionId, cancellationToken, timeout);
                if (!PendingRequests.TryAdd(upstreamId, tcs))
                {
                    throw new InvalidOperationException($"Duplicate request ID '{upstreamId}' detected. Silent overwrite prevented.");
                }
                return tcs;
            }
        }

        public bool TryCompleteRequest(string id, JsonRpcResponse response)
        {
            lock (_lock)
            {
                if (PendingRequests.TryRemove(id, out var tcs))
                {
                    if (tcs is PendingRequestTcs tracked && response != null)
                    {
                        response.Id = tracked.OriginalId;
                    }
                    tcs.TrySetResult(response);
                    return true;
                }
                return false;
            }
        }

        public void CancelAll()
        {
            lock (_lock)
            {
                foreach (var tcs in PendingRequests.Values)
                {
                    tcs.TrySetCanceled();
                }
                PendingRequests.Clear();
            }
        }
    }
}
