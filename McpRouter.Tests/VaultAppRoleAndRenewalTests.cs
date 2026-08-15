using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using McpRouter.Components.Providers;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using Xunit;

namespace McpRouter.Tests
{
    public class VaultAppRoleAndRenewalTests
    {
        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
