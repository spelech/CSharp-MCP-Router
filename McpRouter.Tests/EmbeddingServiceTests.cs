using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Core.Database;
using McpRouter.Models;
using McpRouter.Services;
using Xunit;

namespace McpRouter.Tests
{
    public class EmbeddingServiceTests
    {
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

        private (RouterDbContext db, RouterSettings settings) CreateDbContext(string provider = "OpenAI", string apiUrl = "http://localhost:5000/v1/embeddings", string apiKey = "test-key", string model = "text-embedding-3-small")
        {
            var options = new DbContextOptionsBuilder<RouterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "Encryption:Key", "TestSecretKey1234567890123456789012" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var db = new RouterDbContext(options, config);
            db.Database.EnsureCreated();

            var settings = new RouterSettings
            {
                Id = "global",
                EmbeddingProvider = provider,
                EmbeddingApiUrl = apiUrl,
                EmbeddingApiKey = apiKey,
                EmbeddingApiModel = model
            };
            db.Settings.Add(settings);
            db.SaveChanges();

            return (db, settings);
        }

        [Fact]
        public async Task ApiEmbeddingService_GetEmbeddingAsync_Returns_Vector_From_OpenAI_Response()
        {
            var (db, settings) = CreateDbContext();
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
            var (db, settings) = CreateDbContext();
            var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var service = new ApiEmbeddingService(new HttpClient(handler), settings);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetEmbeddingAsync("test query"));
        }
    }
}
