using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
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
using McpRouter.Controllers;
using McpRouter.Core.Database;
using McpRouter.Middleware;
using McpRouter.Models;
using McpRouter.Services;
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

            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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

            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
            var credentialService = new CredentialService(dbFactory);
            var controller = new ClientsController(dbFactory, mockAudit.Object, credentialService);
            var result = await controller.DeleteClient("123");

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteClient_ReturnsNotFound_WhenAppDoesNotExist()
        {
            var (conn, dbFactory) = CreateDbFactory();
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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
            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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

            var mockAudit = new Mock<McpRouter.Core.Logging.IAuditLogger>();
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
            httpContext.User = authResult.Principal;

            // Setup a ClientSession mock or directly check how IsUserAuthorizedAsync would behave
            // using the HttpContext.Items populated by our authentication handler.
            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(r => r.HttpContext).Returns(httpContext);

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
            var servers = new List<McpServer> { new McpServer { Id = "mcp-github", Enabled = true } };

            var session = new ClientSession("session-1", responseMock.Object, servers, new HttpClient(), null, null, loggerMock.Object);

            // Check authorized target (in scope)
            var authorized = await session.IsUserAuthorizedAsync("callTool", "mcp-github__get_repo", httpContext);
            authorized.Should().BeTrue();

            // Check unauthorized target (out of scope)
            var unauthorized = await session.IsUserAuthorizedAsync("callTool", "mcp-docker__list_containers", httpContext);
            unauthorized.Should().BeFalse();
        }
    }
}
