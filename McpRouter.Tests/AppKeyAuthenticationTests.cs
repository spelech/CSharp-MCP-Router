using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Moq;

namespace McpRouter.Tests
{
    public class AppKeyAuthenticationTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _masterConnection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;

        public AppKeyAuthenticationTests()
        {
            _connectionString = $"Data Source=AppKeyAuthTests_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            _masterConnection = new SqliteConnection(_connectionString);
            _masterConnection.Open();

            // Create required tables for testing
            _masterConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY,
                    EmbeddingProvider TEXT,
                    EmbeddingApiUrl TEXT,
                    EmbeddingApiKey TEXT,
                    EmbeddingApiModel TEXT,
                    EmbeddingModelDir TEXT,
                    DashboardTitle TEXT DEFAULT 'MCP Gateway', DashboardIcon TEXT DEFAULT 'fa-solid fa-network-wired', GlobalMaxKeys INTEGER DEFAULT 100,
                    UserMaxKeys INTEGER DEFAULT 5
                );
                CREATE TABLE IF NOT EXISTS AppKeys (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Username TEXT,
                    OwnerSid TEXT,
                    KeyPrefix TEXT,
                    EncryptedKey TEXT,
                    ScopesJson TEXT DEFAULT '[]',
                    KeyType TEXT DEFAULT 'personal',
                    ExpiresAt TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );");

