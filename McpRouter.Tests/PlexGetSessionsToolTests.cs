using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Core.Database;
using McpRouter.CustomTools;
using McpRouter.Models;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;
using Dapper;

namespace McpRouter.Tests
{
    public class PlexGetSessionsToolTests
    {
        private (SqliteConnection conn, IDbConnectionFactory factory) CreateDbFactory()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Servers (
                    Id TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    Url TEXT,
                    Enabled INTEGER DEFAULT 1
                );
            ");

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(connection);
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (connection, mockDbFactory.Object);
        }

        [Fact]
        public void MetadataProperties_AreConfiguredCorrectly()
        {
            var tool = new PlexGetSessionsTool();
            Assert.Equal("plex_get_sessions", tool.Name);
            Assert.False(string.IsNullOrWhiteSpace(tool.Description));
            Assert.NotNull(tool.InputSchema);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenPlexServerNotFoundInDb()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new PlexGetSessionsTool();
            var httpClient = new HttpClient();

            var doc = JsonDocument.Parse("{}");
            var result = await tool.ExecuteAsync(doc.RootElement, httpClient, dbFactory);

            var json = JsonSerializer.Serialize(result);
            Assert.Contains("Plex service is not configured or disabled.", json);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenPlexServerIsDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            conn.Execute("INSERT INTO Servers (Id, DisplayName, Url, Enabled) VALUES ('plex', 'Plex', 'http://plex:32400', 0)");

            var tool = new PlexGetSessionsTool();
            var httpClient = new HttpClient();

            var doc = JsonDocument.Parse("{}");
            var result = await tool.ExecuteAsync(doc.RootElement, httpClient, dbFactory);

            var json = JsonSerializer.Serialize(result);
            Assert.Contains("Plex service is not configured or disabled.", json);
        }
    }
}
