using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ModelContextGateway.Tests
{
    public class McpErrorCodesTests
    {
        [Fact]
        [Requirement("MCP-ERROR-CODES-SPEC-ALLOCATION", "MCP", RequirementType.Positive, "Verify MCP 2026-07-28 error codes match specification allocation policy.")]
        public void McpErrorCodes_ConstantValues_MatchSpec()
        {
            Assert.Equal(-32001, McpErrorCodes.ConnectionClosed);
            Assert.Equal(-32020, McpErrorCodes.HeaderMismatch);
            Assert.Equal(-32021, McpErrorCodes.MissingRequiredClientCapability);
            Assert.Equal(-32022, McpErrorCodes.UnsupportedProtocolVersion);

            Assert.Equal(-32700, McpErrorCodes.ParseError);
            Assert.Equal(-32600, McpErrorCodes.InvalidRequest);
            Assert.Equal(-32601, McpErrorCodes.MethodNotFound);
            Assert.Equal(-32602, McpErrorCodes.InvalidParams);
            Assert.Equal(-32603, McpErrorCodes.InternalError);
        }

        [Fact]
        [Requirement("MCP-ERROR-CODES-UNSUPPORTED-PROTOCOL", "MCP", RequirementType.Negative, "AdminMcpServer returns UnsupportedProtocolVersion (-32022) when protocol version is invalid.")]
        public async Task AdminMcpServer_UnsupportedProtocolVersion_ReturnsError()
        {
            var serverRepoMock = new Mock<IServerRepository>();
            var appKeyRepoMock = new Mock<IAppKeyRepository>();
            var secretProvMock = new Mock<ISecretProviderRepository>();
            var authProvMock = new Mock<IAuthProviderRepository>();
            var settingRepoMock = new Mock<ISettingRepository>();
            var dbFactoryMock = new Mock<IDbConnectionFactory>();
            var auditLoggerMock = new Mock<IAuditLogger>();
            var credServiceMock = new Mock<ICredentialService>();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var sessionManager = new SessionManager(serviceProvider, httpClientFactory, NullLogger<SessionManager>.Instance);
            var healthCheckService = new BackendHealthCheckService(serviceProvider, httpClientFactory, sessionManager, NullLogger<BackendHealthCheckService>.Instance);

            var dynamicEmbeddingService = new DynamicEmbeddingService(
                httpClientFactory.CreateClient(),
                NullLoggerFactory.Instance,
                serviceProvider
            );

            var adminServer = new AdminMcpServer(
                serverRepoMock.Object,
                appKeyRepoMock.Object,
                secretProvMock.Object,
                authProvMock.Object,
                settingRepoMock.Object,
                dbFactoryMock.Object,
                auditLoggerMock.Object,
                credServiceMock.Object,
                healthCheckService,
                dynamicEmbeddingService,
                sessionManager
            );

            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "initialize",
                Params = JsonDocument.Parse("{\"protocolVersion\":\"9.9.9\"}").RootElement
            };

            var response = await adminServer.ProcessRequestAsync(request);

            Assert.NotNull(response.Error);
            Assert.Equal(McpErrorCodes.UnsupportedProtocolVersion, response.Error.Code);
            Assert.Contains("Unsupported protocol version", response.Error.Message);
        }

        [Fact]
        [Requirement("MCP-ERROR-CODES-MISSING-CAPABILITY", "MCP", RequirementType.Negative, "ClientSession returns MissingRequiredClientCapability (-32021) when client lacks required capability.")]
        public async Task ClientSession_MissingRequiredClientCapability_ReturnsError()
        {
            var responseMock = new Mock<HttpResponse>();
            var embeddingServiceMock = new Mock<IEmbeddingService>();

            var session = new ClientSession(
                "test-session-123",
                responseMock.Object,
                new List<Components.Servers.McpServer>(),
                new HttpClient(),
                embeddingServiceMock.Object,
                NullLogger.Instance
            );

            // Set client capabilities without "sampling"
            var capabilities = JsonDocument.Parse("{\"tools\":{}}").RootElement;
            session.SetClientCapabilities(capabilities);

            var req = new JsonRpcRequest
            {
                Id = "sample-1",
                Method = "sampling/createMessage"
            };

            var response = await session.ForwardRequestToClientAsync(req);

            Assert.NotNull(response.Error);
            Assert.Equal(McpErrorCodes.MissingRequiredClientCapability, response.Error.Code);
            Assert.Contains("sampling", response.Error.Message);
        }

        [Fact]
        [Requirement("MCP-ERROR-CODES-HEADER-ANNOTATION", "MCP", RequirementType.Positive, "McpDualSpecMiddleware extracts Mcp-Method and Mcp-Name headers into HttpContext.Items.")]
        public async Task McpDualSpecMiddleware_ExtractsHeaders_IntoItems()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new McpDualSpecMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/sse";
            context.Request.Headers["Mcp-Method"] = "tools/call";
            context.Request.Headers["Mcp-Name"] = "docker__list";

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal("tools/call", context.Items["MCP_HEADER_METHOD"]);
            Assert.Equal("docker__list", context.Items["MCP_HEADER_NAME"]);
            Assert.Equal("tools/call", context.Items["MCP_METHOD"]);
            Assert.Equal("docker__list", context.Items["MCP_ITEM_NAME"]);
        }
    }
}
