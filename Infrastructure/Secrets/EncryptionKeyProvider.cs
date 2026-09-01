namespace ModelContextGateway.Infrastructure.Secrets
{
    public static class EncryptionKeyProvider
    {
        public static string GetDbEncryptionKey(IConfiguration config, ILogger? logger = null)
        {
            return DbKeyHelper.ResolveDbEncryptionKey(config, logger);
        }

        public static string GetRouterSecret(IConfiguration config, ILogger? logger = null)
        {
            return DbKeyHelper.ResolveDbEncryptionKey(config, logger);
        }

        public static void ResetCache()
        {
            DbKeyHelper.ResetCache();
        }
    }
}
