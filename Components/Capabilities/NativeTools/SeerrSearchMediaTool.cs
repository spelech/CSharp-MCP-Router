using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Models;
using Dapper;

namespace McpRouter.CustomTools
{
    public class SeerrSearchMediaTool : ICustomTool
    {
        public string Name => "seerr_search_media";
        public string Description => "Search for movies or TV shows on Overseerr/Seerr to get their ID, status, and details.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "The search query (movie or show title)" }
            },
            required = new[] { "query" }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, IDbConnectionFactory dbFactory)
        {
            var query = parameters.GetProperty("query").GetString();
            using var conn = dbFactory.CreateConnection();
            var seerr = await conn.QueryFirstOrDefaultAsync<McpServer>("SELECT * FROM Servers WHERE Id = 'seerr'");
            if (seerr == null || !seerr.Enabled)
            {
                return new { error = "Seerr service is not configured or disabled in MCP Router." };
            }

            var apiBase = seerr.Url.Replace("/sse", "").Replace("/mcp", "");
            if (!apiBase.Contains(":5055"))
            {
                apiBase = "http://seerr:5055";
            }
            apiBase = apiBase.TrimEnd('/');

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{apiBase}/api/v1/search?query={Uri.EscapeDataString(query ?? "")}");
            req.Headers.Add("X-Api-Key", seerr.ApiKey);

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }
}
