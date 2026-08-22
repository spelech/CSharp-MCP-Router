using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Core.Protocol;
using McpRouter.Components.Servers;
using McpRouter.Infrastructure.Persistence;
using Dapper;

namespace McpRouter.Core.Routing
{
    /// <summary>
    /// Manages backend tool listing, caching, namespaced routing tables, and tool invocation execution.
    /// </summary>
    public partial class ToolRoutingManager
    {
        private readonly ConcurrentDictionary<string, string> _toolRoutingTable = new();
        private readonly List<object> _cachedTools = new();
        private readonly object _cacheLock = new();
        private bool _isCachePopulated = false;

        /// <summary>
        /// Gets the active thread-safe namespaced tool-to-server routing map.
        /// </summary>
        public ConcurrentDictionary<string, string> ToolRoutingTable => _toolRoutingTable;

        public static List<object> GetMetaModeTools()
        {
            return new List<object>
            {
                new
                {
                    name = "search_tools",
                    description = "Semantically search across all registered internal MCP tools (Excel, Docker, Plex, Home Assistant, etc.) using keywords. Returns the matching tool names, descriptions, and input schemas. Use this first to discover what tools are available.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new { type = "string", description = "The natural language query describing what you want to do (e.g. 'read Excel file data', 'restart Docker container')." }
                        },
                        required = new[] { "query" }
                    }
                },
                new
                {
                    name = "execute_tool",
                    description = "Execute a specific internal MCP tool by name with arguments. Obtain the correct tool name and arguments schema by calling search_tools first.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "The exact name of the tool to execute (e.g., 'docker/list_containers')." },
                            arguments = new { type = "object", description = "The arguments JSON object expected by the target tool." },
                            target_auth_token = new { type = "string", description = "Optional authentication token if the backend tool requires dynamic pass-through authorization." }
                        },
                        required = new[] { "name", "arguments" }
                    }
                }
            };
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _isCachePopulated = false;
                _cachedTools.Clear();
            }
        }

        public List<object> GetCachedTools()
        {
            lock (_cacheLock)
            {
                return new List<object>(_cachedTools);
            }
        }
    }
}
