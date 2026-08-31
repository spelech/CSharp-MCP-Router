using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Moq;

#pragma warning disable CA1416

namespace ModelContextGateway.Tests
{
    public class WindowsRegistrySecretRetrieverTests
    {
        [Fact]
        [Requirement("SEC-04", "SEC", RequirementType.Positive, "WindowsRegistrySecretRetriever securely decrypts DPAPI LocalMachine machine-level encrypted binary values and retrieves plaintext registry strings")]
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
        [Requirement("SEC-04", "SEC", RequirementType.Positive, "WindowsRegistrySecretRetriever securely decrypts DPAPI LocalMachine machine-level encrypted binary values and retrieves plaintext registry strings")]
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
        [Requirement("SEC-04", "SEC", RequirementType.Positive, "WindowsRegistrySecretRetriever returns null gracefully when registry key or path is not found.")]
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
        [Requirement("GUARD-03", "GUARD", RequirementType.Negative, "WindowsRegistrySecretRetriever fails closed and handles registry accessor exceptions gracefully without leaking secrets.")]
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