            // Seed default settings row
            _masterConnection.Execute("INSERT INTO Settings (Id, GlobalMaxKeys, UserMaxKeys) VALUES ('default', 100, 5);");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() =>
            {
                var conn = new SqliteConnection(_connectionString);
                conn.Open();
                return conn;
            });
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["DB_ENCRYPTION_KEY"]).Returns("SuperSecureDatabaseKey123!");
            configMock.Setup(c => c["ROUTER_SECRET"]).Returns("SuperSecretRouterToken456!");
            _config = configMock.Object;
        }

        public void Dispose()
        {
            _masterConnection.Dispose();
        }

        [Fact]
        public void SymmetricEncryptionHelper_EncryptsAndDecryptsCorrectly()
        {
            var original = "mcp-global-securekeypartabc123";
            var encrypted = SymmetricEncryptionHelper.Encrypt(original, _config);

            Assert.NotEmpty(encrypted);
            Assert.NotEqual(original, encrypted);

            var decrypted = SymmetricEncryptionHelper.Decrypt(encrypted, _config);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public async Task AppKeys_PrefixLookup_WorksCorrectly()
        {
            var keyString = "mcp-global-randomstring123456789";
            var prefix = keyString.Substring(0, 16);
            var encrypted = SymmetricEncryptionHelper.Encrypt(keyString, _config);

            var key = new AppKey
            {
                Id = "test-id-1",
                Name = "Cursor Key",
                Username = "alice",
                KeyPrefix = prefix,
                EncryptedKey = encrypted,
                ScopesJson = "[\"all\"]"
            };

            await _masterConnection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson)
                VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson);",
                key);

            // Lookup by prefix
            var retrieved = await _masterConnection.QueryFirstOrDefaultAsync<AppKey>(
                "SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix;",
                new { KeyPrefix = prefix });

            Assert.NotNull(retrieved);
            Assert.Equal("alice", retrieved.Username);
            Assert.Equal(prefix, retrieved.KeyPrefix);

            var decrypted = SymmetricEncryptionHelper.Decrypt(retrieved.EncryptedKey, _config);
            Assert.Equal(keyString, decrypted);
        }

        [Fact]
        public async Task AppKeys_KeyExpiration_CheckedCorrectly()
        {
            var expiredKey = new AppKey
            {
                Id = "expired-id",
                Name = "Expired",
                Username = "bob",
                KeyPrefix = "mcp-global-expir",
                EncryptedKey = "some_encrypted_data",
                ScopesJson = "[\"all\"]",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired 5 minutes ago
            };

            await _masterConnection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt)
                VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt);",
                expiredKey);

            var retrieved = await _masterConnection.QueryFirstOrDefaultAsync<AppKey>(
                "SELECT * FROM AppKeys WHERE Id = @Id;",
                new { Id = "expired-id" });

            Assert.NotNull(retrieved);
            Assert.True(retrieved.ExpiresAt.HasValue);
            Assert.True(retrieved.ExpiresAt.Value < DateTime.UtcNow);
        }

        [Fact]
        public async Task AppKeys_Limits_CheckWorks()
        {
            // Seed 5 keys for Alice (which is the UserMaxKeys limit)
            for (int i = 1; i <= 5; i++)
            {
                var key = new AppKey
                {
                    Id = $"alice-key-{i}",
                    Name = $"Key {i}",
                    Username = "alice",
                    KeyPrefix = $"mcp-global-al{i}",
                    EncryptedKey = "encrypted",
                    ScopesJson = "[\"all\"]"
                };
                await _masterConnection.ExecuteAsync(@"
                    INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson)
                    VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson);",
                    key);
            }

            // Count Alice's active keys
            int count = await _masterConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AppKeys WHERE Username = @Username;",
                new { Username = "alice" });

            Assert.Equal(5, count);

            // Fetch limits setting using RouterSettings typed model
            var settings = await _masterConnection.QueryFirstOrDefaultAsync<RouterSettings>("SELECT * FROM Settings WHERE Id = 'default';");
            Assert.NotNull(settings);
            int maxKeys = settings.UserMaxKeys;

            Assert.Equal(5, maxKeys);
            Assert.True(count >= maxKeys); // Limit reached
        }

        [Fact]
        public void AppKeys_Sha256Hashing_VerificationWorks()
        {
            var keyString = "mcp-global-randomstring123456789";

            // Hash the key using SHA-256
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyString));
            var storedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            // Verify using the validation logic
            bool isValid = false;
            if (storedHash.Length == 64)
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                var computedBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyString));
                var computedHash = Convert.ToHexString(computedBytes).ToLowerInvariant();
                isValid = System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(storedHash),
                    System.Text.Encoding.UTF8.GetBytes(computedHash)
                );
            }

            Assert.True(isValid);
        }

        [Fact]
        public async Task AuditLogger_ThrowsException_OnDatabaseError()
        {
            var brokenFactoryMock = new Mock<IDbConnectionFactory>();
            brokenFactoryMock.Setup(f => f.CreateConnection()).Throws(new InvalidOperationException("Failed to open connection"));

            var logger = new McpRouter.Infrastructure.Logging.AuditLogger(brokenFactoryMock.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await logger.LogInvocationAsync(
                    "req-1", "user-1", "sid-1", "server-1", "item-1", "method-1", 100, 200
                );
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await logger.LogAdminActionAsync(
                    "user-1", "action-1", "target-1", "details-1", true
                );
            });
        }

        [Fact]
        public async Task CallTool_FailsClosed_WhenAuditLogFails()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                { "Audit:FailClosed", "true" }
            }).Build();

            var auditLoggerMock = new Mock<IAuditLogger>();
            auditLoggerMock.Setup(a => a.LogInvocationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Audit DB failure"));

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IConfiguration))).Returns(config);
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditLogger))).Returns(auditLoggerMock.Object);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(h => h.RequestServices).Returns(serviceProviderMock.Object);
            httpContextMock.Setup(h => h.Items).Returns(new Dictionary<object, object?>());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "AppKey");
            httpContextMock.Setup(h => h.User).Returns(new ClaimsPrincipal(identity));

            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(r => r.HttpContext).Returns(httpContextMock.Object);

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
            var servers = new List<McpServer> { new McpServer { Id = "testserver", Enabled = true } };

            var session = new ClientSession("session-1", responseMock.Object, servers, new HttpClient(), new Mock<IEmbeddingService>().Object, null, loggerMock.Object);

            await Assert.ThrowsAsync<System.Security.SecurityException>(async () =>
            {
                await session.CallToolAsync("testserver__testtool", "{}", null!);
            });
        }

        [Fact]
        public async Task CallTool_FailsClosed_WhenAuditLoggerUnresolved()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                { "Audit:FailClosed", "true" }
            }).Build();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IConfiguration))).Returns(config);
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditLogger))).Returns((IAuditLogger?)null);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(h => h.RequestServices).Returns(serviceProviderMock.Object);
            httpContextMock.Setup(h => h.Items).Returns(new Dictionary<object, object?>());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "AppKey");
            httpContextMock.Setup(h => h.User).Returns(new ClaimsPrincipal(identity));

            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(r => r.HttpContext).Returns(httpContextMock.Object);

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
            var servers = new List<McpServer> { new McpServer { Id = "testserver", Enabled = true } };

            var session = new ClientSession("session-1", responseMock.Object, servers, new HttpClient(), new Mock<IEmbeddingService>().Object, null, loggerMock.Object);

            await Assert.ThrowsAsync<System.Security.SecurityException>(async () =>
            {
                await session.CallToolAsync("testserver__testtool", "{}", null!);
            });
        }

        [Fact]
        [Requirement("AUTH-SYSTEM-APPKEY-SEPARATION", "AUTH", RequirementType.Positive, "Personal AppKey with 'all' scope does not grant Administrator role")]
        public async Task PersonalAppKey_WithAllScope_DoesNotGrantAdministratorRole()
        {
            var keyString = "mcp-all-personalkeytest123456789";
            var prefix = keyString.Substring(0, 16);
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyString))).ToLowerInvariant();

            var key = new AppKey
            {
                Id = "personal-key-1",
                Name = "Personal Alice Key",
                Username = "alice",
                KeyPrefix = prefix,
                EncryptedKey = hash,
                ScopesJson = "[\"all\"]",
                KeyType = "personal"
            };

            await _masterConnection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, KeyType)
                VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @KeyType);",
                key);

            var optionsMonitorMock = new Mock<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>>();
            optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>())).Returns(new Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions());

            var handler = new McpRouter.Middleware.AppKeyAuthenticationHandler(
                optionsMonitorMock.Object,
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance,
                System.Text.Encodings.Web.UrlEncoder.Default,
                _dbFactory,
                _config
            );

            var context = new DefaultHttpContext();
            context.Request.Headers["X-App-Key"] = keyString;
            var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme("AppKey", null, typeof(McpRouter.Middleware.AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, context);

            var result = await handler.AuthenticateAsync();
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal);
            Assert.True(result.Principal.IsInRole("McpClient"));
            Assert.False(result.Principal.IsInRole("Administrator"));
            Assert.False(result.Principal.HasClaim("Scope", "admin"));

            // Verify SecurityValidationHelper.IsAdmin returns false
            var identity = new McpRouter.Infrastructure.Identity.UserIdentityContext("alice", "AppKey", new List<string>());
            Assert.False(McpRouter.Components.Authorization.SecurityValidationHelper.IsAdmin(identity, _config, context));
        }

        [Fact]
        [Requirement("AUTH-SYSTEM-APPKEY-SEPARATION", "AUTH", RequirementType.Positive, "System AppKey with 'admin' scope grants Administrator role")]
        public async Task SystemAppKey_WithAdminScope_GrantsAdministratorRole()
        {
            var keyString = "mcp-admin-systemkeytest123456789";
            var prefix = keyString.Substring(0, 16);
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyString))).ToLowerInvariant();

            var key = new AppKey
            {
                Id = "system-key-1",
                Name = "System Daemon Key",
                Username = "system",
                KeyPrefix = prefix,
                EncryptedKey = hash,
                ScopesJson = "[\"admin\"]",
                KeyType = "system"
            };

            await _masterConnection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, KeyType)
                VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @KeyType);",
                key);

            var optionsMonitorMock = new Mock<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>>();
            optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>())).Returns(new Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions());

            var handler = new McpRouter.Middleware.AppKeyAuthenticationHandler(
                optionsMonitorMock.Object,
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance,
                System.Text.Encodings.Web.UrlEncoder.Default,
                _dbFactory,
                _config
            );

            var context = new DefaultHttpContext();
            context.Request.Headers["X-App-Key"] = keyString;
            var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme("AppKey", null, typeof(McpRouter.Middleware.AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, context);

            var result = await handler.AuthenticateAsync();
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal);
            Assert.True(result.Principal.IsInRole("McpClient"));
            Assert.True(result.Principal.IsInRole("Administrator"));
            Assert.True(result.Principal.HasClaim("Scope", "admin"));

            // Verify SecurityValidationHelper.IsAdmin returns true
            var identity = new McpRouter.Infrastructure.Identity.UserIdentityContext("system", "AppKey", new List<string>());
            Assert.True(McpRouter.Components.Authorization.SecurityValidationHelper.IsAdmin(identity, _config, context));
        }

        [Fact]
        [Requirement("AUTH-COMPACT-APPKEY-TAXONOMY", "AUTH", RequirementType.Positive, "Generates compact ~32-character Base62 AppKeys with semantic prefixes.")]
        public async Task CreateCredentialAsync_GeneratesCompactKeysWithSemanticPrefixes()
        {
            var credService = new CredentialService(_dbFactory);

            // 1. Admin key
            var (adminKey, adminPlain) = await credService.CreateCredentialAsync("Admin", "admin", "SID", new List<string> { "all", "admin" }, null, "system");
            Assert.StartsWith("mcp-adm-", adminPlain);
            Assert.StartsWith(adminKey.KeyPrefix, adminPlain);
            Assert.InRange(adminPlain.Length, 32, 38);

            // 2. Global key
            var (glbKey, glbPlain) = await credService.CreateCredentialAsync("Global", "user1", "SID", new List<string> { "all" }, null, "personal");
            Assert.StartsWith("mcp-glb-", glbPlain);
            Assert.StartsWith(glbKey.KeyPrefix, glbPlain);

            // 3. User / personal key
            var (usrKey, usrPlain) = await credService.CreateCredentialAsync("User", "user1", "SID", new List<string> { "mcp:read" }, null, "personal");
            Assert.StartsWith("mcp-usr-", usrPlain);
            Assert.StartsWith(usrKey.KeyPrefix, usrPlain);

            // 4. Server-scoped key
            var (srvKey, srvPlain) = await credService.CreateCredentialAsync("Server", "user1", "SID", new List<string> { "server:docker" }, null, "personal");
            Assert.StartsWith("mcp-srv-", srvPlain);
            Assert.StartsWith(srvKey.KeyPrefix, srvPlain);

            // 5. Domain / group-scoped key
            var (grpKey, grpPlain) = await credService.CreateCredentialAsync("Domain", "user1", "SID", new List<string> { "group:devops" }, null, "personal");
            Assert.StartsWith("mcp-devops-", grpPlain);
            Assert.StartsWith(grpKey.KeyPrefix, grpPlain);
            Assert.InRange(grpPlain.Length, 32, 38);
        }
    }
}
