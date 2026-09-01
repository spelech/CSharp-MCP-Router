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
        /// Verifies that SSE transport handles wait cancellations safely during SendRequestAsync without throwing.
        /// </summary>
        [Fact]
        [Requirement("TRANS-01", "SSE transport safely handles WaitAsync cancellations in SendRequestAsync", Type = RequirementType.Positive, Category = "TRANS")]
        public async Task SendRequestAsync_HandlesWaitCancellationSafely()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/sse",
                SecretProvider = "None"
            };

            var stateManager = new JsonRpcStateManager();
            var transport = new SseTransport(server, new HttpClient(), NullLogger.Instance, stateManager);

            // This will try to wait 5 seconds for the endpoint to be set, then timeout and return a Not Connected error.
            var response = await transport.SendRequestAsync("test-method", "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"test-method\"}");

            Assert.NotNull(response.Error);
            Assert.Equal(-32001, response.Error.Code);
            Assert.Equal("Not connected", response.Error.Message);
        }

        /// <summary>
        /// Verifies that SSE transport handles wait cancellations safely during CallMethodAsync without throwing.
        /// </summary>
        [Fact]
        [Requirement("TRANS-01", "SSE transport safely handles WaitAsync cancellations in CallMethodAsync", Type = RequirementType.Positive, Category = "TRANS")]
        public async Task CallMethodAsync_HandlesWaitCancellationSafely()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/sse",
                SecretProvider = "None"
            };

            var stateManager = new JsonRpcStateManager();
            var transport = new SseTransport(server, new HttpClient(), NullLogger.Instance, stateManager);

            // This will try to wait 5 seconds for the endpoint to be set, then timeout and throw InvalidOperationException.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.CallMethodAsync("test-method", new { }));
            Assert.Contains("has not sent its endpoint event yet", ex.Message);
        }
    }
}
