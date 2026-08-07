using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using McpRouter.Core.Secrets;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace McpRouter.Tests
{
    public class DbKeyHelperTests : IDisposable
    {
        private readonly string _keyFilePath;

        public DbKeyHelperTests()
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            _keyFilePath = Path.Combine(dataDir, "db_key.txt");
            CleanUpKeyFile();
            DbKeyHelper.ResetCache();
        }

        public void Dispose()
        {
            CleanUpKeyFile();
            DbKeyHelper.ResetCache();
        }

        private void CleanUpKeyFile()
        {
            if (File.Exists(_keyFilePath))
            {
                try
                {
                    File.Delete(_keyFilePath);
                }
                catch { }
            }
        }

        [Fact]
        public void ResolveDbEncryptionKey_ReturnsConfiguredKey_WhenPresent()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["DB_ENCRYPTION_KEY"]).Returns("ConfiguredSuperSecureKey123!");

            // Act
            var resolvedKey = DbKeyHelper.ResolveDbEncryptionKey(configMock.Object);

            // Assert
            Assert.Equal("ConfiguredSuperSecureKey123!", resolvedKey);
            Assert.False(File.Exists(_keyFilePath), "Should not create a key file when configured in environment.");
        }

        [Fact]
        public void ResolveDbEncryptionKey_GeneratesAndPersistsKey_WhenAbsent()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["DB_ENCRYPTION_KEY"]).Returns((string?)null);

            // Act - First call: should generate and save to file
            var key1 = DbKeyHelper.ResolveDbEncryptionKey(configMock.Object);

            // Assert
            Assert.False(string.IsNullOrEmpty(key1));
            Assert.True(File.Exists(_keyFilePath), "Should write generated key to data/db_key.txt");

            // Read the file to ensure the key matches
            var fileContent = File.ReadAllText(_keyFilePath).Trim();
            Assert.Equal(key1, fileContent);

            // Act - Second call: should retrieve from cache or file, and must be identical
            var key2 = DbKeyHelper.ResolveDbEncryptionKey(configMock.Object);
            Assert.Equal(key1, key2);

            // Act - Third call (reset cache to force reading from file): should retrieve from file and be identical
            DbKeyHelper.ResetCache();
            var key3 = DbKeyHelper.ResolveDbEncryptionKey(configMock.Object);
            Assert.Equal(key1, key3);
        }
    }
}
