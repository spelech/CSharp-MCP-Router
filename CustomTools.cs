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
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        object InputSchema { get; }
        Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db);
    }

    public static class CustomToolRegistry
    {
        private static readonly Dictionary<string, IMcpTool> _tools = new();

        static CustomToolRegistry()
        {
            Register(new SeerrSearchMediaTool());
            Register(new SeerrRequestMediaTool());
            Register(new SeerrGetRequestsTool());
            Register(new SeerrGetMediaDetailsTool());
            
            Register(new PlexSearchLibraryTool());
            Register(new PlexGetLibrarySectionsTool());
            Register(new PlexGetSessionsTool());
            Register(new PlexGetRecentlyAddedTool());
            Register(new PlexGetMetadataTool());
        }

        private static void Register(IMcpTool tool)
        {
            _tools[tool.Name] = tool;
        }

        public static IEnumerable<IMcpTool> GetAll() => _tools.Values;

        public static IMcpTool? Get(string name)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }
    }

    // =========================================================================
    // OVERSEERR / SEERR TOOLS
    // =========================================================================

    public class SeerrSearchMediaTool : IMcpTool
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

    public class SeerrRequestMediaTool : IMcpTool
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

    public class SeerrGetRequestsTool : IMcpTool
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

    public class SeerrGetMediaDetailsTool : IMcpTool
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


    // =========================================================================
    // PLEX TOOLS
    // =========================================================================

    public class PlexSearchLibraryTool : IMcpTool
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

    public class PlexGetLibrarySectionsTool : IMcpTool
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

    public class PlexGetSessionsTool : IMcpTool
    {
        public string Name => "plex_get_sessions";
        public string Description => "Retrieve active media playing sessions on Plex (who is watching what).";

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

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{plex.Url.TrimEnd('/')}/status/sessions");
            req.Headers.Add("X-Plex-Token", plex.ApiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }

    public class PlexGetRecentlyAddedTool : IMcpTool
    {
        public string Name => "plex_get_recently_added";
        public string Description => "List recently added media items in a specific library section.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                sectionId = new { type = "integer", description = "The ID of the library section" }
            },
            required = new[] { "sectionId" }
        };

        public async Task<object> ExecuteAsync(JsonElement parameters, HttpClient httpClient, RouterDbContext db)
        {
            var sectionId = parameters.GetProperty("sectionId").GetInt32();
            var plex = await db.Servers.FirstOrDefaultAsync(s => s.Id == "plex");
            if (plex == null || !plex.Enabled)
            {
                return new { error = "Plex service is not configured or disabled." };
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{plex.Url.TrimEnd('/')}/library/sections/{sectionId}/recentlyAdded");
            req.Headers.Add("X-Plex-Token", plex.ApiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) ?? new { };
        }
    }

    public class PlexGetMetadataTool : IMcpTool
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
