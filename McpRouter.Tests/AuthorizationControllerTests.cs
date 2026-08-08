using System;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenIddict.Abstractions;
using Xunit;

namespace McpRouter.Tests
{
    public class AuthorizationControllerTests
    {
        [Fact]
        public async Task Exchange_ThrowsInvalidOperationException_WhenRequestNull()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var controller = new AuthorizationController(mockAppManager.Object)
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

            var controller = new AuthorizationController(mockAppManager.Object);
            var json = JsonDocument.Parse("{\"client_name\":\"IntegrationTestApp\"}").RootElement;

            var result = await controller.RegisterClient(json) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            mockAppManager.Verify(m => m.CreateAsync(It.Is<OpenIddictApplicationDescriptor>(d => d.DisplayName == "IntegrationTestApp"), default), Times.Once);
        }
    }
}
