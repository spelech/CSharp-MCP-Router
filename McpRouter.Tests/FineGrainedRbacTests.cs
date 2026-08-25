using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace McpRouter.Tests
{
    public class FineGrainedRbacTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IDbConnectionFactory _dbFactory;

        public FineGrainedRbacTests()
        {
            var dbName = $"Data Source=RbacDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            _connection = new SqliteConnection(dbName);
            _connection.Open();

            // Create the AccessPolicies table in our in-memory SQLite database
            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS AccessPolicies (
                    Id TEXT PRIMARY KEY,
                    TargetId TEXT,
                    RequiredGroup TEXT,
                    IsAllowed INTEGER DEFAULT 1
                );");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() => new SqliteConnection(dbName));
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private ClientSession CreateSession(string username, List<string> groups)
        {
            var context = new DefaultHttpContext();
            var response = context.Response;

            var services = new ServiceCollection();
            services.AddSingleton(_dbFactory);

            var mockProvider = new Mock<IIdentityProvider>();
            mockProvider.Setup(p => p.ResolveIdentityAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(new UserIdentityContext(username, "Test", groups));

            var composite = new CompositeIdentityProvider(new[] { mockProvider.Object });
            services.AddSingleton(composite);

            var mockAuditLogger = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            services.AddSingleton(mockAuditLogger.Object);

            var realConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                { "Audit:FailClosed", "false" }
            }).Build();
            services.AddSingleton<IConfiguration>(realConfig);

            context.RequestServices = services.BuildServiceProvider();

            var httpClient = new HttpClient();
            var loggerMock = new Mock<ILogger>();
            var embeddingMock = new Mock<IEmbeddingService>();

            var servers = new List<McpServer>();
            return new ClientSession("test-session", response, servers, httpClient, embeddingMock.Object, loggerMock.Object);
        }

        private void SeedPolicy(string id, string targetId, string requiredGroup, bool isAllowed)
        {
            _connection.Execute(@"
                INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed)
                VALUES (@Id, @TargetId, @RequiredGroup, @IsAllowed);",
                new { Id = id, TargetId = targetId, RequiredGroup = requiredGroup, IsAllowed = isAllowed ? 1 : 0 });
        }

        [Fact]
        public async Task RBAC_DefaultsToDenied_WhenNoPoliciesConfigured()
        {
            var session = CreateSession("bob", new List<string> { "Users" });

            var authorized = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on");
            Assert.False(authorized);
        }

        [Fact]
        public async Task RBAC_AllowsUser_WhenPolicyMatchesRequiredGroup()
        {
            SeedPolicy("p1", "tool:ha__turn_on", "SmartHomeAdmins", true);

            var session = CreateSession("bob", new List<string> { "SmartHomeAdmins", "Users" });

            var authorized = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on");
            Assert.True(authorized);
        }

        [Fact]
        public async Task RBAC_RejectsUser_WhenPolicyRequiresDifferentGroup()
        {
            SeedPolicy("p1", "tool:ha__turn_on", "SmartHomeAdmins", true);

            var session = CreateSession("bob", new List<string> { "Users" });

            var authorized = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on");
            Assert.False(authorized);
        }

        [Fact]
        public async Task RBAC_RejectsUser_OnExplicitDeny()
        {
            SeedPolicy("p1", "tool:ha__turn_on", "Users", false);

            var session = CreateSession("bob", new List<string> { "Users" });

            var authorized = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on");
            Assert.False(authorized);
        }

        [Fact]
        public async Task CallToolAsync_ReturnsError_WhenUnauthorized()
        {
            SeedPolicy("p1", "tool:ha__turn_on", "SmartHomeAdmins", true);

            var session = CreateSession("bob", new List<string> { "Users" });

            var result = await session.CallToolAsync("ha__turn_on", "{}", null!);
            Assert.NotNull(result);

            var json = JsonSerializer.Serialize(result);
            Assert.Contains("Security Error", json);
        }

        [Fact]
        public async Task GetPromptAsync_ThrowsUnauthorized_WhenUnauthorized()
        {
            SeedPolicy("p1", "prompt:secure_prompt", "Admins", true);

            var session = CreateSession("bob", new List<string> { "Users" });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await session.GetPromptAsync("secure_prompt", "{}");
            });
        }

        [Fact]
        public async Task ReadResourceAsync_ThrowsUnauthorized_WhenUnauthorized()
        {
            SeedPolicy("p1", "resource:router://status", "Admins", true);

            var session = CreateSession("bob", new List<string> { "Users" });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await session.ReadResourceAsync("router://status", "{}");
            });
        }

        [Fact]
        public async Task RBAC_DefaultsToDenied_WhenDbExceptionThrown()
        {
            var context = new DefaultHttpContext();
            var claims = new List<System.Security.Claims.Claim> { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "bob") };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);

            // Do not register IDbConnectionFactory to simulate db resolution/connection failure
            var services = new ServiceCollection();
            context.RequestServices = services.BuildServiceProvider();

            var session = new ClientSession(
                "test-sess",
                context.Response,
                new List<McpServer>(),
                new HttpClient(),
                new Mock<IEmbeddingService>().Object,
                null,
                new Mock<Microsoft.Extensions.Logging.ILogger<ClientSession>>().Object
            );

            var authorized = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on");
            Assert.False(authorized);
        }

        [Fact]
        public async Task ToolsList_FiltersByAuthorization()
        {
            var context = new DefaultHttpContext();
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "bob"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "house_member")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);

            var services = new ServiceCollection();
            services.AddSingleton(_dbFactory);
            context.RequestServices = services.BuildServiceProvider();

            await _connection.ExecuteAsync(@"
                INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed)
                VALUES ('1', 'tool:serverA__tool1', 'house_member', 1);");

            var session = new ClientSession(
                "test-sess",
                context.Response,
                new List<McpServer>(),
                new HttpClient(),
                new Mock<IEmbeddingService>().Object,
                null,
                new Mock<Microsoft.Extensions.Logging.ILogger<ClientSession>>().Object
            );

            bool tool1Auth = await session.IsUserAuthorizedAsync("tools/list", "serverA__tool1", context);
            bool tool2Auth = await session.IsUserAuthorizedAsync("tools/list", "serverB__tool2", context);

            Assert.True(tool1Auth);
            Assert.False(tool2Auth);
        }
    }
}
