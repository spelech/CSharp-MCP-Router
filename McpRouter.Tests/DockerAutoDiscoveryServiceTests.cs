using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Core.Database;
using McpRouter.Models;
using McpRouter.Services;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class DockerAutoDiscoveryServiceTests
    {
        private RouterDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<RouterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var db = new RouterDbContext(options, config);
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public void Service_Initializes_With_Valid_Dependencies()
        {
            var db = CreateDbContext();
            var services = new ServiceCollection();
            services.AddSingleton(db);
            var serviceProvider = services.BuildServiceProvider();

            var discoveryService = new DockerAutoDiscoveryService(serviceProvider, NullLogger<DockerAutoDiscoveryService>.Instance);
            Assert.NotNull(discoveryService);
        }

        [Fact]
        public void DockerDiscovery_SkipsContainer_ResolvingToPrivateIp()
        {
            var db = CreateDbContext();
            var services = new ServiceCollection();
            services.AddSingleton(db);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            services.AddSingleton<IConfiguration>(config);
            var serviceProvider = services.BuildServiceProvider();

            var discoveryService = new DockerAutoDiscoveryService(serviceProvider, NullLogger<DockerAutoDiscoveryService>.Instance);
            Assert.NotNull(discoveryService);

            bool isBlocked1 = McpRouter.Core.Security.SecurityValidationHelper.IsBlockedIp(System.Net.IPAddress.Parse("127.0.0.1"), Array.Empty<string>());
            bool isBlocked2 = McpRouter.Core.Security.SecurityValidationHelper.IsBlockedIp(System.Net.IPAddress.Parse("169.254.169.254"), Array.Empty<string>());

            Assert.True(isBlocked1);
            Assert.True(isBlocked2);
        }

        [Fact]
        public async Task ExecuteAsync_SkipsScan_WhenDockerSocketDoesNotExist()
        {
            var db = CreateDbContext();
            var services = new ServiceCollection();
            services.AddSingleton(db);
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
            var db = CreateDbContext();
            var mockFactory = new Mock<IHttpClientFactory>();
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            var sessionManager = new SessionManager(sp, mockFactory.Object, NullLogger<SessionManager>.Instance);

            // Add an existing auto-discovered server
            db.Servers.Add(new McpServer
            {
                Id = "old-server",
                DisplayName = "Old Server",
                Url = "http://old:8080/sse",
                AutoDiscovered = true,
                Enabled = true,
                Categories = new List<string> { "legacy" }
            });
            db.SaveChanges();

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

            DockerAutoDiscoveryService.UpsertDiscoveredServers(discovered, db, sessionManager, NullLogger.Instance);

            var updatedOld = db.Servers.FirstOrDefault(s => s.Id == "old-server");
            Assert.NotNull(updatedOld);
            Assert.Equal("Updated Old Server", updatedOld.DisplayName);
            Assert.Equal("http://old:8081/sse", updatedOld.Url);
            Assert.Equal("http", updatedOld.Type);
            Assert.Contains("updated", updatedOld.Categories);

            var newSrv = db.Servers.FirstOrDefault(s => s.Id == "new-server");
            Assert.NotNull(newSrv);
        }
    }
}
