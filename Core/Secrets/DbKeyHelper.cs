using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Secrets
{
    public static class DbKeyHelper
    {
        private static string? _cachedKey;
        private static readonly object _lock = new object();

        public static string ResolveDbEncryptionKey(IConfiguration configuration)
        {
            // If already resolved, return cached key to avoid file IO / re-generation
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

                // 1. Check environment variables/configuration: ROUTER_MASTER_KEY or DB_ENCRYPTION_KEY
                var key = configuration["ROUTER_MASTER_KEY"] ?? configuration["DB_ENCRYPTION_KEY"];
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException("FATAL: Master encryption key is missing. Set 'ROUTER_MASTER_KEY' or 'DB_ENCRYPTION_KEY' environment variable. Self-generated key fallback is disabled for security.");
                }

                _cachedKey = key.Trim();
                return _cachedKey;
            }
        }

        /// <summary>
        /// For testing purposes only: resets the cached key so that subsequent calls re-resolve.
        /// </summary>
        public static void ResetCache()
        {
            lock (_lock)
            {
                _cachedKey = null;
            }
        }
    }
}
