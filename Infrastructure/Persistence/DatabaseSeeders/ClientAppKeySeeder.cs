using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Components.AppKeys;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Secrets;
using Dapper;

namespace McpRouter.Infrastructure.Persistence.DatabaseSeeders
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
                var runKeyMigration = configuration["KeyMigration:Enabled"] ?? configuration["RUN_KEY_MIGRATION"] ?? Environment.GetEnvironmentVariable("RUN_KEY_MIGRATION");
                if (string.Equals(runKeyMigration, "true", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = dbFactory.CreateConnection();
                    var appKeys = conn.Query<AppKey>("SELECT Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt FROM AppKeys").ToList();
                    foreach (var key in appKeys)
                    {
                        if (string.IsNullOrEmpty(key.EncryptedKey)) continue;

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
                    logger.LogInformation("AppKey legacy-key migration skipped. Set RUN_KEY_MIGRATION=true for a one-time migration.");
                }
            }
            catch (Exception exKeyMig)
            {
                logger.LogWarning(exKeyMig, "AppKey hashing migration warning");
            }

            // Seed Default Admin AppKey for CLI / system clients if none exists
            try
            {
                using var conn = dbFactory.CreateConnection();
                const string checkPrefix = "mcp-global-steve";
                var existingKey = conn.QueryFirstOrDefault<string>("SELECT Id FROM AppKeys WHERE KeyPrefix = @KeyPrefix;", new { KeyPrefix = checkPrefix });
                if (string.IsNullOrEmpty(existingKey))
                {
                    const string defaultPlaintextKey = "mcp-global-steve-default-cli-key-99";
                    using var sha256 = SHA256.Create();
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(defaultPlaintextKey));
                    var hashedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();

                    var defaultKey = new AppKey
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "CLI Default Admin Key",
                        Username = "steve",
                        KeyPrefix = checkPrefix,
                        EncryptedKey = hashedKey,
                        ScopesJson = "[\"all\"]",
                        ExpiresAt = null,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (dbFactory.ProviderName == "sqlite")
                    {
                        const string insertSql = @"
                            INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt)
                            VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @CreatedAt);";
                        conn.Execute(insertSql, defaultKey);
                    }
                    else if (dbFactory.ProviderName == "mysql")
                    {
                        conn.Execute("sp_SaveAppKey", new
                        {
                            p_Id = defaultKey.Id,
                            p_Name = defaultKey.Name,
                            p_Username = defaultKey.Username,
                            p_KeyPrefix = defaultKey.KeyPrefix,
                            p_EncryptedKey = defaultKey.EncryptedKey,
                            p_OwnerSid = defaultKey.OwnerSid ?? "",
                            p_ExpiresAt = defaultKey.ExpiresAt
                        }, commandType: System.Data.CommandType.StoredProcedure);
                    }
                    else
                    {
                        conn.Execute("sp_SaveAppKey", new
                        {
                            defaultKey.Id,
                            defaultKey.Name,
                            defaultKey.Username,
                            defaultKey.KeyPrefix,
                            defaultKey.EncryptedKey,
                            defaultKey.ScopesJson,
                            defaultKey.OwnerSid,
                            defaultKey.ExpiresAt
                        }, commandType: System.Data.CommandType.StoredProcedure);
                    }
                    logger.LogInformation("Seeded default CLI Admin AppKey for 'steve'.");
                }
            }
            catch (Exception exSeedKey)
            {
                logger.LogWarning(exSeedKey, "Default AppKey seeder warning");
            }
        }

        private static string DecryptLegacyAppKey(string ciphertext, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(ciphertext)) return string.Empty;

            try
            {
                var fullCipher = Convert.FromBase64String(ciphertext);
                if (fullCipher.Length < 16) return string.Empty;

                var secretString = configuration["ROUTER_SECRET"]
                    ?? configuration["ROUTER_MASTER_KEY"]
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

