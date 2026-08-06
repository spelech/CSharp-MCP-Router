using System;
using System.Collections.Generic;
using System.IO;
using McpRouter.Core.Secrets;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpRouter.Tests
{
    public class EncryptionKeyProviderTests
    {
        [Fact]
        public void GetDbEncryptionKey_UsesConfig_WhenProvided()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "ConfiguredDbKey123!" }
            }).Build();

            // Clear cache first
            typeof(EncryptionKeyProvider).GetField("_cachedDbKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            var key = EncryptionKeyProvider.GetDbEncryptionKey(config);
            Assert.Equal("ConfiguredDbKey123!", key);
        }

        [Fact]
        public void GetDbEncryptionKey_GeneratesAndPersists_WhenNotConfigured()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            // Clear cache first
            typeof(EncryptionKeyProvider).GetField("_cachedDbKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            var keyPath = Path.Combine(dataDir, "db_encryption.key");

            if (File.Exists(keyPath))
            {
                File.Delete(keyPath);
            }

            var key = EncryptionKeyProvider.GetDbEncryptionKey(config);
            Assert.NotEmpty(key);
            Assert.True(File.Exists(keyPath));

            var persistedKey = File.ReadAllText(keyPath).Trim();
            Assert.Equal(key, persistedKey);

            // Re-fetch should return cached/persisted key
            var key2 = EncryptionKeyProvider.GetDbEncryptionKey(config);
            Assert.Equal(key, key2);
        }

        [Fact]
        public void GetRouterSecret_UsesConfig_WhenProvided()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ROUTER_SECRET", "ConfiguredRouterSecret123!" }
            }).Build();

            // Clear cache first
            typeof(EncryptionKeyProvider).GetField("_cachedRouterSecret", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            var secret = EncryptionKeyProvider.GetRouterSecret(config);
            Assert.Equal("ConfiguredRouterSecret123!", secret);
        }

        [Fact]
        public void GetRouterSecret_FallsBackToDbEncryptionKey_WhenDbEncryptionKeyProvided()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "FallbackDbKey123!" }
            }).Build();

            // Clear cache first
            typeof(EncryptionKeyProvider).GetField("_cachedRouterSecret", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            var secret = EncryptionKeyProvider.GetRouterSecret(config);
            Assert.Equal("FallbackDbKey123!", secret);
        }
    }
}
