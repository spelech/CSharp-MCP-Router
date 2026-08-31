using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace ModelContextGateway.Tests
{
    public class CompositeSecretRetrieverTests
    {
        [Fact]
        [Requirement("SEC-02", "SEC", RequirementType.Positive, "CompositeSecretRetriever returns null when secret provider is 'None'.")]
        public async Task GetSecretForProviderAsync_ReturnsNull_WhenProviderIsNone()
        {
            var retriever = new CompositeSecretRetriever(new List<ISecretRetriever>());
            var result = await retriever.GetSecretForProviderAsync("None", "path", "key");
            Assert.Null(result);
        }

        [Fact]
        [Requirement("GUARD-03", "GUARD", RequirementType.Negative, "CompositeSecretRetriever throws InvalidOperationException when an unregistered secret provider is requested.")]
        public async Task GetSecretForProviderAsync_ThrowsInvalidOperationException_WhenProviderNotRegistered()
        {
            var retriever = new CompositeSecretRetriever(new List<ISecretRetriever>());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                retriever.GetSecretForProviderAsync("UnknownProvider", "path", "key"));
        }

        [Fact]
        [Requirement("SEC-02", "SEC", RequirementType.Positive, "CompositeSecretRetriever routes to matching provider and caches secret values in memory.")]
        public async Task GetSecretForProviderAsync_RoutesToTargetProvider_AndCachesValue()
        {
            var mockRetriever = new Mock<ISecretRetriever>();
            mockRetriever.Setup(r => r.ProviderName).Returns("Environment");
            mockRetriever.Setup(r => r.GetSecretAsync("env_path", "api_key"))
                         .ReturnsAsync("EnvSecret123");

            var cache = new MemoryCache(new MemoryCacheOptions());
            var composite = new CompositeSecretRetriever(new[] { mockRetriever.Object }, cache);

            var val1 = await composite.GetSecretForProviderAsync("Environment", "env_path", "api_key");
            Assert.Equal("EnvSecret123", val1);

            // Second call should return cached value without invoking mockRetriever again
            var val2 = await composite.GetSecretForProviderAsync("Environment", "env_path", "api_key");
            Assert.Equal("EnvSecret123", val2);

            mockRetriever.Verify(r => r.GetSecretAsync("env_path", "api_key"), Times.Once);
        }

        [Fact]
        [Requirement("SEC-02", "SEC", RequirementType.Positive, "CompositeSecretRetriever normalizes provider alias names like 'Vault' to 'HashiCorpVault'.")]
        public async Task GetSecretForProviderAsync_MatchesVaultAliasNames()
        {
            var mockVaultRetriever = new Mock<ISecretRetriever>();
            mockVaultRetriever.Setup(r => r.ProviderName).Returns("HashiCorpVault");
            mockVaultRetriever.Setup(r => r.GetSecretAsync("secret:app", "key"))
                               .ReturnsAsync("VaultVal");

            var composite = new CompositeSecretRetriever(new[] { mockVaultRetriever.Object });
            var result = await composite.GetSecretForProviderAsync("Vault", "secret:app", "key");

            Assert.Equal("VaultVal", result);
        }
    }
}
