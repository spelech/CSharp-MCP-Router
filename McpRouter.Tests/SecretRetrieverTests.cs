using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpRouter.Core.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using VaultSharp;
using VaultSharp.V1;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SecretsEngines.KeyValue;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;
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

        [Fact]
        public async Task Vault_RetrievesSpecificField_AndRenewsToken()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "Vault:Address", "http://localhost:8200" },
                { "Vault:RoleId", "test-role" },
                { "Vault:SecretId", "test-secret" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            int clientCreationCount = 0;

            // Mock client 1: Token close to expiration (TimeToLive = 100 seconds < 300 seconds)
            var mockClient1 = new Mock<IVaultClient>();
            var mockV1_1 = new Mock<IVaultClientV1>();
            var mockAuth1 = new Mock<IAuthMethod>();
            var mockToken1 = new Mock<ITokenAuthMethod>();
            mockClient1.Setup(c => c.V1).Returns(mockV1_1.Object);
            mockV1_1.Setup(v => v.Auth).Returns(mockAuth1.Object);
            mockAuth1.Setup(a => a.Token).Returns(mockToken1.Object);

            var tokenInfoExpiring = new VaultSharp.V1.AuthMethods.Token.Models.CallingTokenInfo { TimeToLive = 100 }; // 100s < 300s
            var secretTokenExpiring = new VaultSharp.V1.Commons.Secret<VaultSharp.V1.AuthMethods.Token.Models.CallingTokenInfo> { Data = tokenInfoExpiring };
            mockToken1.Setup(t => t.LookupSelfAsync()).ReturnsAsync(secretTokenExpiring);

            // Mock client 2: Renewed token with ample TTL (TimeToLive = 3600 seconds)
            var mockClient2 = new Mock<IVaultClient>();
            var mockV1_2 = new Mock<IVaultClientV1>();
            var mockAuth2 = new Mock<IAuthMethod>();
            var mockToken2 = new Mock<ITokenAuthMethod>();
            var mockSecrets2 = new Mock<ISecretsEngine>();
            var mockKv2 = new Mock<IKeyValueSecretsEngine>();
            var mockKvV2 = new Mock<IKeyValueSecretsEngineV2>();

            mockClient2.Setup(c => c.V1).Returns(mockV1_2.Object);
            mockV1_2.Setup(v => v.Auth).Returns(mockAuth2.Object);
            mockAuth2.Setup(a => a.Token).Returns(mockToken2.Object);
            mockV1_2.Setup(v => v.Secrets).Returns(mockSecrets2.Object);
            mockSecrets2.Setup(s => s.KeyValue).Returns(mockKv2.Object);
            mockKv2.Setup(k => k.V2).Returns(mockKvV2.Object);

            var tokenInfoValid = new VaultSharp.V1.AuthMethods.Token.Models.CallingTokenInfo { TimeToLive = 3600 };
            var secretTokenValid = new VaultSharp.V1.Commons.Secret<VaultSharp.V1.AuthMethods.Token.Models.CallingTokenInfo> { Data = tokenInfoValid };
            mockToken2.Setup(t => t.LookupSelfAsync()).ReturnsAsync(secretTokenValid);

            var secretDataDict = new Dictionary<string, object>
            {
                { "custom-field", "secret-value-777" }
            };
            var secretData = new VaultSharp.V1.Commons.SecretData { Data = secretDataDict };
            var secretContainer = new VaultSharp.V1.Commons.Secret<VaultSharp.V1.Commons.SecretData> { Data = secretData };

            mockKvV2.Setup(k => k.ReadSecretAsync("custom-path/service", null, "custom-mount", null))
                    .ReturnsAsync(secretContainer);

            IVaultClient Factory()
            {
                clientCreationCount++;
                return clientCreationCount == 1 ? mockClient1.Object : mockClient2.Object;
            }

            var retriever = new VaultSecretRetriever(config, memoryCache, Factory);

            var value = await retriever.GetSecretAsync("custom-mount:custom-path/service", "custom-field");

            Assert.Equal("secret-value-777", value);
            Assert.True(clientCreationCount >= 2, "Expected Vault client recreation due to token TTL expiration risk");
            mockKvV2.Verify(k => k.ReadSecretAsync("custom-path/service", null, "custom-mount", null), Times.Once);
        }
    }
}
