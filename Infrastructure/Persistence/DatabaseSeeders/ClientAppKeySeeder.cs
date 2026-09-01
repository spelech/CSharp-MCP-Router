using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;

namespace ModelContextGateway.Infrastructure.Persistence.DatabaseSeeders
{
    public static class ClientAppKeySeeder
    {
        public static void SeedDefaultClientsAndKeys(IDbConnectionFactory dbFactory, ILogger logger, IConfiguration configuration)
        {
            // Explicitly delete/invalidate any unusable 'mcp_' client credentials and log a warning/info message
            try
            {
                using var conn = dbFactory.CreateConnection();
                var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM AppKeys WHERE KeyPrefix LIKE 'mcp!_%' ESCAPE '!' OR EncryptedKey LIKE 'mcp!_%' ESCAPE '!';");
                if (count > 0)
                {
                    conn.Execute("DELETE FROM AppKeys WHERE KeyPrefix LIKE 'mcp!_%' ESCAPE '!' OR EncryptedKey LIKE 'mcp!_%' ESCAPE '!';");
                    logger.LogWarning($"Deleted {count} invalid legacy 'mcp_' client credential records from AppKeys table which were stored in plaintext and unusable.");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clean up legacy invalid 'mcp_' client credentials.");
            }

            // AppKey Hashing Migration: migrate legacy AES-CBC encrypted AppKeys to SHA-256 hashes (gated by RUN_KEY_MIGRATION flag)
            try
            {
                var runKeyMigration = configuration["MCG_RUN_KEY_MIGRATION"]
                    ?? configuration["KeyMigration:Enabled"]
                    ?? configuration["RUN_KEY_MIGRATION"]
                    ?? Environment.GetEnvironmentVariable("MCG_RUN_KEY_MIGRATION")
                    ?? Environment.GetEnvironmentVariable("RUN_KEY_MIGRATION");
                if (string.Equals(runKeyMigration, "true", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = dbFactory.CreateConnection();
                    var appKeys = conn.Query<AppKey>("SELECT Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt FROM AppKeys").ToList();
                    foreach (var key in appKeys)
                    {
                        if (string.IsNullOrEmpty(key.EncryptedKey))
                        {
                            continue;
                        }

                        bool isHashed = key.EncryptedKey.Length == 64
                            && key.EncryptedKey.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

                        if (!isHashed)
                        {
                            var decrypted = DecryptLegacyAppKey(key.EncryptedKey, configuration);
                            if (string.IsNullOrEmpty(decrypted))
                            {
                                logger.LogError($"AppKey Hashing Migration: Failed to decrypt legacy AppKey '{key.Name}' (Id: {key.Id}). Skipping migration for this key to prevent corruption.");
                                continue;
                            }

                            using var sha256 = SHA256.Create();
                            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(decrypted));
                            var hashedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();
                            conn.Execute("UPDATE AppKeys SET EncryptedKey = @EncryptedKey WHERE Id = @Id", new { EncryptedKey = hashedKey, key.Id });
                        }
                    }
                    logger.LogInformation("Migrated legacy AppKeys to SHA-256 hashes.");
                }
                else
                {
                    logger.LogInformation("AppKey legacy-key migration skipped. Set MCG_RUN_KEY_MIGRATION=true for a one-time migration.");
                }
            }
            catch (Exception exKeyMig)
            {
                logger.LogWarning(exKeyMig, "AppKey hashing migration warning");
            }

            // Seed Admin AppKey from environment (MCG_ADMIN_AUTH_KEY / MCG_ADMIN_KEY) or default CLI key if none exists
            try
            {
                var customAdminKey = configuration["MCG_ADMIN_AUTH_KEY"]
                    ?? configuration["MCG_ADMIN_KEY"]
                    ?? Environment.GetEnvironmentVariable("MCG_ADMIN_AUTH_KEY")
                    ?? Environment.GetEnvironmentVariable("MCG_ADMIN_KEY");

                using var conn = dbFactory.CreateConnection();

                if (!string.IsNullOrWhiteSpace(customAdminKey))
                {
                    var trimmedKey = customAdminKey.Trim();
                    var keyPrefix = AppKeyAuthenticationHandler.ExtractKeyPrefix(trimmedKey);

                    using var sha256 = SHA256.Create();
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(trimmedKey));
                    var hashedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();

                    var existingKey = conn.QueryFirstOrDefault<AppKey>(
                        "SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix;",
                        new { KeyPrefix = keyPrefix });

                    if (existingKey == null)
                    {
                        var adminKey = new AppKey
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Name = "Configured Admin Key",
                            Username = "admin",
                            OwnerSid = string.Empty,
                            KeyType = "system",
                            KeyPrefix = keyPrefix,
                            EncryptedKey = hashedKey,
                            ScopesJson = "[\"all\",\"admin\"]",
                            ExpiresAt = null,
                            CreatedAt = DateTime.UtcNow
                        };

                        SaveAppKeyToDb(conn, dbFactory.ProviderName, adminKey);
                        logger.LogInformation("Seeded custom Admin AppKey from environment configuration for 'admin' (Prefix: {Prefix}).", keyPrefix);
                    }
                    else if (!string.Equals(existingKey.EncryptedKey, hashedKey, StringComparison.OrdinalIgnoreCase))
                    {
                        conn.Execute(
                            "UPDATE AppKeys SET EncryptedKey = @EncryptedKey, ScopesJson = '[\"all\",\"admin\"]', KeyType = 'system' WHERE Id = @Id;",
                            new { EncryptedKey = hashedKey, existingKey.Id });
                        logger.LogInformation("Updated Admin AppKey hash from environment configuration for 'admin' (Prefix: {Prefix}).", keyPrefix);
                    }
                }
                else
                {
                    // Check if ANY active system/admin AppKey already exists in the database
                    var existingAdminKey = conn.QueryFirstOrDefault<string>(
                        "SELECT Id FROM AppKeys WHERE ScopesJson LIKE '%admin%' OR KeyType = 'system';");

                    if (string.IsNullOrEmpty(existingAdminKey))
                    {
                        const string base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                        var selector = RandomNumberGenerator.GetString(base62Chars, 8);
                        var secret = RandomNumberGenerator.GetString(base62Chars, 16);
                        var keyPrefix = $"mcp-adm-{selector}";
                        var generatedPlaintextKey = $"{keyPrefix}-{secret}";

                        using var sha256 = SHA256.Create();
                        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(generatedPlaintextKey));
                        var hashedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();

                        var defaultKey = new AppKey
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Name = "Auto-Generated System Admin Key",
                            Username = "admin",
                            OwnerSid = string.Empty,
                            KeyType = "system",
                            KeyPrefix = keyPrefix,
                            EncryptedKey = hashedKey,
                            ScopesJson = "[\"all\",\"admin\"]",
                            ExpiresAt = null,
                            CreatedAt = DateTime.UtcNow
                        };

                        SaveAppKeyToDb(conn, dbFactory.ProviderName, defaultKey);

                        // Persist to .admin.key file in data directory for host admin retrieval
                        try
                        {
                            string dataDir = DbKeyHelper.ResolveDataDirectory(configuration);
                            Directory.CreateDirectory(dataDir);
                            string adminKeyPath = Path.Combine(dataDir, ".admin.key");
                            File.WriteAllText(adminKeyPath, generatedPlaintextKey);
                            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                            {
                                File.SetUnixFileMode(adminKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                            }
                        }
                        catch (Exception exFile)
                        {
                            logger.LogWarning(exFile, "Could not write .admin.key file to data directory.");
                        }

                        logger.LogInformation("Seeded auto-generated high-entropy Admin AppKey for 'admin' (Prefix: {Prefix}, Key written to .admin.key).", keyPrefix);
                    }
                }

