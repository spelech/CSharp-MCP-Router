using System.Text;
using Microsoft.AspNetCore.Http;

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

        [Fact]
        [Requirement("MCP-22", "MCP", RequirementType.Positive, "McpSpecMiddleware extracts clientInfo, clientCapabilities, and protocolVersion from _meta for stateless MCP requests.")]
        public async Task Middleware_Extracts_Stateless_Capabilities_And_ClientInfo_In_Meta()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/sse";
            context.Request.Method = "POST";

            string jsonBody = @"{
                ""jsonrpc"": ""2.0"",
                ""id"": 42,
                ""method"": ""tools/list"",
                ""_meta"": {
                    ""io.modelcontextprotocol/protocolVersion"": ""2026-07-28"",
                    ""io.modelcontextprotocol/clientInfo"": { ""name"": ""agent-client"", ""version"": ""1.2.3"" },
                    ""io.modelcontextprotocol/clientCapabilities"": { ""roots"": { ""listChanged"": true } }
                }
            }";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

            bool nextCalled = false;
            var middleware = new McpSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("tools/list", context.Items["MCP_METHOD"]);
            Assert.Equal(42L, context.Items["MCP_REQ_ID"]);
            Assert.Equal("2026-07-28", context.Items["MCP_SPEC_VERSION"]);
            Assert.NotNull(context.Items["MCP_CLIENT_INFO"]);
            Assert.NotNull(context.Items["MCP_CLIENT_CAPABILITIES"]);
            Assert.NotNull(context.Items["MCP_META"]);
        }

        [Fact]
        [Requirement("MCP-22", "MCP", RequirementType.Negative, "McpSpecMiddleware rejects unsupported protocol versions with JSON-RPC error code -32021.")]
        public async Task Middleware_Rejects_Unsupported_Protocol_Version_With_32021_Error()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/sse";
            context.Request.Method = "POST";
            context.Request.Headers["MCP-Protocol-Version"] = "2099-01-01";
            context.Response.Body = new MemoryStream();

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"tools/list\"}";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

            bool nextCalled = false;
            var middleware = new McpSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(400, context.Response.StatusCode);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            string responseText = await reader.ReadToEndAsync();
            Assert.Contains("-32021", responseText);
            Assert.Contains("Unsupported protocol version", responseText);
        }

        [Fact]
        [Requirement("MCP-23", "MCP", RequirementType.Positive, "McpSpecMiddleware parses subscriptions/listen requests for real-time change stream establishment.")]
        public async Task Middleware_Parses_Subscriptions_Listen_Request()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/mcp";
            context.Request.Method = "POST";

            string jsonBody = "{\"jsonrpc\":\"2.0\",\"id\":\"sub-1\",\"method\":\"subscriptions/listen\",\"params\":{}}";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

            bool nextCalled = false;
            var middleware = new McpSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("subscriptions/listen", context.Items["MCP_METHOD"]);
            Assert.Equal("sub-1", context.Items["MCP_REQ_ID"]);
        }

        [Fact]
        [Requirement("MCP-24", "MCP", RequirementType.Positive, "McpSpecMiddleware extracts OpenTelemetry W3C traceparent, tracestate, and baggage from headers and _meta.")]
        public async Task Middleware_Extracts_Trace_Context_From_Headers_And_Meta()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/sse";
            context.Request.Method = "POST";
            context.Request.Headers["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
            context.Request.Headers["tracestate"] = "vendor=value";
            context.Request.Headers["baggage"] = "userId=123,role=admin";

            string jsonBody = @"{
                ""jsonrpc"": ""2.0"",
                ""id"": 1,
                ""method"": ""tools/call"",
                ""params"": { ""name"": ""query_db"" },
                ""_meta"": {
                    ""traceparent"": ""00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"",
                    ""tracestate"": ""vendor=value""
                }
            }";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

            bool nextCalled = false;
            var middleware = new McpSpecMiddleware(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01", context.Items["MCP_TRACE_PARENT"]);
            Assert.Equal("vendor=value", context.Items["MCP_TRACE_STATE"]);
            Assert.Equal("userId=123,role=admin", context.Items["MCP_BAGGAGE"]);
        }
    }
}
