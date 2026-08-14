using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Controllers;
using McpRouter.Core;
using McpRouter.Core.Database;
using McpRouter.Core.Identity;
using McpRouter.Core.Logging;
using McpRouter.Core.Secrets;
using McpRouter.Models;
using McpRouter.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class CategoryScopedAppKeysTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _masterConnection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;
        private readonly Mock<IAuditLogger> _auditLoggerMock;
        private readonly Mock<ICredentialService> _credentialServiceMock;
        private readonly Mock<IAppKeyRepository> _appKeyRepoMock;
        private readonly Mock<ISettingRepository> _settingRepoMock;

        public CategoryScopedAppKeysTests()
        {
            _connectionString = $"Data Source=CategoryScopedAppKeysTests_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            _masterConnection = new SqliteConnection(_connectionString);
            _masterConnection.Open();

            _masterConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY,
                    GlobalMaxKeys INTEGER DEFAULT 100,
                    UserMaxKeys INTEGER DEFAULT 5
                );
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
                CREATE TABLE IF NOT EXISTS Servers (
                    Id TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    Type TEXT,
                    Categories TEXT,
                    Enabled INTEGER DEFAULT 1,
                    IsLocal INTEGER DEFAULT 1,
                    ExecutionTarget TEXT DEFAULT 'auto',
                    RequiresManualApproval INTEGER DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS AccessPolicies (
                    Id TEXT PRIMARY KEY,
                    TargetId TEXT,
                    TargetType TEXT,
                    RequiredGroup TEXT,
                    IsAllowed INTEGER DEFAULT 1
                );
            ");

            _masterConnection.Execute("INSERT INTO Settings (Id, GlobalMaxKeys, UserMaxKeys) VALUES ('default', 100, 5);");
            _masterConnection.Execute("INSERT INTO Servers (Id, DisplayName, Type, Categories, Enabled) VALUES ('ha', 'Home Assistant', 'sse', '[\"smarthome\",\"iot\"]', 1);");
            _masterConnection.Execute("INSERT INTO Servers (Id, DisplayName, Type, Categories, Enabled) VALUES ('plex', 'Plex Media', 'sse', '[\"media\"]', 1);");
            _masterConnection.Execute("INSERT INTO Servers (Id, DisplayName, Type, Categories, Enabled) VALUES ('docker', 'Docker Host', 'sse', '[\"infrastructure\"]', 1);");

            // Seed RBAC policies for McpClient role
            _masterConnection.Execute("INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed) VALUES ('p1', 'server:ha', 'McpClient', 1);");
            _masterConnection.Execute("INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed) VALUES ('p2', 'server:plex', 'McpClient', 1);");
            _masterConnection.Execute("INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed) VALUES ('p3', 'server:docker', 'McpClient', 1);");
            _masterConnection.Execute("INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed) VALUES ('p4', 'server:server1', 'McpClient', 1);");
            _masterConnection.Execute("INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed) VALUES ('p5', 'server:server2', 'McpClient', 1);");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() =>
            {
                var conn = new SqliteConnection(_connectionString);
                conn.Open();
                return conn;
            });
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;

            var configDict = new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "SuperSecureDatabaseKey123!" },
                { "ROUTER_SECRET", "SuperSecretRouterToken456!" },
                { "Oidc:TrustedProxies", "127.0.0.1,::1" },
                { "Admin:GroupSid", "full_admin" },
                { "TrustForwardedHeaders", "true" }
            };
            _config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            _auditLoggerMock = new Mock<IAuditLogger>();
            _credentialServiceMock = new Mock<ICredentialService>();
            _appKeyRepoMock = new Mock<IAppKeyRepository>();
            _settingRepoMock = new Mock<ISettingRepository>();
        }

        public void Dispose()
        {
            _masterConnection.Dispose();
        }

        private AppKeysController CreateAppKeysController(string username, string role)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_dbFactory);
            services.AddSingleton(_config);
            services.AddSingleton(_auditLoggerMock.Object);
            services.AddSingleton(new CompositeIdentityProvider(new[] { new HeaderIdentityProvider(_config) }));
            var sp = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            httpContext.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
            httpContext.Request.Headers["Remote-User"] = username;
            if (role == "Admin" || role == "full_admin")
            {
                httpContext.Request.Headers["Remote-Groups"] = "full_admin";
                httpContext.Request.Headers["Remote-User-Sid"] = "full_admin";
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            if (role == "Admin" || role == "full_admin")
            {
                claims.Add(new Claim("groups", "full_admin"));
                claims.Add(new Claim(ClaimTypes.GroupSid, "full_admin"));
                claims.Add(new Claim(ClaimTypes.GroupSid, "S-1-5-32-544"));
            }
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            var controller = new AppKeysController(_appKeyRepoMock.Object, _settingRepoMock.Object, _config, _auditLoggerMock.Object, _credentialServiceMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
            return controller;
        }

        private ClientsController CreateClientsController(string username, string role)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_dbFactory);
            services.AddSingleton(_config);
            services.AddSingleton(_auditLoggerMock.Object);
            services.AddSingleton(new CompositeIdentityProvider(new[] { new HeaderIdentityProvider(_config) }));
            var sp = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            httpContext.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
            httpContext.Request.Headers["Remote-User"] = username;
            if (role == "Admin" || role == "full_admin")
            {
                httpContext.Request.Headers["Remote-Groups"] = "full_admin";
                httpContext.Request.Headers["Remote-User-Sid"] = "full_admin";
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            if (role == "Admin" || role == "full_admin")
            {
                claims.Add(new Claim("groups", "full_admin"));
                claims.Add(new Claim(ClaimTypes.GroupSid, "full_admin"));
                claims.Add(new Claim(ClaimTypes.GroupSid, "S-1-5-32-544"));
            }
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            var controller = new ClientsController(_dbFactory, _auditLoggerMock.Object, _credentialServiceMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
            return controller;
        }

        #region Scope Validation Tests (AppKeysController & ClientsController)

        [Fact]
        public async Task AppKeysController_CreateAppKey_ValidCategory_Succeeds()
        {
            var controller = CreateAppKeysController("user1", "User");
            _credentialServiceMock
                .Setup(c => c.CreateCredentialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int?>()))
                .ReturnsAsync((new AppKey { Id = "key-1", Name = "SmartHome Key", Username = "user1" }, "mcp-test-plain-secret"));

            var req = new CreateAppKeyRequest
            {
                Name = "SmartHome Key",
                Scopes = new List<string> { "category:smarthome" }
            };

            var res = await controller.CreateAppKey(req);
            var okResult = Assert.IsType<OkObjectResult>(res);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task AppKeysController_CreateAppKey_UnknownCategory_NonAdmin_FailsWithBadRequest()
        {
            var controller = CreateAppKeysController("user1", "User");
            var req = new CreateAppKeyRequest
            {
                Name = "Invalid Category Key",
                Scopes = new List<string> { "category:nonexistent_cat" }
            };

            var res = await controller.CreateAppKey(req);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Equal(400, badReq.StatusCode);
        }

        [Fact]
        public async Task AppKeysController_CreateAppKey_EmptyCategory_FailsWithBadRequest()
        {
            var controller = CreateAppKeysController("admin1", "Admin");
            var req = new CreateAppKeyRequest
            {
                Name = "Empty Category Key",
                Scopes = new List<string> { "category:  " }
            };

            var res = await controller.CreateAppKey(req);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Equal(400, badReq.StatusCode);
        }

        [Fact]
        public async Task AppKeysController_CreateAppKey_UnknownCategory_Admin_Succeeds()
        {
            var controller = CreateAppKeysController("admin1", "Admin");
            _credentialServiceMock
                .Setup(c => c.CreateCredentialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int?>()))
                .ReturnsAsync((new AppKey { Id = "key-2", Name = "Admin Future Cat Key", Username = "admin1" }, "mcp-test-plain-secret"));

            var req = new CreateAppKeyRequest
            {
                Name = "Admin Future Cat Key",
                Scopes = new List<string> { "category:future_category" }
            };

            var res = await controller.CreateAppKey(req);
            var okResult = Assert.IsType<OkObjectResult>(res);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ClientsController_CreateClient_ValidCategory_Succeeds()
        {
            var controller = CreateClientsController("admin1", "Admin");
            _credentialServiceMock
                .Setup(c => c.CreateCredentialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int?>()))
                .ReturnsAsync((new AppKey { Id = "client-1", Name = "HomeAssistant Client", Username = "client-id-1" }, "mcp-test-plain-secret"));

            var model = new ClientsController.CreateClientModel
            {
                DisplayName = "HomeAssistant Client",
                Scopes = new List<string> { "category:smarthome" }
            };

            var res = await controller.CreateClient(model);
            var okResult = Assert.IsType<OkObjectResult>(res);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ClientsController_CreateClient_EmptyCategory_ReturnsBadRequest()
        {
            var controller = CreateClientsController("admin1", "Admin");
            var model = new ClientsController.CreateClientModel
            {
                DisplayName = "Empty Category Client",
                Scopes = new List<string> { "category:" }
            };

            var res = await controller.CreateClient(model);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Equal(400, badReq.StatusCode);
        }

        #endregion

        #region Authorization & Isolation Tests (ClientSession)

        private ClientSession CreateClientSessionWithAppKey(List<string> scopes, List<McpServer> servers, out DefaultHttpContext httpContext)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_dbFactory);
            services.AddSingleton(_config);
            services.AddSingleton(_auditLoggerMock.Object);
            services.AddSingleton(new CompositeIdentityProvider(new[] { new HeaderIdentityProvider() }));
            var sp = services.BuildServiceProvider();

            httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Items["AppKeyUsed"] = true;
            httpContext.Items["AppKeyScopes"] = JsonSerializer.Serialize(scopes);
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testapp"),
                new Claim(ClaimTypes.Role, "McpClient"),
                new Claim("groups", "McpClient")
            }, "AppKey");
            httpContext.User = new ClaimsPrincipal(identity);

            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(r => r.HttpContext).Returns(httpContext);

            var loggerMock = new Mock<ILogger<ClientSession>>();
            var session = new ClientSession(
                "session-cat-1",
                responseMock.Object,
                servers,
                new HttpClient(),
                new Mock<IEmbeddingService>().Object,
                null,
                loggerMock.Object,
                sp
            );
            return session;
        }

        [Fact]
        public async Task ClientSession_CategoryScope_AuthorizesMatchingServerTools_AndDeniesOthers()
        {
            var servers = new List<McpServer>
            {
                new McpServer { Id = "ha", DisplayName = "Home Assistant", Categories = new List<string> { "smarthome" }, Enabled = true },
                new McpServer { Id = "plex", DisplayName = "Plex Media", Categories = new List<string> { "media" }, Enabled = true }
            };

            var session = CreateClientSessionWithAppKey(new List<string> { "category:smarthome" }, servers, out var httpContext);

            // ha is in smarthome -> Authorized
            var haAuth = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on", httpContext);
            Assert.True(haAuth);

            // plex is in media -> Denied
            var plexAuth = await session.IsUserAuthorizedAsync("tools/call", "plex__play", httpContext);
            Assert.False(plexAuth);
        }

        [Fact]
        public async Task ClientSession_GroupAliasScope_AuthorizesIdenticallyToCategory()
        {
            var servers = new List<McpServer>
            {
                new McpServer { Id = "ha", DisplayName = "Home Assistant", Categories = new List<string> { "smarthome" }, Enabled = true },
                new McpServer { Id = "plex", DisplayName = "Plex Media", Categories = new List<string> { "media" }, Enabled = true }
            };

            var session = CreateClientSessionWithAppKey(new List<string> { "group:smarthome" }, servers, out var httpContext);

            var haAuth = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on", httpContext);
            Assert.True(haAuth);

            var plexAuth = await session.IsUserAuthorizedAsync("tools/call", "plex__play", httpContext);
            Assert.False(plexAuth);
        }

        [Fact]
        public async Task ClientSession_CategoryScope_IsCaseInsensitive()
        {
            var servers = new List<McpServer>
            {
                new McpServer { Id = "ha", DisplayName = "Home Assistant", Categories = new List<string> { "SmartHome" }, Enabled = true }
            };

            var session = CreateClientSessionWithAppKey(new List<string> { "category:smarthome" }, servers, out var httpContext);

            var isAuth = await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on", httpContext);
            Assert.True(isAuth);
        }

        [Fact]
        public async Task ClientSession_ExecuteTool_EnforcesCategoryScopeOnInnerTarget()
        {
            var servers = new List<McpServer>
            {
                new McpServer { Id = "ha", DisplayName = "Home Assistant", Categories = new List<string> { "smarthome" }, Enabled = true },
                new McpServer { Id = "plex", DisplayName = "Plex Media", Categories = new List<string> { "media" }, Enabled = true }
            };

            var session = CreateClientSessionWithAppKey(new List<string> { "category:smarthome" }, servers, out var httpContext);

            // execute_tool targeting plex__play (out of category) -> Denied with permission error
            var plexPayload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new
                {
                    name = "execute_tool",
                    arguments = new { name = "plex__play", arguments = new { } }
                }
            });
            var plexRes = await session.CallToolAsync("execute_tool", plexPayload, _dbFactory, httpContext);
            var plexJson = JsonSerializer.Serialize(plexRes);
            Assert.Contains("does not have permission", plexJson);
        }

        [Fact]
        public async Task ClientSession_ResourcesAndTemplates_FilteredByCategoryScope()
        {
            var servers = new List<McpServer>
            {
                new McpServer { Id = "ha", DisplayName = "Home Assistant", Categories = new List<string> { "smarthome" }, Enabled = true },
                new McpServer { Id = "plex", DisplayName = "Plex Media", Categories = new List<string> { "media" }, Enabled = true }
            };

            var session = CreateClientSessionWithAppKey(new List<string> { "category:smarthome" }, servers, out var httpContext);

            var haResAuth = await session.IsUserAuthorizedAsync("resources/read", "mcp://ha/entities", httpContext);
            Assert.True(haResAuth);

            var plexResAuth = await session.IsUserAuthorizedAsync("resources/read", "mcp://plex/status", httpContext);
            Assert.False(plexResAuth);

            var haTemplateAuth = await session.IsUserAuthorizedAsync("resources/templates/list", "mcp://ha/entities/{id}", httpContext);
            Assert.True(haTemplateAuth);

            var plexTemplateAuth = await session.IsUserAuthorizedAsync("resources/templates/list", "mcp://plex/library/{id}", httpContext);
            Assert.False(plexTemplateAuth);
        }

        [Fact]
        public async Task ClientSession_DynamicServerMembership_UpdatesAccessDynamically()
        {
            // Initial state in DB: server1 has category 'dev', server2 has category 'prod'
            _masterConnection.Execute("INSERT OR REPLACE INTO Servers (Id, DisplayName, Type, Categories, Enabled) VALUES ('server1', 'Server 1', 'sse', '[\"dev\"]', 1);");
            _masterConnection.Execute("INSERT OR REPLACE INTO Servers (Id, DisplayName, Type, Categories, Enabled) VALUES ('server2', 'Server 2', 'sse', '[\"prod\"]', 1);");

            var servers = new List<McpServer>
            {
                new McpServer { Id = "server1", DisplayName = "Server 1", Categories = new List<string> { "dev" }, Enabled = true },
                new McpServer { Id = "server2", DisplayName = "Server 2", Categories = new List<string> { "prod" }, Enabled = true }
            };

            var session = CreateClientSessionWithAppKey(new List<string> { "category:dev" }, servers, out var httpContext);

            // Initially: server1 is authorized, server2 is denied
            Assert.True(await session.IsUserAuthorizedAsync("tools/call", "server1__tool1", httpContext));
            Assert.False(await session.IsUserAuthorizedAsync("tools/call", "server2__tool2", httpContext));

            // Dynamically add category 'dev' to server2 in DB
            _masterConnection.Execute("UPDATE Servers SET Categories = '[\"prod\",\"dev\"]' WHERE Id = 'server2';");

            // Server2 is now dynamically authorized without creating a new session or key!
            Assert.True(await session.IsUserAuthorizedAsync("tools/call", "server2__tool2", httpContext));

            // Dynamically remove category 'dev' from server1 in DB
            _masterConnection.Execute("UPDATE Servers SET Categories = '[\"other\"]' WHERE Id = 'server1';");

            // Server1 is now dynamically denied!
            Assert.False(await session.IsUserAuthorizedAsync("tools/call", "server1__tool1", httpContext));
        }

        [Fact]
        public async Task ClientSession_MixedScopes_CombinesCategoryAndSpecificToolScopes()
        {
            var servers = new List<McpServer>
            {
                new McpServer { Id = "ha", DisplayName = "Home Assistant", Categories = new List<string> { "smarthome" }, Enabled = true },
                new McpServer { Id = "docker", DisplayName = "Docker Host", Categories = new List<string> { "infrastructure" }, Enabled = true }
            };

            // Key has category:smarthome AND tool:docker__ps
            var session = CreateClientSessionWithAppKey(new List<string> { "category:smarthome", "tool:docker__ps" }, servers, out var httpContext);

            // All ha tools allowed
            Assert.True(await session.IsUserAuthorizedAsync("tools/call", "ha__turn_on", httpContext));
            Assert.True(await session.IsUserAuthorizedAsync("tools/call", "ha__turn_off", httpContext));

            // Specific docker tool allowed
            Assert.True(await session.IsUserAuthorizedAsync("tools/call", "docker__ps", httpContext));

            // Other docker tool denied
            Assert.False(await session.IsUserAuthorizedAsync("tools/call", "docker__restart", httpContext));
        }

        [Fact]
        public async Task ClientSession_Complete_FiltersServerNamesByCategoryScope()
        {
            var servers = new List<McpServer>
            {
                new McpServer { Id = "ha", DisplayName = "Home Assistant", Categories = new List<string> { "smarthome" }, Enabled = true },
                new McpServer { Id = "plex", DisplayName = "Plex Media", Categories = new List<string> { "media" }, Enabled = true }
            };

            var session = CreateClientSessionWithAppKey(new List<string> { "category:smarthome" }, servers, out var httpContext);

            var completionPayload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "completion/complete",
                @params = new
                {
                    @ref = new
                    {
                        type = "ref/resource",
                        uriTemplate = "logs://{server_name}/today"
                    },
                    argument = new
                    {
                        name = "server_name",
                        value = ""
                    }
                }
            });

            var result = await session.CompleteAsync(completionPayload, httpContext);
            var resultJson = JsonSerializer.Serialize(result);

            // 'ha' should be in completions, 'plex' should not
            Assert.Contains("ha", resultJson);
            Assert.DoesNotContain("plex", resultJson);
        }

        #endregion
    }
}
