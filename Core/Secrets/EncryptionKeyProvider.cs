using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Secrets
{
    public static class EncryptionKeyProvider
    {
        private static string? _cachedDbKey;
        private static string? _cachedRouterSecret;

        public static string GetDbEncryptionKey(IConfiguration config)
        {
            if (!string.IsNullOrEmpty(_cachedDbKey))
                return _cachedDbKey;

            var key = config["DB_ENCRYPTION_KEY"];
            if (!string.IsNullOrEmpty(key))
            {
                _cachedDbKey = key;
                return key;
            }

            // Fallback to a dynamically generated, persistent key file
            _cachedDbKey = GetOrCreatePersistentKey("db_encryption.key");
            return _cachedDbKey;
        }

        public static string GetRouterSecret(IConfiguration config)
        {
            if (!string.IsNullOrEmpty(_cachedRouterSecret))
                return _cachedRouterSecret;

            var secret = config["ROUTER_SECRET"];
            if (!string.IsNullOrEmpty(secret))
            {
                _cachedRouterSecret = secret;
                return secret;
            }

            // Fallback to DB_ENCRYPTION_KEY if set
            var dbKey = config["DB_ENCRYPTION_KEY"];
            if (!string.IsNullOrEmpty(dbKey))
            {
                _cachedRouterSecret = dbKey;
                return dbKey;
            }

            // Fallback to a dynamically generated, persistent key file
            _cachedRouterSecret = GetOrCreatePersistentKey("router_secret.key");
            return _cachedRouterSecret;
        }

        private static string GetOrCreatePersistentKey(string fileName)
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataDir))
            {
                try
                {
                    Directory.CreateDirectory(dataDir);
                }
                catch
                {
                    // Fallback to current directory if base path isn't writable/available
                    dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
                    if (!Directory.Exists(dataDir))
                    {
                        Directory.CreateDirectory(dataDir);
                    }
                }
            }

            var keyPath = Path.Combine(dataDir, fileName);
            if (File.Exists(keyPath))
            {
                try
                {
                    var key = File.ReadAllText(keyPath, Encoding.UTF8).Trim();
                    if (!string.IsNullOrEmpty(key))
                    {
                        return key;
                    }
                }
                catch
                {
                    // Ignore and recreate if corrupt or unreadable
                }
            }

            // Generate a 32-byte (256-bit) cryptographically secure random base64 string
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var generatedKey = Convert.ToBase64String(bytes);

            try
            {
                File.WriteAllText(keyPath, generatedKey, Encoding.UTF8);
            }
            catch
            {
                // If writing fails (e.g. read-only file system), return the generated key anyway
            }

            return generatedKey;
        }
    }
}
