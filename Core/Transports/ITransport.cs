using System;
using System.Threading.Tasks;
using McpRouter.Models;

namespace McpRouter.Core.Transports
{
    public interface ITransport : IDisposable
    {
        Task ConnectAsync();
        void StartReader(Func<JsonRpcMessage, Task> onMessageReceived);
        Task<JsonRpcResponse> SendRequestAsync(string method, string body);
        Task SendNotificationAsync(string method, string body);
        Task SendResponseAsync(string responseJson);
        TimeSpan RequestTimeout { get; set; }
    }
}
