using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Components.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using Xunit;
using McpRouter.Core.Routing;
using McpRouter.Models;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Tests
{
    public class AuthorizationControllerTests
    {
        [Fact]
        public async Task Exchange_ThrowsInvalidOperationException_WhenRequestNull()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var controller = new AuthorizationController(mockAppManager.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Exchange());
        }

        [Fact]
        public async Task RegisterClient_CreatesApplicationAndReturnsOk()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            mockAppManager.Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), default))
                          .ReturnsAsync(new object());
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();

            // Mock embedding service and settings
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mockServiceProvider = new Mock<IServiceProvider>();
            
            var embeddingServiceMock = new Mock<DynamicEmbeddingService>(new HttpClient(), mockLoggerFactory.Object, mockServiceProvider.Object);
            embeddingServiceMock.Setup(m => m.GetSettings()).Returns(new RouterSettings { AllowOpenClientRegistration = true });

            var mockAuthService = new Mock<IAuthorizationService>();

            var services = new ServiceCollection();
            services.AddSingleton(embeddingServiceMock.Object);
            services.AddSingleton(mockAuthService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            var controller = new AuthorizationController(mockAppManager.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"IntegrationTestApp\"}").RootElement;
            var result = await controller.RegisterClient(json) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            mockAppManager.Verify(m => m.CreateAsync(It.Is<OpenIddictApplicationDescriptor>(d => d.DisplayName == "IntegrationTestApp"), default), Times.Once);
        }
    }
}
