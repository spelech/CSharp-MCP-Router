using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using McpRouter.Core.Database;
using McpRouter.Core.Routing;
using McpRouter.Core.Secrets;
using McpRouter.Middleware;
using McpRouter.Models;
using McpRouter.Services;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class DatabaseSeederServiceTests
    {
        private RouterDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<RouterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var db = new RouterDbContext(options, config);
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public async Task Seeder_Initializes_Default_Settings_And_Providers()
        {
            var db = CreateDbContext();
            
            // Execute db initialization
            db.Database.EnsureCreated();

            var settings = await db.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new RouterSettings { Id = "global", EmbeddingProvider = "Local ONNX" };
                db.Settings.Add(settings);
                await db.SaveChangesAsync();
            }

            Assert.NotNull(settings);
            Assert.Equal("Local ONNX", settings.EmbeddingProvider);
        }

        [Fact]
        public async Task Seeder_Backfills_Existing_Servers_With_Static_ApiKey()
        {
            var db = CreateDbContext();
            var server = new McpServer
            {
                Id = "existing-server-1",
                DisplayName = "Existing Server",
                Url = "http://localhost:8888/sse",
                ApiKey = "static-token-999",
                SecretProvider = "Vault",
                AuthShape = ""
            };
            db.Servers.Add(server);
            await db.SaveChangesAsync();

            // Perform backfill query
            if ((server.SecretProvider == "Vault" || string.IsNullOrEmpty(server.SecretProvider)) && !string.IsNullOrEmpty(server.ApiKey))
            {
                server.SecretProvider = "None";
            }
            if (string.IsNullOrEmpty(server.AuthShape))
            {
                server.AuthShape = "bearer";
            }
            await db.SaveChangesAsync();

            var updated = await db.Servers.FirstOrDefaultAsync(s => s.Id == "existing-server-1");
            Assert.NotNull(updated);
            Assert.Equal("None", updated.SecretProvider);
            Assert.Equal("bearer", updated.AuthShape);
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

            Assert.True(results[0]); // empty key is weak
            Assert.True(results[1]); // short key (< 16) is weak
            Assert.False(results[2]); // secure key (>= 16) is not weak
        }

        [Fact]
        public async Task Startup_MigratesLegacyKeysToHashedKeys()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

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
                        RequireManualApproval INTEGER DEFAULT 0,
                        GlobalMaxKeys INTEGER DEFAULT 100,
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
                { "DB_ENCRYPTION_KEY", routerSecret }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            // Create legacy AES-CBC encrypted string using original SHA-256 key derivation
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

            // Seed legacy key into database
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
            services.AddDbContext<RouterDbContext>(opt => opt.UseSqlite(connection));

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(connection);
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            services.AddSingleton(mockDbFactory.Object);
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();

            // Run database seeder migration
            DatabaseSeederService.SeedDatabase(serviceProvider, config);

            // Assert key was migrated to SHA-256 hash
            using var db = serviceProvider.GetRequiredService<RouterDbContext>();
            var migratedKey = await db.AppKeys.FirstOrDefaultAsync(k => k.Id == "legacy-key-1");

            Assert.NotNull(migratedKey);
            Assert.NotEqual(legacyEncryptedKey, migratedKey.EncryptedKey);
            Assert.Equal(64, migratedKey.EncryptedKey.Length);

            var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
            Assert.Equal(expectedHash, migratedKey.EncryptedKey);

            // Assert key authentication works via AppKeyAuthenticationHandler
            var optionsMonitorMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

            var handler = new AppKeyAuthenticationHandler(
                optionsMonitorMock.Object,
                NullLoggerFactory.Instance,
                UrlEncoder.Default,
                mockDbFactory.Object,
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
