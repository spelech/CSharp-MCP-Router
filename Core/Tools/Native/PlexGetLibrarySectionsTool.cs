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
public class PlexGetLibrarySectionsTool : ICustomTool
    {
        public string Name => "plex_get_library_sections";
        public string Description => "List all configured library sections on Plex (Movies, TV Shows, Music, etc.) with their IDs.";

        public object InputSchema => new
        {
            type = "object",
            properties = new { }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            var plex = await db.Servers.FirstOrDefaultAsync(s => s.Id == "plex");
            if (plex == null || !plex.Enabled)
            {
                return new { error = "Plex service is not configured or disabled." };
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{plex.Url.TrimEnd('/')}/library/sections");
            req.Headers.Add("X-Plex-Token", plex.ApiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }
}
