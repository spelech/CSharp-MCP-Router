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
public class SeerrGetRequestsTool : ICustomTool
    {
        public string Name => "seerr_get_requests";
        public string Description => "Retrieve a list of media requests from Overseerr/Seerr.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                take = new { type = "integer", description = "Number of items to return (default 20)" },
                skip = new { type = "integer", description = "Offset for pagination (default 0)" },
                filter = new { type = "string", @enum = new[] { "all", "pending", "approved", "processing", "available", "failed" }, description = "Filter requests by status" }
            }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            int take = parameters.TryGetProperty("take", out var t) ? t.GetInt32() : 20;
            int skip = parameters.TryGetProperty("skip", out var s) ? s.GetInt32() : 0;
            string filter = parameters.TryGetProperty("filter", out var f) ? f.GetString() ?? "all" : "all";

            var seerr = await db.Servers.FirstOrDefaultAsync(s => s.Id == "seerr");
            if (seerr == null || !seerr.Enabled)
            {
                return new { error = "Seerr service is not configured or disabled." };
            }

            var apiBase = seerr.Url.Replace("/sse", "").Replace("/mcp", "");
            if (!apiBase.Contains(":5055"))
            {
                apiBase = "http://seerr:5055";
            }
            apiBase = apiBase.TrimEnd('/');

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{apiBase}/api/v1/request?take={take}&skip={skip}&filter={filter}");
            req.Headers.Add("X-Api-Key", seerr.ApiKey);

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }
}
