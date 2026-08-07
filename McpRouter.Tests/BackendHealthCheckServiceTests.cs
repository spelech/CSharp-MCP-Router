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
using McpRouter.Core;
using McpRouter.Core.Database;
using McpRouter.Models;
using McpRouter.Services;
using Xunit;

namespace McpRouter.Tests
{
    public class BackendHealthCheckServiceTests
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

        private RouterDbContext CreateInMemoryDbContext()
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
        public async Task ProbeServerAsync_Sets_Connected_When_Endpoint_Responds_200()
        {
            var db = CreateInMemoryDbContext();
            var server = new McpServer
            {
                Id = "test-1",
                DisplayName = "Test Server 1",
                Url = "http://localhost:9999/sse",
                Enabled = true,
                ApiKey = "secret123"
            };
            db.Servers.Add(server);
            await db.SaveChangesAsync();

            var handler = new MockHttpMessageHandler(req =>
            {
                Assert.Equal("Bearer secret123", req.Headers.Authorization?.ToString());
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var client = new HttpClient(handler);

            var services = new ServiceCollection();
            services.AddSingleton(db);
            var serviceProvider = services.BuildServiceProvider();

            var httpClientFactory = new MockHttpClientFactory(client);
            var sessionManager = new SessionManager(serviceProvider, httpClientFactory, NullLogger<SessionManager>.Instance);
            var logger = NullLogger<BackendHealthCheckService>.Instance;

            var healthService = new BackendHealthCheckService(serviceProvider, httpClientFactory, sessionManager, logger);

            await healthService.ProbeServerAsync(server);

            var status = sessionManager.BackendStatuses["test-1"];
            Assert.Equal("Connected", status.Status);
            Assert.Equal(1, status.Attempts);
            Assert.Empty(status.Error);
        }

        [Fact]
        public async Task ProbeServerAsync_Sets_Failed_When_Endpoint_Throws_Exception()
        {
            var db = CreateInMemoryDbContext();
            var server = new McpServer
            {
                Id = "test-2",
                DisplayName = "Test Server 2",
                Url = "http://invalid-host:9999/sse",
                Enabled = true
            };
            db.Servers.Add(server);
            await db.SaveChangesAsync();

            var handler = new MockHttpMessageHandler(req => throw new HttpRequestMessageException("Connection refused"));
            var client = new HttpClient(handler);

            var services = new ServiceCollection();
            services.AddSingleton(db);
            var serviceProvider = services.BuildServiceProvider();

            var httpClientFactory = new MockHttpClientFactory(client);
            var sessionManager = new SessionManager(serviceProvider, httpClientFactory, NullLogger<SessionManager>.Instance);
            var logger = NullLogger<BackendHealthCheckService>.Instance;

            var healthService = new BackendHealthCheckService(serviceProvider, httpClientFactory, sessionManager, logger);

            await healthService.ProbeServerAsync(server);

            var status = sessionManager.BackendStatuses["test-2"];
            Assert.Equal("Failed", status.Status);
            Assert.Equal(1, status.Attempts);
            Assert.Contains("Connection refused", status.Error);
        }

        [Fact]
        public async Task ProbeServerAsync_Sets_Disabled_When_Server_Not_Enabled()
        {
            var db = CreateInMemoryDbContext();
            var server = new McpServer
            {
                Id = "test-3",
                DisplayName = "Test Server 3",
                Url = "http://localhost:9999/sse",
                Enabled = false
            };

            var services = new ServiceCollection();
            services.AddSingleton(db);
            var serviceProvider = services.BuildServiceProvider();

            var httpClientFactory = new MockHttpClientFactory(new HttpClient());
            var sessionManager = new SessionManager(serviceProvider, httpClientFactory, NullLogger<SessionManager>.Instance);
            var logger = NullLogger<BackendHealthCheckService>.Instance;

            var healthService = new BackendHealthCheckService(serviceProvider, httpClientFactory, sessionManager, logger);

            await healthService.ProbeServerAsync(server);

            var status = sessionManager.BackendStatuses["test-3"];
            Assert.Equal("Disabled", status.Status);
        }

        [Fact]
        public async Task ProbeAllServersAsync_Probes_All_Enabled_Servers()
        {
            var db = CreateInMemoryDbContext();
            db.Servers.Add(new McpServer { Id = "s1", DisplayName = "S1", Url = "http://localhost:1111/sse", Enabled = true });
            db.Servers.Add(new McpServer { Id = "s2", DisplayName = "S2", Url = "http://localhost:2222/sse", Enabled = true });
            db.Servers.Add(new McpServer { Id = "s3", DisplayName = "S3", Url = "http://localhost:3333/sse", Enabled = false });
            await db.SaveChangesAsync();

            var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler);

            var services = new ServiceCollection();
            services.AddSingleton(db);
            var serviceProvider = services.BuildServiceProvider();

            var httpClientFactory = new MockHttpClientFactory(client);
            var sessionManager = new SessionManager(serviceProvider, httpClientFactory, NullLogger<SessionManager>.Instance);
            var logger = NullLogger<BackendHealthCheckService>.Instance;

            var healthService = new BackendHealthCheckService(serviceProvider, httpClientFactory, sessionManager, logger);

            await healthService.ProbeAllServersAsync();

            Assert.Equal("Connected", sessionManager.BackendStatuses["s1"].Status);
            Assert.Equal("Connected", sessionManager.BackendStatuses["s2"].Status);
            Assert.False(sessionManager.BackendStatuses.ContainsKey("s3"));
        }

        private class MockHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public MockHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        private class HttpRequestMessageException : HttpRequestException
        {
            public HttpRequestMessageException(string message) : base(message) { }
        }
    }
}
