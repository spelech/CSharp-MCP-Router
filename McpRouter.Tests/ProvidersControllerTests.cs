using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Controllers;
using McpRouter.Core.Database;
using McpRouter.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class ProvidersControllerTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IDbConnectionFactory _dbFactory;

        public ProvidersControllerTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:;Mode=Memory;Cache=Shared");
            _connection.Open();

            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS SecretProviders (
                    ProviderId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProviderName TEXT UNIQUE NOT NULL,
                    DisplayName TEXT NOT NULL,
                    EncryptedConfigJson TEXT,
                    IsEnabled INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS AuthProviderConfigs (
                    AuthId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProviderName TEXT UNIQUE NOT NULL,
                    DisplayName TEXT NOT NULL,
                    UserHeader TEXT DEFAULT 'Remote-User',
                    GroupsHeader TEXT DEFAULT 'Remote-Groups',
                    ConfigJson TEXT,
                    IsEnabled INTEGER DEFAULT 1
                );");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() =>
            {
                var conn = new SqliteConnection("DataSource=:memory:;Mode=Memory;Cache=Shared");
                conn.Open();
                return conn;
            });
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private ProvidersController CreateController()
        {
            var controller = new ProvidersController(_dbFactory);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return controller;
        }

        [Fact]
        public async Task SecretProviders_Save_And_Get()
        {
            var controller = CreateController();
            var mockAudit = new Mock<IAuditLogger>();

            var dto = new SecretProviderDto
            {
                ProviderName = "Vault",
                DisplayName = "HashiCorp Vault",
                ConfigJson = "{\"address\":\"https://vault:8200\"}",
                IsEnabled = true
            };

            var saveResult = await controller.SaveSecretProvider(dto, mockAudit.Object);
            var okSave = Assert.IsAssignableFrom<ObjectResult>(saveResult);
            Assert.Equal(200, okSave.StatusCode);

            var getResult = await controller.GetSecretProviders();
            var okResult = Assert.IsAssignableFrom<ObjectResult>(getResult);
            Assert.Equal(200, okResult.StatusCode);
            var providers = Assert.IsAssignableFrom<IEnumerable<SecretProviderDto>>(okResult.Value);
            Assert.Single(providers);
            Assert.Equal("Vault", providers.First().ProviderName);
        }

        [Fact]
        public async Task AuthProviders_Save_And_Get()
        {
            var controller = CreateController();
            var mockAudit = new Mock<IAuditLogger>();

            var dto = new AuthProviderDto
            {
                ProviderName = "PocketID_TinyAuth",
                DisplayName = "PocketID / TinyAuth Portal",
                UserHeader = "Remote-User",
                GroupsHeader = "Remote-Groups",
                IsEnabled = true
            };

            var saveResult = await controller.SaveAuthProvider(dto, mockAudit.Object);
            var okSave = Assert.IsAssignableFrom<ObjectResult>(saveResult);
            Assert.Equal(200, okSave.StatusCode);

            var getResult = await controller.GetAuthProviders();
            var okResult = Assert.IsAssignableFrom<ObjectResult>(getResult);
            Assert.Equal(200, okResult.StatusCode);
            var providers = Assert.IsAssignableFrom<IEnumerable<AuthProviderDto>>(okResult.Value);
            Assert.Single(providers);
            Assert.Equal("PocketID_TinyAuth", providers.First().ProviderName);
        }
    }
}
