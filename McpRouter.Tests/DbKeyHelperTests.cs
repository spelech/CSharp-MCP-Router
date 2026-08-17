using McpRouter.Tests.Attributes;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using McpRouter.Infrastructure.Secrets;

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

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
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

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public void ResolveDbEncryptionKey_ThrowsInvalidOperation_WhenMasterKeyAbsent()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["DB_ENCRYPTION_KEY"]).Returns((string?)null);
            configMock.Setup(c => c["ROUTER_MASTER_KEY"]).Returns((string?)null);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => DbKeyHelper.ResolveDbEncryptionKey(configMock.Object));
            Assert.Contains("Master encryption key is missing", ex.Message);
            Assert.False(File.Exists(_keyFilePath), "Should not create a key file when master key is absent.");
        }
    }
}
