using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ModelContextGateway.Core.Routing;
using ModelContextGateway.Tests.Attributes;

namespace ModelContextGateway.Tests
{
    public class AuthorizationControllerTests
    {
        [Fact]
        [Requirement("AUTH-106", "SEC", RequirementType.Negative, "Exchange throws InvalidOperationException when request is null.")]
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
        [Requirement("AUTH-107", "SEC", RequirementType.Positive, "RegisterClient successfully handles DCR requests when open DCR is enabled.")]
        public async Task RegisterClient_CreatesApplicationAndReturnsOk()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            mockAppManager.Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), default))
                          .ReturnsAsync(new object());
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();

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

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object, mockAppManager.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"IntegrationTestApp\",\"redirect_uris\":[\"https://oauth.google.com/callback\"]}").RootElement;
            var result = await controller.RegisterClient(json) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            mockAppManager.Verify(m => m.CreateAsync(It.Is<OpenIddictApplicationDescriptor>(d => d.DisplayName == "IntegrationTestApp" && d.RedirectUris.Count == 1), default), Times.Once);
            mockOAuthRepo.Verify(m => m.SaveOAuthClientAsync(It.Is<OAuthClient>(c => c.ClientName == "IntegrationTestApp" && !string.IsNullOrEmpty(c.ClientSecretHash))), Times.Once);
        }

        [Fact]
        [Requirement("AUTH-108", "SEC", RequirementType.Negative, "Authorize throws InvalidOperationException when OIDC request is null.")]
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
        [Requirement("AUTH-109", "SEC", RequirementType.Positive, "RegisterClient uses IOAuthClientRepository when IOpenIddictApplicationManager is null.")]
        public async Task RegisterClient_UsesOAuthClientRepository_WhenApplicationManagerNull()
        {
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();
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

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"GeminiClient\",\"redirect_uris\":[\"https://client.example.com/cb\"]}").RootElement;
            var result = await controller.RegisterClient(json) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            mockOAuthRepo.Verify(m => m.SaveOAuthClientAsync(It.Is<OAuthClient>(c =>
                c.ClientName == "GeminiClient" &&
                !string.IsNullOrEmpty(c.ClientId) &&
                !string.IsNullOrEmpty(c.ClientSecretHash) &&
                c.RedirectUrisJson.Contains("https://client.example.com/cb")
            )), Times.Once);
        }

        [Fact]
        [Requirement("AUTH-110", "SEC", RequirementType.Positive, "OpenIddict ApplyConfigurationResponseContext populates registration_endpoint discovery metadata.")]
        public void ApplyConfigurationResponseContext_SetsRegistrationEndpoint()
        {
            var options = new OpenIddict.Server.OpenIddictServerOptions();
            var context = new OpenIddict.Server.OpenIddictServerEvents.ApplyConfigurationResponseContext(
                new OpenIddict.Server.OpenIddictServerTransaction
                {
                    Options = options
                })
            {
                Response = new OpenIddict.Abstractions.OpenIddictResponse()
            };

            context.Response.SetParameter("issuer", "https://mcp.wileyriley.com/");
            var issuer = ((string?)context.Response.GetParameter("issuer"))?.TrimEnd('/') ?? "";
            if (!string.IsNullOrEmpty(issuer))
            {
                context.Response.SetParameter("registration_endpoint", $"{issuer}/api/register");
            }

            Assert.Equal("https://mcp.wileyriley.com/api/register", (string?)context.Response.GetParameter("registration_endpoint"));
        }

        [Fact]
        [Requirement("AUTH-111", "SEC", RequirementType.Positive, "Exchange validates client_credentials grant type against SHA-256 ClientSecretHash.")]
        public async Task Exchange_ClientCredentials_ValidSecret_ReturnsSignInResult()
        {
            var rawSecret = "super-secret-token-123456";
            var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret))).ToLowerInvariant();

            var mockOAuthRepo = new Mock<IOAuthClientRepository>();
            mockOAuthRepo.Setup(r => r.GetOAuthClientByIdAsync("test-client"))
                .ReturnsAsync(new OAuthClient
                {
                    ClientId = "test-client",
                    ClientSecretHash = secretHash,
                    ClientName = "Test Client",
                    ClientType = "confidential"
                });

            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();

            var openIdRequest = new OpenIddictRequest
            {
                GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
                ClientId = "test-client",
                ClientSecret = rawSecret
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddict.Server.OpenIddictServerTransaction
                {
                    Request = openIdRequest
                }
            });

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var result = await controller.Exchange();
            var signInResult = Assert.IsType<SignInResult>(result);
            Assert.Equal(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, signInResult.AuthenticationScheme);
            Assert.Equal("test-client", signInResult.Principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value);
            Assert.Equal("Test Client", signInResult.Principal.FindFirst(OpenIddictConstants.Claims.Name)?.Value);
        }

        [Fact]
        [Requirement("AUTH-111", "SEC", RequirementType.Negative, "Exchange with invalid client_secret returns Forbid.")]
        public async Task Exchange_ClientCredentials_InvalidSecret_ReturnsForbid()
        {
            var rawSecret = "super-secret-token-123456";
            var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret))).ToLowerInvariant();

            var mockOAuthRepo = new Mock<IOAuthClientRepository>();
            mockOAuthRepo.Setup(r => r.GetOAuthClientByIdAsync("test-client"))
                .ReturnsAsync(new OAuthClient
                {
                    ClientId = "test-client",
                    ClientSecretHash = secretHash,
                    ClientName = "Test Client",
                    ClientType = "confidential"
                });

            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();

            var openIdRequest = new OpenIddictRequest
            {
                GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
                ClientId = "test-client",
                ClientSecret = "wrong-secret"
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddict.Server.OpenIddictServerTransaction
                {
                    Request = openIdRequest
                }
            });

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var result = await controller.Exchange();
            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.Contains(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, forbidResult.AuthenticationSchemes);
        }

        [Fact]
        [Requirement("AUTH-111", "SEC", RequirementType.Negative, "Exchange with expired OAuthClient credentials returns Forbid.")]
        public async Task Exchange_ClientCredentials_ExpiredClient_ReturnsForbid()
        {
            var rawSecret = "super-secret-token-123456";
            var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret))).ToLowerInvariant();

            var mockOAuthRepo = new Mock<IOAuthClientRepository>();
            mockOAuthRepo.Setup(r => r.GetOAuthClientByIdAsync("test-client"))
                .ReturnsAsync(new OAuthClient
                {
                    ClientId = "test-client",
                    ClientSecretHash = secretHash,
                    ClientName = "Test Client",
                    ClientType = "confidential",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
                });

            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();

            var openIdRequest = new OpenIddictRequest
            {
                GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
                ClientId = "test-client",
                ClientSecret = rawSecret
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddict.Server.OpenIddictServerTransaction
                {
                    Request = openIdRequest
                }
            });

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var result = await controller.Exchange();
            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.Contains(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, forbidResult.AuthenticationSchemes);
        }

        [Fact]
        [Requirement("AUTH-112", "SEC", RequirementType.Positive, "Authorize resolves client application from IOAuthClientRepository and redirects to consent.")]
        public async Task Authorize_ResolvesClientAndRedirectsToConsent()
        {
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();
            mockOAuthRepo.Setup(r => r.GetOAuthClientByIdAsync("client-xyz"))
                .ReturnsAsync(new OAuthClient
                {
                    ClientId = "client-xyz",
                    ClientName = "Awesome MCP App"
                });

            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();

            var openIdRequest = new OpenIddictRequest
            {
                ClientId = "client-xyz",
                ResponseType = OpenIddictConstants.ResponseTypes.Code
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "steve"),
                new Claim(OpenIddictConstants.Claims.Subject, "steve")
            }, "TestAuth"));

            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService.Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(user, "TestAuth")));

            var services = new ServiceCollection();
            services.AddSingleton(mockAuthService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { User = user, RequestServices = serviceProvider };
            httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddict.Server.OpenIddictServerTransaction
                {
                    Request = openIdRequest
                }
            });

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var result = await controller.Authorize();
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.StartsWith("/consent", redirectResult.Url);
            Assert.Contains("client_name=Awesome%20MCP%20App", redirectResult.Url);
        }
    }
}
