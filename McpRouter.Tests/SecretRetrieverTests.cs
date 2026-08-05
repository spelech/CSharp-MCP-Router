using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpRouter.Core.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class SecretRetrieverTests
    {
        [Fact]
        public async Task CompositeSecretRetriever_Returns_First_Available_Secret()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var inMemoryConfig = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            var vaultRetriever = new VaultSecretRetriever(config, memoryCache);
            var winRegRetriever = new WindowsRegistrySecretRetriever();

            var composite = new CompositeSecretRetriever(new ISecretRetriever[] { vaultRetriever, winRegRetriever });

            // On non-configured environments, should safely return null without throwing exceptions
            var secret = await composite.GetSecretAsync("SOFTWARE\\NonExistentPath", "ApiKey");
            Assert.Null(secret);
        }

        [Fact]
        public async Task EnvironmentSecretRetriever_Resolves_Secret_Successfully()
        {
            var key = "TEST_API_KEY_ENV";
            var expectedVal = "env_secret_999";
            System.Environment.SetEnvironmentVariable(key, expectedVal);

            try
            {
                var retriever = new EnvironmentSecretRetriever();
                var secret = await retriever.GetSecretAsync("non-existent-path", key);
                Assert.Equal(expectedVal, secret);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(key, null);
            }
        }

        [Fact]
        public void VaultSecretRetriever_Constructor_Suppresses_Exceptions_On_Invalid_Url()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            // An invalid url that would normally fail VaultClientSettings instantiation
            var configDict = new Dictionary<string, string?>
            {
                { "Vault:Address", "invalid_address_that_is_not_uri" },
                { "Vault:RoleId", "some-role" },
                { "Vault:SecretId", "some-secret" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            // The constructor must suppress any URI parsing exception safely
            var retriever = new VaultSecretRetriever(config, memoryCache);
            Assert.NotNull(retriever);
        }

        [Fact]
        public async Task CompositeSecretRetriever_Caches_With_TTL()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockRetriever = new Mock<ISecretRetriever>();
            mockRetriever.Setup(r => r.ProviderName).Returns("Mock");

            int fetchCount = 0;
            mockRetriever.Setup(r => r.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(() => {
                    fetchCount++;
                    return "rotated_secret_" + fetchCount;
                });

            var composite = new CompositeSecretRetriever(new[] { mockRetriever.Object }, memoryCache);

            // First call - miss, should fetch
            var secret1 = await composite.GetSecretAsync("path", "key");
            Assert.Equal("rotated_secret_1", secret1);
            Assert.Equal(1, fetchCount);

            // Second call - hit, should return cached
            var secret2 = await composite.GetSecretAsync("path", "key");
            Assert.Equal("rotated_secret_1", secret2);
            Assert.Equal(1, fetchCount); // Fetch count still 1!

            // Force clear/expire cache to simulate TTL rotation
            memoryCache.Remove("secret_cache:path:key");

            // Third call - miss, should fetch again reflecting rotated secret
            var secret3 = await composite.GetSecretAsync("path", "key");
            Assert.Equal("rotated_secret_2", secret3);
            Assert.Equal(2, fetchCount); // Fetch count became 2!
        }
    }
}
