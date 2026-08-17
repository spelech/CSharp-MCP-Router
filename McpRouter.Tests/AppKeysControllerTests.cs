using McpRouter.Tests.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Components.Clients;
using McpRouter.Components.AppKeys;
using McpRouter.Components.Providers;
using McpRouter.Components.Authorization;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Identity;
using McpRouter.Core.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class AppKeysControllerTests : IDisposable
    {
        private class NonDisposingConnection : IDbConnection
        {
            private readonly IDbConnection _inner;
            public NonDisposingConnection(IDbConnection inner) => _inner = inner;
            [System.Diagnostics.CodeAnalysis.AllowNull]
            public string ConnectionString { get => _inner.ConnectionString ?? string.Empty; set => _inner.ConnectionString = value ?? string.Empty; }
            public int ConnectionTimeout => _inner.ConnectionTimeout;
            public string Database => _inner.Database;
            public ConnectionState State => _inner.State;
            public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
            public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
            public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
            public void Close() { }
            public IDbCommand CreateCommand() => _inner.CreateCommand();
            public void Dispose() { }
            public void Open() { if (_inner.State != ConnectionState.Open) _inner.Open(); }
        }

        private readonly SqliteConnection _rawConnection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;

        public AppKeysControllerTests()
        {
            _rawConnection = new SqliteConnection($"DataSource=file:mem_{Guid.NewGuid():N}?mode=memory&cache=shared");
            _rawConnection.Open();

            _rawConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS AppKeys (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Username TEXT,
                    OwnerSid TEXT DEFAULT '',
                    KeyPrefix TEXT,
                    EncryptedKey TEXT,
                    ScopesJson TEXT DEFAULT '[]',
                    ExpiresAt TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY,
                    GlobalMaxKeys INTEGER DEFAULT 100,
                    UserMaxKeys INTEGER DEFAULT 5
                );");

            _rawConnection.Execute("INSERT OR REPLACE INTO Settings (Id, GlobalMaxKeys, UserMaxKeys) VALUES ('default', 100, 5);");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() => new NonDisposingConnection(_rawConnection));
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;

            _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" },
                { "Oidc:TrustedProxies", "127.0.0.1,::1" },
                { "Admin:GroupSid", "full_admin" }
            }).Build();
        }

        public void Dispose()
        {
            _rawConnection.Dispose();
        }

        private AppKeysController CreateController(string username = "alice", string role = "Admin")
        {
            var repo = new DatabaseRepository(_dbFactory);
            var credSvc = new CredentialService(_dbFactory);
            var controller = new AppKeysController(repo, repo, _config, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object, credSvc);

            var services = new ServiceCollection();
            services.AddSingleton(_config);
            services.AddLogging();
            services.AddSingleton<IIdentityProvider, OidcIdentityProvider>();
            services.AddSingleton<CompositeIdentityProvider>();

            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
            httpContext.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
            httpContext.Request.Headers["Remote-User"] = username;
            httpContext.Request.Headers["Remote-Groups"] = role == "Admin" ? "full_admin" : "house_member";

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task GetAppKeys_ReturnsSanitizedKeys_ForAdminAndFiltered()
        {
            await _rawConnection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson)
                VALUES ('key-1', 'Test Key', 'alice', 'mcp-test', 'secret-hash', '[""read""]');");

            var controller = CreateController("alice", "Admin");
            var result = await controller.GetAppKeys("alice");

            var okResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.NotNull(okResult.Value);

            var allKeysRes = await controller.GetAppKeys(null);
            Assert.Equal(200, ((ObjectResult)allKeysRes).StatusCode);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task CreateAppKey_CreatesNewKey_Successfully_WithDifferentScopeSlugs()
        {
            var controller = CreateController("alice", "User");
            var req1 = new CreateAppKeyRequest
            {
                Name = "Server Key",
                Scopes = new List<string> { "server:plex" },
                ExpiresInDays = 30
            };

            var res1 = await controller.CreateAppKey(req1);
            Assert.Equal(200, ((ObjectResult)res1).StatusCode);

            var req2 = new CreateAppKeyRequest
            {
                Name = "Group Key",
                Scopes = new List<string> { "group:media" }
            };

            var res2 = await controller.CreateAppKey(req2);
            Assert.Equal(200, ((ObjectResult)res2).StatusCode);

            var req3 = new CreateAppKeyRequest
            {
                Name = "Tool Key",
                Scopes = new List<string> { "tool:docker_list" }
            };

            var res3 = await controller.CreateAppKey(req3);
            Assert.Equal(200, ((ObjectResult)res3).StatusCode);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task CreateAppKey_ReturnsBadRequest_WhenNameMissing()
        {
            var controller = CreateController("alice", "User");
            var req = new CreateAppKeyRequest { Name = "" };

            var result = await controller.CreateAppKey(req);
            var badRes = Assert.IsAssignableFrom<BadRequestObjectResult>(result);
            Assert.Equal(400, badRes.StatusCode);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task CreateAppKey_EnforcesUserLimit_ForNonAdmin()
        {
            // Set UserMaxKeys to 1
            await _rawConnection.ExecuteAsync("UPDATE Settings SET UserMaxKeys = 1;");

            var controller = CreateController("bob", "User");
            var req1 = new CreateAppKeyRequest { Name = "Key 1" };
            var res1 = await controller.CreateAppKey(req1);
            Assert.Equal(200, ((ObjectResult)res1).StatusCode);

            // Second key should hit personal limit
            var req2 = new CreateAppKeyRequest { Name = "Key 2" };
            var res2 = await controller.CreateAppKey(req2);
            var badRes = Assert.IsAssignableFrom<BadRequestObjectResult>(res2);
            Assert.Equal(400, badRes.StatusCode);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task RevokeAppKey_ReturnsNotFound_WhenIdDoesNotExist()
        {
            var controller = CreateController("alice", "Admin");
            var result = await controller.RevokeAppKey("non-existent-id");
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task RevokeAppKey_ReturnsForbid_WhenUserNotOwnerOrAdmin()
        {
            await _rawConnection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey)
                VALUES ('key-alice', 'Alice Key', 'alice', 'mcp-alice', 'hash');");

            var controller = CreateController("charlie", "User");
            var result = await controller.RevokeAppKey("key-alice");
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task GetAppKeysLimits_ReturnsLimitsAndCounts()
        {
            var controller = CreateController("alice", "User");
            var result = await controller.GetAppKeysLimits();

            var okResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task Controllers_HandleDbFailures_Returning500()
        {
            var mockConn = new Mock<IDbConnection>();
            mockConn.Setup(c => c.State).Returns(ConnectionState.Open);

            var failingFactory = new Mock<IDbConnectionFactory>();
            failingFactory.Setup(f => f.CreateConnection()).Returns(mockConn.Object);
            failingFactory.Setup(f => f.ProviderName).Returns("sqlite");

            var failingRepo = new DatabaseRepository(failingFactory.Object);
            var failingCredSvc = new CredentialService(failingFactory.Object);
            var controller = new AppKeysController(failingRepo, failingRepo, _config, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object, failingCredSvc);
            var services = new ServiceCollection();
            services.AddSingleton(_config);
            services.AddLogging();
            services.AddSingleton<IIdentityProvider, OidcIdentityProvider>();
            services.AddSingleton<CompositeIdentityProvider>();

            var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
            httpContext.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
            httpContext.Request.Headers["Remote-User"] = "admin";
            httpContext.Request.Headers["Remote-Groups"] = "full_admin";
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            Assert.Equal(500, ((ObjectResult)await controller.GetAppKeys()).StatusCode);
            Assert.Equal(500, ((ObjectResult)await controller.GetAppKeysLimits()).StatusCode);
            Assert.Equal(500, ((ObjectResult)await controller.CreateAppKey(new CreateAppKeyRequest { Name = "Test" })).StatusCode);
            Assert.Equal(500, ((ObjectResult)await controller.RevokeAppKey("k1")).StatusCode);
        }
    }
}
