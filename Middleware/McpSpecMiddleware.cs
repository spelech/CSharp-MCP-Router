using System.Text.Json.Nodes;

namespace ModelContextGateway.Middleware
{
    /// <summary>
    /// Implements MCP 2026-07-28 Spec-Compliant Header Annotation, Protocol Negotiation, and Legacy Body Fallback.
    /// Inspects Mcp-Method, Mcp-Name, Mcp-Session-Id, and MCP-Protocol-Version HTTP headers or JSON-RPC request bodies,
    /// storing parsed metadata in HttpContext.Items for downstream endpoint execution.
    /// </summary>
    public class McpSpecMiddleware
    {
        private readonly RequestDelegate _next;

        public McpSpecMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsMcpPath(context.Request.Path))
            {
                string method = string.Empty;
                string itemName = string.Empty;
                string rawBody = string.Empty;
                object? id = null;
                JsonNode? paramsNode = null;

                // 1. Primary: Parse 2026-07-28 Specification Headers
                if (context.Request.Headers.TryGetValue("Mcp-Method", out var methodHeader))
                {
                    method = methodHeader.ToString();
                    if (context.Request.Headers.TryGetValue("Mcp-Name", out var nameHeader))
                    {
                        itemName = nameHeader.ToString();
                    }
                }

                if (context.Request.Headers.TryGetValue("Mcp-Session-Id", out var sessionHeader))
                {
                    context.Items["MCP_SESSION_ID"] = sessionHeader.ToString();
                }

                // 2. Body Inspection (for Streamable HTTP POSTs and Legacy MCP Clients)
                if (context.Request.Method == "POST")
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    rawBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(rawBody))
                    {
                        try
                        {
                            var json = JsonNode.Parse(rawBody);
                            if (json is JsonObject obj)
                            {
                                if (string.IsNullOrEmpty(method) && obj.TryGetPropertyValue("method", out var mNode) && mNode != null)
                                {
                                    method = mNode.ToString();
                                }

                                if (obj.TryGetPropertyValue("id", out var idNode) && idNode != null)
                                {
                                    if (idNode is JsonValue val)
                                    {
                                        if (val.TryGetValue<long>(out var longVal))
                                        {
                                            id = longVal;
                                        }
                                        else if (val.TryGetValue<string>(out var strVal))
                                        {
                                            id = strVal;
                                        }
                                    }
                                    else
                                    {
                                        id = idNode.ToString();
                                    }
                                }

                                if (obj.TryGetPropertyValue("params", out var pNode) && pNode != null)
                                {
                                    paramsNode = pNode;
                                }

                                if (string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(method))
                                {
                                    itemName = method switch
                                    {
                                        "tools/call" => paramsNode?["name"]?.ToString() ?? string.Empty,
                                        "prompts/get" => paramsNode?["name"]?.ToString() ?? string.Empty,
                                        "resources/read" => paramsNode?["uri"]?.ToString() ?? string.Empty,
                                        _ => string.Empty
                                    };
                                }
                            }
                        }
                        catch
                        {
                            // If payload is not valid JSON, let downstream handlers process it
                        }
                    }
                }

                if (!string.IsNullOrEmpty(method))
                {
                    bool isNotification = method.StartsWith("notifications/") || id == null;

                    context.Items["MCP_METHOD"] = method;
                    context.Items["MCP_ITEM_NAME"] = itemName;
                    context.Items["MCP_RAW_BODY"] = rawBody;
                    context.Items["MCP_REQ_ID"] = id;
                    context.Items["MCP_IS_NOTIFICATION"] = isNotification;
                    context.Items["MCP_SPEC_VERSION"] = context.Request.Headers["MCP-Protocol-Version"].FirstOrDefault() ?? "2026-07-28";
                }
            }

            await _next(context);
        }

        public static bool IsMcpPath(PathString path)
        {
            if (!path.HasValue)
            {
                return false;
            }

            var val = path.Value;
            if (string.IsNullOrEmpty(val) || val == "/")
            {
                return false;
            }

            // Exclude static assets, frontend files, health, and API endpoints
            if (path.StartsWithSegments("/api") ||
                path.StartsWithSegments("/health") ||
                path.StartsWithSegments("/oauth") ||
                path.StartsWithSegments("/.well-known") ||
                path.StartsWithSegments("/css") ||
                path.StartsWithSegments("/js") ||
                path.StartsWithSegments("/assets") ||
                path.StartsWithSegments("/swagger") ||
                val.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                val.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                val.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                val.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                val.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                val.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
