using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Xunit;
using McpRouter.Infrastructure.Secrets;
using McpRouter.Tests.Attributes;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace McpRouter.Tests
{
    public class DbKeyHelperTests : IDisposable
    {
        private readonly string _tempDataDir;

        public DbKeyHelperTests()
        {
            _tempDataDir = Path.Combine(Path.GetTempPath(), $"mcp_key_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDataDir);
            DbKeyHelper.ResetCache();
        }

        public void Dispose()
        {
            DbKeyHelper.ResetCache();
            if (Directory.Exists(_tempDataDir))
            {
                try { Directory.Delete(_tempDataDir, true); } catch { }
            }
        }

        [Fact]
        [Requirement("SEC-KEYFILE-ENV-PRECEDENCE", "SEC", RequirementType.Positive, "Explicit environment variables ROUTER_MASTER_KEY or ROUTER_SECRET take precedence over keyfiles.")]
        public void ResolveDbEncryptionKey_ReturnsConfiguredEnvKey_WhenPresent()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir },
                { "ROUTER_MASTER_KEY", "ConfiguredEnvMasterKey1234567890123456789012==" }
            }).Build();

            var resolvedKey = DbKeyHelper.ResolveDbEncryptionKey(config);

            Assert.Equal("ConfiguredEnvMasterKey1234567890123456789012==", resolvedKey);
            var keyFilePath = Path.Combine(_tempDataDir, ".master.key");
            Assert.False(File.Exists(keyFilePath), "Should not create a key file when configured in environment.");
        }

        [Fact]
        [Requirement("SEC-KEYFILE-FILE-SECRET", "SEC", RequirementType.Positive, "File-based secrets configured via ROUTER_MASTER_KEY_FILE or standard Docker secrets paths are resolved.")]
        public void ResolveDbEncryptionKey_ReturnsFileSecret_WhenKeyFileSpecified()
        {
            var secretFile = Path.Combine(_tempDataDir, "docker_secret.txt");
            File.WriteAllText(secretFile, "DockerMountedSecretKey12345678901234567890==");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir },
                { "ROUTER_MASTER_KEY_FILE", secretFile }
            }).Build();

            var resolvedKey = DbKeyHelper.ResolveDbEncryptionKey(config);

            Assert.Equal("DockerMountedSecretKey12345678901234567890==", resolvedKey);
        }

        [Fact]
        [Requirement("SEC-KEYFILE-AUTOGEN", "SEC", RequirementType.Positive, "Blank-slate initialization auto-generates a 256-bit base64 master key and persists it to .master.key.")]
        public void ResolveDbEncryptionKey_AutoGeneratesAndPersistsKey_WhenBlankSlate()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir }
            }).Build();

            var resolvedKey = DbKeyHelper.ResolveDbEncryptionKey(config);

            Assert.False(string.IsNullOrWhiteSpace(resolvedKey));
            var keyBytes = Convert.FromBase64String(resolvedKey);
            Assert.Equal(32, keyBytes.Length); // 256-bit

            var keyFilePath = Path.Combine(_tempDataDir, ".master.key");
            Assert.True(File.Exists(keyFilePath), "Auto-generated key must be persisted to .master.key");
            Assert.Equal(resolvedKey, File.ReadAllText(keyFilePath).Trim());
        }

        [Fact]
        [Requirement("SEC-KEYFILE-RELOAD", "SEC", RequirementType.Positive, "Existing .master.key file is loaded across gateway restarts without key mutation.")]
        public void ResolveDbEncryptionKey_LoadsExistingKeyFile_OnSubsequentBoot()
        {
            var keyFilePath = Path.Combine(_tempDataDir, ".master.key");
            const string existingKey = "ExistingPersistentKeyFile1234567890123456==";
            File.WriteAllText(keyFilePath, existingKey);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATA_DIR", _tempDataDir }
            }).Build();

            var resolvedKey = DbKeyHelper.ResolveDbEncryptionKey(config);

            Assert.Equal(existingKey, resolvedKey);
        }
    }
}
