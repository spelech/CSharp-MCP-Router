using System.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using VaultSharp;

namespace McpRouter.Tests
{
    public class VaultAppRoleAndRenewalTests
    {
        /// <summary>
        /// Verifies that VaultSecretRetriever authenticates with HashiCorp Vault using AppRole credentials.
        /// </summary>
        [Fact]
        [Requirement("SEC-01", "VaultSecretRetriever authenticates with HashiCorp Vault using AppRole RoleID and SecretID credentials", Type = RequirementType.Positive, Category = "SEC")]
        public async Task EnsureVaultClientAsync_CreatesClient_WithAppRoleCredentials()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Vault:Address", "https://vault.corp.local:8200" },
                { "Vault:RoleId", "approle-role-123" },
                { "Vault:SecretId", "approle-secret-456" }
            }).Build();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var retriever = new VaultSecretRetriever(config, cache);

            var client = await retriever.EnsureVaultClientAsync();
            Assert.NotNull(client);
        }

        /// <summary>
        /// Verifies that VaultSecretRetriever loads AppRole credentials dynamically from repository.
        /// </summary>
        [Fact]
        [Requirement("SEC-01", "VaultSecretRetriever loads AppRole credentials dynamically from persisted secret provider database repository", Type = RequirementType.Positive, Category = "SEC")]
        public async Task EnsureVaultClientAsync_LoadsFromSecretRepo_WhenConfigJsonHasAppRole()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var mockRepo = new Mock<ISecretProviderRepository>();
            mockRepo.Setup(r => r.GetSecretProvidersAsync()).ReturnsAsync(new List<SecretProviderDto>
            {
                new SecretProviderDto
                {
                    ProviderName = "Vault",
                    IsEnabled = true,
                    ConfigJson = "{\"address\":\"https://vault-db.local:8200\",\"roleId\":\"db-role-id\",\"secretId\":\"db-secret-id\",\"mountPath\":\"custom-secret\"}"
                }
            });

            var retriever = new VaultSecretRetriever(config, cache, mockRepo.Object);
            var client = await retriever.EnsureVaultClientAsync();
            Assert.NotNull(client);
        }

        /// <summary>
        /// Ensures Vault client returns null when Vault provider is disabled in repository.
        /// </summary>
        [Fact]
        [Requirement("GUARD-02", "Vault client returns null and disables secret fetching when Vault provider is marked disabled", Type = RequirementType.Negative, Category = "GUARD")]
        public async Task EnsureVaultClientAsync_ReturnsNull_WhenVaultProviderDisabledInRepo()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var mockRepo = new Mock<ISecretProviderRepository>();
            mockRepo.Setup(r => r.GetSecretProvidersAsync()).ReturnsAsync(new List<SecretProviderDto>
            {
                new SecretProviderDto
                {
                    ProviderName = "Vault",
                    IsEnabled = false,
                    ConfigJson = "{\"address\":\"https://vault-db.local:8200\",\"token\":\"some-token\"}"
                }
            });

            var retriever = new VaultSecretRetriever(config, cache, mockRepo.Object);
            var client = await retriever.EnsureVaultClientAsync();
            Assert.Null(client);
        }

        /// <summary>
        /// Verifies that reloading config flushes cached Vault client and forces recreation.
        /// </summary>
        [Fact]
        [Requirement("SEC-01", "Vault client configuration reload flushes cached tokens and recreates authenticated client", Type = RequirementType.Positive, Category = "SEC")]
        public async Task ReloadConfigAsync_ClearsClient_ForcesRecreation()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Vault:Address", "https://vault.local:8200" },
                { "Vault:Token", "test-token" }
            }).Build();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var retriever = new VaultSecretRetriever(config, cache);

            var client1 = await retriever.EnsureVaultClientAsync();
            Assert.NotNull(client1);

            await retriever.ReloadConfigAsync();

            var client2 = await retriever.EnsureVaultClientAsync();
            Assert.NotNull(client2);
            Assert.NotSame(client1, client2);
        }

        /// <summary>
        /// Ensures Vault secret retrieval failures throw SecurityException and fail closed.
        /// </summary>
        [Fact]
        [Requirement("GUARD-02", "Vault secret retrieval failures throw SecurityException and fail closed", Type = RequirementType.Negative, Category = "GUARD")]
        public async Task GetSecretAsync_ThrowsSecurityException_OnVaultException()
        {
            var config = new ConfigurationBuilder().Build();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var mockVaultClient = new Mock<IVaultClient>();
            var retriever = new VaultSecretRetriever(config, cache, () => mockVaultClient.Object);

            await Assert.ThrowsAsync<SecurityException>(() =>
                retriever.GetSecretAsync("secret:services/api", "key"));
        }
    }
}
