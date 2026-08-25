using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace McpRouter.Tests
{
    public class ClientSessionTests
    {
        private (SqliteConnection conn, IDbConnectionFactory factory) CreateDbFactory()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(connection);
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (connection, mockDbFactory.Object);
        }

        private class NullAuditLogger : IAuditLogger
        {
            public Task LogInvocationAsync(string requestId, string userPrincipalName, string userSid, string serverCodeName, string itemName, string requestMethod, int executionTimeMs, int statusCode, string? requestPayload = null, string? responsePayload = null, string? errorMessage = null) => Task.CompletedTask;
            public Task LogAdminActionAsync(string username, string action, string target, string details, bool success, string? errorMessage = null) => Task.CompletedTask;
        }

        private HttpContext CreateMockHttpContext(IDbConnectionFactory dbFactory)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAuditLogger, NullAuditLogger>();
            services.AddSingleton(dbFactory);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" },
                { "Admin:GroupSid", "full_admin" }
            }).Build();
            services.AddSingleton<IConfiguration>(config);
            services.AddLogging();

            var sp = services.BuildServiceProvider();
            var ctx = new DefaultHttpContext();
            ctx.RequestServices = sp;

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "admin_user"),
                new Claim("Sid", "full_admin"),
                new Claim(ClaimTypes.Role, "full_admin")
            };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            return ctx;
        }

        [Fact]
        public async Task ClientSession_InitializationAndLifecycle_ExecutesSuccessfully()
        {
            var (_, dbFactory) = CreateDbFactory();
            var logger = NullLogger<ClientSession>.Instance;
            var httpContext = CreateMockHttpContext(dbFactory);

            var servers = new List<McpServer>
            {
                new McpServer { Id = "test-s1", DisplayName = "Test Server 1", Type = "sse", Url = "http://localhost:8080/sse", Enabled = true }
            };

            var session = new ClientSession(
                sessionId: "test-client-sess-1",
                clientResponse: null!,
                servers: servers,
                httpClient: new HttpClient(),
                embeddingService: new DummyEmbeddingService(),
                sessionManager: null,
                logger: logger
            );

            session.StartInitialization("{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":1}");

            // List tools, resources, prompts
            var tools = await session.ListToolsAsync("{}", httpContext);
            Assert.NotNull(tools);

            var resources = await session.ListResourcesAsync("{}", httpContext);
            Assert.NotNull(resources);

            var templates = await session.ListResourceTemplatesAsync("{}");
            Assert.NotNull(templates);

            var prompts = await session.ListPromptsAsync("{}", httpContext);
            Assert.NotNull(prompts);

            // Call built-in tool search_tools
            var callResult = await session.CallToolAsync("search_tools", "{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"id\":2,\"params\":{\"name\":\"search_tools\",\"arguments\":{\"query\":\"docker\"}}}", dbFactory, httpContext);
            Assert.NotNull(callResult);

            // Read built-in resource router://status
            var resResult = await session.ReadResourceAsync("router://status", "{}", httpContext);
            Assert.NotNull(resResult);

            // Get built-in prompt router__diagnose_failure
            var promptResult = await session.GetPromptAsync("router__diagnose_failure", "{}", httpContext);
            Assert.NotNull(promptResult);

            // Notifications & Cancel
            session.CancelRequest("req-123");
            Assert.False(session.TryHandleClientResponse("req-123", "{}"));
            await session.BroadcastNotificationAsync("notifications/initialized", "{}");
        }
    }
}
