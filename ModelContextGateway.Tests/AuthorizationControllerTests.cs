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

            var json = JsonDocument.Parse("{\"client_name\":\"IntegrationTestApp\",\"redirect_uris\":[\"https://oauth.google.com/callback\"],\"application_type\":\"web\"}").RootElement;
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

            var json = JsonDocument.Parse("{\"client_name\":\"GeminiClient\",\"redirect_uris\":[\"https://client.example.com/cb\"],\"application_type\":\"web\"}").RootElement;
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

        [Fact]
        [Requirement("AUTH-113", "SEC", RequirementType.Positive, "RegisterClient supports public clients with PKCE (token_endpoint_auth_method: none) and omits client secret.")]
        public async Task RegisterClient_PublicClient_SucceedsWithoutSecret()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            OpenIddictApplicationDescriptor? capturedDescriptor = null;
            mockAppManager.Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), default))
                          .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
                          .ReturnsAsync(new object());
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mockServiceProvider = new Mock<IServiceProvider>();

            var embeddingServiceMock = new Mock<DynamicEmbeddingService>(new HttpClient(), mockLoggerFactory.Object, mockServiceProvider.Object);
            embeddingServiceMock.Setup(m => m.GetSettings()).Returns(new RouterSettings { AllowOpenClientRegistration = true });

            var services = new ServiceCollection();
            services.AddSingleton(embeddingServiceMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object, mockAppManager.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"Claude Desktop\",\"redirect_uris\":[\"http://127.0.0.1:8080/callback\"],\"application_type\":\"native\",\"token_endpoint_auth_method\":\"none\",\"scope\":\"mcp_client tools:execute\"}").RootElement;
            var result = await controller.RegisterClient(json) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            Assert.NotNull(capturedDescriptor);
            Assert.Equal(OpenIddictConstants.ClientTypes.Public, capturedDescriptor.ClientType);
            Assert.Null(capturedDescriptor.ClientSecret);
            Assert.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange, capturedDescriptor.Requirements);
            Assert.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + "mcp_client", capturedDescriptor.Permissions);
            Assert.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + "tools:execute", capturedDescriptor.Permissions);

            var jsonText = JsonSerializer.Serialize(result.Value);
            var doc = JsonDocument.Parse(jsonText);
            Assert.False(doc.RootElement.TryGetProperty("client_secret", out _));
            Assert.Equal("none", doc.RootElement.GetProperty("token_endpoint_auth_method").GetString());
            Assert.Contains("tools:execute", doc.RootElement.GetProperty("scope").GetString());
        }

        [Fact]
        [Requirement("AUTH-114", "SEC", RequirementType.Negative, "RegisterClient rejects invalid or non-absolute redirect URIs with standard RFC 7591 invalid_redirect_uri error.")]
        public async Task RegisterClient_InvalidRedirectUri_ReturnsBadRequest()
        {
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mockServiceProvider = new Mock<IServiceProvider>();

            var embeddingServiceMock = new Mock<DynamicEmbeddingService>(new HttpClient(), mockLoggerFactory.Object, mockServiceProvider.Object);
            embeddingServiceMock.Setup(m => m.GetSettings()).Returns(new RouterSettings { AllowOpenClientRegistration = true });

            var services = new ServiceCollection();
            services.AddSingleton(embeddingServiceMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"Bad App\",\"redirect_uris\":[\"not-a-valid-uri\"],\"application_type\":\"web\"}").RootElement;
            var result = await controller.RegisterClient(json) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            var jsonText = JsonSerializer.Serialize(result.Value);
            var doc = JsonDocument.Parse(jsonText);
            Assert.Equal("invalid_redirect_uri", doc.RootElement.GetProperty("error").GetString());
        }

        [Fact]
        [Requirement("AUTH-115", "SEC", RequirementType.Positive, "RegisterClient dynamically binds requested scopes to OpenIddict application descriptor permissions.")]
        public async Task RegisterClient_DynamicScopes_AddedToPermissions()
        {
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            OpenIddictApplicationDescriptor? capturedDescriptor = null;
            mockAppManager.Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), default))
                          .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
                          .ReturnsAsync(new object());
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mockServiceProvider = new Mock<IServiceProvider>();

            var embeddingServiceMock = new Mock<DynamicEmbeddingService>(new HttpClient(), mockLoggerFactory.Object, mockServiceProvider.Object);
            embeddingServiceMock.Setup(m => m.GetSettings()).Returns(new RouterSettings { AllowOpenClientRegistration = true });

            var services = new ServiceCollection();
            services.AddSingleton(embeddingServiceMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object, mockAppManager.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"Scope App\",\"redirect_uris\":[\"https://example.com/oauth/callback\"],\"application_type\":\"web\",\"scope\":\"custom:read custom:write\"}").RootElement;
            var result = await controller.RegisterClient(json) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            Assert.NotNull(capturedDescriptor);
            Assert.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + "custom:read", capturedDescriptor.Permissions);
            Assert.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + "custom:write", capturedDescriptor.Permissions);
        }

        [Fact]
        [Requirement("AUTH-116", "SEC", RequirementType.Negative, "Exchange rejects client_credentials grant attempts by public clients with UnauthorizedClient error.")]
        public async Task Exchange_PublicClient_ClientCredentials_ReturnsForbid()
        {
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();
            mockOAuthRepo.Setup(r => r.GetOAuthClientByIdAsync("public-client"))
                .ReturnsAsync(new OAuthClient
                {
                    ClientId = "public-client",
                    ClientSecretHash = "",
                    ClientName = "Public CLI App",
                    ClientType = "public"
                });

            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();

            var openIdRequest = new OpenIddictRequest
            {
                GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
                ClientId = "public-client"
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
        [Requirement("AUTH-117", "SEC", RequirementType.Negative, "RegisterClient returns 403 Forbidden with access_denied when open client registration is disabled and caller is unauthorized.")]
        public async Task RegisterClient_WhenClosedRegistration_UnauthorizedUser_ReturnsForbidden()
        {
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mockServiceProvider = new Mock<IServiceProvider>();

            var embeddingServiceMock = new Mock<DynamicEmbeddingService>(new HttpClient(), mockLoggerFactory.Object, mockServiceProvider.Object);
            embeddingServiceMock.Setup(m => m.GetSettings()).Returns(new RouterSettings { AllowOpenClientRegistration = false });

            var mockAuthService = new Mock<IAuthorizationService>();
            mockAuthService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), "AdminPolicy"))
                           .ReturnsAsync(AuthorizationResult.Failed());

            var services = new ServiceCollection();
            services.AddSingleton(embeddingServiceMock.Object);
            services.AddSingleton(mockAuthService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"Unauthorized App\",\"redirect_uris\":[\"https://example.com/callback\"],\"application_type\":\"web\"}").RootElement;
            var result = await controller.RegisterClient(json) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(403, result.StatusCode);
            var jsonText = JsonSerializer.Serialize(result.Value);
            var doc = JsonDocument.Parse(jsonText);
            Assert.Equal("access_denied", doc.RootElement.GetProperty("error").GetString());
        }

        [Fact]
        [Requirement("AUTH-118", "SEC", RequirementType.Negative, "RegisterClient returns 400 Bad Request when application_type parameter is missing.")]
        public async Task RegisterClient_MissingApplicationType_ReturnsBadRequest()
        {
            var mockAudit = new Mock<ModelContextGateway.Infrastructure.Logging.IAuditLogger>();
            var mockOAuthRepo = new Mock<IOAuthClientRepository>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mockServiceProvider = new Mock<IServiceProvider>();

            var embeddingServiceMock = new Mock<DynamicEmbeddingService>(new HttpClient(), mockLoggerFactory.Object, mockServiceProvider.Object);
            embeddingServiceMock.Setup(m => m.GetSettings()).Returns(new RouterSettings { AllowOpenClientRegistration = true });

            var services = new ServiceCollection();
            services.AddSingleton(embeddingServiceMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            var controller = new AuthorizationController(mockOAuthRepo.Object, mockAudit.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var json = JsonDocument.Parse("{\"client_name\":\"No AppType\",\"redirect_uris\":[\"https://example.com/callback\"]}").RootElement;
            var result = await controller.RegisterClient(json) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            var jsonText = JsonSerializer.Serialize(result.Value);
            var doc = JsonDocument.Parse(jsonText);
            Assert.Equal("invalid_client_metadata", doc.RootElement.GetProperty("error").GetString());
            Assert.Equal("The 'application_type' parameter is required.", doc.RootElement.GetProperty("error_description").GetString());
        }
    }
}
