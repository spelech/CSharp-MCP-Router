using System;
using System.Net.Http;
using McpRouter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class SessionManagerTests
    {
        [Fact]
        public void PerformanceMetrics_And_TotalRequests_IncrementCorrectly()
        {
            var services = new ServiceCollection().BuildServiceProvider();
            var mockFactory = new Mock<IHttpClientFactory>();
            var logger = NullLogger<SessionManager>.Instance;

            var manager = new SessionManager(services, mockFactory.Object, logger);

            Assert.Equal(0, manager.TotalRequests);
            Assert.Equal(0, manager.TotalInputTokens);
            Assert.Equal(0, manager.TotalOutputTokens);
            Assert.Equal(0, manager.TotalDurationMs);

            manager.IncrementTotalRequests();
            manager.AddPerformanceMetrics(100, 200, 50);

            Assert.Equal(1, manager.TotalRequests);
            Assert.Equal(100, manager.TotalInputTokens);
            Assert.Equal(200, manager.TotalOutputTokens);
            Assert.Equal(50, manager.TotalDurationMs);
        }

        [Fact]
        public void UpdateBackendStatus_TracksBackendHealth()
        {
            var services = new ServiceCollection().BuildServiceProvider();
            var mockFactory = new Mock<IHttpClientFactory>();
            var logger = NullLogger<SessionManager>.Instance;

            var manager = new SessionManager(services, mockFactory.Object, logger);

            manager.UpdateBackendStatus("docker", "Connected", 1, "");
            Assert.True(manager.BackendStatuses.TryGetValue("docker", out var status));
            Assert.Equal("Connected", status.Status);
            Assert.Equal(1, status.Attempts);
            Assert.Empty(status.Error);

            manager.UpdateBackendStatus("docker", "Failed", 2, "Timeout");
            Assert.Equal("Failed", manager.BackendStatuses["docker"].Status);
            Assert.Equal("Timeout", manager.BackendStatuses["docker"].Error);
        }
    }
}
