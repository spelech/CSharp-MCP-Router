using System.Net;
using Dapper;
using Microsoft.Data.Sqlite;
using Moq;

namespace ModelContextGateway.Tests
{
    public class EmbeddingServiceTests
    {
        public EmbeddingServiceTests()
        {
            Environment.SetEnvironmentVariable("ALLOW_PRIVATE_IPS", "true");
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }

        private (SqliteConnection conn, IDbConnectionFactory factory, RouterSettings settings) CreateDbFactory(string provider = "OpenAI", string apiUrl = "http://localhost:5000/v1/embeddings", string apiKey = "test-key", string model = "text-embedding-3-small")
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY,
                    EmbeddingProvider TEXT,
                    EmbeddingApiUrl TEXT,
                    EmbeddingApiKey TEXT,
                    DashboardTitle TEXT DEFAULT 'MCP Gateway', DashboardIcon TEXT DEFAULT 'fa-solid fa-network-wired', EmbeddingApiModel TEXT
                );
            ");

            var settings = new RouterSettings
            {
                Id = "global",
                EmbeddingProvider = provider,
                EmbeddingApiUrl = apiUrl,
                EmbeddingApiKey = apiKey,
                EmbeddingApiModel = model
            };
            connection.Execute("INSERT INTO Settings (Id, EmbeddingProvider, EmbeddingApiUrl, EmbeddingApiKey, EmbeddingApiModel) VALUES (@Id, @EmbeddingProvider, @EmbeddingApiUrl, @EmbeddingApiKey, @EmbeddingApiModel)", settings);

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(connection);
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (connection, mockDbFactory.Object, settings);
        }

        [Fact]
        public async Task ApiEmbeddingService_GetEmbeddingAsync_Returns_Vector_From_OpenAI_Response()
        {
            var (conn, dbFactory, settings) = CreateDbFactory();
            var handler = new MockHttpMessageHandler(req =>
            {
                Assert.Equal("Bearer test-key", req.Headers.Authorization?.ToString());
                var json = "{\"data\":[{\"embedding\":[0.1, 0.2, 0.3]}]}";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            });

            var service = new ApiEmbeddingService(new HttpClient(handler), settings);
            var vector = await service.GetEmbeddingAsync("test query");

            Assert.NotNull(vector);
            Assert.Equal(3, vector.Length);
            Assert.Equal(0.1F, vector[0]);
            Assert.Equal(0.2F, vector[1]);
            Assert.Equal(0.3F, vector[2]);
        }

        [Fact]
        public async Task ApiEmbeddingService_GetEmbeddingAsync_Throws_On_Http_Error()
        {
            var (conn, dbFactory, settings) = CreateDbFactory();
            var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var service = new ApiEmbeddingService(new HttpClient(handler), settings);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetEmbeddingAsync("test query"));
        }
    }
}
