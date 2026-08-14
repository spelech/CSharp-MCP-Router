using System;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Controllers;
using McpRouter.Core.Database;
using McpRouter.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class ProvidersControllerTests : IDisposable
    {
        private const string ConnectionString = "Data Source=InMemoryProvidersDb;Mode=Memory;Cache=Shared";
        private readonly SqliteConnection _masterConnection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly DatabaseRepository _dbRepo;

        public ProvidersControllerTests()
        {
            _masterConnection = new SqliteConnection(ConnectionString);
            _masterConnection.Open();

            _masterConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS SecretProviders (
                    ProviderName TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    EncryptedConfigJson TEXT,
                    IsEnabled INTEGER
                );
                CREATE TABLE IF NOT EXISTS AuthProviderConfigs (
                    ProviderName TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    UserHeader TEXT,
                    GroupsHeader TEXT,
                    ConfigJson TEXT,
                    IsEnabled INTEGER
                );");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() =>
            {
                var conn = new SqliteConnection(ConnectionString);
                conn.Open();
                return conn;
            });
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;
            _dbRepo = new DatabaseRepository(_dbFactory);
        }

        public void Dispose()
        {
            _masterConnection.Dispose();
        }

        [Fact]
        public async Task GetSecretProviders_ReturnsOkWithList()
        {
            var controller = new ProvidersController(_dbRepo, _dbRepo);
            var result = await controller.GetSecretProviders() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task SaveSecretProvider_ReturnsBadRequest_WhenProviderNameMissing()
        {
            var mockAudit = new Mock<IAuditLogger>();
            var controller = new ProvidersController(_dbRepo, _dbRepo);
            var dto = new SecretProviderDto { ProviderName = "" };

            var result = await controller.SaveSecretProvider(dto, mockAudit.Object) as BadRequestObjectResult;
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SaveSecretProvider_ReturnsBadRequest_WhenHttpUrlPassedInConfig()
        {
            var mockAudit = new Mock<IAuditLogger>();
            var controller = new ProvidersController(_dbRepo, _dbRepo);
            var dto = new SecretProviderDto
            {
                ProviderName = "Vault",
                DisplayName = "HashiCorp Vault",
                ConfigJson = "{\"url\":\"http://insecure-vault.local:8200\"}"
            };

            var result = await controller.SaveSecretProvider(dto, mockAudit.Object) as BadRequestObjectResult;
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SaveSecretProvider_SavesSuccessfully()
        {
            var mockAudit = new Mock<IAuditLogger>();
            var controller = new ProvidersController(_dbRepo, _dbRepo);
            var dto = new SecretProviderDto
            {
                ProviderName = "Vault",
                DisplayName = "HashiCorp Vault",
                ConfigJson = "{\"url\":\"https://vault.local:8200\"}",
                IsEnabled = true
            };

            var result = await controller.SaveSecretProvider(dto, mockAudit.Object) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetAuthProviders_ReturnsOkWithList()
        {
            var controller = new ProvidersController(_dbRepo, _dbRepo);
            var result = await controller.GetAuthProviders() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task SaveAuthProvider_ReturnsBadRequest_WhenProviderNameMissing()
        {
            var mockAudit = new Mock<IAuditLogger>();
            var controller = new ProvidersController(_dbRepo, _dbRepo);
            var dto = new AuthProviderDto { ProviderName = "" };

            var result = await controller.SaveAuthProvider(dto, mockAudit.Object) as BadRequestObjectResult;
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SaveAuthProvider_SavesSuccessfully()
        {
            var mockAudit = new Mock<IAuditLogger>();
            var controller = new ProvidersController(_dbRepo, _dbRepo);
            var dto = new AuthProviderDto
            {
                ProviderName = "PocketID",
                DisplayName = "PocketID OIDC",
                UserHeader = "Remote-User",
                GroupsHeader = "Remote-Groups",
                ConfigJson = "{\"authority\":\"https://sso.local\"}",
                IsEnabled = true
            };

            var result = await controller.SaveAuthProvider(dto, mockAudit.Object) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetSecretProviders_Returns500_OnDbException()
        {
            var mockFailingFactory = new Mock<IDbConnectionFactory>();
            mockFailingFactory.Setup(f => f.CreateConnection()).Throws(new Exception("DB Connection Failed"));

            var failingRepo = new DatabaseRepository(mockFailingFactory.Object);
            var controller = new ProvidersController(failingRepo, failingRepo);
            var result = await controller.GetSecretProviders() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task GetAuthProviders_Returns500_OnDbException()
        {
            var mockFailingFactory = new Mock<IDbConnectionFactory>();
            mockFailingFactory.Setup(f => f.CreateConnection()).Throws(new Exception("DB Connection Failed"));

            var failingRepo = new DatabaseRepository(mockFailingFactory.Object);
            var controller = new ProvidersController(failingRepo, failingRepo);
            var result = await controller.GetAuthProviders() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }
    }
}
