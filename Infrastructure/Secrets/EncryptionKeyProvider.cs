using System;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Infrastructure.Secrets
{
    public static class EncryptionKeyProvider
    {
        public static string GetDbEncryptionKey(IConfiguration config)
        {
            return DbKeyHelper.ResolveDbEncryptionKey(config);
        }

        public static string GetRouterSecret(IConfiguration config)
        {
            return DbKeyHelper.ResolveDbEncryptionKey(config);
        }

        public static void ResetCache()
        {
            DbKeyHelper.ResetCache();
        }
    }
}
