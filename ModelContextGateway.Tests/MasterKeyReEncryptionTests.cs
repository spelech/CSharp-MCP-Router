using System.Data;
using System.Text.Json;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace McpRouter.Tests
{
    public class MasterKeyReEncryptionTests : IDisposable
    {
        private class NonDisposingConnection : IDbConnection
        {
            private readonly IDbConnection _inner;
            public NonDisposingConnection(IDbConnection inner)
            {
                _inner = inner;
            }

            [System.Diagnostics.CodeAnalysis.AllowNull]
            public string ConnectionString
            {
                get => _inner.ConnectionString ?? string.Empty;
                set => _inner.ConnectionString = value ?? string.Empty;
            }

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

        private readonly string _tempDataDir;
        private readonly SqliteConnection _rawConnection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;
        private readonly DatabaseRepository _repo;

        public MasterKeyReEncryptionTests()
        {
            _tempDataDir = Path.Combine(Path.GetTempPath(), $"mcp_reencrypt_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDataDir);

            DbKeyHelper.ResetCache();

            _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir }
            }).Build();

            _rawConnection = new SqliteConnection($"DataSource=file:mem_reencrypt_{Guid.NewGuid():N}?mode=memory&cache=shared");
            _rawConnection.Open();

            _rawConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS SecretProviders (
                    ProviderName TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    EncryptedConfigJson TEXT,
                    IsEnabled INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS AuthProviderConfigs (
                    ProviderName TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    UserHeader TEXT,
                    GroupsHeader TEXT,
                    EncryptedConfigJson TEXT,
                    IsEnabled INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS UserServerCredentials (
                    Id TEXT PRIMARY KEY,
                    Username TEXT,
                    ServerId TEXT,
                    EncryptedSecretJson TEXT
                );
                CREATE TABLE IF NOT EXISTS Servers (
                    Id TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    Url TEXT,
                    Enabled INTEGER DEFAULT 1,
                    Hidden INTEGER DEFAULT 0,
                    Type TEXT DEFAULT 'sse',
                    SecretProvider TEXT DEFAULT 'None',
                    SecretItemKey TEXT NULL,
                    SecretMount TEXT NULL,
                    SecretPath TEXT NULL,
                    SecretField TEXT NULL,
                    AuthShape TEXT DEFAULT 'bearer',
                    CustomHeaderName TEXT NULL,
                    Categories TEXT DEFAULT '[]',
                    ApiKey TEXT NULL,
                    HeadersJson TEXT NULL,
                    AutoDiscovered INTEGER DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY,
                    DashboardTitle TEXT,
                    DashboardIcon TEXT,
                    EmbeddingProvider TEXT,
                    EmbeddingApiUrl TEXT,
                    EmbeddingApiKey TEXT,
                    EmbeddingApiModel TEXT,
                    EmbeddingModelDir TEXT,
                    GlobalMaxKeys INTEGER,
                    UserMaxKeys INTEGER
                );
            ");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() => new NonDisposingConnection(_rawConnection));
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;

            _repo = new DatabaseRepository(_dbFactory, _config);
        }

        public void Dispose()
        {
            _rawConnection.Dispose();
            DbKeyHelper.ResetCache();
            if (Directory.Exists(_tempDataDir))
            {
                try { Directory.Delete(_tempDataDir, true); } catch { }
            }
        }

        [Fact]
        [Requirement("SEC-MASTERKEY-ATOMIC-REENCRYPTION", "SEC", RequirementType.Positive, "Atomically re-encrypts database credentials when setting a custom master key.")]
        public async Task SetMasterKey_AtomicallyReEncryptsDatabaseCredentials()
        {
            // 1. Establish initial auto-generated master key
            var initialKey = "InitialMasterKey12345678901234567890123456==";
            DbKeyHelper.SetCachedKey(initialKey, MasterKeySource.AutoGenerated);

            // Write initial key to .master.key file
            var keyFilePath = Path.Combine(_tempDataDir, ".master.key");
            File.WriteAllText(keyFilePath, initialKey);

            // 2. Seed SecretProviders, AuthProviders, and UserSecrets with initial key
            var vaultConfigPlain = "{\"address\":\"https://vault.local:8200\",\"token\":\"s.InitialVaultSecretToken999\"}";
            var adConfigPlain = "{\"server\":\"ldaps.corp.local\",\"bindPassword\":\"InitialLdapSecretP@ss999!\"}";
            var userSecretPlain = "{\"apiKey\":\"sk-InitialUserApiKey-12345\"}";

            await _repo.SaveSecretProviderAsync(new SecretProviderDto
            {
                ProviderName = "Vault",
                DisplayName = "HashiCorp Vault",
                ConfigJson = vaultConfigPlain,
                IsEnabled = true
            });

            await _repo.SaveAuthProviderAsync(new AuthProviderDto
            {
                ProviderName = "ActiveDirectory",
                DisplayName = "Active Directory",
                ConfigJson = adConfigPlain,
                IsEnabled = true
            });

            var userSecretStore = new DatabaseUserSecretStore(_repo, _config);
            await userSecretStore.SaveSecretAsync("alice", "docker-server", userSecretPlain);

            // Directly inspect initial raw encrypted ciphertexts in DB
            using (var conn = _dbFactory.CreateConnection())
            {
                var rawVaultEncrypted = await conn.ExecuteScalarAsync<string>("SELECT EncryptedConfigJson FROM SecretProviders WHERE ProviderName = 'Vault';");
                var rawAdEncrypted = await conn.ExecuteScalarAsync<string>("SELECT EncryptedConfigJson FROM AuthProviderConfigs WHERE ProviderName = 'ActiveDirectory';");
                var rawUserSecretEncrypted = await conn.ExecuteScalarAsync<string>("SELECT EncryptedSecretJson FROM UserServerCredentials WHERE Id = 'alice_docker-server';");

                rawVaultEncrypted.Should().NotBeNullOrEmpty();
                rawAdEncrypted.Should().NotBeNullOrEmpty();
                rawUserSecretEncrypted.Should().NotBeNullOrEmpty();

                rawVaultEncrypted.Should().NotContain("InitialVaultSecretToken999");
                rawAdEncrypted.Should().NotContain("InitialLdapSecretP@ss999!");
                rawUserSecretEncrypted.Should().NotContain("sk-InitialUserApiKey-12345");
            }

            // 3. Execute atomic re-encryption to new master key
            var newMasterKey = "CustomConfiguredMasterKey98765432109876543210987==";
            await _repo.ReencryptDatabaseSecretsAsync(newMasterKey);

            // 4. Verify in-memory state & keyfile updated
            DbKeyHelper.ActiveKeySource.Should().Be(MasterKeySource.Configured);
            File.ReadAllText(keyFilePath).Trim().Should().Be(newMasterKey);

            // 5. Inspect database to ensure raw ciphertexts changed and are decryptable with new key
            using (var conn = _dbFactory.CreateConnection())
            {
                var newRawVault = await conn.ExecuteScalarAsync<string>("SELECT EncryptedConfigJson FROM SecretProviders WHERE ProviderName = 'Vault';");
                var newRawAd = await conn.ExecuteScalarAsync<string>("SELECT EncryptedConfigJson FROM AuthProviderConfigs WHERE ProviderName = 'ActiveDirectory';");
                var newRawUser = await conn.ExecuteScalarAsync<string>("SELECT EncryptedSecretJson FROM UserServerCredentials WHERE Id = 'alice_docker-server';");

                // Verify ciphertexts are re-encrypted
                newRawVault.Should().NotBeNullOrEmpty();
                newRawAd.Should().NotBeNullOrEmpty();
                newRawUser.Should().NotBeNullOrEmpty();

                // Verify decryption with old key fails
                SymmetricEncryptionHelper.TryDecryptWithKey(newRawVault!, initialKey, out _).Should().BeFalse();
                SymmetricEncryptionHelper.TryDecryptWithKey(newRawAd!, initialKey, out _).Should().BeFalse();
                SymmetricEncryptionHelper.TryDecryptWithKey(newRawUser!, initialKey, out _).Should().BeFalse();

                // Verify decryption with new key succeeds
                SymmetricEncryptionHelper.TryDecryptWithKey(newRawVault!, newMasterKey, out var decVault).Should().BeTrue();
                decVault.Should().Be(vaultConfigPlain);

                SymmetricEncryptionHelper.TryDecryptWithKey(newRawAd!, newMasterKey, out var decAd).Should().BeTrue();
                decAd.Should().Be(adConfigPlain);

                SymmetricEncryptionHelper.TryDecryptWithKey(newRawUser!, newMasterKey, out var decUser).Should().BeTrue();
                decUser.Should().Be(userSecretPlain);
            }

            // 6. Verify repository and secret store transparently read correctly with active key
            var providers = await _repo.GetSecretProvidersAsync();
            providers.First(p => p.ProviderName == "Vault").ConfigJson.Should().Be(vaultConfigPlain);

            var auths = await _repo.GetAuthProvidersAsync();
            auths.First(p => p.ProviderName == "ActiveDirectory").ConfigJson.Should().Be(adConfigPlain);

            var readSecret = await userSecretStore.GetSecretAsync("alice", "docker-server");
            readSecret.Should().Be(userSecretPlain);
        }

        [Fact]
        [Requirement("SEC-MASTERKEY-ATOMIC-REENCRYPTION", "SEC", RequirementType.Positive, "Rejects master key rotation when key source is external or Vault.")]
        public async Task SetMasterKey_RejectsWhenKeySourceIsExternalOrVault()
        {
            var initialKey = "InitialExternalKey12345678901234567890123==";
            DbKeyHelper.SetCachedKey(initialKey, MasterKeySource.External);

            var newKey = "NewAttemptedKey12345678901234567890123456==";
            var actExternal = async () => await _repo.ReencryptDatabaseSecretsAsync(newKey);
            await actExternal.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*managed externally*");

            DbKeyHelper.SetCachedKey(initialKey, MasterKeySource.Vault);
            var actVault = async () => await _repo.ReencryptDatabaseSecretsAsync(newKey);
            await actVault.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*managed externally*");
        }

        [Fact]
        [Requirement("SEC-MASTERKEY-ATOMIC-REENCRYPTION", "SEC", RequirementType.Positive, "Admin MCP tool manage_system with set_master_key successfully rotates key.")]
        public async Task AdminMcpServer_ManageSystem_SetMasterKey_ReencryptsCleanly()
        {
            var initialKey = "AutoGeneratedKey12345678901234567890123456==";
            DbKeyHelper.SetCachedKey(initialKey, MasterKeySource.AutoGenerated);

            var keyFilePath = Path.Combine(_tempDataDir, ".master.key");
            File.WriteAllText(keyFilePath, initialKey);

            // Seed a provider
            await _repo.SaveSecretProviderAsync(new SecretProviderDto
            {
                ProviderName = "Vault",
                DisplayName = "HashiCorp Vault",
                ConfigJson = "{\"token\":\"s.SuperSecretAdminMcp123\"}",
                IsEnabled = true
            });

            var services = new ServiceCollection();
            services.AddSingleton(_config);
            services.AddSingleton(_dbFactory);
            services.AddLogging();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var mockAudit = new Mock<IAuditLogger>();
            var mockCred = new Mock<ICredentialService>();
            var dynamicEmbedding = new DynamicEmbeddingService(httpClientFactory.CreateClient(), serviceProvider.GetRequiredService<ILoggerFactory>(), serviceProvider);
            var sessionManager = new SessionManager(serviceProvider, httpClientFactory, serviceProvider.GetRequiredService<ILogger<SessionManager>>());
            var healthCheck = new BackendHealthCheckService(serviceProvider, httpClientFactory, sessionManager, NullLogger<BackendHealthCheckService>.Instance);

            var adminMcpServer = new AdminMcpServer(
                _repo,
                _repo,
                _repo,
                _repo,
                _repo,
                _dbFactory,
                mockAudit.Object,
                mockCred.Object,
                healthCheck,
                dynamicEmbedding,
                sessionManager,
                null,
                new HttpClient(),
                _config,
                NullLogger<AdminMcpServer>.Instance,
                _repo
            );

            var newKey = "McpAdminRotatedKey12345678901234567890123==";
            var argsJson = JsonSerializer.SerializeToElement(new
            {
                action = "set_master_key",
                newKey = newKey
            });

            var result = await adminMcpServer.CallToolAsync("manage_system", argsJson, "admin");
            result.Should().NotBeNull();

            DbKeyHelper.ActiveKeySource.Should().Be(MasterKeySource.Configured);
            File.ReadAllText(keyFilePath).Trim().Should().Be(newKey);

            // Verify provider can be read back decrypted
            var providers = await _repo.GetSecretProvidersAsync();
            providers.First(p => p.ProviderName == "Vault").ConfigJson.Should().Be("{\"token\":\"s.SuperSecretAdminMcp123\"}");

            mockAudit.Verify(a => a.LogAdminActionAsync(
                "admin",
                "masterkey.reencrypt",
                "MasterKey",
                It.IsAny<string>(),
                true,
                null
            ), Times.Once);
        }

        [Fact]
        [Requirement("SEC-MASTERKEY-ATOMIC-REENCRYPTION", "SEC", RequirementType.Positive, "Rejects null, empty, or short master keys.")]
        public async Task SetMasterKey_RejectsInvalidOrShortKeys()
        {
            DbKeyHelper.SetCachedKey("InitialKey12345678901234567890123456==", MasterKeySource.AutoGenerated);

            var actNull = async () => await _repo.ReencryptDatabaseSecretsAsync(null!);
            await actNull.Should().ThrowAsync<ArgumentException>();

            var actEmpty = async () => await _repo.ReencryptDatabaseSecretsAsync("   ");
            await actEmpty.Should().ThrowAsync<ArgumentException>();

            var actShort = async () => await _repo.ReencryptDatabaseSecretsAsync("short-key-123");
            await actShort.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*16 characters*");
        }
    }
}
