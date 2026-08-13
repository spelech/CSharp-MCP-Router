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

        public TaskCompletionSource<JsonRpcResponse> CreateRequest(string id)
        {
            var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!PendingRequests.TryAdd(id, tcs))
            {
                throw new InvalidOperationException($"A pending request with ID '{id}' already exists. Cannot overwrite silently.");
            }
            return tcs;
        }

        public PendingRequestTcs CreateTrackedRequest(
            string upstreamId,
            object? originalId,
            string? sessionId,
            CancellationToken cancellationToken,
            TimeSpan timeout)
        {
            var tcs = new PendingRequestTcs(originalId, upstreamId, sessionId, cancellationToken, timeout);
            if (!PendingRequests.TryAdd(upstreamId, tcs))
            {
                throw new InvalidOperationException($"A pending request with upstream ID '{upstreamId}' already exists. Cannot overwrite silently.");
            }
            return tcs;
        }

        public bool TryCompleteRequest(string id, JsonRpcResponse response)
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

        public void CancelAll()
        {
            foreach (var tcs in PendingRequests.Values)
            {
                tcs.TrySetCanceled();
            }
            PendingRequests.Clear();
        }
    }
}
