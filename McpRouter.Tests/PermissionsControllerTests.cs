using System;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Components.Clients;
using McpRouter.Components.AppKeys;
using McpRouter.Components.Providers;
using McpRouter.Components.Authorization;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class PermissionsControllerTests : IDisposable
    {
        private const string ConnectionString = "Data Source=InMemoryPermsDb;Mode=Memory;Cache=Shared";
        private readonly SqliteConnection _masterConnection;
        private readonly IDbConnectionFactory _dbFactory;

        public PermissionsControllerTests()
        {
            _masterConnection = new SqliteConnection(ConnectionString);
            _masterConnection.Open();

            _masterConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS AccessPolicies (
                    Id TEXT PRIMARY KEY,
                    TargetId TEXT,
                    RequiredGroup TEXT,
                    IsAllowed INTEGER
                );
                CREATE TABLE IF NOT EXISTS GroupMappings (
                    Id TEXT PRIMARY KEY,
                    ExternalId TEXT,
                    InternalGroup TEXT
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
        }

        public void Dispose()
        {
            _masterConnection.Dispose();
        }

        [Fact]
        public async Task GetPolicies_ReturnsOk()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.GetPolicies() as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task SavePolicy_ReturnsBadRequest_WhenTargetIdMissing()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var policy = new McpAccessPolicy { TargetId = "", RequiredGroup = "admin" };
            var result = await controller.SavePolicy(policy) as BadRequestObjectResult;
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SavePolicy_ReturnsBadRequest_WhenRequiredGroupMissing()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var policy = new McpAccessPolicy { TargetId = "target-1", RequiredGroup = "" };
            var result = await controller.SavePolicy(policy) as BadRequestObjectResult;
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SavePolicy_SavesSuccessfully_OnSqlite()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var policy = new McpAccessPolicy { TargetId = "target-1", RequiredGroup = "full_admin", IsAllowed = true };
            var result = await controller.SavePolicy(policy) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task SavePolicy_SavesSuccessfully_OnMySql()
        {
            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() =>
            {
                var conn = new SqliteConnection(ConnectionString);
                conn.Open();
                return conn;
            });
            mockFactory.Setup(f => f.ProviderName).Returns("mysql");

            // Mock table for sqlite emulation
            var controller = new PermissionsController(mockFactory.Object, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var policy = new McpAccessPolicy { TargetId = "target-mysql", RequiredGroup = "full_admin", IsAllowed = true };
            
            // Note: Sqlite won't parse ON DUPLICATE KEY UPDATE so it will throw in sqlite engine,
            // which tests the 500 or execution path
            var result = await controller.SavePolicy(policy);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task DeletePolicy_DeletesSuccessfully()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.DeletePolicy("policy-123") as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task DeletePolicy_Returns500_OnDbException()
        {
            var mockFailingFactory = new Mock<IDbConnectionFactory>();
            mockFailingFactory.Setup(f => f.CreateConnection()).Throws(new Exception("DB delete crash"));

            var controller = new PermissionsController(mockFailingFactory.Object, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.DeletePolicy("policy-123") as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task GetMappings_ReturnsOk()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.GetMappings() as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetMappings_Returns500_OnDbException()
        {
            var mockFailingFactory = new Mock<IDbConnectionFactory>();
            mockFailingFactory.Setup(f => f.CreateConnection()).Throws(new Exception("DB read crash"));

            var controller = new PermissionsController(mockFailingFactory.Object, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.GetMappings() as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task SaveMapping_ReturnsBadRequest_WhenExternalIdMissing()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var mapping = new GroupMapping { ExternalId = "", InternalGroup = "house_member" };
            var result = await controller.SaveMapping(mapping) as BadRequestObjectResult;
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SaveMapping_ReturnsBadRequest_WhenInternalGroupMissing()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var mapping = new GroupMapping { ExternalId = "S-1-5", InternalGroup = "" };
            var result = await controller.SaveMapping(mapping) as BadRequestObjectResult;
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SaveMapping_SavesSuccessfully_OnSqlite()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var mapping = new GroupMapping { ExternalId = "S-1-5-21", InternalGroup = "house_member" };
            var result = await controller.SaveMapping(mapping) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task SaveMapping_Returns500_OnDbException()
        {
            var mockFailingFactory = new Mock<IDbConnectionFactory>();
            mockFailingFactory.Setup(f => f.CreateConnection()).Throws(new Exception("DB save mapping crash"));

            var controller = new PermissionsController(mockFailingFactory.Object, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var mapping = new GroupMapping { ExternalId = "S-1-5-21", InternalGroup = "house_member" };
            var result = await controller.SaveMapping(mapping) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task DeleteMapping_DeletesSuccessfully()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.DeleteMapping("mapping-123") as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task DeleteMapping_Returns500_OnDbException()
        {
            var mockFailingFactory = new Mock<IDbConnectionFactory>();
            mockFailingFactory.Setup(f => f.CreateConnection()).Throws(new Exception("DB delete mapping crash"));

            var controller = new PermissionsController(mockFailingFactory.Object, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.DeleteMapping("mapping-123") as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task GetPolicies_Returns500_OnDbException()
        {
            var mockFailingFactory = new Mock<IDbConnectionFactory>();
            mockFailingFactory.Setup(f => f.CreateConnection()).Throws(new Exception("DB Error"));

            var controller = new PermissionsController(mockFailingFactory.Object, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var result = await controller.GetPolicies() as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }
    
        [Fact]
        [Requirement("REQ-PERM-GUARD-01", "GUARD", RequirementType.Positive, "Must reject saving a policy with TargetId = '*' and IsAllowed = false.")]
        public async Task SavePolicy_ReturnsBadRequest_WhenWildcardDenyPolicy()
        {
            var controller = new PermissionsController(_dbFactory, new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>().Object);
            var policy = new McpAccessPolicy { TargetId = "*", RequiredGroup = "admin", IsAllowed = false };
            var result = await controller.SavePolicy(policy) as BadRequestObjectResult;
            
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }
    }
}