                // Seed Client AppKeys from environment (MCG_CLIENT_APP_KEYS / MCG_CLIENT_KEYS / MCG_DEFAULT_CLIENT_KEY) or auto-generate default .client.key
                try
                {
                    var customClientKeys = configuration["MCG_CLIENT_APP_KEYS"]
                        ?? configuration["MCG_CLIENT_KEYS"]
                        ?? configuration["MCG_APP_KEYS"]
                        ?? configuration["MCG_DEFAULT_CLIENT_KEY"]
                        ?? Environment.GetEnvironmentVariable("MCG_CLIENT_APP_KEYS")
                        ?? Environment.GetEnvironmentVariable("MCG_CLIENT_KEYS")
                        ?? Environment.GetEnvironmentVariable("MCG_APP_KEYS")
                        ?? Environment.GetEnvironmentVariable("MCG_DEFAULT_CLIENT_KEY");

                    if (!string.IsNullOrWhiteSpace(customClientKeys))
                    {
                        var keyEntries = customClientKeys.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var rawEntry in keyEntries)
                        {
                            if (string.IsNullOrWhiteSpace(rawEntry))
                            {
                                continue;
                            }

                            // Format: token[:Name[:scope1;scope2...]]
                            var parts = rawEntry.Split(':', 3);
                            var token = parts[0].Trim();
                            if (string.IsNullOrWhiteSpace(token))
                            {
                                continue;
                            }

                            var keyPrefix = AppKeyAuthenticationHandler.ExtractKeyPrefix(token);
                            var keyName = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : $"Configured Client Key ({keyPrefix})";
                            var scopesList = new List<string> { "all" };
                            if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
                            {
                                scopesList = parts[2].Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                                if (scopesList.Count == 0)
                                {
                                    scopesList.Add("all");
                                }
                            }

                            using var sha256 = SHA256.Create();
                            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
                            var hashedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();
                            var scopesJson = System.Text.Json.JsonSerializer.Serialize(scopesList);

                            var existingKey = conn.QueryFirstOrDefault<AppKey>(
                                "SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix;",
                                new { KeyPrefix = keyPrefix });

                            if (existingKey == null)
                            {
                                var clientKey = new AppKey
                                {
                                    Id = Guid.NewGuid().ToString("N"),
                                    Name = keyName,
                                    Username = "user",
                                    OwnerSid = string.Empty,
                                    KeyType = "personal",
                                    KeyPrefix = keyPrefix,
                                    EncryptedKey = hashedKey,
                                    ScopesJson = scopesJson,
                                    ExpiresAt = null,
                                    CreatedAt = DateTime.UtcNow
                                };

                                SaveAppKeyToDb(conn, dbFactory.ProviderName, clientKey);
                                logger.LogInformation("Seeded custom Client AppKey '{Name}' from environment configuration (Prefix: {Prefix}, Scopes: {Scopes}).", keyName, keyPrefix, scopesJson);
                            }
                            else if (!string.Equals(existingKey.EncryptedKey, hashedKey, StringComparison.OrdinalIgnoreCase))
                            {
                                conn.Execute(
                                    "UPDATE AppKeys SET EncryptedKey = @EncryptedKey, ScopesJson = @ScopesJson, Name = @Name WHERE Id = @Id;",
                                    new { EncryptedKey = hashedKey, ScopesJson = scopesJson, Name = keyName, existingKey.Id });
                                logger.LogInformation("Updated Client AppKey '{Name}' hash from environment configuration (Prefix: {Prefix}).", keyName, keyPrefix);
                            }
                        }
                    }
                    else
                    {
                        // Check if ANY client AppKey (non-system / non-admin) already exists in the database
                        var existingClientKey = conn.QueryFirstOrDefault<string>(
                            "SELECT Id FROM AppKeys WHERE KeyType = 'personal' OR (ScopesJson NOT LIKE '%admin%' AND KeyType != 'system');");

                        if (string.IsNullOrEmpty(existingClientKey))
                        {
                            const string base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                            var selector = RandomNumberGenerator.GetString(base62Chars, 8);
                            var secret = RandomNumberGenerator.GetString(base62Chars, 16);
                            var keyPrefix = $"mcp-glb-{selector}";
                            var generatedPlaintextKey = $"{keyPrefix}-{secret}";

                            using var sha256 = SHA256.Create();
                            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(generatedPlaintextKey));
                            var hashedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();

                            var defaultClientKey = new AppKey
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                Name = "Auto-Generated Default Client Key",
                                Username = "user",
                                OwnerSid = string.Empty,
                                KeyType = "personal",
                                KeyPrefix = keyPrefix,
                                EncryptedKey = hashedKey,
                                ScopesJson = "[\"all\"]",
                                ExpiresAt = null,
                                CreatedAt = DateTime.UtcNow
                            };

