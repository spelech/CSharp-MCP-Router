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
public class PlexGetMetadataTool : ICustomTool
    {
        public string Name => "plex_get_metadata";
        public string Description => "Get full detailed metadata details for a specific item (ratingKey) on Plex.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                ratingKey = new { type = "string", description = "The unique Plex ratingKey of the item" }
            },
            required = new[] { "ratingKey" }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            var ratingKey = parameters.GetProperty("ratingKey").GetString();
            var plex = await db.Servers.FirstOrDefaultAsync(s => s.Id == "plex");
            if (plex == null || !plex.Enabled)
            {
                return new { error = "Plex service is not configured or disabled." };
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{plex.Url.TrimEnd('/')}/library/metadata/{ratingKey}");
            req.Headers.Add("X-Plex-Token", plex.ApiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }
}
