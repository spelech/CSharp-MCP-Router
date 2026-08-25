using System.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpRouter.Tests
{
    public class HttpTransportTests
    {
        /// <summary>
        /// Verifies that HTTP transport resolves plaintext API key when secret provider is None.
        /// </summary>
        [Fact]
        [Requirement("TRANS-02", "HTTP stateless transport resolves static API keys when secret provider is None", Type = RequirementType.Positive, Category = "TRANS")]
        public async Task ResolveTokenAsync_ReturnsApiKey_WhenProviderNone()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/mcp",
                SecretProvider = "None",
                ApiKey = "plaintext-api-key"
            };

            var transport = new HttpTransport(server, new HttpClient(), NullLogger.Instance);
            var token = await transport.ResolveTokenAsync();
            Assert.Equal("plaintext-api-key", token);
        }

        /// <summary>
        /// Ensures HTTP transport fails closed with SecurityException when secret retriever fails.
        /// </summary>
        [Fact]
        [Requirement("GUARD-02", "HTTP stateless transport fails closed with SecurityException when secret resolution fails", Type = RequirementType.Negative, Category = "GUARD")]
        public async Task ResolveTokenAsync_ThrowsSecurityException_WhenSecretProviderFails()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/mcp",
                SecretProvider = "Vault",
                SecretMount = "secret",
                SecretPath = "my-app",
                SecretField = "key"
            };

            var mockRetriever = new EnvironmentSecretRetriever(); // returns null
            var transport = new HttpTransport(server, new HttpClient(), NullLogger.Instance, mockRetriever);

            await Assert.ThrowsAsync<SecurityException>(() => transport.ResolveTokenAsync());
        }

        /// <summary>
        /// Ensures HTTP transport fails closed when no secret retriever is registered.
        /// </summary>
        [Fact]
        [Requirement("GUARD-02", "HTTP stateless transport fails closed with InvalidOperationException when no secret retriever is configured", Type = RequirementType.Negative, Category = "GUARD")]
        public async Task ResolveTokenAsync_ThrowsInvalidOperationException_WhenNoRetrieverRegistered()
        {
            var server = new McpServer
            {
                Id = "test-s1",
                Url = "http://localhost:8080/mcp",
                SecretProvider = "Vault"
            };

            var transport = new HttpTransport(server, new HttpClient(), NullLogger.Instance, secretRetriever: null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => transport.ResolveTokenAsync());
        }
    }
}
