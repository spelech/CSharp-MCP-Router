using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Moq;

namespace ModelContextGateway.Tests
{
    public class OAuthClientRepositoryTests : IDisposable
    {
        private class NonDisposingConnection : IDbConnection
        {
            private readonly IDbConnection _inner;
            public NonDisposingConnection(IDbConnection inner) => _inner = inner;
            [System.Diagnostics.CodeAnalysis.AllowNull]
            public string ConnectionString { get => _inner.ConnectionString ?? string.Empty; set => _inner.ConnectionString = value ?? string.Empty; }
            public int ConnectionTimeout => _inner.ConnectionTimeout;
            public string Database => _inner.Database;
            public ConnectionState State => _inner.State;
            public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
            public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
            public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
            public void Close() { }
            public IDbCommand CreateCommand() => _inner.CreateCommand();
            public void Dispose() { }
            public void Open()
            {
                if (_inner.State != ConnectionState.Open)
                {
                    _inner.Open();
                }
            }
        }

        private readonly SqliteConnection _rawConnection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IOAuthClientRepository _repo;

        public OAuthClientRepositoryTests()
        {
            _rawConnection = new SqliteConnection($"DataSource=file:mem_oauth_{Guid.NewGuid():N}?mode=memory&cache=shared");
            _rawConnection.Open();

            _rawConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS OAuthClients (
                    ClientId TEXT PRIMARY KEY,
                    ClientSecretHash TEXT DEFAULT '',
                    ClientName TEXT NOT NULL,
                    ClientType TEXT DEFAULT 'confidential',
                    RedirectUrisJson TEXT DEFAULT '[]',
                    GrantTypesJson TEXT DEFAULT '[]',
                    ScopesJson TEXT DEFAULT '[]',
                    OwnerSid TEXT DEFAULT '',
                    CreatedBy TEXT DEFAULT '',
                    ExpiresAt TEXT NULL,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() => new NonDisposingConnection(_rawConnection));
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;

            _repo = new DatabaseRepository(_dbFactory);
        }

        public void Dispose()
        {
            _rawConnection.Dispose();
        }

        [Fact]
        [Requirement("DB-07", "DB", RequirementType.Positive, "OAuthClient repository handles CRUD operations")]
        public async Task SaveAndGetOAuthClientById_Success()
        {
            var now = DateTime.UtcNow;
            var client = new OAuthClient
            {
                ClientId = "client-app-1",
                ClientSecretHash = "hash123",
                ClientName = "Test Application",
                ClientType = "confidential",
                RedirectUrisJson = "[\"https://app.example.com/callback\"]",
                GrantTypesJson = "[\"authorization_code\",\"refresh_token\"]",
                ScopesJson = "[\"openid\",\"tools:read\"]",
                OwnerSid = "user-sid-456",
                CreatedBy = "alice",
                CreatedAt = now,
                ExpiresAt = now.AddDays(30)
            };

            await _repo.SaveOAuthClientAsync(client);

            var retrieved = await _repo.GetOAuthClientByIdAsync("client-app-1");

            Assert.NotNull(retrieved);
            Assert.Equal("client-app-1", retrieved.ClientId);
            Assert.Equal("hash123", retrieved.ClientSecretHash);
            Assert.Equal("Test Application", retrieved.ClientName);
            Assert.Equal("confidential", retrieved.ClientType);
            Assert.Equal("[\"https://app.example.com/callback\"]", retrieved.RedirectUrisJson);
            Assert.Equal("[\"authorization_code\",\"refresh_token\"]", retrieved.GrantTypesJson);
            Assert.Equal("[\"openid\",\"tools:read\"]", retrieved.ScopesJson);
            Assert.Equal("user-sid-456", retrieved.OwnerSid);
            Assert.Equal("alice", retrieved.CreatedBy);
            Assert.NotNull(retrieved.ExpiresAt);
        }

        [Fact]
        [Requirement("DB-07", "DB", RequirementType.Positive, "OAuthClient repository handles CRUD operations")]
        public async Task SaveOAuthClient_UpdateExisting_Success()
        {
            var client = new OAuthClient
            {
                ClientId = "client-app-update",
                ClientSecretHash = "secret-hash-1",
                ClientName = "Initial Name",
                ClientType = "confidential",
                RedirectUrisJson = "[\"https://init.example.com\"]",
                GrantTypesJson = "[\"authorization_code\"]",
                ScopesJson = "[\"openid\"]",
                OwnerSid = "sid-1",
                CreatedBy = "bob",
                ExpiresAt = DateTime.UtcNow.AddDays(10)
            };

            await _repo.SaveOAuthClientAsync(client);

            // Update fields
            client.ClientName = "Updated Name";
            client.ClientType = "public";
            client.ClientSecretHash = "";
            client.RedirectUrisJson = "[\"https://updated.example.com\"]";
            client.GrantTypesJson = "[\"authorization_code\",\"refresh_token\"]";
            client.ScopesJson = "[\"openid\",\"profile\"]";
            client.ExpiresAt = null;

            await _repo.SaveOAuthClientAsync(client);

            var retrieved = await _repo.GetOAuthClientByIdAsync("client-app-update");
            Assert.NotNull(retrieved);
            Assert.Equal("Updated Name", retrieved.ClientName);
            Assert.Equal("public", retrieved.ClientType);
            Assert.Equal("", retrieved.ClientSecretHash);
            Assert.Equal("[\"https://updated.example.com\"]", retrieved.RedirectUrisJson);
            Assert.Equal("[\"authorization_code\",\"refresh_token\"]", retrieved.GrantTypesJson);
            Assert.Equal("[\"openid\",\"profile\"]", retrieved.ScopesJson);
            Assert.Null(retrieved.ExpiresAt);
        }

