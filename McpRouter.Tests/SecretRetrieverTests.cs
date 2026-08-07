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
        public async Task Vault_RetrievesSpecificField_AndRenewsToken()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "Vault:Address", "https://localhost:8200" },
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

        [Fact]
        public async Task Vault_RecreatesClient_On_LookupSelfAsync_Exception()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Vault:Address", "https://localhost:8200" },
                { "Vault:RoleId", "role" },
                { "Vault:SecretId", "secret" }
            }).Build();

            int clientCreationCount = 0;

            // Client 1: LookupSelfAsync throws exception
            var mockClient1 = new Mock<IVaultClient>();
            var mockV1_1 = new Mock<IVaultClientV1>();
            var mockAuth1 = new Mock<IAuthMethod>();
            var mockToken1 = new Mock<ITokenAuthMethod>();
            mockClient1.Setup(c => c.V1).Returns(mockV1_1.Object);
            mockV1_1.Setup(v => v.Auth).Returns(mockAuth1.Object);
            mockAuth1.Setup(a => a.Token).Returns(mockToken1.Object);
            mockToken1.Setup(t => t.LookupSelfAsync()).ThrowsAsync(new Exception("Token expired or invalid"));

            // Client 2: Re-created client succeeds
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

            var secretDataDict = new Dictionary<string, object> { { "key1", "val1" } };
            var secretData = new VaultSharp.V1.Commons.SecretData { Data = secretDataDict };
            mockKvV2.Setup(k => k.ReadSecretAsync("my-path", null, "secret", null))
                    .ReturnsAsync(new VaultSharp.V1.Commons.Secret<VaultSharp.V1.Commons.SecretData> { Data = secretData });

            IVaultClient Factory()
            {
                clientCreationCount++;
                return clientCreationCount == 1 ? mockClient1.Object : mockClient2.Object;
            }

            var retriever = new VaultSecretRetriever(config, memoryCache, Factory);
            var val = await retriever.GetSecretAsync("my-path", "key1");

            Assert.Equal("val1", val);
            Assert.Equal(2, clientCreationCount);
        }

        [Fact]
        public async Task Vault_GetSecretAsync_Checks_Cache_First()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            memoryCache.Set("vault:secret:my-path:key1", "cached-value");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Vault:Address", "https://localhost:8200" },
                { "Vault:RoleId", "role" },
                { "Vault:SecretId", "secret" }
            }).Build();

            int clientCreationCount = 0;
            IVaultClient Factory()
            {
                clientCreationCount++;
                return new Mock<IVaultClient>().Object;
            }

            var retriever = new VaultSecretRetriever(config, memoryCache, Factory);
            var val = await retriever.GetSecretAsync("my-path", "key1");

            Assert.Equal("cached-value", val);
            Assert.Equal(0, clientCreationCount); // No client created because cache hit!
        }
    }
}
