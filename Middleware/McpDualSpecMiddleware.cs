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
            var isMcpPath = context.Request.Path.StartsWithSegments("/sse") ||
                            context.Request.Path.StartsWithSegments("/mcp") ||
                            context.Request.Path.StartsWithSegments("/message");

            if (isMcpPath && HttpMethods.IsPost(context.Request.Method))
            {
                context.Request.EnableBuffering();
                string body = "";
                using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                string bodyMethod = "";
                string bodyItemName = "";
                JsonNode? idNode = null;

                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        var json = JsonNode.Parse(body);
                        bodyMethod = json?["method"]?.ToString() ?? "";
                        idNode = json?["id"];
                        bodyItemName = bodyMethod switch
                        {
                            "tools/call" => json?["params"]?["name"]?.ToString() ?? "",
                            "prompts/get" => json?["params"]?["name"]?.ToString() ?? "",
                            "resources/read" => json?["params"]?["uri"]?.ToString() ?? "",
                            _ => ""
                        };
                    }
                    catch
                    {
                        // Invalid JSON payload; allow downstream handler to format standard JSON-RPC parse error if appropriate
                    }
                }

                var hasMethodHeader = context.Request.Headers.TryGetValue("Mcp-Method", out var methodHeader);
                var headerMethod = hasMethodHeader ? methodHeader.ToString() : "";
                var hasNameHeader = context.Request.Headers.TryGetValue("Mcp-Name", out var nameHeader);
                var headerItemName = hasNameHeader ? nameHeader.ToString() : "";

                bool methodHeaderMissing = !hasMethodHeader;
                bool nameHeaderMissing = !string.IsNullOrEmpty(bodyItemName) && !hasNameHeader;
                bool methodMismatch = hasMethodHeader && !string.IsNullOrEmpty(bodyMethod) && !string.Equals(headerMethod, bodyMethod, StringComparison.Ordinal);
                bool nameMismatch = hasNameHeader && !string.IsNullOrEmpty(bodyItemName) && !string.Equals(headerItemName, bodyItemName, StringComparison.Ordinal);

                // MCP 2026-07-28: Standard MCP request headers (Mcp-Method, Mcp-Name) are required on Streamable HTTP POST requests.
                if (methodHeaderMissing || nameHeaderMissing || methodMismatch || nameMismatch)
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    object? originalId = idNode != null
                        ? (idNode.GetValueKind() == System.Text.Json.JsonValueKind.Number ? (object)idNode.GetValue<long>() : idNode.ToString())
                        : null;

                    var errorResponse = new
                    {
                        jsonrpc = "2.0",
                        id = originalId,
                        error = new
                        {
                            code = -32020,
                            message = "Header mismatch error: Streamable HTTP POST requests require standard Mcp-Method and Mcp-Name headers matching the JSON-RPC payload."
                        }
                    };
                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return;
                }

                string method = headerMethod;
                string itemName = !string.IsNullOrEmpty(headerItemName) ? headerItemName : bodyItemName;

                context.Items["MCP_METHOD"] = method;
                context.Items["MCP_ITEM_NAME"] = itemName;
                context.Items["MCP_SPEC_VERSION"] = context.Request.Headers["MCP-Protocol-Version"].FirstOrDefault() ?? "2026-07-28";
            }
            else if (isMcpPath)
            {
                if (context.Request.Headers.TryGetValue("Mcp-Method", out var methodHeader))
                {
                    context.Items["MCP_METHOD"] = methodHeader.ToString();
                    if (context.Request.Headers.TryGetValue("Mcp-Name", out var nameHeader))
                    {
                        context.Items["MCP_ITEM_NAME"] = nameHeader.ToString();
                    }
                    context.Items["MCP_SPEC_VERSION"] = context.Request.Headers["MCP-Protocol-Version"].FirstOrDefault() ?? "2026-07-28";
                }
            }

            await _next(context);
        }
    }
}
