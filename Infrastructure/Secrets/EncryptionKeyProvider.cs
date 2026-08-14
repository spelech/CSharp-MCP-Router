using System;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Infrastructure.Secrets
{
    public static class EncryptionKeyProvider
    {
        private static string? _cachedRouterSecret;

        public static string GetDbEncryptionKey(IConfiguration config)
        {
            return DbKeyHelper.ResolveDbEncryptionKey(config);
        }

        public static string GetRouterSecret(IConfiguration config)
        {
            if (!string.IsNullOrEmpty(_cachedRouterSecret))
                return _cachedRouterSecret;

            var secret = config["ROUTER_SECRET"] ?? config["ROUTER_MASTER_KEY"] ?? config["DB_ENCRYPTION_KEY"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("FATAL: Master encryption key is missing. Set 'ROUTER_SECRET' or 'ROUTER_MASTER_KEY' environment variable. Self-generated key fallback is disabled for security.");
            }

            _cachedRouterSecret = secret.Trim();
            return _cachedRouterSecret;
        }

        public static void ResetCache()
        {
            _cachedRouterSecret = null;
        }
    }
}
