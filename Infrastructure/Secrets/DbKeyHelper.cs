using System;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Infrastructure.Secrets
{
    public static class DbKeyHelper
    {
        private static string? _cachedKey;
        private static readonly object _lock = new object();

        public static string ResolveDbEncryptionKey(IConfiguration configuration)
        {
            if (_cachedKey != null)
            {
                return _cachedKey;
            }

            lock (_lock)
            {
                if (_cachedKey != null)
                {
                    return _cachedKey;
                }

                var key = configuration["ROUTER_MASTER_KEY"] ?? configuration["DB_ENCRYPTION_KEY"];
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException("FATAL: Master encryption key is missing. Set 'ROUTER_MASTER_KEY' or 'DB_ENCRYPTION_KEY' environment variable. Self-generated key fallback is disabled for security.");
                }

                _cachedKey = key.Trim();
                return _cachedKey;
            }
        }

        public static void ResetCache()
        {
            lock (_lock)
            {
                _cachedKey = null;
            }
        }
    }
}
