using System;
using System.Collections.Generic;
using System.IO;
using McpRouter.Infrastructure.Secrets;
using McpRouter.Tests.Attributes;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpRouter.Tests
{
    [Collection("DbKeyTests")]
    public class EncryptionKeyProviderTests : IDisposable
    {
        private readonly string _tempDataDir;

        public EncryptionKeyProviderTests()
        {
            _tempDataDir = Path.Combine(Path.GetTempPath(), $"mcp_enc_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDataDir);
            DbKeyHelper.ResetCache();
            EncryptionKeyProvider.ResetCache();
        }

        public void Dispose()
        {
            DbKeyHelper.ResetCache();
            EncryptionKeyProvider.ResetCache();
            if (Directory.Exists(_tempDataDir))
            {
                try { Directory.Delete(_tempDataDir, true); } catch { }
            }
        }

        [Fact]
        [Requirement("SEC-KEY-PROVIDER-CONFIG", "SEC", RequirementType.Positive, "EncryptionKeyProvider returns configured DB_ENCRYPTION_KEY or ROUTER_SECRET.")]
        public void GetDbEncryptionKey_UsesConfig_WhenProvided()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir },
                { "DB_ENCRYPTION_KEY", "ConfiguredDbKey123!" }
            }).Build();

            var key = EncryptionKeyProvider.GetDbEncryptionKey(config);
            Assert.Equal("ConfiguredDbKey123!", key);
        }

        [Fact]
        [Requirement("SEC-KEY-PROVIDER-AUTOGEN", "SEC", RequirementType.Positive, "EncryptionKeyProvider delegates to DbKeyHelper to auto-generate master key when unconfigured.")]
        public void GetDbEncryptionKey_AutoGenerates_WhenUnconfigured()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir }
            }).Build();

            var key = EncryptionKeyProvider.GetDbEncryptionKey(config);
            Assert.False(string.IsNullOrWhiteSpace(key));
            Assert.Equal(32, Convert.FromBase64String(key).Length);
        }

        [Fact]
        [Requirement("SEC-KEY-PROVIDER-SECRET", "SEC", RequirementType.Positive, "EncryptionKeyProvider returns configured ROUTER_SECRET.")]
        public void GetRouterSecret_UsesConfig_WhenProvided()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir },
                { "ROUTER_SECRET", "ConfiguredRouterSecret123!" }
            }).Build();

            var secret = EncryptionKeyProvider.GetRouterSecret(config);
            Assert.Equal("ConfiguredRouterSecret123!", secret);
        }

        [Fact]
        [Requirement("SEC-KEY-PROVIDER-FALLBACK", "SEC", RequirementType.Positive, "EncryptionKeyProvider falls back to DB_ENCRYPTION_KEY when ROUTER_SECRET is unconfigured.")]
        public void GetRouterSecret_FallsBackToDbEncryptionKey_WhenDbEncryptionKeyProvided()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir },
                { "DB_ENCRYPTION_KEY", "FallbackDbKey123!" }
            }).Build();

            var secret = EncryptionKeyProvider.GetRouterSecret(config);
            Assert.Equal("FallbackDbKey123!", secret);
        }
    }
}
