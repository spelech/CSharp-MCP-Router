using System.Text;
using Microsoft.AspNetCore.Http;

namespace ModelContextGateway.Tests
{
    public class McpDualSpecMiddlewareTests
    {
        [Fact]
        [Requirement("MCP-08", "MCP", RequirementType.Positive, "Middleware parses 2026-07-28 spec headers Mcp-Method and Mcp-Name")]
        public async Task Middleware_Parses_2026_Spec_Headers()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";
            context.Request.Headers["Mcp-Method"] = "tools/call";
            context.Request.Headers["Mcp-Name"] = "weather_search";
            context.Request.Headers["MCP-Protocol-Version"] = "2026-07-28";

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"weather_search\"}}";
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
            Assert.Equal("weather_search", context.Items["MCP_ITEM_NAME"]);
            Assert.Equal("2026-07-28", context.Items["MCP_SPEC_VERSION"]);
        }

        [Fact]
        [Requirement("GUARD-01", "GUARD", RequirementType.Negative, "Middleware returns HeaderMismatch error (-32020) when headers missing on POST")]
        public async Task Middleware_Returns_HeaderMismatchError_When_Headers_Missing_On_POST()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":42,\"method\":\"tools/call\",\"params\":{\"name\":\"query_db\"}}";
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);

            bool nextCalled = false;
            var middleware = new McpDualSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(400, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            var responseStr = await reader.ReadToEndAsync();

            using var doc = System.Text.Json.JsonDocument.Parse(responseStr);
            var root = doc.RootElement;
            Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
            Assert.Equal(42, root.GetProperty("id").GetInt64());
            var error = root.GetProperty("error");
            Assert.Equal(-32020, error.GetProperty("code").GetInt32());
            Assert.Contains("Header mismatch error", error.GetProperty("message").GetString());
        }

        [Fact]
        [Requirement("GUARD-01", "GUARD", RequirementType.Negative, "Middleware returns HeaderMismatch error when Mcp-Method is mismatched")]
        public async Task Middleware_Returns_HeaderMismatchError_When_McpMethod_Mismatched()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";
            context.Request.Headers["Mcp-Method"] = "tools/list";
            context.Response.Body = new MemoryStream();

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":10,\"method\":\"tools/call\",\"params\":{\"name\":\"query_db\"}}";
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);

            bool nextCalled = false;
            var middleware = new McpDualSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(400, context.Response.StatusCode);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            var responseStr = await reader.ReadToEndAsync();

            using var doc = System.Text.Json.JsonDocument.Parse(responseStr);
            Assert.Equal(-32020, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
        }

        [Fact]
        [Requirement("GUARD-01", "GUARD", RequirementType.Negative, "Middleware returns HeaderMismatch error when Mcp-Name is missing for tool call")]
        public async Task Middleware_Returns_HeaderMismatchError_When_McpName_Missing_For_ToolCall()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";
            context.Request.Headers["Mcp-Method"] = "tools/call";
            context.Response.Body = new MemoryStream();

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":12,\"method\":\"tools/call\",\"params\":{\"name\":\"query_db\"}}";
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);

            bool nextCalled = false;
            var middleware = new McpDualSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(400, context.Response.StatusCode);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            var responseStr = await reader.ReadToEndAsync();

            using var doc = System.Text.Json.JsonDocument.Parse(responseStr);
            Assert.Equal(-32020, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
        }

        [Fact]
        [Requirement("GUARD-01", "GUARD", RequirementType.Negative, "Middleware returns HeaderMismatch error when Mcp-Name is mismatched")]
        public async Task Middleware_Returns_HeaderMismatchError_When_McpName_Mismatched()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";
            context.Request.Headers["Mcp-Method"] = "tools/call";
            context.Request.Headers["Mcp-Name"] = "wrong_name";
            context.Response.Body = new MemoryStream();

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":11,\"method\":\"tools/call\",\"params\":{\"name\":\"query_db\"}}";
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);

            bool nextCalled = false;
            var middleware = new McpDualSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(400, context.Response.StatusCode);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            var responseStr = await reader.ReadToEndAsync();

            using var doc = System.Text.Json.JsonDocument.Parse(responseStr);
            Assert.Equal(-32020, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
        }
    }
}
