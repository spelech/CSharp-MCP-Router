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
public class SeerrGetMediaDetailsTool : ICustomTool
    {
        public string Name => "seerr_get_media_details";
        public string Description => "Get full information about a specific movie or TV show from Overseerr, including its request/media status.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                mediaType = new { type = "string", @enum = new[] { "movie", "tv" }, description = "Type: movie or tv" },
                tmdbId = new { type = "integer", description = "The TMDB ID of the media" }
            },
            required = new[] { "mediaType", "tmdbId" }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            var mediaType = parameters.GetProperty("mediaType").GetString();
            var tmdbId = parameters.GetProperty("tmdbId").GetInt32();

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

            // Route to movie or tv endpoint
            var endpoint = mediaType == "tv" ? $"/api/v1/tv/{tmdbId}" : $"/api/v1/movie/{tmdbId}";

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{apiBase}{endpoint}");
            req.Headers.Add("X-Api-Key", seerr.ApiKey);

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }
}
