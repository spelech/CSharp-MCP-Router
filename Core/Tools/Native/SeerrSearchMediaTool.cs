using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using McpRouter.Models;

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

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            var query = parameters.GetProperty("query").GetString();
            var seerr = await db.Servers.FirstOrDefaultAsync(s => s.Id == "seerr");
            if (seerr == null || !seerr.Enabled)
            {
                return new { error = "Seerr service is not configured or disabled in MCP Router." };
            }

            // Translate internally to Seerr URL
            // URL might end in /sse or /mcp, we want the API root
            // e.g., http://seerr:5055/api/v1
            var apiBase = seerr.Url.Replace("/sse", "").Replace("/mcp", "");
            if (!apiBase.Contains(":5055"))
            {
                apiBase = "http://seerr:5055"; // Default container target
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
