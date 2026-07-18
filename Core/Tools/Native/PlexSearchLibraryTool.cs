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
public class PlexSearchLibraryTool : ICustomTool
    {
        public string Name => "plex_search_library";
        public string Description => "Perform a global search across all libraries in Plex Media Server.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "The title or keyword to search for" }
            },
            required = new[] { "query" }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            var query = parameters.GetProperty("query").GetString();
            var plex = await db.Servers.FirstOrDefaultAsync(s => s.Id == "plex");
            if (plex == null || !plex.Enabled)
            {
                return new { error = "Plex service is not configured or disabled in MCP Router." };
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{plex.Url.TrimEnd('/')}/hubs/search?query={Uri.EscapeDataString(query ?? "")}");
            req.Headers.Add("X-Plex-Token", plex.ApiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }
}
