using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;

namespace ModelContextGateway.Tests
{
    public class PerRequestLogLevelAndDeprecatedProtocolTests
    {
        [Fact]
        public async Task AdminMcpServer_ProcessRequestAsync_Ping_ReturnsMethodNotFound()
        {
            var mockServerRepo = new Mock<IServerRepository>();
            var mockAppKeyRepo = new Mock<IAppKeyRepository>();
            var mockSecretProviderRepo = new Mock<ISecretProviderRepository>();
            var mockAuthProviderRepo = new Mock<IAuthProviderRepository>();
            var mockSettingRepo = new Mock<ISettingRepository>();
            var mockDbFactory = new Mock<IDbConnectionFactory>();
            var mockAuditLogger = new Mock<IAuditLogger>();
            var mockCredService = new Mock<ICredentialService>();
            var mockSessionMgr = new Mock<SessionManager>(Mock.Of<IServiceProvider>(), Mock.Of<IHttpClientFactory>(), Mock.Of<ILogger<SessionManager>>());

            var adminServer = new AdminMcpServer(
                mockServerRepo.Object,
                mockAppKeyRepo.Object,
                mockSecretProviderRepo.Object,
                mockAuthProviderRepo.Object,
                mockSettingRepo.Object,
                mockDbFactory.Object,
                mockAuditLogger.Object,
                mockCredService.Object,
                healthCheckService: null!,
                dynamicEmbeddingService: null!,
                sessionManager: mockSessionMgr.Object
            );

            var pingReq = new JsonRpcRequest
            {
                Id = "test-ping-id",
                Method = "ping"
            };

            var response = await adminServer.ProcessRequestAsync(pingReq);

            Assert.NotNull(response.Error);
            Assert.Equal(-32601, response.Error.Code);
            Assert.Contains("ping", response.Error.Message);
        }

        [Fact]
        public void McpLogLevelHelper_ExtractPerRequestLogLevel_ParamsMeta()
        {
            var json = @"{
                ""jsonrpc"": ""2.0"",
                ""method"": ""tools/call"",
                ""params"": {
                    ""name"": ""calculate"",
                    ""_meta"": {
                        ""io.modelcontextprotocol/logLevel"": ""warn""
                    }
                }
            }";

            using var doc = JsonDocument.Parse(json);
            var logLevel = McpLogLevelHelper.ExtractPerRequestLogLevel(doc.RootElement);

            Assert.Equal("warn", logLevel);
        }

        [Fact]
        public void McpLogLevelHelper_ExtractPerRequestLogLevel_TopLevelMeta()
        {
            var json = @"{
                ""jsonrpc"": ""2.0"",
                ""method"": ""tools/call"",
                ""_meta"": {
                    ""io.modelcontextprotocol/logLevel"": ""debug""
                }
            }";

            using var doc = JsonDocument.Parse(json);
            var logLevel = McpLogLevelHelper.ExtractPerRequestLogLevel(doc.RootElement);

            Assert.Equal("debug", logLevel);
        }

        [Fact]
        public void McpLogLevelHelper_ExtractPerRequestLogLevel_ReturnsNull_WhenMissing()
        {
            var json = @"{
                ""jsonrpc"": ""2.0"",
                ""method"": ""tools/call"",
                ""params"": {
                    ""name"": ""calculate""
                }
            }";

            using var doc = JsonDocument.Parse(json);
            var logLevel = McpLogLevelHelper.ExtractPerRequestLogLevel(doc.RootElement);

            Assert.Null(logLevel);
        }

        [Theory]
        [InlineData(null, "info", false)]
        [InlineData("", "info", false)]
        [InlineData("info", "debug", false)]
        [InlineData("info", "info", true)]
        [InlineData("info", "error", true)]
        [InlineData("warn", "info", false)]
        [InlineData("warn", "warning", true)]
        [InlineData("warn", "error", true)]
        public void McpLogLevelHelper_ShouldEmitLogNotification_EvaluatesCorrectly(string? requestedLevel, string? notificationLevel, bool expected)
        {
            var shouldEmit = McpLogLevelHelper.ShouldEmitLogNotification(requestedLevel, notificationLevel);
            Assert.Equal(expected, shouldEmit);
        }
    }
}
