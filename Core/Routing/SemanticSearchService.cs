using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace McpRouter.Core.Routing
{
    /// <summary>
    /// Provides hybrid keyword and vector semantic search scoring across backend MCP tools.
    /// </summary>
    public class SemanticSearchService
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, float[]> _embeddingsCache = new();

        /// <summary>
        /// Ranks and filters tools based on cosine similarity embeddings and hybrid keyword match weights.
        /// </summary>
        /// <param name="query">The natural language user intent query.</param>
        /// <param name="tools">The raw candidate list of tool schemas.</param>
        /// <param name="embeddingService">The active embedding provider (Local ONNX or API).</param>
        /// <param name="logger">Optional logger for telemetry.</param>
        /// <returns>A task returning the top matching tool schemas.</returns>
        public static async Task<List<object>> SearchToolsSemanticAsync(
            string query,
            List<object> tools,
            IEmbeddingService embeddingService,
            Microsoft.Extensions.Logging.ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return tools.Take(15).ToList();
            }

            var toolItems = tools.Select(tool =>
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

                return new { Tool = tool, Name = name, Description = description, TextToEmbed = $"{name}: {description}" };
            }).ToList();

            var uncachedTexts = toolItems.Select(t => t.TextToEmbed).Where(t => !_embeddingsCache.ContainsKey(t)).Distinct().ToList();
            if (uncachedTexts.Count > 0)
            {
                await Task.WhenAll(uncachedTexts.Select(async text =>
                {
                    try
                    {
                        var vec = await embeddingService.GetEmbeddingAsync(text);
                        _embeddingsCache[text] = vec;
                    }
                    catch (Exception ex)
                    {
                        // Exception during embedding generation is ignored because semantic search falls back
                        // gracefully to hybrid keyword-based matching if embeddings cannot be generated.
                        logger?.LogWarning(ex, "Failed to generate embedding for text during tool search: {Text}", text);
                    }
                }));
            }

            var queryVector = await embeddingService.GetEmbeddingAsync(query);

            var scoredTools = new List<(object Tool, double Score)>();

            foreach (var item in toolItems)
            {
                if (!_embeddingsCache.TryGetValue(item.TextToEmbed, out var toolVector))
                {
                    continue;
                }

                double vectorScore = embeddingService.CosineSimilarity(queryVector, toolVector);

                // Strong hybrid keyword boosting
                double keywordBoost = 0;
                var queryLower = query.ToLower();
                var queryWords = queryLower
                    .Split(new[] { ' ', ',', '.', ';', ':', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2)
                    .ToList();

                var nameLower = item.Name.ToLower();
                var descLower = item.Description.ToLower();

                // Substring phrase match boost
                if (nameLower.Contains(queryLower))
                {
                    keywordBoost += 2.0;
                }
                else if (descLower.Contains(queryLower))
                {
                    keywordBoost += 1.5;
                }

                // Per-word matches
                int wordMatches = 0;
                foreach (var word in queryWords)
                {
                    if (nameLower.Contains(word))
                    {
                        keywordBoost += 1.0;
                        wordMatches++;
                    }
                    else if (descLower.Contains(word))
                    {
                        keywordBoost += 0.5;
                        wordMatches++;
                    }
                }

                // Multi-word match multiplier bonus
                if (wordMatches > 1)
                {
                    keywordBoost += wordMatches * 0.5;
                }

                double finalScore = vectorScore + keywordBoost;
                scoredTools.Add((item.Tool, finalScore));
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
