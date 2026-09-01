using System.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace ModelContextGateway.Tests
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



        /// <summary>
        /// Ensures SSE transport handles exceptions when waiting for SSE endpoint URL gracefully by logging them.
        /// </summary>
        [Fact]
        [Requirement("TRANS-01", "TRANS", RequirementType.Positive, "SSE transport logs exceptions gracefully when waiting for endpoint URL without throwing unhandled exceptions.")]
        public async Task SendRequestAsync_HandlesEndpointWaitTimeoutGracefully()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/sse",
                SecretProvider = "None"
            };

            var stateManager = new JsonRpcStateManager();
            var transport = new SseTransport(server, new HttpClient(), NullLogger.Instance, stateManager);

            // Since _messageUrl is null and we aren't starting the reader,
            // calling SendRequestAsync should attempt to wait for 5s, timeout, log (silently via NullLogger),
            // and return a Not connected JsonRpcResponse without crashing.

            // Note: to make the test faster, we can rely on the existing 5s timeout, but if we want it instant
            // we'd need to inject a smaller timeout. For now, 5s is acceptable or we just verify it completes.
            var response = await transport.SendRequestAsync("testMethod", "{}");

            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Equal(-32001, response.Error.Code);
            Assert.Equal("Not connected", response.Error.Message);
        }
    }
}
