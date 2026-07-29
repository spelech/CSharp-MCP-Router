using System.IO;
using System.Text;
using System.Threading.Tasks;
using McpRouter.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace McpRouter.Tests
{
    public class McpDualSpecMiddlewareTests
    {
        [Fact]
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
