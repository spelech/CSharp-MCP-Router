using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq.Protected;
using Moq;
using Microsoft.Extensions.Logging;
using McpRouter.Infrastructure.Transports;
using McpRouter.Components.Servers;
using McpRouter.Core.Protocol;
using McpRouter.Tests.Attributes;

namespace McpRouter.Tests
{
    public class IdentityHeaderTests
    {
        [Fact]
        [Requirement("REQ-AUTH-101", "AUTH", RequirementType.Positive, "HTTP transport injects X-Forwarded-User header based on connected user identity.")]
        public async Task HttpTransport_InjectsXForwardedUserHeader()
        {
            var server = new McpServer { Id = "test-server", Type = "http", Url = "http://localhost/mcp" };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            
            HttpRequestMessage? capturedRequest = null;
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"result\":{}}")
                })
                .Callback<HttpRequestMessage, System.Threading.CancellationToken>((req, ct) => capturedRequest = req);

            var httpClient = new HttpClient(handlerMock.Object);
            var logger = Mock.Of<ILogger>();
            
            var transport = new HttpTransport(server, httpClient, logger, null, null, null, "testuser@domain.com");
            
            await transport.SendRequestAsync("test/method", "{}");
            
            Assert.NotNull(capturedRequest);
            Assert.True(capturedRequest.Headers.Contains("X-Forwarded-User"));
            var values = capturedRequest.Headers.GetValues("X-Forwarded-User");
            Assert.Contains("testuser@domain.com", values);
        }
    }
}
