using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace McpRouter.Infrastructure.Secrets
{
    public class CompositeSecretRetriever : ISecretRetriever
    {
        private readonly IEnumerable<ISecretRetriever> _retrievers;
        private readonly IMemoryCache? _cache;
        public string ProviderName => "Composite";

        public CompositeSecretRetriever(IEnumerable<ISecretRetriever> retrievers, IMemoryCache? cache = null)
        {
            _retrievers = retrievers;
            _cache = cache;
        }

        public Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            return GetSecretForProviderAsync("Vault", secretPath, keyName);
        }

        public async Task<string?> GetSecretForProviderAsync(string providerName, string secretPath, string keyName)
        {
            if (string.Equals(providerName, "None", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string cacheKey = $"secret_cache:{providerName}:{secretPath}:{keyName}";
            if (_cache != null && _cache.TryGetValue(cacheKey, out string? cachedVal) && cachedVal != null)
            {
                return cachedVal;
            }

            ISecretRetriever? targetRetriever = null;
            foreach (var retriever in _retrievers)
            {
                if (string.Equals(retriever.ProviderName, providerName, StringComparison.OrdinalIgnoreCase) ||
                    (providerName.Equals("Vault", StringComparison.OrdinalIgnoreCase) && retriever.ProviderName.Equals("HashiCorpVault", StringComparison.OrdinalIgnoreCase)) ||
                    (providerName.Equals("HashiCorpVault", StringComparison.OrdinalIgnoreCase) && retriever.ProviderName.Equals("Vault", StringComparison.OrdinalIgnoreCase)) ||
                    (retriever.ProviderName.Equals("TokenExchange", StringComparison.OrdinalIgnoreCase) &&
                     (providerName.Equals("OBO", StringComparison.OrdinalIgnoreCase) ||
                      providerName.Equals("PocketID", StringComparison.OrdinalIgnoreCase) ||
                      providerName.Equals("OAuth2", StringComparison.OrdinalIgnoreCase) ||
                      providerName.Equals("OAuth2TokenExchange", StringComparison.OrdinalIgnoreCase) ||
                      providerName.Equals("AzureAD", StringComparison.OrdinalIgnoreCase) ||
                      providerName.Equals("Okta", StringComparison.OrdinalIgnoreCase))))
                {
                    targetRetriever = retriever;
                    break;
                }
            }

            if (targetRetriever == null)
            {
                throw new InvalidOperationException($"No secret retriever registered for SecretProvider '{providerName}'.");
            }

            var secret = await targetRetriever.GetSecretAsync(secretPath, keyName);
            if (!string.IsNullOrEmpty(secret) && _cache != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));
                _cache.Set(cacheKey, secret, cacheOptions);
            }

            return secret;
        }
    }
}
