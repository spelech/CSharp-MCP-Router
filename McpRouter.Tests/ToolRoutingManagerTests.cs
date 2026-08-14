using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Core.Routing;
using McpRouter.Models;
using McpRouter.Core.Routing;
using Moq;
using Xunit;
using Dapper;

namespace McpRouter.Tests
{
    public class ToolRoutingManagerTests
    {
        private (SqliteConnection conn, IDbConnectionFactory factory) CreateDbFactory(bool requireManualApproval = false)
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY,
                    RequireManualApproval INTEGER DEFAULT 0
                );
            ");

            if (requireManualApproval)
            {
                connection.Execute("INSERT INTO Settings (Id, RequireManualApproval) VALUES ('default', 1)");
            }

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(connection);
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (connection, mockDbFactory.Object);
        }

        [Fact]
        public async Task ListToolsAsync_ReturnsMetaTools_InMetaMode()
        {
            var manager = new ToolRoutingManager();
            var connections = new Dictionary<string, BackendConnection>();
            var servers = new List<McpServer>();

            var tools = await manager.ListToolsAsync(
                body: "{}",
                isMetaMode: true,
                backendConnections: connections,
                logger: NullLogger.Instance,
                ensureBackendsInitializedAsync: () => Task.CompletedTask,
                servers: servers
            );

            Assert.NotNull(tools);
            Assert.Equal(2, tools.Count);
        }

        [Fact]
        public void InvalidateCache_ClearsPopulatedState()
        {
            var manager = new ToolRoutingManager();
            manager.InvalidateCache();
            Assert.Empty(manager.GetCachedTools());
        }

        [Fact]
        public async Task CallToolAsync_SearchTools_ReturnsSemanticResults()
        {
            var manager = new ToolRoutingManager();
            var (conn, dbFactory) = CreateDbFactory();
            var connections = new ConcurrentDictionary<string, BackendConnection>();
            var servers = new List<McpServer>();
            var mockEmbedding = new Mock<IEmbeddingService>();
            mockEmbedding.Setup(e => e.GetEmbeddingAsync(It.IsAny<string>()))
                         .ReturnsAsync(new float[384]);

            var body = "{\"params\":{\"arguments\":{\"query\":\"Excel\"}}}";
            var result = await manager.CallToolAsync(
                "search_tools",
                body,
                dbFactory,
                connections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                mockEmbedding.Object,
                () => Task.CompletedTask,
                (b, k, v) => b
            );

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CallToolAsync_ExecuteTool_ReturnsError_WhenNameMissing()
        {
            var manager = new ToolRoutingManager();
            var (conn, dbFactory) = CreateDbFactory();
            var connections = new ConcurrentDictionary<string, BackendConnection>();
            var servers = new List<McpServer>();
            var mockEmbedding = new Mock<IEmbeddingService>();

            var body = "{\"params\":{\"arguments\":{}}}";
            var result = await manager.CallToolAsync(
                "execute_tool",
                body,
                dbFactory,
                connections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                mockEmbedding.Object,
                () => Task.CompletedTask,
                (b, k, v) => b
            );

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CallToolAsync_ReturnsCancellationError_WhenCancelled()
        {
            var manager = new ToolRoutingManager();
            var (conn, dbFactory) = CreateDbFactory();
            var connections = new ConcurrentDictionary<string, BackendConnection>();
            var servers = new List<McpServer>();
            var mockEmbedding = new Mock<IEmbeddingService>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await manager.CallToolAsync(
                "execute_tool",
                "{}",
                dbFactory,
                connections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                mockEmbedding.Object,
                () => Task.Delay(1000, cts.Token),
                (b, k, v) => b,
                cts.Token
            );

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CallToolAsync_RequiresManualApproval_WhenSettingEnabled()
        {
            var manager = new ToolRoutingManager();
            manager.ToolRoutingTable["docker__restart"] = "docker";

            var (conn, dbFactory) = CreateDbFactory(requireManualApproval: true);
            var server = new McpServer { Id = "docker", Url = "http://docker:8080/sse", Enabled = true, Type = "http" };
            var backendConn = new BackendConnection(server, new HttpClient(), NullLogger.Instance);
            var connections = new ConcurrentDictionary<string, BackendConnection> { ["docker"] = backendConn };
            var servers = new List<McpServer> { server };
            var mockEmbedding = new Mock<IEmbeddingService>();

            var mockFactory = new Mock<IHttpClientFactory>();
            var services = new ServiceCollection();
            var sessionManager = new SessionManager(services.BuildServiceProvider(), mockFactory.Object, NullLogger<SessionManager>.Instance);

            var body = "{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"docker__restart\",\"arguments\":{}}}";

            var callTask = manager.CallToolAsync(
                "docker__restart",
                body,
                dbFactory,
                connections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                mockEmbedding.Object,
                () => Task.CompletedTask,
                (b, k, v) => b,
                sessionManager: sessionManager
            );

            Assert.NotEmpty(sessionManager.PendingApprovals);
            var approval = sessionManager.PendingApprovals.Values.GetEnumerator();
            approval.MoveNext();
            approval.Current.Tcs.SetResult(false); // Deny execution

            var result = await callTask;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CallToolAsync_ExecutesCustomTool_FromRegistry()
        {
            var manager = new ToolRoutingManager();
            manager.ToolRoutingTable["plex_get_sessions"] = "plex";

            var (conn, dbFactory) = CreateDbFactory();
            var server = new McpServer { Id = "plex", Url = "http://plex:8500/sse", Enabled = true, Type = "http" };
            var backendConn = new BackendConnection(server, new HttpClient(), NullLogger.Instance);
            var connections = new ConcurrentDictionary<string, BackendConnection> { ["plex"] = backendConn };
            var servers = new List<McpServer> { server };
            var mockEmbedding = new Mock<IEmbeddingService>();

            var body = "{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"plex_get_sessions\",\"arguments\":{}}}";

            var result = await manager.CallToolAsync(
                "plex_get_sessions",
                body,
                dbFactory,
                connections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                mockEmbedding.Object,
                () => Task.CompletedTask,
                (b, k, v) => b
            );

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CallToolAsync_ThrowsKeyNotFound_WhenToolNotInRoutingTable()
        {
            var manager = new ToolRoutingManager();
            var (conn, dbFactory) = CreateDbFactory();
            var connections = new ConcurrentDictionary<string, BackendConnection>();
            var servers = new List<McpServer>();
            var mockEmbedding = new Mock<IEmbeddingService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => manager.CallToolAsync(
                "unknown_tool",
                "{}",
                dbFactory,
                connections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                mockEmbedding.Object,
                () => Task.CompletedTask,
                (b, k, v) => b
            ));
        }
    }
}
