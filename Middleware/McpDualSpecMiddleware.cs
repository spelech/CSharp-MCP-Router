using System.Text.Json.Nodes;

namespace ModelContextGateway.Middleware
{
    /// <summary>
    /// Implements MCP 2026-07-28 Spec-Compliant Header Annotation and Dual-Spec Fallback.
    /// Inspects Mcp-Method and Mcp-Name HTTP headers, storing parsed metadata in HttpContext.Items for downstream use,
    /// and always invokes the next delegate (no short-circuit).
    /// </summary>
    public class McpDualSpecMiddleware
    {
        private readonly RequestDelegate _next;

        public McpDualSpecMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only process MCP SSE or HTTP endpoint calls
            if (context.Request.Path.StartsWithSegments("/sse") || context.Request.Path.StartsWithSegments("/mcp"))
            {
                string method = "";
                string itemName = "";

                // 1. Primary: Parse 2026-07-28 Specification Headers
                if (context.Request.Headers.TryGetValue("Mcp-Method", out var methodHeader))
                {
                    method = methodHeader.ToString();
                    context.Items["MCP_HEADER_METHOD"] = method;

                    if (context.Request.Headers.TryGetValue("Mcp-Name", out var nameHeader))
                    {
                        itemName = nameHeader.ToString();
                        context.Items["MCP_HEADER_NAME"] = itemName;
                    }
                }
                // 2. Fallback: Body Inspection for Older/Legacy MCP Clients
                else if (context.Request.Method == "POST")
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrEmpty(body))
                    {
                        try
                        {
                            var json = JsonNode.Parse(body);
                            method = json?["method"]?.ToString() ?? "";
                            itemName = method switch
                            {
                                "tools/call" => json?["params"]?["name"]?.ToString() ?? "",
                                "prompts/get" => json?["params"]?["name"]?.ToString() ?? "",
                                "resources/read" => json?["params"]?["uri"]?.ToString() ?? "",
                                _ => ""
                            };
                        }
                        catch
                        {
                            // If payload is not valid JSON, let downstream handlers process it
                        }
                    }
                }

                // Store parsed spec metadata in HttpContext items for downstream handlers / controllers
                if (!string.IsNullOrEmpty(method))
                {
                    context.Items["MCP_METHOD"] = method;
                    context.Items["MCP_ITEM_NAME"] = itemName;
                    context.Items["MCP_SPEC_VERSION"] = context.Request.Headers["MCP-Protocol-Version"].FirstOrDefault() ?? "2026-07-28";
                }
            }

            await _next(context);
        }
    }
}
