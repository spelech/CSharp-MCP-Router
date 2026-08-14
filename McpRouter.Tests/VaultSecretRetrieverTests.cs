using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using McpRouter.Core.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace McpRouter.Tests
{
    public class VaultSecretRetrieverTests
    {
        [Fact]
        public void ProviderName_ReturnsHashiCorpVault()
        {
            var retriever = new VaultSecretRetriever(new ConfigurationBuilder().Build(), new MemoryCache(new MemoryCacheOptions()));
            Assert.Equal("HashiCorpVault", retriever.ProviderName);
        }

        [Fact]
        public async Task EnsureVaultClientAsync_ThrowsArgumentException_WhenAddressInvalidScheme()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Vault:Address", "ftp://vault.local:8200" },
                { "Vault:RoleId", "role-1" },
                { "Vault:SecretId", "secret-1" }
            }).Build();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var retriever = new VaultSecretRetriever(config, cache);

            await Assert.ThrowsAsync<ArgumentException>(() => retriever.EnsureVaultClientAsync());
        }

        [Fact]
        public async Task EnsureVaultClientAsync_ReturnsNull_WhenCredentialsMissing()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Vault:Address", "https://vault.local:8200" }
            }).Build();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var retriever = new VaultSecretRetriever(config, cache);

            var client = await retriever.EnsureVaultClientAsync();
            Assert.Null(client);

            var emptyConfig = new ConfigurationBuilder().Build();
            var emptyRetriever = new VaultSecretRetriever(emptyConfig, cache);
            Assert.Null(await emptyRetriever.EnsureVaultClientAsync());
        }

        [Fact]
        public async Task EnsureVaultClientAsync_CreatesClient_WhenValidConfig()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Vault:Address", "https://vault.local:8200" },
                { "Vault:RoleId", "test-role-id" },
                { "Vault:SecretId", "test-secret-id" }
            }).Build();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var retriever = new VaultSecretRetriever(config, cache);

            var client = await retriever.EnsureVaultClientAsync();
            Assert.NotNull(client);

            // Calling again returns cached instance
            var client2 = await retriever.EnsureVaultClientAsync();
            Assert.Same(client, client2);
        }

        [Fact]
        public async Task GetSecretAsync_ReturnsCachedValue_WhenPresent()
        {
            var config = new ConfigurationBuilder().Build();
            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set("vault:secret:my-app:api_key", "CachedSecret123");

            var retriever = new VaultSecretRetriever(config, cache);
            var result = await retriever.GetSecretAsync("secret:my-app", "api_key");

            Assert.Equal("CachedSecret123", result);
        }

        [Fact]
        public async Task GetSecretAsync_ReturnsNull_WhenClientIsNull()
        {
            var config = new ConfigurationBuilder().Build();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var retriever = new VaultSecretRetriever(config, cache);
            var result = await retriever.GetSecretAsync("secret:my-app", "api_key");
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSecretAsync_UsesCustomVaultClientFactory()
        {
            var config = new ConfigurationBuilder().Build();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var mockVaultClient = new Mock<IVaultClient>();

            var retriever = new VaultSecretRetriever(config, cache, () => mockVaultClient.Object);

            await Assert.ThrowsAsync<SecurityException>(() => retriever.GetSecretAsync("secret:my-app", "api_key"));
        }
    }
}
