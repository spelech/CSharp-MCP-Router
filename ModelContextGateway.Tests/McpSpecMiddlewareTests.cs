using System.Text;
using Microsoft.AspNetCore.Http;
using ModelContextGateway.Tests.Attributes;

namespace ModelContextGateway.Tests
{
    public class McpSpecMiddlewareTests
    {
        [Fact]
        [Requirement("MCP-21", "MCP", RequirementType.Positive, "McpSpecMiddleware extracts Mcp-Method, Mcp-Name, and MCP-Protocol-Version request headers per MCP 2026-07-28 spec.")]
        public async Task Middleware_Parses_2026_Spec_Headers()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";
            context.Request.Headers["Mcp-Method"] = "tools/call";
            context.Request.Headers["Mcp-Name"] = "weather_search";
            context.Request.Headers["MCP-Protocol-Version"] = "2026-07-28";
            context.Request.Headers["Mcp-Session-Id"] = "sess-123";

            bool nextCalled = false;
            var middleware = new McpSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("tools/call", context.Items["MCP_METHOD"]);
            Assert.Equal("weather_search", context.Items["MCP_ITEM_NAME"]);
            Assert.Equal("2026-07-28", context.Items["MCP_SPEC_VERSION"]);
            Assert.Equal("sess-123", context.Items["MCP_SESSION_ID"]);
        }

        [Fact]
        [Requirement("MCP-21", "MCP", RequirementType.Positive, "McpSpecMiddleware falls back transparently to parsing JSON-RPC body when spec headers are missing.")]
        public async Task Middleware_Falls_Back_To_Json_Body_When_Headers_Missing()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"query_db\"}}";
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);

            bool nextCalled = false;
            var middleware = new McpSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("tools/call", context.Items["MCP_METHOD"]);
            Assert.Equal("query_db", context.Items["MCP_ITEM_NAME"]);
            Assert.Equal(1L, context.Items["MCP_REQ_ID"]);
            Assert.False((bool)context.Items["MCP_IS_NOTIFICATION"]!);
        }

        [Fact]
        [Requirement("MCP-21", "MCP", RequirementType.Positive, "McpSpecMiddleware matches all MCP endpoints including /admin/sse, /mcg-admin, and /{targetServerId}.")]
        public async Task Middleware_Matches_Admin_And_Target_Proxy_Paths()
        {
            var paths = new[] { "/admin/sse", "/mcg-admin", "/docker", "/router-admin", "/message", "/admin/message" };

            foreach (var path in paths)
            {
                var context = new DefaultHttpContext();
                context.Request.Path = path;
                context.Request.Method = "POST";
                string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":\"abc\",\"method\":\"tools/list\"}";
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

                var middleware = new McpSpecMiddleware(ctx => Task.CompletedTask);
                await middleware.InvokeAsync(context);

                Assert.Equal("tools/list", context.Items["MCP_METHOD"]);
                Assert.Equal("abc", context.Items["MCP_REQ_ID"]);
            }
        }

        [Fact]
        [Requirement("MCP-21", "MCP", RequirementType.Positive, "McpSpecMiddleware detects notifications and sets MCP_IS_NOTIFICATION flag.")]
        public async Task Middleware_Detects_Notifications_Correctly()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/admin/sse";
            context.Request.Method = "POST";
            string jsonBody = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

            var middleware = new McpSpecMiddleware(ctx => Task.CompletedTask);
            await middleware.InvokeAsync(context);

            Assert.Equal("notifications/initialized", context.Items["MCP_METHOD"]);
            Assert.True((bool)context.Items["MCP_IS_NOTIFICATION"]!);
        }

        [Fact]
        [Requirement("MCP-21", "MCP", RequirementType.Positive, "McpSpecMiddleware ignores non-MCP paths like static files, health, and API endpoints.")]
        public async Task Middleware_Skips_Non_Mcp_Paths()
        {
            var nonMcpPaths = new[] { "/health", "/api/config/branding", "/css/site.css", "/assets/app.js", "/index.html" };

            foreach (var path in nonMcpPaths)
            {
                var context = new DefaultHttpContext();
                context.Request.Path = path;
                context.Request.Method = "POST";
                string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}";
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

                var middleware = new McpSpecMiddleware(ctx => Task.CompletedTask);
                await middleware.InvokeAsync(context);

                Assert.False(context.Items.ContainsKey("MCP_METHOD"));
            }
        }
    }
}
