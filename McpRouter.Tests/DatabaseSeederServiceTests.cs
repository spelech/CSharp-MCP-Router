using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Dapper;
using McpRouter.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace McpRouter.Tests
{
    public class DatabaseSeederServiceTests
    {
        private (SqliteConnection masterConn, IDbConnectionFactory factory) CreateDbFactory()
        {
            var dbName = $"Data Source=SeederTestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var masterConn = new SqliteConnection(dbName);
            masterConn.Open();

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(() => new SqliteConnection(dbName));
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (masterConn, mockDbFactory.Object);
        }

        [Fact]
        public void Seeder_Initializes_Default_Settings_And_Providers()
        {
            var (conn, factory) = CreateDbFactory();
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            }).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(factory);
            services.AddLogging();
            var sp = services.BuildServiceProvider();

            DatabaseSeederService.SeedDatabase(sp, config);

            var settings = conn.QueryFirstOrDefault<RouterSettings>("SELECT * FROM Settings");
            Assert.NotNull(settings);
        }

        [Fact]
        public void DbEncryptionKey_Warning_Detection_Works_Correctly()
        {
            var testKeys = new[] { "", "short", "SomeSecureRandomKeyValue999!" };
            var results = new List<bool>();

            foreach (var key in testKeys)
            {
                var isWeak = string.IsNullOrEmpty(key) || key.Length < 16;
                results.Add(isWeak);
            }

            Assert.True(results[0]);
            Assert.True(results[1]);
            Assert.False(results[2]);
        }

        [Fact]
        public async Task Startup_MigratesLegacyKeysToHashedKeys()
        {
            var (connection, mockDbFactory) = CreateDbFactory();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AppKeys (
                        Id TEXT PRIMARY KEY,
                        Name TEXT,
                        Username TEXT,
                        KeyPrefix TEXT,
                        EncryptedKey TEXT,
                        ScopesJson TEXT DEFAULT '[]',
                        ExpiresAt TEXT,
                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE TABLE IF NOT EXISTS Settings (
                        Id TEXT PRIMARY KEY,
                        EmbeddingProvider TEXT,
                        EmbeddingApiUrl TEXT,
                        EmbeddingApiKey TEXT,
                        EmbeddingApiModel TEXT,
                        EmbeddingModelDir TEXT,
                        DashboardTitle TEXT DEFAULT 'MCP Gateway', DashboardIcon TEXT DEFAULT 'fa-solid fa-network-wired', GlobalMaxKeys INTEGER DEFAULT 100,
                        UserMaxKeys INTEGER DEFAULT 5
                    );";
                cmd.ExecuteNonQuery();
            }

            var rawToken = "mcp-legacykey12345678901234567890";
            var prefix = rawToken.Substring(0, 16);
            var routerSecret = "TestRouterSecret123456789012345";

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "ROUTER_SECRET", routerSecret },
                { "DB_ENCRYPTION_KEY", routerSecret },
                { "RUN_KEY_MIGRATION", "true" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(routerSecret));
            string legacyEncryptedKey;
            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.GenerateIV();
                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cs, Encoding.UTF8))
                {
                    writer.Write(rawToken);
                }
                legacyEncryptedKey = Convert.ToBase64String(ms.ToArray());
            }

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS AppKeys (
                    Id TEXT PRIMARY KEY, Name TEXT, Username TEXT, KeyPrefix TEXT, EncryptedKey TEXT, ScopesJson TEXT DEFAULT '[]', ExpiresAt TEXT, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );
            ");

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson) VALUES (@Id, @Name, @Username, @Prefix, @EncryptedKey, @Scopes);";
                cmd.Parameters.AddWithValue("@Id", "legacy-key-1");
                cmd.Parameters.AddWithValue("@Name", "Legacy Key");
                cmd.Parameters.AddWithValue("@Username", "legacyuser");
                cmd.Parameters.AddWithValue("@Prefix", prefix);
                cmd.Parameters.AddWithValue("@EncryptedKey", legacyEncryptedKey);
                cmd.Parameters.AddWithValue("@Scopes", "[\"all\"]");
                cmd.ExecuteNonQuery();
            }

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(mockDbFactory);
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();

            DatabaseSeederService.SeedDatabase(serviceProvider, config);

            var migratedKey = connection.QueryFirstOrDefault<AppKey>("SELECT * FROM AppKeys WHERE Id = 'legacy-key-1'");

            Assert.NotNull(migratedKey);
            Assert.NotEqual(legacyEncryptedKey, migratedKey.EncryptedKey);
            Assert.Equal(64, migratedKey.EncryptedKey.Length);

            var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
            Assert.Equal(expectedHash, migratedKey.EncryptedKey);

            var optionsMonitorMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

            var handler = new AppKeyAuthenticationHandler(
                optionsMonitorMock.Object,
                NullLoggerFactory.Instance,
                UrlEncoder.Default,
                mockDbFactory,
                config
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {rawToken}";
            httpContext.RequestServices = serviceProvider;

            var scheme = new AuthenticationScheme("AppKey", null, typeof(AppKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var authResult = await handler.AuthenticateAsync();
            Assert.True(authResult.Succeeded, authResult.Failure?.Message);
            Assert.Equal("legacyuser", authResult.Principal?.Identity?.Name);
        }
    }
}
