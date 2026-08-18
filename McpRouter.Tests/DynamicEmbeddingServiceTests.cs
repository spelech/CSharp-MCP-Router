using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
    public class DynamicEmbeddingServiceTests
    {
        public DynamicEmbeddingServiceTests()
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

        private (IServiceProvider provider, SqliteConnection masterConn) CreateServiceProvider()
        {
            var dbName = $"Data Source=EmbeddingTestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var masterConn = new SqliteConnection(dbName);
            masterConn.Open();

            masterConn.Execute(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY,
                    EmbeddingProvider TEXT,
                    EmbeddingApiUrl TEXT,
                    EmbeddingApiKey TEXT,
                    EmbeddingApiModel TEXT,
                    EmbeddingModelDir TEXT,
                    DashboardTitle TEXT DEFAULT 'MCP Gateway', DashboardIcon TEXT DEFAULT 'fa-solid fa-network-wired', GlobalMaxKeys INTEGER DEFAULT 100,
                    UserMaxKeys INTEGER DEFAULT 5
                );
            ");

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(() => new SqliteConnection(dbName));
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");

            var services = new ServiceCollection();
            services.AddSingleton(mockDbFactory.Object);
            var provider = services.BuildServiceProvider();

            return (provider, masterConn);
        }

        [Fact]
        public void DynamicEmbeddingService_Gets_And_Saves_Settings()
        {
            var (provider, conn) = CreateServiceProvider();
            var service = new DynamicEmbeddingService(new HttpClient(), NullLoggerFactory.Instance, provider);

            var settings = service.GetSettings();
            Assert.NotNull(settings);

            settings.EmbeddingProvider = "API";
            settings.EmbeddingApiUrl = "http://localhost:5000/v1/embeddings";
            settings.EmbeddingApiKey = "test-api-key";

            service.SaveSettings(settings);

            var saved = conn.QueryFirstOrDefault<RouterSettings>("SELECT * FROM Settings WHERE Id = 'default'");
            Assert.NotNull(saved);
            Assert.Equal("API", saved.EmbeddingProvider);
            Assert.Equal("http://localhost:5000/v1/embeddings", saved.EmbeddingApiUrl);
        }

        [Fact]
        public void PrivateOrLoopback_Blocked_When_AllowPrivateIps_False()
        {
            Environment.SetEnvironmentVariable("ALLOW_PRIVATE_IPS", "false");
            try
            {
                var (provider, conn) = CreateServiceProvider();
                var service = new DynamicEmbeddingService(new HttpClient(), NullLoggerFactory.Instance, provider);

                var settings = new RouterSettings
                {
                    EmbeddingProvider = "api",
                    EmbeddingApiUrl = "http://127.0.0.1:5000/v1/embeddings"
                };

                Assert.Throws<ArgumentException>(() => service.SaveSettings(settings));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ALLOW_PRIVATE_IPS", "true");
            }
        }

        [Fact]
        public void ReloadSettings_UpdatesSettingsAndActiveService()
        {
            var (provider, conn) = CreateServiceProvider();
            var service = new DynamicEmbeddingService(new HttpClient(), NullLoggerFactory.Instance, provider);

            var newSettings = new RouterSettings
            {
                EmbeddingProvider = "api",
                EmbeddingApiUrl = "https://embeddings.remote.io/v1"
            };

            service.ReloadSettings(newSettings);
            Assert.Equal("api", service.GetSettings().EmbeddingProvider);
        }

        [Fact]
        public async Task DynamicEmbeddingService_GetEmbeddingAsync_Uses_ApiProvider_When_Configured()
        {
            var (provider, conn) = CreateServiceProvider();
            var handler = new MockHttpMessageHandler(req =>
            {
                var json = "{\"data\":[{\"embedding\":[1.0, 0.0, 0.0]}]}";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            });

            var service = new DynamicEmbeddingService(new HttpClient(handler), NullLoggerFactory.Instance, provider);

            var settings = service.GetSettings();
            settings.EmbeddingProvider = "API";
            settings.EmbeddingApiUrl = "http://localhost:5000/v1/embeddings";
            service.SaveSettings(settings);

            var embedding = await service.GetEmbeddingAsync("hello world");
            Assert.NotNull(embedding);
            Assert.Equal(3, embedding.Length);
            Assert.Equal(1.0F, embedding[0]);
        }

        [Fact]
        public void CosineSimilarity_Calculates_Correct_Vector_Distance()
        {
            var (provider, conn) = CreateServiceProvider();
            var service = new DynamicEmbeddingService(new HttpClient(), NullLoggerFactory.Instance, provider);

            var v1 = new float[] { 1.0F, 0.0F, 0.0F };
            var v2 = new float[] { 1.0F, 0.0F, 0.0F };
            var v3 = new float[] { 0.0F, 1.0F, 0.0F };

            var simIdentical = service.CosineSimilarity(v1, v2);
            var simOrthogonal = service.CosineSimilarity(v1, v3);

            Assert.Equal(1.0, simIdentical, 3);
            Assert.Equal(0.0, simOrthogonal, 3);

            // Null and empty edge cases
            Assert.Equal(0.0, service.CosineSimilarity(null!, v2));
            Assert.Equal(0.0, service.CosineSimilarity(v1, null!));
            Assert.Equal(0.0, service.CosineSimilarity(Array.Empty<float>(), Array.Empty<float>()));
            Assert.Equal(0.0, service.CosineSimilarity(new float[] { 1f }, new float[] { 1f, 2f }));
        }

        [Fact]
        public async Task PreWarmAsync_Executes_Without_Throwing()
        {
            var (provider, conn) = CreateServiceProvider();
            var handler = new MockHttpMessageHandler(req =>
            {
                var json = "{\"data\":[{\"embedding\":[0.1, 0.2]}]}";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            });

            var service = new DynamicEmbeddingService(new HttpClient(handler), NullLoggerFactory.Instance, provider);
            var settings = service.GetSettings();
            settings.EmbeddingProvider = "API";
            settings.EmbeddingApiUrl = "http://localhost:5000/v1/embeddings";
            service.SaveSettings(settings);

            await service.PreWarmAsync();
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_Uses_UnderlyingProvider()
        {
            var (provider, conn) = CreateServiceProvider();
            var handler = new MockHttpMessageHandler(req =>
            {
                var json = "{\"data\":[{\"embedding\":[0.5, 0.5]}]}";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            });

            var service = new DynamicEmbeddingService(new HttpClient(handler), NullLoggerFactory.Instance, provider);
            var settings = service.GetSettings();
            settings.EmbeddingProvider = "API";
            settings.EmbeddingApiUrl = "http://localhost:5000/v1/embeddings";
            service.SaveSettings(settings);

            var emb = await service.GenerateEmbeddingAsync("testing alias");
            Assert.Equal(2, emb.Length);
            Assert.Equal(0.5F, emb[0]);
        }
    }
}