                            SaveAppKeyToDb(conn, dbFactory.ProviderName, defaultClientKey);

                            // Persist to .client.key file in data directory for AI IDE and client tool connection
                            try
                            {
                                string dataDir = DbKeyHelper.ResolveDataDirectory(configuration);
                                Directory.CreateDirectory(dataDir);
                                string clientKeyPath = Path.Combine(dataDir, ".client.key");
                                File.WriteAllText(clientKeyPath, generatedPlaintextKey);
                                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                                {
                                    try { File.SetUnixFileMode(clientKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { }
                                }
                            }
                            catch (Exception exFile)
                            {
                                logger.LogWarning(exFile, "Could not write .client.key file to data directory.");
                            }

                            logger.LogInformation("Seeded auto-generated high-entropy Client AppKey for 'user' (Prefix: {Prefix}, Key written to .client.key).", keyPrefix);
                        }
                    }
                }
                catch (Exception exSeedClientKey)
                {
                    logger.LogWarning(exSeedClientKey, "Default Client AppKey seeder warning");
                }
            }
            catch (Exception exSeedKey)
            {
                logger.LogWarning(exSeedKey, "Default AppKey seeder warning");
            }
        }

        private static void SaveAppKeyToDb(IDbConnection conn, string providerName, AppKey key)
        {
            if (providerName == "sqlite")
            {
                const string insertSql = @"
                    INSERT INTO AppKeys (Id, Name, Username, OwnerSid, KeyType, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt)
                    VALUES (@Id, @Name, @Username, @OwnerSid, @KeyType, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @CreatedAt);";
                conn.Execute(insertSql, key);
            }
            else if (providerName == "mysql")
            {
                conn.Execute("sp_SaveAppKey", new
                {
                    p_Id = key.Id,
                    p_Name = key.Name,
                    p_Username = key.Username,
                    p_KeyPrefix = key.KeyPrefix,
                    p_EncryptedKey = key.EncryptedKey,
                    p_ScopesJson = key.ScopesJson,
                    p_OwnerSid = key.OwnerSid ?? "",
                    p_KeyType = string.IsNullOrEmpty(key.KeyType) ? "system" : key.KeyType,
                    p_ExpiresAt = key.ExpiresAt
                }, commandType: CommandType.StoredProcedure);
            }
            else
            {
                conn.Execute("sp_SaveAppKey", new
                {
                    key.Id,
                    key.Name,
                    key.Username,
                    KeyType = string.IsNullOrEmpty(key.KeyType) ? "system" : key.KeyType,
                    key.KeyPrefix,
                    key.EncryptedKey,
                    key.ScopesJson,
                    key.OwnerSid,
                    key.ExpiresAt
                }, commandType: CommandType.StoredProcedure);
            }
        }

        private static string DecryptLegacyAppKey(string ciphertext, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(ciphertext))
            {
                return string.Empty;
            }

            try
            {
                var fullCipher = Convert.FromBase64String(ciphertext);
                if (fullCipher.Length < 16)
                {
                    return string.Empty;
                }

                var secretString = configuration["MCG_SECRET"]
                    ?? configuration["MCG_MASTER_KEY"]
                    ?? DbKeyHelper.ResolveDbEncryptionKey(configuration);

                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretString));
                }

                using var aes = System.Security.Cryptography.Aes.Create();
                aes.Key = keyBytes;

                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
