using System;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using McpRouter.Core.Secrets;
using McpRouter.Core.Transports;
using McpRouter.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpRouter.Tests
{
    public class SseTransportTests
    {
        [Fact]
        public async Task ResolveTokenAsync_ReturnsApiKey_WhenProviderNone()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/sse",
                SecretProvider = "None",
                ApiKey = "sse-plaintext-key"
            };

            var stateManager = new JsonRpcStateManager();
            var transport = new SseTransport(server, new HttpClient(), NullLogger.Instance, stateManager);
            var token = await transport.ResolveTokenAsync();
            Assert.Equal("sse-plaintext-key", token);
        }

        [Fact]
        public async Task ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/sse",
                SecretProvider = "Vault"
            };

            var mockRetriever = new EnvironmentSecretRetriever(); // returns null
            var stateManager = new JsonRpcStateManager();
            var transport = new SseTransport(server, new HttpClient(), NullLogger.Instance, stateManager, mockRetriever);

            await Assert.ThrowsAsync<SecurityException>(() => transport.ResolveTokenAsync());
        }

        [Fact]
        public async Task ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/sse",
                SecretProvider = "Vault"
            };

            var stateManager = new JsonRpcStateManager();
            var transport = new SseTransport(server, new HttpClient(), NullLogger.Instance, stateManager, secretRetriever: null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => transport.ResolveTokenAsync());
        }
    }
}
