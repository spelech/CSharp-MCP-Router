using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Persistence;
using McpRouter.CustomTools;
using McpRouter.Models;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;
using Dapper;

namespace McpRouter.Tests
{
    public class NativeToolsTests
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
        public async Task PlexGetLibrarySectionsTool_ReturnsError_WhenPlexDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new PlexGetLibrarySectionsTool();
            var doc = JsonDocument.Parse("{}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Plex", JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task PlexGetMetadataTool_ReturnsError_WhenPlexDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new PlexGetMetadataTool();
            var doc = JsonDocument.Parse("{\"ratingKey\":\"123\"}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Plex", JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task PlexGetRecentlyAddedTool_ReturnsError_WhenPlexDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new PlexGetRecentlyAddedTool();
            var doc = JsonDocument.Parse("{\"sectionId\":1}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Plex", JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task PlexSearchLibraryTool_ReturnsError_WhenPlexDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new PlexSearchLibraryTool();
            var doc = JsonDocument.Parse("{\"query\":\"Inception\"}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Plex", JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task SeerrGetMediaDetailsTool_ReturnsError_WhenSeerrDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new SeerrGetMediaDetailsTool();
            var doc = JsonDocument.Parse("{\"tmdbId\":101,\"mediaType\":\"movie\"}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Seerr", JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task SeerrGetRequestsTool_ReturnsError_WhenSeerrDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new SeerrGetRequestsTool();
            var doc = JsonDocument.Parse("{}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Seerr", JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task SeerrRequestMediaTool_ReturnsError_WhenSeerrDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new SeerrRequestMediaTool();
            var doc = JsonDocument.Parse("{\"mediaId\":202,\"mediaType\":\"movie\"}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Seerr", JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task SeerrSearchMediaTool_ReturnsError_WhenSeerrDisabled()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var tool = new SeerrSearchMediaTool();
            var doc = JsonDocument.Parse("{\"query\":\"Batman\"}");
            var result = await tool.ExecuteAsync(doc.RootElement, new HttpClient(), dbFactory);
            Assert.Contains("Seerr", JsonSerializer.Serialize(result));
        }

        [Fact]
        public void CustomToolRegistry_ReturnsAllTools()
        {
            CustomToolRegistry.Register(new PlexGetSessionsTool());
            var tools = CustomToolRegistry.GetAll();
            Assert.NotEmpty(tools);
        }
    }
}
