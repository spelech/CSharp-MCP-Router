using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using McpRouter.Models;

using McpRouter.Core.Database;
using Dapper;

namespace McpRouter.Services.DatabaseSeeders
{
    public static class ClientAppKeySeeder
    {
        public static void SeedDefaultClientsAndKeys(IDbConnectionFactory dbFactory, ILogger logger, IConfiguration configuration)
        {
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
                    ?? McpRouter.Core.Secrets.DbKeyHelper.ResolveDbEncryptionKey(configuration);

                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretString));
                }

                using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.Key = keyBytes;

                    var iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
