using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using McpRouter.Components.Clients;
using McpRouter.Components.AppKeys;
using McpRouter.Components.Providers;
using McpRouter.Components.Authorization;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Middleware;
using McpRouter.Models;
using McpRouter.Core.Routing;
using Dapper;

namespace McpRouter.Tests
{
    public class ClientsControllerTests
    {
        private (SqliteConnection conn, IDbConnectionFactory factory) CreateDbFactory()
        {
            var dbName = $"Data Source=ClientsControllerTests_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var connection = new SqliteConnection(dbName);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS AppKeys (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Username TEXT,
                    KeyPrefix TEXT,
                    EncryptedKey TEXT,
                    ScopesJson TEXT DEFAULT '[]',
                    OwnerSid TEXT,
                    KeyType TEXT DEFAULT 'personal',
                    ExpiresAt TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );
            ");

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(() => new SqliteConnection(dbName));
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (connection, mockDbFactory.Object);
        }

        private AppKeyAuthenticationHandler CreateAuthenticationHandler(IDbConnectionFactory dbFactory, IConfiguration config)
        {
            var optionsMonitorMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

            return new AppKeyAuthenticationHandler(
                optionsMonitorMock.Object,
                NullLoggerFactory.Instance,
                UrlEncoder.Default,
                dbFactory,
                config
            );
        }

        [Fact]
        public async Task GetClients_ReturnsOk_WithClientsAndMappedProperties()
        {
            var (conn, dbFactory) = CreateDbFactory();
            conn.Execute("INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson) VALUES ('id-1', 'Client One', 'client-1', 'mcp_prefix1', 'secret1', '[\"mcp_client\"]')");

            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var result = await controller.GetClients();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var list = (okResult.Value as IEnumerable<object>)?.ToList();

            list.Should().NotBeNull();
            list.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateClient_ReturnsOk_WithGeneratedCredentials()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var model = new ClientsController.CreateClientModel
            {
                DisplayName = "Test CLI",
                Scopes = new List<string> { "custom_scope" }
            };

            var result = await controller.CreateClient(model);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value;

            value.Should().NotBeNull();
            var displayNameProp = value!.GetType().GetProperty("DisplayName")?.GetValue(value, null) as string;
            displayNameProp.Should().Be("Test CLI");
        }

