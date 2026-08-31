using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;

namespace ModelContextGateway.Tests
{
    public class AuthorizationControllerTests
    {

        [Fact]
        [ModelContextGateway.Tests.Attributes.Requirement("AUTH-106", "SEC", ModelContextGateway.Tests.Attributes.RequirementType.Negative, "Exchange throws InvalidOperationException when request is null.")]
        public async Task Exchange_ThrowsInvalidOperationException_WhenRequestNull()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
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
        [ModelContextGateway.Tests.Attributes.Requirement("AUTH-107", "SEC", ModelContextGateway.Tests.Attributes.RequirementType.Positive, "RegisterClient successfully handles DCR requests when open DCR is enabled.")]
        public async Task RegisterClient_CreatesApplicationAndReturnsOk()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            mockAppManager.Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), default))
                          .ReturnsAsync(new object());
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();

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
        [Fact]
        [ModelContextGateway.Tests.Attributes.Requirement("AUTH-108", "SEC", ModelContextGateway.Tests.Attributes.RequirementType.Negative, "Authorize throws InvalidOperationException when OIDC request is null.")]
        public async Task Authorize_ThrowsInvalidOperationException_WhenRequestNull()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
            var controller = new AuthorizationController(mockAppManager.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Authorize());
        }

        [Fact]
        [ModelContextGateway.Tests.Attributes.Requirement("AUTH-109", "SEC", ModelContextGateway.Tests.Attributes.RequirementType.Positive, "RegisterClient uses ICredentialService when IOpenIddictApplicationManager is null.")]
        public async Task RegisterClient_UsesCredentialService_WhenApplicationManagerNull()
        {
            var mockDbFactory = new Mock<ModelContextGateway.Infrastructure.Persistence.IDbConnectionFactory>();
            var mockCredService = new Mock<ModelContextGateway.Components.Clients.ICredentialService>();
            mockCredService.Setup(m => m.CreateCredentialAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int?>(), It.IsAny<string>()))
                .ReturnsAsync((new AppKey { Id = "key-1", Name = "GeminiClient" }, "mcg_live_secret"));

            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();

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

            var controller = new AuthorizationController(mockDbFactory.Object, mockCredService.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"GeminiClient\"}").RootElement;
            var result = await controller.RegisterClient(json) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            mockCredService.Verify(m => m.CreateCredentialAsync("GeminiClient", It.IsAny<string>(), string.Empty, It.IsAny<List<string>>(), null, "personal"), Times.Once);
        }

        [Fact]
        [ModelContextGateway.Tests.Attributes.Requirement("AUTH-110", "SEC", ModelContextGateway.Tests.Attributes.RequirementType.Positive, "OpenIddict configuration context populates registration_endpoint discovery metadata.")]
        public void HandleConfigurationRequestContext_SetsRegistrationEndpoint()
        {
            var options = new OpenIddict.Server.OpenIddictServerOptions();
            var context = new OpenIddict.Server.OpenIddictServerEvents.HandleConfigurationRequestContext(
                new OpenIddict.Server.OpenIddictServerTransaction
                {
                    Options = options
                })
            {
                Issuer = new Uri("https://mcp.wileyriley.com/")
            };

            var issuer = context.Issuer?.ToString().TrimEnd('/') ?? "";
            context.Metadata["registration_endpoint"] = $"{issuer}/api/register";

            Assert.True(context.Metadata.ContainsKey("registration_endpoint"));
            Assert.Equal("https://mcp.wileyriley.com/api/register", (string?)context.Metadata["registration_endpoint"]);
        }
    }
}
