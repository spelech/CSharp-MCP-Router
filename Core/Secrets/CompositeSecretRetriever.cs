using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace McpRouter.Core.Secrets
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

        public async Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            string cacheKey = $"secret_cache:{secretPath}:{keyName}";
            if (_cache != null && _cache.TryGetValue(cacheKey, out string? cachedVal) && cachedVal != null)
            {
                return cachedVal;
            }

            foreach (var retriever in _retrievers)
            {
                try
                {
                    var secret = await retriever.GetSecretAsync(secretPath, keyName);
                    if (!string.IsNullOrEmpty(secret))
                    {
                        if (_cache != null)
                        {
                            var cacheOptions = new MemoryCacheEntryOptions()
                                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                                .SetSlidingExpiration(TimeSpan.FromMinutes(2));
                            _cache.Set(cacheKey, secret, cacheOptions);
                        }
                        return secret;
                    }
                }
                catch
                {
                    // Suppress and fail silently to prevent connection string disclosure / leakage
                }
            }

            return null;
        }
    }
}
