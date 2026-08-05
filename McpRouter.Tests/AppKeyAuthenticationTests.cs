using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Core.Database;
using McpRouter.Core.Secrets;
using McpRouter.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class AppKeyAuthenticationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;

        public AppKeyAuthenticationTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // Create required tables for testing
            _connection.Execute(@"
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
                );
                CREATE TABLE IF NOT EXISTS AppKeys (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Username TEXT,
                    KeyPrefix TEXT,
                    EncryptedKey TEXT,
                    ScopesJson TEXT DEFAULT '[]',
                    ExpiresAt TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );");

            // Seed default settings row
            _connection.Execute("INSERT INTO Settings (Id, GlobalMaxKeys, UserMaxKeys) VALUES ('default', 100, 5);");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(_connection);
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["DB_ENCRYPTION_KEY"]).Returns("SuperSecureDatabaseKey123!");
            configMock.Setup(c => c["ROUTER_SECRET"]).Returns("SuperSecretRouterToken456!");
            _config = configMock.Object;
        }

        public void Dispose()
        {
            _connection.Dispose();
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

            await _connection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson)
                VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson);",
                key);

            // Lookup by prefix
            var retrieved = await _connection.QueryFirstOrDefaultAsync<AppKey>(
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

            await _connection.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt)
                VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt);",
                expiredKey);

            var retrieved = await _connection.QueryFirstOrDefaultAsync<AppKey>(
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
                await _connection.ExecuteAsync(@"
                    INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson)
                    VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson);",
                    key);
            }

            // Count Alice's active keys
            int count = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AppKeys WHERE Username = @Username;",
                new { Username = "alice" });

            Assert.Equal(5, count);

            // Fetch limits setting using RouterSettings typed model
            var settings = await _connection.QueryFirstOrDefaultAsync<RouterSettings>("SELECT * FROM Settings LIMIT 1;");
            Assert.NotNull(settings);
            int maxKeys = settings.UserMaxKeys;

            Assert.Equal(5, maxKeys);
            Assert.True(count >= maxKeys); // Limit reached
        }
    }
}
