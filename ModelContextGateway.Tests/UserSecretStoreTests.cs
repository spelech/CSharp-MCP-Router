using Microsoft.Extensions.Configuration;
using Moq;

namespace ModelContextGateway.Tests
{
    public class UserSecretStoreTests
    {
        [Fact]
        [Requirement("AUTH-001", "AUTH", RequirementType.Positive, "Verify DatabaseUserSecretStore encrypts and decrypts secret correctly.")]
        public async Task DatabaseUserSecretStore_SavesAndRetrieves_Secret()
        {
            // Arrange
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string, string?>("ROUTER_MASTER_KEY", "12345678901234567890123456789012") }).Build();
            var mockRepo = new Mock<IUserCredentialRepository>();
            UserCredentialDto? savedDto = null;
            mockRepo.Setup(r => r.SaveCredentialAsync(It.IsAny<UserCredentialDto>())).Callback<UserCredentialDto>(d => savedDto = d).Returns(Task.CompletedTask);
            mockRepo.Setup(r => r.GetCredentialAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(() => savedDto);

            var store = new DatabaseUserSecretStore(mockRepo.Object, config);

            // Act
            await store.SaveSecretAsync("testuser", "server1", "mysecret");
            var retrieved = await store.GetSecretAsync("testuser", "server1");

            // Assert
            Assert.NotNull(savedDto);
            Assert.NotEqual("mysecret", savedDto.EncryptedSecretJson);
            Assert.Equal("mysecret", retrieved);
        }
    }
}
