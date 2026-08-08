using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Core.Database;

namespace McpRouter.CustomTools
{
    public interface ICustomTool
    {
        string Name { get; }
        string Description { get; }
        object InputSchema { get; }
        Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, IDbConnectionFactory dbFactory);
    }
}
