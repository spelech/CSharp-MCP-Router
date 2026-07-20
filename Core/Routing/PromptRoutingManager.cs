using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Routing
{
    public class PromptRoutingManager
    {
        private readonly Dictionary<string, string> _promptRoutingTable = new();

        public async Task<List<object>> ListPromptsAsync(string body, IEnumerable<KeyValuePair<string, BackendConnection>> backendConnections, ILogger logger, Func<Task> ensureBackendsInitializedAsync)
        {
            var allPrompts = new List<object>();
            var tasks = new List<Task<(string ServerId, JsonElement Prompts)>>();
            
            await ensureBackendsInitializedAsync();

            foreach (var entry in backendConnections)
            {
                var conn = entry.Value;
                var serverId = entry.Key;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var reqBody = "{\"jsonrpc\":\"2.0\",\"method\":\"prompts/list\",\"id\":\"refresh-prompt-list\"}";
                        var resp = await conn.SendRequestAsync("prompts/list", reqBody);
                        if (resp.Result != null && resp.Result.Value.TryGetProperty("prompts", out var promptsList))
                        {
                            return (serverId, promptsList);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error listing prompts on server {ServerId}", serverId);
                    }
                    return (serverId, default(JsonElement));
                }));
            }

            var completed = await Task.WhenAll(tasks);
            foreach (var item in completed)
            {
                if (item.Prompts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var prompt in item.Prompts.EnumerateArray())
                    {
                        if (prompt.TryGetProperty("name", out var nameProp))
                        {
                            var rawName = nameProp.GetString() ?? string.Empty;
                            var exposedName = item.ServerId + "__" + rawName;
                            
                            _promptRoutingTable[exposedName] = item.ServerId;
                            
                            var promptDict = JsonSerializer.Deserialize<Dictionary<string, object>>(prompt.GetRawText());
                            if (promptDict != null)
                            {
                                promptDict["name"] = exposedName;
                                if (promptDict.TryGetValue("description", out var descVal))
                                    promptDict["description"] = $"[{item.ServerId}] {descVal}";
                                allPrompts.Add(promptDict);
                            }
                        }
                    }
                }
            }
            // Append built-in meta-prompts
            allPrompts.Add(new Dictionary<string, object> {
                { "name", "router__diagnose_failure" },
                { "description", "[router] Diagnose an MCP tool execution failure and generate remediation suggestions." },
                { "arguments", new[] {
                    new { name = "tool_name", description = "Name of the failing tool", required = true },
                    new { name = "error_message", description = "Exception message or error payload", required = true }
                } }
            });
            allPrompts.Add(new Dictionary<string, object> {
                { "name", "router__route_multi_task" },
                { "description", "[router] Plan how to break down and route a complex multi-server task across different MCP backends." },
                { "arguments", new[] {
                    new { name = "task_description", description = "Description of the overall goal", required = true }
                } }
            });
            allPrompts.Add(new Dictionary<string, object> {
                { "name", "router__audit_permissions" },
                { "description", "[router] Audit tool permissions and suggest dynamic authorization constraints for sensitive tools." },
                { "arguments", new[] {
                    new { name = "server_name", description = "Filter audit by a specific backend server (optional)", required = false }
                } }
            });

            return allPrompts;
        }

        public async Task<object?> GetPromptAsync(string promptName, string body, ConcurrentDictionary<string, BackendConnection> backendConnections, Func<Task> ensureBackendsInitializedAsync, Func<string, string, string, string> rewriteRequestJson)
        {
            if (promptName.StartsWith("router__"))
            {
                return ResolveLocalPrompt(promptName, body);
            }

            await ensureBackendsInitializedAsync();

            if (_promptRoutingTable.TryGetValue(promptName, out var serverId) && backendConnections.TryGetValue(serverId, out var conn))
            {
                var prefix = serverId + "__";
                var rawName = promptName.Substring(prefix.Length);
                string routingBody = rewriteRequestJson(body, "name", rawName);
                var resp = await conn.SendRequestAsync("prompts/get", routingBody);
                return resp.Result;
            }
            throw new KeyNotFoundException($"Prompt {promptName} not found in routing table.");
        }

        private object ResolveLocalPrompt(string promptName, string body)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var args = new Dictionary<string, string>();
            if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("arguments", out var argsProp))
            {
                foreach (var prop in argsProp.EnumerateObject())
                {
                    args[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            string text = string.Empty;
            if (promptName == "router__diagnose_failure")
            {
                args.TryGetValue("tool_name", out var toolName);
                args.TryGetValue("error_message", out var errMsg);
                text = $"You are an expert systems administrator diagnosing a failure in the MCP tool '{toolName}'. The error received was:\n\n{errMsg}\n\nAnalyze this failure. Consider configuration issues, API key availability, schema mismatches, and connection timeouts. Output a detailed diagnostic report with 3 actionable remediation steps.";
            }
            else if (promptName == "router__route_multi_task")
            {
                args.TryGetValue("task_description", out var taskDesc);
                text = $"You are an orchestrator routing tasks. We have a complex request:\n\n{taskDesc}\n\nWe have multiple MCP backends (e.g., Home Assistant, Docker, Excel, UniFi). Break this request down into sequential tool invocations. For each step, specify which backend tool to call and what arguments to supply.";
            }
            else if (promptName == "router__audit_permissions")
            {
                args.TryGetValue("server_name", out var serverName);
                var target = string.IsNullOrEmpty(serverName) ? "all MCP servers" : $"MCP server '{serverName}'";
                text = $"You are a security auditor auditing tool access permissions for {target}. List all available tools, classify them as read-only or read-write, identify sensitive tools (like file system writes or container actions), and suggest security constraints.";
            }
            else
            {
                throw new KeyNotFoundException($"Built-in prompt {promptName} is not implemented.");
            }

            return new {
                description = "Router Local Meta-Prompt",
                messages = new[] {
                    new {
                        role = "user",
                        content = new {
                            type = "text",
                            text = text
                        }
                    }
                }
            };
        }
    }
}
