using System;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Secrets;
using McpRouter.Infrastructure.Transports;
using McpRouter.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpRouter.Tests
{
    public class SseTransportTests
    {
        /// <summary>
        /// Verifies that SSE transport resolves plaintext API key when secret provider is None.
        /// </summary>
        [Fact]
        [Requirement("TRANS-01", "SSE transport resolves static plaintext API keys when provider is None", Type = RequirementType.Positive, Category = "TRANS")]
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

        /// <summary>
        /// Ensures SSE transport fails closed with SecurityException when secret retriever fails.
        /// </summary>
        [Fact]
        [Requirement("GUARD-02", "SSE transport fails closed with SecurityException when secret provider resolution fails", Type = RequirementType.Negative, Category = "GUARD")]
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

        /// <summary>
        /// Ensures SSE transport fails closed when no secret retriever is registered.
        /// </summary>
        [Fact]
        [Requirement("GUARD-02", "SSE transport fails closed with InvalidOperationException when no secret retriever is configured", Type = RequirementType.Negative, Category = "GUARD")]
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
