using System.Collections.Concurrent;
using System.Threading.Tasks;
using McpRouter.Models;

namespace McpRouter.Core.Transports
{
    public class JsonRpcStateManager
    {
        public ConcurrentDictionary<string, TaskCompletionSource<JsonRpcResponse>> PendingRequests { get; } = new();

        public TaskCompletionSource<JsonRpcResponse> CreateRequest(string id)
        {
            var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            PendingRequests[id] = tcs;
            return tcs;
        }

        public bool TryCompleteRequest(string id, JsonRpcResponse response)
        {
            if (PendingRequests.TryRemove(id, out var tcs))
            {
                tcs.SetResult(response);
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
