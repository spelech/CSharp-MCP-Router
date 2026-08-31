using System.Text;
using Microsoft.AspNetCore.Http;

namespace ModelContextGateway.Tests
{
    public class McpDualSpecMiddlewareTests
    {
        [Fact]
        [Requirement("MCP-21", "MCP", RequirementType.Positive, "McpDualSpecMiddleware extracts Mcp-Method, Mcp-Name, and MCP-Protocol-Version request headers per MCP 2026-07-28 spec.")]
        public async Task Middleware_Parses_2026_Spec_Headers()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";
            context.Request.Headers["Mcp-Method"] = "tools/call";
            context.Request.Headers["Mcp-Name"] = "weather_search";
            context.Request.Headers["MCP-Protocol-Version"] = "2026-07-28";

            bool nextCalled = false;
            var middleware = new McpDualSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("tools/call", context.Items["MCP_METHOD"]);
            Assert.Equal("weather_search", context.Items["MCP_ITEM_NAME"]);
            Assert.Equal("2026-07-28", context.Items["MCP_SPEC_VERSION"]);
        }

        [Fact]
        [Requirement("MCP-21", "MCP", RequirementType.Positive, "McpDualSpecMiddleware falls back transparently to parsing JSON-RPC body when spec headers are missing.")]
        public async Task Middleware_Falls_Back_To_Json_Body_When_Headers_Missing()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"query_db\"}}";
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);

            bool nextCalled = false;
            var middleware = new McpDualSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("tools/call", context.Items["MCP_METHOD"]);
            Assert.Equal("query_db", context.Items["MCP_ITEM_NAME"]);
        }
    }
}