        [Fact]
        public async Task DeleteClient_ReturnsNoContent_WhenAppExists()
        {
            var (conn, dbFactory) = CreateDbFactory();
            conn.Execute("INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey) VALUES ('123', 'Client', 'user', 'pref', 'sec')");

            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);
            var result = await controller.DeleteClient("123");

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteClient_ReturnsNotFound_WhenAppDoesNotExist()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var result = await controller.DeleteClient("nonexistent");
            result.Should().BeOfType<NotFoundResult>();
        }

        // --- NEW COMPREHENSIVE TESTS ---

        [Fact]
        public async Task CreateThenAuthenticate_IntegrationTest_Succeeds()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            // 1. Create a client using the endpoint
            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Automated Test App",
                Scopes = new List<string> { "all" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            responseValue.Should().NotBeNull();

            var clientId = responseValue!.GetType().GetProperty("ClientId")?.GetValue(responseValue, null) as string;
            var clientSecret = responseValue!.GetType().GetProperty("ClientSecret")?.GetValue(responseValue, null) as string;

            clientId.Should().NotBeNullOrEmpty();
            clientSecret.Should().NotBeNullOrEmpty();
            clientSecret.Should().StartWith("mcp-");

            // 2. Authenticate against AppKeyAuthenticationHandler using the returned credential
            var configMock = new Mock<IConfiguration>();
            var services = new ServiceCollection();
            services.AddSingleton(dbFactory);
            services.AddSingleton(configMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var handler = CreateAuthenticationHandler(dbFactory, configMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider;
            httpContext.Request.Headers["Authorization"] = $"Bearer {clientSecret}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeTrue(authResult.Failure?.Message);
            authResult.Principal.Should().NotBeNull();
            authResult.Principal!.Identity!.Name.Should().Be(clientId);

            // HttpContext items should be populated correctly
            httpContext.Items["AppKeyUsed"].Should().Be(true);
            httpContext.Items["AppKeyOwner"].Should().Be(clientId);
            ((string)httpContext.Items["AppKeyScopes"]!).Should().Contain("all");
        }

        [Fact]
        public async Task DatabaseAssertion_PlaintextNotPersisted()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Secure App",
                Scopes = new List<string> { "custom_scope" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            var clientSecret = responseValue!.GetType().GetProperty("ClientSecret")?.GetValue(responseValue, null) as string;

            // Assert that plaintext is not saved
            var storedKey = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys;");
            storedKey.Should().NotBeNull();
            storedKey!.EncryptedKey.Should().NotBe(clientSecret);
            storedKey.EncryptedKey.Should().HaveLength(64); // SHA-256 is 32 bytes (64 hex characters)
            storedKey.EncryptedKey.Should().NotContain("+").And.NotContain("/").And.NotContain("="); // Hexadecimal, not Base64
        }

        [Fact]
        public async Task InvalidPrefix_Test_ReturnsNoResult()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Some App",
                Scopes = new List<string> { "all" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            var clientSecret = responseValue!.GetType().GetProperty("ClientSecret")?.GetValue(responseValue, null) as string;

            // Change mcp- prefix to invalid prefix mcp_
            var invalidSecret = "mcp_" + clientSecret!.Substring(4);

            var configMock = new Mock<IConfiguration>();
            var handler = CreateAuthenticationHandler(dbFactory, configMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {invalidSecret}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeFalse();
            authResult.None.Should().BeTrue(); // NoResult
        }

        [Fact]
        public async Task InvalidHash_Test_Fails()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Some App",
                Scopes = new List<string> { "all" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            var clientSecret = responseValue!.GetType().GetProperty("ClientSecret")?.GetValue(responseValue, null) as string;

            // Tamper with the credential string
            var invalidSecret = clientSecret + "x";

            var configMock = new Mock<IConfiguration>();
            var handler = CreateAuthenticationHandler(dbFactory, configMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {invalidSecret}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeFalse();
            authResult.Failure.Should().NotBeNull();
            authResult.Failure!.Message.Should().Be("Invalid App Key.");
        }

        [Fact]
        public async Task Expired_Test_Fails()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var credentialService = new CredentialService(dbFactory);

            // Create expired credential manually or via helper
            var scopes = new List<string> { "all" };
            var (appKey, plaintextKey) = await credentialService.CreateCredentialAsync(
                "Expired App", "client-expired", "sid-expired", scopes, -1 // expired yesterday
            );

            var configMock = new Mock<IConfiguration>();
            var handler = CreateAuthenticationHandler(dbFactory, configMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {plaintextKey}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeFalse();
            authResult.Failure.Should().NotBeNull();
            authResult.Failure!.Message.Should().Be("App Key has expired.");
        }

        [Fact]
        public async Task RevokedOrDeleted_Test_Fails()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "To Be Revoked",
                Scopes = new List<string> { "all" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            var id = responseValue!.GetType().GetProperty("ClientId")?.GetValue(responseValue, null) as string;
            var clientSecret = responseValue!.GetType().GetProperty("ClientSecret")?.GetValue(responseValue, null) as string;

            // Delete / Revoke the client.
            // Find the database ID first
            var storedKey = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE Username = @Username;", new { Username = id });
            storedKey.Should().NotBeNull();

            var deleteResult = await controller.DeleteClient(storedKey!.Id);
            deleteResult.Should().BeOfType<NoContentResult>();

            // Try to authenticate with the deleted secret
            var configMock = new Mock<IConfiguration>();
            var handler = CreateAuthenticationHandler(dbFactory, configMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {clientSecret}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeFalse();
            authResult.Failure.Should().NotBeNull();
            authResult.Failure!.Message.Should().Be("Invalid App Key prefix.");
        }

        [Fact]
        public async Task OutOfScope_Test_BehavesIdenticallyToAppKeys()
        {
            var (conn, dbFactory) = CreateDbFactory();

            // Create AccessPolicies table and insert rules so the McpClient role is authorized
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS AccessPolicies (
                    Id TEXT PRIMARY KEY,
                    TargetId TEXT,
                    RequiredGroup TEXT,
                    IsAllowed INTEGER DEFAULT 1
                );
            ");
            conn.Execute("INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed) VALUES ('p1', 'server:mcp-github', 'McpClient', 1);");
            conn.Execute("INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed) VALUES ('p2', 'server:mcp-docker', 'McpClient', 1);");

            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            // Register client with server-specific scope
            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Server Specific App",
                Scopes = new List<string> { "server:mcp-github" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            var clientSecret = responseValue!.GetType().GetProperty("ClientSecret")?.GetValue(responseValue, null) as string;

            // Authenticate first
            var configMock = new Mock<IConfiguration>();
            var services = new ServiceCollection();
            services.AddSingleton(dbFactory);
            services.AddSingleton(configMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var handler = CreateAuthenticationHandler(dbFactory, configMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider;
            httpContext.Request.Headers["Authorization"] = $"Bearer {clientSecret}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeTrue();
            httpContext.User = authResult.Principal!;

            // Setup a ClientSession mock or directly check how IsUserAuthorizedAsync would behave
            // using the HttpContext.Items populated by our authentication handler.
            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(r => r.HttpContext).Returns(httpContext);

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
            var servers = new List<McpServer> { new McpServer { Id = "mcp-github", Enabled = true } };

            var session = new ClientSession("session-1", responseMock.Object, servers, new HttpClient(), new Mock<IEmbeddingService>().Object, null, loggerMock.Object);

            // Check authorized target (in scope)
            var authorized = await session.IsUserAuthorizedAsync("callTool", "mcp-github__get_repo", httpContext);
            authorized.Should().BeTrue();

            // Check unauthorized target (out of scope)
            var unauthorized = await session.IsUserAuthorizedAsync("callTool", "mcp-docker__list_containers", httpContext);
            unauthorized.Should().BeFalse();
        }

        [Fact]
        public async Task CreateClient_AdminCreator_DoesNotInheritAdminSid_AndCannotAccessAdminPolicy()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            // Simulate creating admin user with Admin SID
            var adminSid = "S-1-5-32-544";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin_user"),
                new Claim("Sid", adminSid)
            };
            var adminPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "OidcHeader"));

            var httpContext = new DefaultHttpContext();
            httpContext.User = adminPrincipal;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Create client
            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Machine Client App",
                Scopes = new List<string> { "all" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            var clientId = responseValue!.GetType().GetProperty("ClientId")?.GetValue(responseValue, null) as string;
            var clientSecret = responseValue!.GetType().GetProperty("ClientSecret")?.GetValue(responseValue, null) as string;

            // 1. Verify DB record has EMPTY OwnerSid, NOT admin's SID
            var storedKey = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE Username = @Username;", new { Username = clientId });
            storedKey.Should().NotBeNull();
            storedKey!.OwnerSid.Should().BeEmpty();

            // 2. Authenticate with the generated client secret
            var configDict = new Dictionary<string, string?>
            {
                ["Admin:GroupSid"] = adminSid
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var services = new ServiceCollection();
            services.AddSingleton(dbFactory);
            services.AddSingleton<IConfiguration>(config);
            services.AddLogging();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx =>
                          {
                              var hc = ctx.Resource as HttpContext;
                              var cfg = hc?.RequestServices?.GetService<IConfiguration>();
                              var targetSid = cfg?["Admin:GroupSid"] ?? "S-1-5-32-544";
                              return ctx.User.HasClaim("Sid", targetSid);
                          });
                });
            });

            var sp = services.BuildServiceProvider();
            var authService = sp.GetRequiredService<IAuthorizationService>();

            var handler = CreateAuthenticationHandler(dbFactory, config);
            var clientContext = new DefaultHttpContext();
            clientContext.RequestServices = sp;
            clientContext.Request.Headers["Authorization"] = $"Bearer {clientSecret}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, clientContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeTrue();
            authResult.Principal.Should().NotBeNull();

            // The client principal must NOT have the admin's SID claim
            authResult.Principal!.HasClaim("Sid", adminSid).Should().BeFalse();

            // The client principal MUST be denied access to AdminPolicy
            var authPolicyResult = await authService.AuthorizeAsync(authResult.Principal!, clientContext, "AdminPolicy");
            authPolicyResult.Succeeded.Should().BeFalse("Machine client credentials must NOT inherit administrative privileges!");
        }

        [Fact]
        public async Task CreateClient_WithExpiresInDays_SetsExpiration_AndEnforcesExpiredAuthentication()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            // 1. Create client with 30-day expiration
            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Expiring App",
                Scopes = new List<string> { "all" },
                ExpiresInDays = 30
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value;
            var expiresAt = responseValue!.GetType().GetProperty("ExpiresAt")?.GetValue(responseValue, null) as DateTime?;

            expiresAt.Should().NotBeNull();
            expiresAt.Value.Should().BeAfter(DateTime.UtcNow.AddDays(29));

            // 2. Create client with expired duration (-1 days)
            var expiredModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Already Expired App",
                Scopes = new List<string> { "all" },
                ExpiresInDays = -1
            };
            var expiredResult = await controller.CreateClient(expiredModel);
            var expiredOk = expiredResult.Should().BeOfType<OkObjectResult>().Subject;
            var expiredValue = expiredOk.Value;
            var expiredSecret = expiredValue!.GetType().GetProperty("ClientSecret")?.GetValue(expiredValue, null) as string;

            // Authenticate with expired client secret
            var configMock = new Mock<IConfiguration>();
            var handler = CreateAuthenticationHandler(dbFactory, configMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {expiredSecret}";

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            authResult.Succeeded.Should().BeFalse();
            authResult.Failure!.Message.Should().Be("App Key has expired.");
        }

        [Fact]
        public async Task GetClients_NeverLeaksRawBearerSecretOrEncryptedKey()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            // Create client
            var createModel = new ClientsController.CreateClientModel
            {
                DisplayName = "Leak Prevention App",
                Scopes = new List<string> { "all" }
            };
            var createResult = await controller.CreateClient(createModel);
            var okResult = createResult.Should().BeOfType<OkObjectResult>().Subject;
            var rawSecret = okResult.Value!.GetType().GetProperty("ClientSecret")?.GetValue(okResult.Value, null) as string;

            // Query GetClients
            var listResult = await controller.GetClients();
            var listOk = listResult.Should().BeOfType<OkObjectResult>().Subject;
            var list = (listOk.Value as IEnumerable<object>)?.ToList();

            list.Should().NotBeNull();
            list.Should().HaveCount(1);

            var item = list![0];
            var json = JsonSerializer.Serialize(item);

            // Ensure json never contains raw secret or encrypted hash
            json.Should().NotContain(rawSecret!);
            json.Should().NotContain("EncryptedKey");
            json.Should().NotContain("PlaintextKey");
            json.Should().NotContain("ClientSecret");
        }

        [Fact]
        public async Task RevokeCredential_HandlesSqlServerNoCount_AndReturnsAccurateStatus()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var credentialService = new CredentialService(dbFactory);

            var (appKey, plaintext) = await credentialService.CreateCredentialAsync(
                "Test Key", "test-user", "", new List<string> { "all" }, null
            );

            // Revoking existing credential returns true
            var revoked = await credentialService.RevokeCredentialAsync(appKey.Id);
            revoked.Should().BeTrue();

            // Revoking already revoked or non-existent credential returns false
            var revokedAgain = await credentialService.RevokeCredentialAsync(appKey.Id);
            revokedAgain.Should().BeFalse();

            var nonExistentRevoked = await credentialService.RevokeCredentialAsync("does-not-exist");
            nonExistentRevoked.Should().BeFalse();
        }

        [Fact]
        public async Task CredentialService_GeneratesHighEntropySelectorPrefix()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var credentialService = new CredentialService(dbFactory);

            var (appKey, plaintextKey) = await credentialService.CreateCredentialAsync(
                "High Entropy App", "client-high-entropy", "", new List<string> { "all" }, null
            );

            // Prefix format: mcp-global-{32 hex chars} (length: 11 + 32 = 43)
            appKey.KeyPrefix.Should().StartWith("mcp-global-");
            appKey.KeyPrefix.Length.Should().Be(43);

            // Plaintext format: mcp-global-{32 hex chars}-{64 hex chars}
            plaintextKey.Should().StartWith(appKey.KeyPrefix + "-");
            plaintextKey.Length.Should().Be(43 + 1 + 64); // 108 characters
        }

        [Fact]
        public async Task CreateClient_ReturnsBadRequest_WhenDisplayNameMissing()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var model = new ClientsController.CreateClientModel { DisplayName = "" };
            var result = await controller.CreateClient(model);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateClient_ReturnsBadRequest_WhenCategoryScopeEmpty()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);

            var model = new ClientsController.CreateClientModel
            {
                DisplayName = "Invalid Category App",
                Scopes = new List<string> { "category:" }
            };
            var result = await controller.CreateClient(model);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateClient_Returns500_WhenCredentialServiceThrows()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var mockCredService = new Mock<ICredentialService>();
            mockCredService.Setup(c => c.CreateCredentialAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int?>()
            )).ThrowsAsync(new Exception("Database disk full"));

            var controller = new ClientsController(dbFactory, mockAudit.Object, mockCredService.Object);
            var model = new ClientsController.CreateClientModel { DisplayName = "Faulty App" };
            var result = await controller.CreateClient(model);
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteClient_Returns500_WhenCredentialServiceThrows()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Infrastructure.Logging.IAuditLogger>();
            var mockCredService = new Mock<ICredentialService>();
            mockCredService.Setup(c => c.RevokeCredentialAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database locked"));

            var controller = new ClientsController(dbFactory, mockAudit.Object, mockCredService.Object);
            var result = await controller.DeleteClient("client-123");
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }
    }
}

