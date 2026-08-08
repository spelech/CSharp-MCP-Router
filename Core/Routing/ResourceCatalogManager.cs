using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace McpRouter.Core.Routing
{
    /// <summary>
    /// Auto-generated XML documentation.
    /// </summary>
    public class ResourceCatalogManager
    {
        public async Task<List<object>> SearchResourcesAsync(string query, List<object> resources)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return resources.Take(15).ToList();
            }

            var queryLower = query.ToLower();
            var results = new List<object>();

            foreach (var res in resources)
            {
                string name = "";
                string description = "";

                if (res is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("name", out var n)) name = n?.ToString() ?? "";
                    if (dict.TryGetValue("description", out var d)) description = d?.ToString() ?? "";
                }
                else if (res is JsonElement je)
                {
                    if (je.TryGetProperty("name", out var n)) name = n.GetString() ?? "";
                    if (je.TryGetProperty("description", out var d)) description = d.GetString() ?? "";
                }

                if (name.Contains(queryLower, StringComparison.OrdinalIgnoreCase) ||
                    description.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(res);
                }
            }

            return results.Take(15).ToList();
        }

        public bool TryMatchLogsTemplate(string uri, out string serverId)
        {
            var match = Regex.Match(uri, @"^logs://([^/]+)/today$");
            if (match.Success)
            {
                serverId = match.Groups[1].Value;
                return true;
            }
            serverId = string.Empty;
            return false;
        }
    }
}
