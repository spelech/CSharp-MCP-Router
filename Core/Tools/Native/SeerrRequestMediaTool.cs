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
public class SeerrRequestMediaTool : ICustomTool
    {
        public string Name => "seerr_request_media";
        public string Description => "Submit a new media request for a movie or TV show on Overseerr/Seerr.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                mediaType = new { type = "string", @enum = new[] { "movie", "tv" }, description = "Type: movie or tv" },
                mediaId = new { type = "integer", description = "The TMDB ID of the movie or show" },
                seasons = new { type = "array", items = new { type = "integer" }, description = "List of season numbers to request (for TV shows). Leave empty for all." }
            },
            required = new[] { "mediaType", "mediaId" }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            var mediaType = parameters.GetProperty("mediaType").GetString();
            var mediaId = parameters.GetProperty("mediaId").GetInt32();
            
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

            // Overseerr request structure
            var requestBody = new Dictionary<string, object>
            {
                { "mediaType", mediaType ?? "movie" },
                { "mediaId", mediaId }
            };

            if (mediaType == "tv" && parameters.TryGetProperty("seasons", out var seasonsProp) && seasonsProp.ValueKind == JsonValueKind.Array)
            {
                var seasons = new List<int>();
                foreach (var item in seasonsProp.EnumerateArray())
                {
                    seasons.Add(item.GetInt32());
                }
                if (seasons.Count > 0)
                {
                    requestBody["seasons"] = seasons;
                }
                else
                {
                    requestBody["seasons"] = "all";
                }
            }
            else if (mediaType == "tv")
            {
                requestBody["seasons"] = "all";
            }

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{apiBase}/api/v1/request")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.Add("X-Api-Key", seerr.ApiKey);

            var resp = await httpClient.SendAsync(req);
            var content = await resp.Content.ReadAsStringAsync();
            
            if (!resp.IsSuccessStatusCode)
            {
                return new { error = $"Request failed with status {resp.StatusCode}: {content}" };
            }
            
            return JsonSerializer.Deserialize<object>(content) ?? new { success = true };
        }
    }
}
