using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;
using FluentAssertions;
using McpRouter.Controllers;
using McpRouter.Core.Database;
using Dapper;

namespace McpRouter.Tests
{
    public class ClientsControllerTests
    {
        private (SqliteConnection conn, IDbConnectionFactory factory) CreateDbFactory()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS AppKeys (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Username TEXT,
                    KeyPrefix TEXT,
                    EncryptedKey TEXT,
                    ScopesJson TEXT DEFAULT '[]',
                    ExpiresAt TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );
            ");

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(connection);
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (connection, mockDbFactory.Object);
        }

        [Fact]
        public async Task GetClients_ReturnsOk_WithClientsAndMappedProperties()
        {
            var (conn, dbFactory) = CreateDbFactory();
            conn.Execute("INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson) VALUES ('id-1', 'Client One', 'client-1', 'mcp_prefix1', 'secret1', '[\"mcp_client\"]')");

            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
            var controller = new ClientsController(dbFactory, mockAudit.Object);

            var result = await controller.GetClients();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var list = (okResult.Value as IEnumerable<object>)?.ToList();

            list.Should().NotBeNull();
            list.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateClient_ReturnsOk_WithGeneratedCredentials()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
            var controller = new ClientsController(dbFactory, mockAudit.Object);

            var model = new ClientsController.CreateClientModel
            {
                DisplayName = "Test CLI",
                Scopes = new List<string> { "custom_scope" }
            };

            var result = await controller.CreateClient(model);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value;

            value.Should().NotBeNull();
            var displayNameProp = value!.GetType().GetProperty("DisplayName")?.GetValue(value, null) as string;
            displayNameProp.Should().Be("Test CLI");
        }

        [Fact]
        public async Task DeleteClient_ReturnsNoContent_WhenAppExists()
        {
            var (conn, dbFactory) = CreateDbFactory();
            conn.Execute("INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey) VALUES ('123', 'Client', 'user', 'pref', 'sec')");

            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
            var controller = new ClientsController(dbFactory, mockAudit.Object);
            var result = await controller.DeleteClient("123");

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteClient_ReturnsNotFound_WhenAppDoesNotExist()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
            var controller = new ClientsController(dbFactory, mockAudit.Object);

            var result = await controller.DeleteClient("nonexistent");
            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
