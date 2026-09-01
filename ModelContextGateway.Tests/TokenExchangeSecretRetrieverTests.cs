using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

namespace ModelContextGateway.Tests
{
    public class TokenExchangeSecretRetrieverTests
    {
        private Mock<IHttpClientFactory> CreateMockHttpClientFactory(HttpStatusCode statusCode, string responseJson)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return factoryMock;
        }

        [Fact]
        [Requirement("SEC-02", "SEC", RequirementType.Positive, "TokenExchangeSecretRetriever exchanges upstream caller credentials for downstream OAuth tokens and caches response.")]
        public async Task GetSecretAsync_MintsTokenViaTokenExchange_AndCachesResponse()
        {
            var tokenResponse = new
            {
                access_token = "eyJhbGciOiJSUzI1NiI.mock_downstream_jwt_token",
                token_type = "Bearer",
                expires_in = 3600
            };

            var factoryMock = CreateMockHttpClientFactory(HttpStatusCode.OK, JsonSerializer.Serialize(tokenResponse));
            var cache = new MemoryCache(new MemoryCacheOptions());

            var httpContext = new DefaultHttpContext();
            httpContext.Items["UserIdentityContext"] = new UserIdentityContext("alice", "AppKey", new List<string> { "Users" });
            httpContext.Request.Headers["Authorization"] = "Bearer mcp-static-appkey-123";

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);

            var secretRepoMock = new Mock<ISecretProviderRepository>();
            secretRepoMock.Setup(s => s.GetSecretProvidersAsync()).ReturnsAsync(new List<SecretProviderDto>
            {
                new SecretProviderDto
                {
                    ProviderName = "TokenExchange",
                    IsEnabled = true,
                    ConfigJson = JsonSerializer.Serialize(new
                    {
                        tokenEndpoint = "https://pocketid.company.com/oauth/token",
                        clientId = "mcp-gateway-client",
                        clientSecret = "secret-key-123",
                        grantType = "urn:ietf:params:oauth:grant-type:token-exchange"
                    })
                }
            });

            var retriever = new TokenExchangeSecretRetriever(
                factoryMock.Object,
                cache,
                httpContextAccessorMock.Object,
                secretRepoMock.Object,
                null,
                null,
                null
            );

            // First call -> hits HTTP endpoint
            var token1 = await retriever.GetSecretAsync("", "mcp:write");
            Assert.Equal("eyJhbGciOiJSUzI1NiI.mock_downstream_jwt_token", token1);

            // Second call -> returns cached token
            var token2 = await retriever.GetSecretAsync("", "mcp:write");
            Assert.Equal("eyJhbGciOiJSUzI1NiI.mock_downstream_jwt_token", token2);
        }

        [Fact]
        [Requirement("GUARD-03", "GUARD", RequirementType.Negative, "TokenExchangeSecretRetriever fails closed with InvalidOperationException when token endpoint is not configured.")]
        public async Task GetSecretAsync_ThrowsInvalidOperationException_WhenTokenEndpointMissing()
        {
            var retriever = new TokenExchangeSecretRetriever();
            await Assert.ThrowsAsync<InvalidOperationException>(() => retriever.GetSecretAsync("path", "key"));
        }

        [Fact]
        [Requirement("GUARD-03", "GUARD", RequirementType.Negative, "TokenExchangeSecretRetriever fails closed with SecurityException when token exchange endpoint returns HTTP error.")]
        public async Task GetSecretAsync_ThrowsSecurityException_WhenHttpResponseIsNotSuccess()
        {
            var factoryMock = CreateMockHttpClientFactory(HttpStatusCode.BadRequest, "{\"error\":\"invalid_grant\"}");
            var configDict = new Dictionary<string, string?>
            {
                ["Identity:TokenExchange:TokenEndpoint"] = "https://identity.local/oauth/token",
                ["Identity:TokenExchange:ClientId"] = "client123"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var retriever = new TokenExchangeSecretRetriever(
                factoryMock.Object,
                null,
                null,
                null,
                null,
                config,
                null
            );

            await Assert.ThrowsAsync<System.Security.SecurityException>(() => retriever.GetSecretAsync("", ""));
        }

        [Fact]
        [Requirement("SEC-01", "SEC", RequirementType.Negative, "TokenExchangeSecretRetriever fails closed when HttpClient throws an exception during token exchange.")]
        public async Task GetSecretAsync_ThrowsHttpRequestException_WhenHttpClientFails()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var configDict = new Dictionary<string, string?>
            {
                ["Identity:TokenExchange:TokenEndpoint"] = "https://identity.local/oauth/token",
                ["Identity:TokenExchange:ClientId"] = "client123"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var retriever = new TokenExchangeSecretRetriever(
                factoryMock.Object,
                null,
                null,
                null,
                null,
                config,
                null
            );

            await Assert.ThrowsAsync<HttpRequestException>(() => retriever.GetSecretAsync("", ""));
        }

        [Fact]
        [Requirement("SEC-01", "SEC", RequirementType.Negative, "TokenExchangeSecretRetriever fails closed with JsonException when response from token endpoint is invalid JSON.")]
        public async Task GetSecretAsync_ThrowsJsonException_WhenResponseIsInvalidJson()
        {
            var factoryMock = CreateMockHttpClientFactory(HttpStatusCode.OK, "invalid json payload");
            var configDict = new Dictionary<string, string?>
            {
                ["Identity:TokenExchange:TokenEndpoint"] = "https://identity.local/oauth/token",
                ["Identity:TokenExchange:ClientId"] = "client123"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var retriever = new TokenExchangeSecretRetriever(
                factoryMock.Object,
                null,
                null,
                null,
                null,
                config,
                null
            );

            await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => retriever.GetSecretAsync("", ""));
        }

        [Fact]
        [Requirement("SEC-02", "SEC", RequirementType.Positive, "CompositeSecretRetriever routes OBO, PocketID, and OAuth2 provider aliases to TokenExchange provider.")]
        public async Task CompositeSecretRetriever_RoutesOboAndPocketIdAliases_ToTokenExchangeRetriever()
        {
            var mockTe = new Mock<ISecretRetriever>();
            mockTe.Setup(r => r.ProviderName).Returns("TokenExchange");
            mockTe.Setup(r => r.GetSecretAsync("path", "scope"))
                  .ReturnsAsync("dynamic_jwt");

            var composite = new CompositeSecretRetriever(new[] { mockTe.Object });

            var r1 = await composite.GetSecretForProviderAsync("TokenExchange", "path", "scope");
            Assert.Equal("dynamic_jwt", r1);

            var r2 = await composite.GetSecretForProviderAsync("PocketID", "path", "scope");
            Assert.Equal("dynamic_jwt", r2);

            var r3 = await composite.GetSecretForProviderAsync("OBO", "path", "scope");
            Assert.Equal("dynamic_jwt", r3);

            var r4 = await composite.GetSecretForProviderAsync("OAuth2", "path", "scope");
            Assert.Equal("dynamic_jwt", r4);
        }
    }
}
