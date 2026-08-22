using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using McpRouter.Components.AppKeys;
using McpRouter.Infrastructure.Secrets;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using McpRouter.Tests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using McpRouter.Infrastructure.Identity;
using System.Collections.Generic;

namespace McpRouter.Tests
{
    public class UserCredentialsControllerTests
    {
        [Fact]
        [Requirement("AUTH-002", "AUTH", RequirementType.Positive, "Verify UserCredentialsController returns configured server IDs.")]
        public async Task GetUserCredentials_ReturnsServerIds()
        {
            // Arrange
            var mockStore = new Mock<IUserSecretStore>();
            mockStore.Setup(s => s.GetServerIdsAsync("testuser")).ReturnsAsync(new[] { "server1", "server2" });

            var mockIdp = new Mock<IIdentityProvider>();
            var identityContext = new UserIdentityContext("testuser", "TestAuth", new List<string>());
            mockIdp.Setup(i => i.ResolveIdentityAsync(It.IsAny<HttpContext>())).ReturnsAsync(identityContext);
            var compositeIdp = new CompositeIdentityProvider(new[] { mockIdp.Object });

            var services = new ServiceCollection();
            services.AddSingleton(compositeIdp);
            var sp = services.BuildServiceProvider();

            var controller = new UserCredentialsController(mockStore.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth")),
                    RequestServices = sp
                }
            };

            // Act
            var result = await controller.GetConfiguredCredentials();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var servers = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<string>>(okResult.Value);
            Assert.Contains("server1", servers);
        }
    }
}
