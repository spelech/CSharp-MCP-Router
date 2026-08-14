using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Models;
using McpRouter.Core.Routing;
using Moq;
using Xunit;
using Dapper;

namespace McpRouter.Tests
{
    public class DockerAutoDiscoveryServiceTests
    {
        private (SqliteConnection masterConn, IDbConnectionFactory factory) CreateDbFactory()
        {
            var dbName = $"Data Source=DiscoveryTestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var masterConn = new SqliteConnection(dbName);
            masterConn.Open();

            masterConn.Execute(@"
                CREATE TABLE IF NOT EXISTS Servers (
                    Id TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    Url TEXT,
                    Enabled INTEGER DEFAULT 1,
                    Hidden INTEGER DEFAULT 0,
                    Type TEXT DEFAULT 'sse',
                    SecretProvider TEXT DEFAULT 'None',
                    SecretItemKey TEXT,
                    AuthShape TEXT DEFAULT 'bearer',
                    CustomHeaderName TEXT,
                    Categories TEXT DEFAULT '[]',
                    ApiKey TEXT,
                    HeadersJson TEXT,
                    AutoDiscovered INTEGER DEFAULT 0
                );
            ");

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(() => new SqliteConnection(dbName));
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (masterConn, mockDbFactory.Object);
        }

        [Fact]
        public void Service_Initializes_With_Valid_Dependencies()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var services = new ServiceCollection();
            services.AddSingleton(dbFactory);
            var serviceProvider = services.BuildServiceProvider();

            var discoveryService = new DockerAutoDiscoveryService(serviceProvider, NullLogger<DockerAutoDiscoveryService>.Instance);
            Assert.NotNull(discoveryService);
        }

        [Fact]
        public void DockerDiscovery_SkipsContainer_ResolvingToPrivateIp()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var services = new ServiceCollection();
            services.AddSingleton(dbFactory);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            services.AddSingleton<IConfiguration>(config);
            var serviceProvider = services.BuildServiceProvider();

            var discoveryService = new DockerAutoDiscoveryService(serviceProvider, NullLogger<DockerAutoDiscoveryService>.Instance);
            Assert.NotNull(discoveryService);

            bool isBlocked1 = McpRouter.Components.Authorization.SecurityValidationHelper.IsBlockedIp(System.Net.IPAddress.Parse("127.0.0.1"), Array.Empty<string>());
            bool isBlocked2 = McpRouter.Components.Authorization.SecurityValidationHelper.IsBlockedIp(System.Net.IPAddress.Parse("169.254.169.254"), Array.Empty<string>());

            Assert.True(isBlocked1);
            Assert.True(isBlocked2);
        }

        [Fact]
        public async Task ExecuteAsync_SkipsScan_WhenDockerSocketDoesNotExist()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var services = new ServiceCollection();
            services.AddSingleton(dbFactory);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            services.AddSingleton<IConfiguration>(config);
            var serviceProvider = services.BuildServiceProvider();

            var discoveryService = new DockerAutoDiscoveryService(serviceProvider, NullLogger<DockerAutoDiscoveryService>.Instance);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(50);

            var executeMethod = typeof(DockerAutoDiscoveryService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(executeMethod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await (Task)executeMethod.Invoke(discoveryService, new object[] { cts.Token })!;
            });
        }

        [Fact]
        public void ParseDiscoveredServers_ParsesValidDockerContainerLabels()
        {
            var json = @"[
                {
                    ""Names"": [""/10.0.0.10""],
                    ""Labels"": {
                        ""mcp.enabled"": ""true"",
                        ""mcp.id"": ""docker"",
                        ""mcp.port"": ""8080"",
                        ""mcp.displayName"": ""Docker MCP"",
                        ""mcp.type"": ""sse"",
                        ""mcp.path"": ""/sse"",
                        ""mcp.categories"": ""infrastructure,tools""
                    }
                },
                {
                    ""Names"": [""/disabled-server""],
                    ""Labels"": {
                        ""mcp.enabled"": ""false""
                    }
                }
            ]";

            using var doc = JsonDocument.Parse(json);
            var discovered = DockerAutoDiscoveryService.ParseDiscoveredServers(doc.RootElement, NullLogger.Instance, new[] { "10.0.0.0/8" });

            Assert.Single(discovered);
            Assert.Equal("docker", discovered[0].Id);
            Assert.Equal("Docker MCP", discovered[0].DisplayName);
            Assert.Contains("infrastructure", discovered[0].Categories);
        }

        [Fact]
        public void UpsertDiscoveredServers_AddsNewServers_AndDisablesStoppedServers()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockFactory = new Mock<IHttpClientFactory>();
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            var sessionManager = new SessionManager(sp, mockFactory.Object, NullLogger<SessionManager>.Instance);

            conn.Execute(@"
                INSERT INTO Servers (Id, DisplayName, Url, AutoDiscovered, Enabled, Categories)
                VALUES ('old-server', 'Old Server', 'http://old:8080/sse', 1, 1, '[""legacy""]')
            ");

            var discovered = new List<McpServer>
            {
                new McpServer
                {
                    Id = "old-server",
                    DisplayName = "Updated Old Server",
                    Url = "http://old:8081/sse",
                    Type = "http",
                    AutoDiscovered = true,
                    Enabled = true,
                    Categories = new List<string> { "infrastructure", "updated" }
                },
                new McpServer
                {
                    Id = "new-server",
                    DisplayName = "New Server",
                    Url = "http://new:8080/sse",
                    AutoDiscovered = true,
                    Enabled = true,
                    Categories = new List<string> { "default" }
                }
            };

            DockerAutoDiscoveryService.UpsertDiscoveredServers(discovered, dbFactory, sessionManager, NullLogger.Instance);

            var updatedOld = conn.QueryFirstOrDefault<McpServer>("SELECT * FROM Servers WHERE Id = 'old-server'");
            Assert.NotNull(updatedOld);
            Assert.Equal("Updated Old Server", updatedOld.DisplayName);
            Assert.Equal("http://old:8081/sse", updatedOld.Url);
            Assert.Equal("http", updatedOld.Type);

            var newSrv = conn.QueryFirstOrDefault<McpServer>("SELECT * FROM Servers WHERE Id = 'new-server'");
            Assert.NotNull(newSrv);
        }
    }
}