        [Fact]
        [Requirement("DB-07", "DB", RequirementType.Positive, "OAuthClient repository handles CRUD operations")]
        public async Task GetOAuthClients_ReturnsAllClientsOrderedByCreatedAt()
        {
            var c1 = new OAuthClient
            {
                ClientId = "client-1",
                ClientName = "App 1",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            var c2 = new OAuthClient
            {
                ClientId = "client-2",
                ClientName = "App 2",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.SaveOAuthClientAsync(c1);
            await _repo.SaveOAuthClientAsync(c2);

            var all = (await _repo.GetOAuthClientsAsync()).ToList();

            Assert.Equal(2, all.Count);
            Assert.Equal("client-2", all[0].ClientId);
            Assert.Equal("client-1", all[1].ClientId);
        }

        [Fact]
        [Requirement("DB-07", "DB", RequirementType.Positive, "OAuthClient repository handles CRUD operations")]
        public async Task DeleteOAuthClient_ExistingClient_ReturnsTrueAndRemovesClient()
        {
            var client = new OAuthClient
            {
                ClientId = "client-delete-me",
                ClientName = "To Delete"
            };

            await _repo.SaveOAuthClientAsync(client);
            var saved = await _repo.GetOAuthClientByIdAsync("client-delete-me");
            Assert.NotNull(saved);

            var deleted = await _repo.DeleteOAuthClientAsync("client-delete-me");
            Assert.True(deleted);

            var lookupAfterDelete = await _repo.GetOAuthClientByIdAsync("client-delete-me");
            Assert.Null(lookupAfterDelete);
        }

        [Fact]
        [Requirement("DB-07", "DB", RequirementType.Positive, "OAuthClient repository handles CRUD operations")]
        public async Task DeleteOAuthClient_NonExistentClient_ReturnsFalse()
        {
            var deleted = await _repo.DeleteOAuthClientAsync("nonexistent-client");
            Assert.False(deleted);
        }

        [Fact]
        [Requirement("DB-07", "DB", RequirementType.Positive, "OAuthClient repository handles CRUD operations")]
        public async Task GetOAuthClientById_NonExistentClient_ReturnsNull()
        {
            var result = await _repo.GetOAuthClientByIdAsync("nonexistent-client");
            Assert.Null(result);
        }

        [Fact]
        [Requirement("AUTH-118", "AUTH", RequirementType.Positive, "FindDcrClientAsync resolves existing DCR client matching client name and type.")]
        public async Task FindDcrClient_ReturnsMatchingClient()
        {
            var client = new OAuthClient
            {
                ClientId = "dcr-client-find",
                ClientName = "Google Antigravity",
                ClientType = "public",
                CreatedBy = "dcr",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.SaveOAuthClientAsync(client);

            var found = await _repo.FindDcrClientAsync("Google Antigravity", "public");
            Assert.NotNull(found);
            Assert.Equal("dcr-client-find", found.ClientId);

            var notFound = await _repo.FindDcrClientAsync("NonExistentApp", "public");
            Assert.Null(notFound);
        }

        [Fact]
        [Requirement("AUTH-119", "AUTH", RequirementType.Positive, "CleanupDcrClientsAsync prunes duplicate and expired dynamic client registrations across all database providers.")]
        public async Task CleanupDcrClients_PrunesDuplicateRegistrations_AndExpiredClients()
        {
            // Seed 3 duplicate DCR entries for "Google Antigravity"
            var c1 = new OAuthClient
            {
                ClientId = "dcr-old-1",
                ClientName = "Google Antigravity",
                ClientType = "public",
                CreatedBy = "dcr",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            };
            var c2 = new OAuthClient
            {
                ClientId = "dcr-old-2",
                ClientName = "Google Antigravity",
                ClientType = "public",
                CreatedBy = "dcr",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };
            var c3 = new OAuthClient
            {
                ClientId = "dcr-latest",
                ClientName = "Google Antigravity",
                ClientType = "public",
                CreatedBy = "dcr",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            // Seed an expired DCR client
            var expired = new OAuthClient
            {
                ClientId = "dcr-expired",
                ClientName = "Ephemeral Bot",
                ClientType = "confidential",
                CreatedBy = "dcr",
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            // Seed a manual admin client (should NOT be pruned)
            var manual = new OAuthClient
            {
                ClientId = "manual-client",
                ClientName = "Admin Integration",
                ClientType = "confidential",
                CreatedBy = "admin",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            await _repo.SaveOAuthClientAsync(c1);
            await _repo.SaveOAuthClientAsync(c2);
            await _repo.SaveOAuthClientAsync(c3);
            await _repo.SaveOAuthClientAsync(expired);
            await _repo.SaveOAuthClientAsync(manual);

            var cleaned = await _repo.CleanupDcrClientsAsync(30);
            Assert.True(cleaned >= 3); // 2 duplicates of Google Antigravity + 1 expired client

            // Verify newest Google Antigravity is kept
            var latest = await _repo.GetOAuthClientByIdAsync("dcr-latest");
            Assert.NotNull(latest);

            // Verify older duplicates are deleted
            Assert.Null(await _repo.GetOAuthClientByIdAsync("dcr-old-1"));
            Assert.Null(await _repo.GetOAuthClientByIdAsync("dcr-old-2"));

            // Verify expired client is deleted
            Assert.Null(await _repo.GetOAuthClientByIdAsync("dcr-expired"));

            // Verify manual admin client is preserved
            Assert.NotNull(await _repo.GetOAuthClientByIdAsync("manual-client"));
        }
    }
}
