using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using McpRouter.Services;

namespace McpRouter.Core.Routing
{
    public class SemanticSearchService
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, float[]> _embeddingsCache = new();

        public static async Task<List<object>> SearchToolsSemanticAsync(string query, List<object> tools, IEmbeddingService embeddingService)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return tools.Take(15).ToList();
            }

            var queryVector = await embeddingService.GetEmbeddingAsync(query);
            var scoredTools = new List<(object Tool, double Score)>();

            foreach (var tool in tools)
            {
                string name = "";
                string description = "";

                if (tool is JsonElement je)
                {
                    name = je.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    description = je.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                }
                else if (tool is System.Collections.IDictionary dict)
                {
                    name = dict.Contains("name") ? dict["name"]?.ToString() ?? "" : "";
                    description = dict.Contains("description") ? dict["description"]?.ToString() ?? "" : "";
                }
                else
                {
                    var type = tool.GetType();
                    name = type.GetProperty("name")?.GetValue(tool)?.ToString() ?? 
                           type.GetProperty("Name")?.GetValue(tool)?.ToString() ?? "";
                    description = type.GetProperty("description")?.GetValue(tool)?.ToString() ?? 
                                  type.GetProperty("Description")?.GetValue(tool)?.ToString() ?? "";
                }

                var textToEmbed = $"{name}: {description}";
                var cacheKey = textToEmbed;

                if (!_embeddingsCache.TryGetValue(cacheKey, out var toolVector))
                {
                    try
                    {
                        toolVector = await embeddingService.GetEmbeddingAsync(textToEmbed);
                        _embeddingsCache[cacheKey] = toolVector;
                    }
                    catch
                    {
                        continue;
                    }
                }

                double score = embeddingService.CosineSimilarity(queryVector, toolVector);
                scoredTools.Add((tool, score));
            }

            return scoredTools
                .OrderByDescending(x => x.Score)
                .Select(x => x.Tool)
                .Take(15)
                .ToList();
        }

        public static List<object> SearchTools(string query, List<object> tools)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return tools;
            }

            var queryWords = query.ToLower()
                .Split(new[] { ' ', ',', '.', ';', ':', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            if (queryWords.Count == 0)
            {
                queryWords = query.ToLower()
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            var scoredTools = new List<(object Tool, double Score)>();

            foreach (var tool in tools)
            {
                string name = "";
                string description = "";

                if (tool is JsonElement je)
                {
                    name = je.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    description = je.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                }
                else if (tool is System.Collections.IDictionary dict)
                {
                    name = dict.Contains("name") ? dict["name"]?.ToString() ?? "" : "";
                    description = dict.Contains("description") ? dict["description"]?.ToString() ?? "" : "";
                }
                else
                {
                    var type = tool.GetType();
                    name = type.GetProperty("name")?.GetValue(tool)?.ToString() ?? 
                           type.GetProperty("Name")?.GetValue(tool)?.ToString() ?? "";
                    description = type.GetProperty("description")?.GetValue(tool)?.ToString() ?? 
                                  type.GetProperty("Description")?.GetValue(tool)?.ToString() ?? "";
                }

                var fullText = (name + " " + description).ToLower();
                double score = 0;
                int matches = 0;

                if (fullText.Contains(query.ToLower()))
                {
                    score += 10.0;
                }

                if (name.ToLower().Contains(query.ToLower()))
                {
                    score += 5.0;
                }

                foreach (var word in queryWords)
                {
                    if (name.ToLower().Contains(word))
                    {
                        score += 3.0;
                        matches++;
                    }
                    else if (description.ToLower().Contains(word))
                    {
                        score += 1.0;
                        matches++;
                    }
                }

                if (matches > 1)
                {
                    score += matches * 2.0;
                }

                if (score > 0)
                {
                    scoredTools.Add((tool, score));
                }
            }

            var results = scoredTools
                .OrderByDescending(x => x.Score)
                .Select(x => x.Tool)
                .Take(15)
                .ToList();

            if (results.Count == 0)
            {
                results = tools.Take(10).ToList();
            }

            return results;
        }
    }
}
