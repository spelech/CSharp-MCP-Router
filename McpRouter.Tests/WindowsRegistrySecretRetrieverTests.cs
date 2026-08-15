using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using McpRouter.Infrastructure.Secrets;
using Moq;
using Xunit;

#pragma warning disable CA1416

namespace McpRouter.Tests
{
    public class WindowsRegistrySecretRetrieverTests
    {
        [Fact]
        public async Task GetSecretAsync_ReturnsPlainString_WhenRegistryValueIsString()
        {
            var mockRegistry = new Mock<IRegistryAccessor>();
            mockRegistry.Setup(r => r.GetValue("SOFTWARE\\McpRouter\\Secrets", "ApiKey"))
                .Returns("my-secret-key-123");

            var retriever = new WindowsRegistrySecretRetriever(mockRegistry.Object, null);

            var secret = await retriever.GetSecretAsync("SOFTWARE\\McpRouter\\Secrets", "ApiKey");

            secret.Should().Be("my-secret-key-123");
        }

        [Fact]
        public async Task GetSecretAsync_DecryptsDpapiBytes_WhenRegistryValueIsByteArray()
        {
            var mockRegistry = new Mock<IRegistryAccessor>();
            var mockDpapi = new Mock<IDpapiProtector>();

            var encryptedBytes = new byte[] { 1, 2, 3, 4 };
            var decryptedBytes = Encoding.UTF8.GetBytes("decrypted-dpapi-secret");

            mockRegistry.Setup(r => r.GetValue("SOFTWARE\\McpRouter\\Secrets", "EncryptedToken"))
                .Returns(encryptedBytes);

            mockDpapi.Setup(d => d.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine))
                .Returns(decryptedBytes);

            var retriever = new WindowsRegistrySecretRetriever(mockRegistry.Object, mockDpapi.Object);

            var secret = await retriever.GetSecretAsync("SOFTWARE\\McpRouter\\Secrets", "EncryptedToken");

            secret.Should().Be("decrypted-dpapi-secret");
            mockDpapi.Verify(d => d.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine), Times.Once);
        }

        [Fact]
        public async Task GetSecretAsync_ReturnsNull_WhenKeyNotFoundOrNull()
        {
            var mockRegistry = new Mock<IRegistryAccessor>();
            mockRegistry.Setup(r => r.GetValue("SOFTWARE\\NonExistent", "MissingKey"))
                .Returns((object?)null);

            var retriever = new WindowsRegistrySecretRetriever(mockRegistry.Object, null);

            var secret = await retriever.GetSecretAsync("SOFTWARE\\NonExistent", "MissingKey");

            secret.Should().BeNull();
        }

        [Fact]
        public async Task GetSecretAsync_HandlesExceptionGracefully_ReturnsNull()
        {
            var mockRegistry = new Mock<IRegistryAccessor>();
            mockRegistry.Setup(r => r.GetValue(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new System.Exception("Registry read error"));

            var retriever = new WindowsRegistrySecretRetriever(mockRegistry.Object, null);

            var secret = await retriever.GetSecretAsync("SOFTWARE\\Errors", "Key");

            secret.Should().BeNull();
        }
    }
}
