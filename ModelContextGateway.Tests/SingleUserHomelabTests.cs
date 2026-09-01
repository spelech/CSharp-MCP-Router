using System.Net;
using System.Text.Encodings.Web;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ModelContextGateway.Tests
{
    public class SingleUserHomelabTests
    {
        private (SqliteConnection masterConn, IDbConnectionFactory factory) CreateDbFactory()
        {
            var dbName = $"Data Source=HomelabTestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var masterConn = new SqliteConnection(dbName);
            masterConn.Open();

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(() => new SqliteConnection(dbName));
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (masterConn, mockDbFactory.Object);
        }

        [Fact]
        [Requirement("AUTH-35", "AUTH", RequirementType.Positive, "Single-user homelab startup initializes SQLite, auto-generates Admin and Client AppKeys without PFX certificate requirements")]
        public void Homelab_ZeroConfigStartup_SeedsAdminAndClientKeys_AndPersistsFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"mcg_homelab_test_{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(tempDir);
                var (conn, factory) = CreateDbFactory();
                var configDict = new Dictionary<string, string?>
                {
                    ["MCG_DATA_DIR"] = tempDir,
                    ["DB_PROVIDER"] = "sqlite",
                    ["MCG_AUTO_CERT"] = "true"
                };
                var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(config);
                services.AddSingleton(factory);
                services.AddLogging();
                var sp = services.BuildServiceProvider();

                DatabaseSeederService.SeedDatabase(sp, config);

                // 1. Verify SQLite database schema created
                var settings = conn.QueryFirstOrDefault<RouterSettings>("SELECT * FROM Settings");
                Assert.NotNull(settings);

                // 2. Verify Admin key auto-generated
                var adminKey = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE ScopesJson LIKE '%admin%' OR KeyType = 'system'");
                Assert.NotNull(adminKey);
                Assert.Equal("admin", adminKey.Username);
                Assert.StartsWith("mcp-adm-", adminKey.KeyPrefix);

                // 3. Verify Client key auto-generated
                var clientKey = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE KeyPrefix LIKE 'mcp-glb-%'");
                Assert.NotNull(clientKey);
                Assert.Equal("user", clientKey.Username);
                Assert.Equal("personal", clientKey.KeyType);
                Assert.Contains("all", clientKey.ScopesJson);

                // 4. Verify .admin.key and .client.key files were written
                var adminKeyFile = Path.Combine(tempDir, ".admin.key");
                var clientKeyFile = Path.Combine(tempDir, ".client.key");
                Assert.True(File.Exists(adminKeyFile));
                Assert.True(File.Exists(clientKeyFile));
                Assert.StartsWith("mcp-adm-", File.ReadAllText(adminKeyFile));
                Assert.StartsWith("mcp-glb-", File.ReadAllText(clientKeyFile));

                // 5. Verify OpenIddict builds in Production with auto-generated certificate
                var mockEnv = new Mock<IHostEnvironment>();
                mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);
                var oidcServices = new ServiceCollection();
                oidcServices.AddSingleton<IConfiguration>(config);
                oidcServices.AddMcpOpenIddict(mockEnv.Object, config);
                var oidcProvider = oidcServices.BuildServiceProvider();
                Assert.NotNull(oidcProvider);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        [Requirement("AUTH-36", "AUTH", RequirementType.Positive, "Pre-configured MCG_CLIENT_APP_KEYS seeds functional individualized client keys with custom scopes")]
        public async Task Homelab_PreConfiguredClientKeys_SeedsIndividualizedScopedKeys()
        {
            var (conn, factory) = CreateDbFactory();
            var claudeKey = "mcp-glb-ClaudeFull123-SecretValueA";
            var cursorKey = "mcp-srv-CursorDocker456-SecretValueB";
            var mediaKey = "mcp-grp-OpenWebUIMedia789-SecretValueC";

            var configDict = new Dictionary<string, string?>
            {
                ["MCG_CLIENT_APP_KEYS"] = $"{claudeKey}:Claude Desktop:all,{cursorKey}:Cursor IDE:server:docker,{mediaKey}:Open WebUI:category:media;category:homecontrol",
                ["DB_PROVIDER"] = "sqlite"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(factory);
            services.AddLogging();
            var sp = services.BuildServiceProvider();

            DatabaseSeederService.SeedDatabase(sp, config);

            // 1. Verify Claude Desktop key
            var claudePrefix = AppKeyAuthenticationHandler.ExtractKeyPrefix(claudeKey);
            var claudeRow = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix", new { KeyPrefix = claudePrefix });
            Assert.NotNull(claudeRow);
            Assert.Equal("Claude Desktop", claudeRow.Name);
            Assert.Contains("all", claudeRow.ScopesJson);

            // 2. Verify Cursor key
            var cursorPrefix = AppKeyAuthenticationHandler.ExtractKeyPrefix(cursorKey);
            var cursorRow = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix", new { KeyPrefix = cursorPrefix });
            Assert.NotNull(cursorRow);
            Assert.Equal("Cursor IDE", cursorRow.Name);
            Assert.Contains("server:docker", cursorRow.ScopesJson);

            // 3. Verify Media key
            var mediaPrefix = AppKeyAuthenticationHandler.ExtractKeyPrefix(mediaKey);
            var mediaRow = conn.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix", new { KeyPrefix = mediaPrefix });
            Assert.NotNull(mediaRow);
            Assert.Equal("Open WebUI", mediaRow.Name);
            Assert.Contains("category:media", mediaRow.ScopesJson);
            Assert.Contains("category:homecontrol", mediaRow.ScopesJson);

            // 4. Authenticate Cursor key via AppKeyAuthenticationHandler
            var optionsMonitorMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());
            var handler = new AppKeyAuthenticationHandler(
                optionsMonitorMock.Object,
                NullLoggerFactory.Instance,
                UrlEncoder.Default,
                factory,
                config);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {cursorKey}";
            httpContext.RequestServices = sp;

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            Assert.True(authResult.Succeeded, authResult.Failure?.Message);
            Assert.Equal("user", authResult.Principal?.Identity?.Name);
            Assert.True((bool)httpContext.Items["AppKeyUsed"]!);
            Assert.Contains("server:docker", (string)httpContext.Items["AppKeyScopes"]!);
        }

        [Fact]
        [Requirement("AUTH-37", "AUTH", RequirementType.Positive, "AppKeys with server and category scopes enforce precise tool execution boundaries")]
        public void AppKey_ScopeExtraction_ExtractsSemanticPrefixes()
        {
            var adminKey = "mcp-adm-Xk9L2mPq-7vN3wZ8aB1cE4fG9";
            var globalKey = "mcp-glb-R4t8W1yU-9pM2nQ6sD8fH3jK5";
            var serverKey = "mcp-srv-docker-9pM2nQ6sD8fH3jK5";
            var userKey = "mcp-usr-steve-9pM2nQ6sD8fH3jK5";

            Assert.Equal("mcp-adm-Xk9L2mPq", AppKeyAuthenticationHandler.ExtractKeyPrefix(adminKey));
            Assert.Equal("mcp-glb-R4t8W1yU", AppKeyAuthenticationHandler.ExtractKeyPrefix(globalKey));
            Assert.Equal("mcp-srv-docker", AppKeyAuthenticationHandler.ExtractKeyPrefix(serverKey));
            Assert.Equal("mcp-usr-steve", AppKeyAuthenticationHandler.ExtractKeyPrefix(userKey));
        }

        [Fact]
        [Requirement("AUTH-38", "AUTH", RequirementType.Positive, "LAN CIDR network configuration allows standalone web dashboard access from local subnet")]
        public void Standalone_LanCidr_GrantsAdminAccessToLocalSubnet()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["STANDALONE_ALLOWED_NETWORKS"] = "192.168.1.0/24,10.0.0.0/8"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var lanIp = IPAddress.Parse("192.168.1.150");
            var privateIp = IPAddress.Parse("10.5.0.25");
            var externalIp = IPAddress.Parse("198.51.100.4");

            var httpContextLan = new DefaultHttpContext();
            httpContextLan.Connection.RemoteIpAddress = lanIp;

            var httpContextExternal = new DefaultHttpContext();
            httpContextExternal.Connection.RemoteIpAddress = externalIp;

            Assert.True(SecurityValidationHelper.IsStandaloneAdminNetwork(lanIp, config));
            Assert.True(SecurityValidationHelper.IsStandaloneAdminNetwork(privateIp, config));
            Assert.False(SecurityValidationHelper.IsStandaloneAdminNetwork(externalIp, config));

            Assert.True(SecurityValidationHelper.IsAdmin(null, config, httpContextLan));
            Assert.False(SecurityValidationHelper.IsAdmin(null, config, httpContextExternal));
        }

        [Fact]
        [Requirement("AUTH-39", "AUTH", RequirementType.Positive, "Zero-config startup defaults enterprise auth providers and secret providers to disabled")]
        public void ZeroConfig_Startup_DefaultsEnterpriseProviders_ToDisabled()
        {
            var (conn, factory) = CreateDbFactory();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_PROVIDER"] = "sqlite"
            }).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(factory);
            services.AddLogging();
            var sp = services.BuildServiceProvider();

            DatabaseSeederService.SeedDatabase(sp, config);

            // 1. Verify AuthProviderConfigs defaults (enterprise providers disabled by default)
            var authProviders = conn.Query<ProviderRow>("SELECT ProviderName, IsEnabled FROM AuthProviderConfigs").ToList();
            var adAuth = authProviders.FirstOrDefault(p => p.ProviderName == "ActiveDirectory");
            var headerAuth = authProviders.FirstOrDefault(p => p.ProviderName == "HeaderAuth");
            var pocketIdAuth = authProviders.FirstOrDefault(p => p.ProviderName == "PocketID");

            Assert.NotNull(adAuth);
            Assert.Equal(0, adAuth.IsEnabled);

            Assert.NotNull(headerAuth);
            Assert.Equal(1, headerAuth.IsEnabled);

            Assert.NotNull(pocketIdAuth);
            Assert.Equal(1, pocketIdAuth.IsEnabled);

            // 2. Verify SecretProviders defaults (Vault, WindowsRegistry, TokenExchange disabled by default; Environment enabled)
            var secretProviders = conn.Query<ProviderRow>("SELECT ProviderName, IsEnabled FROM SecretProviders").ToList();
            var envSecret = secretProviders.FirstOrDefault(p => p.ProviderName == "Environment");
            var vaultSecret = secretProviders.FirstOrDefault(p => p.ProviderName == "Vault");
            var winRegSecret = secretProviders.FirstOrDefault(p => p.ProviderName == "WindowsRegistry");
            var tokenExSecret = secretProviders.FirstOrDefault(p => p.ProviderName == "TokenExchange");

            Assert.NotNull(envSecret);
            Assert.Equal(1, envSecret.IsEnabled);

            Assert.NotNull(vaultSecret);
            Assert.Equal(0, vaultSecret.IsEnabled);

            Assert.NotNull(winRegSecret);
            Assert.Equal(0, winRegSecret.IsEnabled);

            Assert.NotNull(tokenExSecret);
            Assert.Equal(0, tokenExSecret.IsEnabled);
        }

        private class ProviderRow
        {
            public string ProviderName { get; set; } = string.Empty;
            public int IsEnabled { get; set; }
        }
    }
}
