using McpRouter.Tests.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using McpRouter.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpRouter.Tests
{
    [Collection("DbKeyTests")]
    public class EncryptionKeyProviderTests
    {
        [Fact]
        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]
        public void GetDbEncryptionKey_UsesConfig_WhenProvided()
        {
            DbKeyHelper.ResetCache();
            EncryptionKeyProvider.ResetCache();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "ConfiguredDbKey123!" }
            }).Build();

            var key = EncryptionKeyProvider.GetDbEncryptionKey(config);
            Assert.Equal("ConfiguredDbKey123!", key);
        }

        [Fact]

        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]
        public void GetDbEncryptionKey_ThrowsInvalidOperation_WhenNotConfigured()
        {
            DbKeyHelper.ResetCache();
            EncryptionKeyProvider.ResetCache();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            var ex = Assert.Throws<InvalidOperationException>(() => EncryptionKeyProvider.GetDbEncryptionKey(config));
            Assert.Contains("Master encryption key is missing", ex.Message);
        }

        [Fact]

        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]
        public void GetRouterSecret_UsesConfig_WhenProvided()
        {
            DbKeyHelper.ResetCache();
            EncryptionKeyProvider.ResetCache();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ROUTER_SECRET", "ConfiguredRouterSecret123!" }
            }).Build();

            var secret = EncryptionKeyProvider.GetRouterSecret(config);
            Assert.Equal("ConfiguredRouterSecret123!", secret);
        }

        [Fact]

        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]
        public void GetRouterSecret_FallsBackToDbEncryptionKey_WhenDbEncryptionKeyProvided()
        {
            DbKeyHelper.ResetCache();
            EncryptionKeyProvider.ResetCache();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "FallbackDbKey123!" }
            }).Build();

            var secret = EncryptionKeyProvider.GetRouterSecret(config);
            Assert.Equal("FallbackDbKey123!", secret);
        }
    }
}
