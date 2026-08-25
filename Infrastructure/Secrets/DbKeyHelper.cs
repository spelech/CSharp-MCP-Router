using System.Security.Cryptography;
using System.Text.RegularExpressions;

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

                // 1. Direct Environment / Config Variables
                var key = configuration["ROUTER_SECRET"]
                    ?? configuration["ROUTER_MASTER_KEY"]
                    ?? configuration["DB_ENCRYPTION_KEY"];

                if (!string.IsNullOrWhiteSpace(key))
                {
                    _cachedKey = key.Trim();
                    return _cachedKey;
                }

                // 2. Explicit File Path from Environment / Config (Docker / K8s file secrets)
                var filePath = configuration["ROUTER_SECRET_FILE"]
                    ?? configuration["ROUTER_MASTER_KEY_FILE"]
                    ?? configuration["DB_ENCRYPTION_KEY_FILE"];

                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    var fileSecret = File.ReadAllText(filePath).Trim();
                    if (!string.IsNullOrWhiteSpace(fileSecret))
                    {
                        _cachedKey = fileSecret;
                        return _cachedKey;
                    }
                }

                // 3. Standard Docker / Kubernetes Secrets Default Paths
                var defaultDockerSecretPaths = new[]
                {
                    "/run/secrets/router_master_key",
                    "/run/secrets/router_secret",
                    "/run/secrets/master_key"
                };

                foreach (var dPath in defaultDockerSecretPaths)
                {
                    if (File.Exists(dPath))
                    {
                        var dSecret = File.ReadAllText(dPath).Trim();
                        if (!string.IsNullOrWhiteSpace(dSecret))
                        {
                            _cachedKey = dSecret;
                            return _cachedKey;
                        }
                    }
                }

                // 4. Persistent Keyfile in Data Directory (./data/.master.key)
                string dataDir = ResolveDataDirectory(configuration);
                var keyFilePath = Path.Combine(dataDir, ".master.key");

                if (File.Exists(keyFilePath))
                {
                    var existingKey = File.ReadAllText(keyFilePath).Trim();
                    if (!string.IsNullOrWhiteSpace(existingKey))
                    {
                        _cachedKey = existingKey;
                        return _cachedKey;
                    }
                }

                // 5. Auto-Generate and Persist to Keyfile
                try
                {
                    Directory.CreateDirectory(dataDir);
                    var newKeyBytes = RandomNumberGenerator.GetBytes(32);
                    var newKey = Convert.ToBase64String(newKeyBytes);

                    File.WriteAllText(keyFilePath, newKey);

                    // Apply strict POSIX permissions (0600) on Linux/macOS if supported
                    if (!OperatingSystem.IsWindows())
                    {
                        try
                        {
                            File.SetUnixFileMode(keyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                        }
                        catch
                        {
                            // Ignored on file systems without POSIX permissions support
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[SECURITY] Auto-generated persistent master encryption key saved to '{keyFilePath}'.");
                    Console.WriteLine("[SECURITY] Backup this key to ensure recoverable access to encrypted credentials across storage re-provisioning.");
                    Console.ResetColor();

                    _cachedKey = newKey;
                    return _cachedKey;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"FATAL: Master encryption key is missing and failed to persist auto-generated key to '{keyFilePath}'. " +
                        "Ensure the data directory is writable or configure 'ROUTER_MASTER_KEY' or 'ROUTER_MASTER_KEY_FILE'.", ex);
                }
            }
        }

        public static string ResolveDataDirectory(IConfiguration configuration)
        {
            var customDataDir = configuration["DATA_DIR"] ?? configuration["DataDir"];
            if (!string.IsNullOrWhiteSpace(customDataDir))
            {
                return Path.GetFullPath(customDataDir);
            }

            var connStr = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("Sqlite")
                ?? configuration["ConnectionStrings:DefaultConnection"]
                ?? configuration["ConnectionStrings:Sqlite"];

            if (!string.IsNullOrWhiteSpace(connStr) && connStr.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(connStr, @"Data Source=([^;]+)", RegexOptions.IgnoreCase);
                if (match.Success && !match.Groups[1].Value.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    var sqliteFile = match.Groups[1].Value.Trim();
                    var dir = Path.GetDirectoryName(sqliteFile);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        return Path.GetFullPath(dir);
                    }
                }
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
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
