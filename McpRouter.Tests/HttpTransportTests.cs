using System;
using System.Collections.Generic;
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
    public class HttpTransportTests
    {
        [Fact]
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

        [Fact]
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

        [Fact]
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
