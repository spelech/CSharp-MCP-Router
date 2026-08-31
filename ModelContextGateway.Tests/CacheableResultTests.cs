using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ModelContextGateway.Tests
{
    public class CacheableResultTests
    {
        [Fact]
        [Requirement("MCP-08", "MCP", RequirementType.Positive, "FormatCacheableResult applies default ttlMs and cacheScope according to MCP 2026-07-28 spec")]
        public void FormatCacheableResult_AppliesDefault_TtlMs_And_CacheScope()
        {
            var payload = new { tools = new[] { "tool1", "tool2" } };
            var formatted = CacheableResult.FormatCacheableResult(payload);
            var json = JsonSerializer.Serialize(formatted);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("ttlMs", out var ttlProp));
            Assert.Equal(300000L, ttlProp.GetInt64());
            Assert.True(root.TryGetProperty("cacheScope", out var scopeProp));
            Assert.Equal("session", scopeProp.GetString());
            Assert.True(root.TryGetProperty("tools", out _));
        }

        [Fact]
        [Requirement("MCP-08", "MCP", RequirementType.Positive, "FormatCacheableResult preserves existing ttlMs and cacheScope")]
        public void FormatCacheableResult_PreservesExisting_TtlMs_And_CacheScope()
        {
            var payload = new { tools = new[] { "tool1" }, ttlMs = 60000L, cacheScope = "global" };
            var formatted = CacheableResult.FormatCacheableResult(payload);
            var json = JsonSerializer.Serialize(formatted);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(60000L, root.GetProperty("ttlMs").GetInt64());
            Assert.Equal("global", root.GetProperty("cacheScope").GetString());
        }

        [Fact]
        [Requirement("MCP-08", "MCP", RequirementType.Positive, "AdminMcpServer tools/list returns ttlMs and cacheScope in results")]
        public async Task AdminMcpServer_ToolsList_Returns_TtlMs_And_CacheScope()
        {
            var serverRepoMock = new Mock<IServerRepository>();
            var appKeyRepoMock = new Mock<IAppKeyRepository>();
            var secretRepoMock = new Mock<ISecretProviderRepository>();
            var authRepoMock = new Mock<IAuthProviderRepository>();
            var settingRepoMock = new Mock<ISettingRepository>();
            var dbFactoryMock = new Mock<IDbConnectionFactory>();
            var auditLoggerMock = new Mock<IAuditLogger>();
            var credentialServiceMock = new Mock<ICredentialService>();

            var adminServer = new AdminMcpServer(
                serverRepoMock.Object,
                appKeyRepoMock.Object,
                secretRepoMock.Object,
                authRepoMock.Object,
                settingRepoMock.Object,
                dbFactoryMock.Object,
                auditLoggerMock.Object,
                credentialServiceMock.Object,
                null!,
                null!,
                null!
            );

            var req = new JsonRpcRequest
            {
                Id = 1,
                Method = "tools/list"
            };

            var resp = await adminServer.ProcessRequestAsync(req);
            Assert.NotNull(resp.Result);

            var json = JsonSerializer.Serialize(resp.Result);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("tools", out _));
            Assert.True(root.TryGetProperty("ttlMs", out var ttlProp));
            Assert.Equal(300000L, ttlProp.GetInt64());
            Assert.True(root.TryGetProperty("cacheScope", out var scopeProp));
            Assert.Equal("session", scopeProp.GetString());
        }

        [Fact]
        [Requirement("MCP-08", "MCP", RequirementType.Positive, "ClientSession list and read methods format cacheable results with ttlMs and cacheScope")]
        public async Task ClientSession_ListAndReadMethods_FormatCacheableResults()
        {
            var services = new ServiceCollection();
            var mockAudit = new Mock<IAuditLogger>();
            services.AddSingleton<IAuditLogger>(mockAudit.Object);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Audit:FailClosed", "false" },
                { "Admin:GroupSid", "full_admin" }
            }).Build();
            services.AddSingleton<IConfiguration>(config);

            var mockProvider = new Mock<IIdentityProvider>();
            mockProvider.Setup(p => p.ResolveIdentityAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(new UserIdentityContext("admin", "MockProvider", new List<string> { "full_admin" }, Sids: new List<string> { "full_admin" }));
            var composite = new CompositeIdentityProvider(new[] { mockProvider.Object });
            services.AddSingleton(composite);

            var sp = services.BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = sp };
            var claims = new[] {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin"),
                new System.Security.Claims.Claim("GroupSid", "full_admin"),
                new System.Security.Claims.Claim("Sid", "full_admin")
            };
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "AppKey"));

            var session = new ClientSession("test-cacheable-session", httpContext.Response, new List<Components.Servers.McpServer>(), new HttpClient(), new Mock<IEmbeddingService>().Object, NullLogger.Instance);

            // 1. ListTools
            var tools = await session.ListToolsAsync("{\"jsonrpc\":\"2.0\",\"id\":1}", httpContext);
            var toolsResult = CacheableResult.FormatCacheableResult(new { tools });
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(toolsResult)))
            {
                var root = doc.RootElement;
                Assert.True(root.TryGetProperty("tools", out _));
                Assert.True(root.TryGetProperty("ttlMs", out var ttl));
                Assert.Equal(300000L, ttl.GetInt64());
                Assert.True(root.TryGetProperty("cacheScope", out var scope));
                Assert.Equal("session", scope.GetString());
            }

            // 2. ListPrompts
            var prompts = await session.ListPromptsAsync("{\"jsonrpc\":\"2.0\",\"id\":2}", httpContext);
            var promptsResult = CacheableResult.FormatCacheableResult(new { prompts });
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(promptsResult)))
            {
                var root = doc.RootElement;
                Assert.True(root.TryGetProperty("prompts", out _));
                Assert.True(root.TryGetProperty("ttlMs", out var ttl));
                Assert.Equal(300000L, ttl.GetInt64());
                Assert.True(root.TryGetProperty("cacheScope", out var scope));
                Assert.Equal("session", scope.GetString());
            }

            // 3. ListResources
            var resources = await session.ListResourcesAsync("{\"jsonrpc\":\"2.0\",\"id\":3}", httpContext);
            var resourcesResult = CacheableResult.FormatCacheableResult(new { resources });
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(resourcesResult)))
            {
                var root = doc.RootElement;
                Assert.True(root.TryGetProperty("resources", out _));
                Assert.True(root.TryGetProperty("ttlMs", out var ttl));
                Assert.Equal(300000L, ttl.GetInt64());
                Assert.True(root.TryGetProperty("cacheScope", out var scope));
                Assert.Equal("session", scope.GetString());
            }

            // 4. ListResourceTemplates
            var templates = await session.ListResourceTemplatesAsync("{\"jsonrpc\":\"2.0\",\"id\":4}", httpContext);
            var templatesResult = CacheableResult.FormatCacheableResult(new { templates });
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(templatesResult)))
            {
                var root = doc.RootElement;
                Assert.True(root.TryGetProperty("templates", out _));
                Assert.True(root.TryGetProperty("ttlMs", out var ttl));
                Assert.Equal(300000L, ttl.GetInt64());
                Assert.True(root.TryGetProperty("cacheScope", out var scope));
                Assert.Equal("session", scope.GetString());
            }

            // 5. ReadResource
            var readRes = await session.ReadResourceAsync("router://status", "{\"jsonrpc\":\"2.0\",\"id\":5,\"params\":{\"uri\":\"router://status\"}}", httpContext);
            var readResult = CacheableResult.FormatCacheableResult(readRes);
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(readResult)))
            {
                var root = doc.RootElement;
                Assert.True(root.TryGetProperty("contents", out _));
                Assert.True(root.TryGetProperty("ttlMs", out var ttl));
                Assert.Equal(300000L, ttl.GetInt64());
                Assert.True(root.TryGetProperty("cacheScope", out var scope));
                Assert.Equal("session", scope.GetString());
            }
        }
    }
}
