using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Core.Secrets;
using McpRouter.Core.Transports;
using McpRouter.Models;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class TransportsAuthShapeTests
    {
        private async Task InvokeApplyAuthAndCustomHeadersAsync(object transport, HttpRequestMessage request)
        {
            var method = transport.GetType().GetMethod("ApplyAuthAndCustomHeadersAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method.Invoke(transport, new object[] { request })!;
            await task;
        }

        [Theory]
        [InlineData("bearer", "secret123", "Authorization", "Bearer secret123")]
        [InlineData("basic", "secret123", "Authorization", "Basic secret123")]
        [InlineData("raw", "secret123", "Authorization", "secret123")]
        [InlineData("x-api-key", "secret123", "X-API-Key", "secret123")]
        public async Task SseTransport_ApplyAuthAndCustomHeaders_Formats_Standard_Headers(string authShape, string token, string expectedHeaderKey, string expectedHeaderValue)
        {
            var server = new McpServer
            {
                Id = "srv1",
                Url = "http://localhost:5000/sse",
                SecretProvider = "None",
                ApiKey = token,
                AuthShape = authShape
            };
            var httpClient = new HttpClient();
            var logger = NullLogger<SseTransport>.Instance;
            var transport = new SseTransport(server, httpClient, logger, null!);

            var request = new HttpRequestMessage(HttpMethod.Get, server.Url);
            await InvokeApplyAuthAndCustomHeadersAsync(transport, request);

            Assert.True(request.Headers.Contains(expectedHeaderKey));
            var val = string.Join(" ", request.Headers.GetValues(expectedHeaderKey));
            Assert.Equal(expectedHeaderValue, val);
        }

        [Fact]
        public async Task SseTransport_ApplyAuthAndCustomHeaders_Formats_CustomHeader()
        {
            var server = new McpServer
            {
                Id = "slack",
                Url = "http://localhost:5000/sse",
                SecretProvider = "None",
                ApiKey = "xoxb-test-token",
                AuthShape = "custom-header",
                CustomHeaderName = "Slack-Bot-Token"
            };
            var transport = new SseTransport(server, new HttpClient(), NullLogger<SseTransport>.Instance, null!);

            var request = new HttpRequestMessage(HttpMethod.Get, server.Url);
            await InvokeApplyAuthAndCustomHeadersAsync(transport, request);

            Assert.True(request.Headers.Contains("Slack-Bot-Token"));
            Assert.Equal("xoxb-test-token", string.Join("", request.Headers.GetValues("Slack-Bot-Token")));
        }

        [Fact]
        public async Task SseTransport_ApplyAuthAndCustomHeaders_Appends_QueryParameter()
        {
            var server = new McpServer
            {
                Id = "query-srv",
                Url = "http://localhost:5000/sse?existing=1",
                SecretProvider = "None",
                ApiKey = "query-token-123",
                AuthShape = "query",
                CustomHeaderName = "api_key"
            };
            var transport = new SseTransport(server, new HttpClient(), NullLogger<SseTransport>.Instance, null!);

            var request = new HttpRequestMessage(HttpMethod.Get, server.Url);
            await InvokeApplyAuthAndCustomHeadersAsync(transport, request);

            Assert.NotNull(request.RequestUri);
            Assert.Contains("api_key=query-token-123", request.RequestUri.Query);
            Assert.Contains("existing=1", request.RequestUri.Query);
        }

        [Fact]
        public async Task HttpTransport_ApplyAuthAndCustomHeaders_Formats_CustomHeader()
        {
            var server = new McpServer
            {
                Id = "custom-http",
                Url = "http://localhost:5000/mcp",
                SecretProvider = "None",
                ApiKey = "http-secret-key",
                AuthShape = "custom-header",
                CustomHeaderName = "X-Service-Auth"
            };
            var transport = new HttpTransport(server, new HttpClient(), NullLogger<HttpTransport>.Instance);

            var request = new HttpRequestMessage(HttpMethod.Post, server.Url);
            await InvokeApplyAuthAndCustomHeadersAsync(transport, request);

            Assert.True(request.Headers.Contains("X-Service-Auth"));
            Assert.Equal("http-secret-key", string.Join("", request.Headers.GetValues("X-Service-Auth")));
        }

        [Fact]
        public async Task SseTransport_ApplyAuthAndCustomHeaders_Parses_HeadersJson()
        {
            var server = new McpServer
            {
                Id = "headers-json",
                Url = "http://localhost:5000/sse",
                SecretProvider = "None",
                HeadersJson = "{\"X-Custom-Env\": \"production\", \"X-Agent\": \"antigravity\"}"
            };
            var transport = new SseTransport(server, new HttpClient(), NullLogger<SseTransport>.Instance, null!);

            var request = new HttpRequestMessage(HttpMethod.Get, server.Url);
            await InvokeApplyAuthAndCustomHeadersAsync(transport, request);

            Assert.True(request.Headers.Contains("X-Custom-Env"));
            Assert.Equal("production", string.Join("", request.Headers.GetValues("X-Custom-Env")));
            Assert.True(request.Headers.Contains("X-Agent"));
            Assert.Equal("antigravity", string.Join("", request.Headers.GetValues("X-Agent")));
        }

        [Fact]
        public async Task SseTransport_ResolveTokenAsync_Uses_Custom_Path_Field_And_Mount()
        {
            var server = new McpServer
            {
                Id = "vault-srv",
                Url = "http://localhost:5000/sse",
                SecretProvider = "Vault",
                SecretMount = "kv-custom",
                SecretPath = "apps/my-app",
                SecretField = "token-key"
            };

            var mockRetriever = new Mock<ISecretRetriever>();
            mockRetriever.Setup(r => r.GetSecretAsync("kv-custom:apps/my-app", "token-key"))
                         .ReturnsAsync("resolved-vault-secret-999");

            var transport = new SseTransport(server, new HttpClient(), NullLogger<SseTransport>.Instance, null!, mockRetriever.Object);

            var token = await transport.ResolveTokenAsync();

            Assert.Equal("resolved-vault-secret-999", token);
            mockRetriever.Verify(r => r.GetSecretAsync("kv-custom:apps/my-app", "token-key"), Times.Once);
        }

        [Fact]
        public async Task HttpTransport_ResolveTokenAsync_Defaults_To_Url_And_ApiKey_When_Not_Configured()
        {
            var server = new McpServer
            {
                Id = "vault-default-http",
                Url = "http://localhost:5000/mcp",
                SecretProvider = "Vault",
                SecretItemKey = "MyKey"
            };

            var mockRetriever = new Mock<ISecretRetriever>();
            mockRetriever.Setup(r => r.GetSecretAsync("http://localhost:5000/mcp", "MyKey"))
                         .ReturnsAsync("resolved-default-secret-123");

            var transport = new HttpTransport(server, new HttpClient(), NullLogger<HttpTransport>.Instance, mockRetriever.Object);

            var token = await transport.ResolveTokenAsync();

            Assert.Equal("resolved-default-secret-123", token);
            mockRetriever.Verify(r => r.GetSecretAsync("http://localhost:5000/mcp", "MyKey"), Times.Once);
        }
    }
}
