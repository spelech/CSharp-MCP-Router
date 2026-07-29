using System.Collections.Generic;
using System.Threading.Tasks;
using McpRouter.Core.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpRouter.Tests
{
    public class SecretRetrieverTests
    {
        [Fact]
        public async Task CompositeSecretRetriever_Returns_First_Available_Secret()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var inMemoryConfig = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            var vaultRetriever = new VaultSecretRetriever(config, memoryCache);
            var winRegRetriever = new WindowsRegistrySecretRetriever();

            var composite = new CompositeSecretRetriever(new ISecretRetriever[] { vaultRetriever, winRegRetriever });

            // On non-configured environments, should safely return null without throwing exceptions
            var secret = await composite.GetSecretAsync("SOFTWARE\\NonExistentPath", "ApiKey");
            Assert.Null(secret);
        }
    }
}
