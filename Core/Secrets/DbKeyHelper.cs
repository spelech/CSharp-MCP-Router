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

                // 1. Check DB_ENCRYPTION_KEY environment variable/configuration
                var key = configuration["DB_ENCRYPTION_KEY"];
                if (!string.IsNullOrEmpty(key))
                {
                    _cachedKey = key;
                    return _cachedKey;
                }

                // 2. Resolve or generate a persistent local key file
                var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                var keyFilePath = Path.Combine(dataDir, "db_key.txt");
                if (File.Exists(keyFilePath))
                {
                    try
                    {
                        var fileKey = File.ReadAllText(keyFilePath).Trim();
                        if (!string.IsNullOrEmpty(fileKey))
                        {
                            _cachedKey = fileKey;
                            return _cachedKey;
                        }
                    }
                    catch
                    {
                        // Fallback to generating a new key if read fails
                    }
                }

                // 3. Generate a cryptographically secure 256-bit (32-byte) key
                var keyBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(keyBytes);
                }
                var newKey = Convert.ToBase64String(keyBytes);

                try
                {
                    File.WriteAllText(keyFilePath, newKey);
                }
                catch
                {
                    // If write fails, return the transient generated key
                }

                _cachedKey = newKey;
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
