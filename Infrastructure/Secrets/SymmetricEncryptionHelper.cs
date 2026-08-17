using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Infrastructure.Secrets
{
    public static class SymmetricEncryptionHelper
    {
        private static readonly object KeyLock = new object();
        private static (string secretString, byte[] key)? _cachedKey;

        private static byte[] DeriveKey(string secretString)
        {
            if (_cachedKey.HasValue && _cachedKey.Value.secretString == secretString)
            {
                return _cachedKey.Value.key;
            }

            lock (KeyLock)
            {
                if (_cachedKey.HasValue && _cachedKey.Value.secretString == secretString)
                {
                    return _cachedKey.Value.key;
                }

                var secretBytes = Encoding.UTF8.GetBytes(secretString);
                var deploymentSalt = SHA256.HashData(Encoding.UTF8.GetBytes(secretString + "_McpRouter_Salt_v2"));
                var derivedKey = Rfc2898DeriveBytes.Pbkdf2(secretBytes, deploymentSalt, 600_000, HashAlgorithmName.SHA256, 32);
                _cachedKey = (secretString, derivedKey);

                return derivedKey;
            }
        }

        private static byte[] GetEncryptionKey(IConfiguration config)
        {
            var secretString = config["ROUTER_SECRET"]
                ?? config["ROUTER_MASTER_KEY"]
                ?? DbKeyHelper.ResolveDbEncryptionKey(config);

            return DeriveKey(secretString);
        }

        public static string Encrypt(string plaintext, IConfiguration config)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;

            var keyBytes = GetEncryptionKey(config);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var ciphertextBytes = new byte[plaintextBytes.Length];

            using var aesGcm = new AesGcm(keyBytes, 16);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertextBytes, tag);

            // Pack: Nonce (12) + Tag (16) + Ciphertext (N)
            var result = new byte[nonce.Length + tag.Length + ciphertextBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertextBytes, 0, result, nonce.Length + tag.Length, ciphertextBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static bool TryDecrypt(string ciphertext, IConfiguration config, out string plaintext)
        {
            plaintext = ciphertext;
            if (string.IsNullOrEmpty(ciphertext)) return true;

            byte[] fullCipher;
            try
            {
                fullCipher = Convert.FromBase64String(ciphertext);
            }
            catch
            {
                return true; // Not valid base64 -> treat as raw plaintext
            }

            if (fullCipher.Length < 28) // 12 nonce + 16 tag minimum
            {
                return true; // Payload too short -> treat as raw plaintext
            }

            var primaryKey = GetEncryptionKey(config);
            if (TryDecryptPayload(fullCipher, primaryKey, out plaintext))
            {
                return true;
            }

            var legacySecret = config["DB_ENCRYPTION_KEY"] ?? config["ROUTER_MASTER_KEY"];
            if (!string.IsNullOrEmpty(legacySecret))
            {
                var legacyKey = DeriveKey(legacySecret);
                if (TryDecryptPayload(fullCipher, legacyKey, out plaintext))
                {
                    return true;
                }
            }

            System.Console.WriteLine($"[SymmetricEncryptionHelper] WARNING: Decryption failed for payload. Key tag mismatch or payload corrupted.");
            plaintext = ciphertext; // preserve original ciphertext
            return false;
        }

        public static string Decrypt(string ciphertext, IConfiguration config)
        {
            if (TryDecrypt(ciphertext, config, out var plaintext))
            {
                return plaintext;
            }
            return string.Empty; // match original failure behavior
        }

        private static bool TryDecryptPayload(byte[] fullCipher, byte[] keyBytes, out string plaintext)
        {
            plaintext = string.Empty;
            try
            {
                var nonce = new byte[12];
                var tag = new byte[16];
                var cipherBytes = new byte[fullCipher.Length - 28];

                Array.Copy(fullCipher, 0, nonce, 0, 12);
                Array.Copy(fullCipher, 12, tag, 0, 16);
                Array.Copy(fullCipher, 28, cipherBytes, 0, cipherBytes.Length);

                var plaintextBytes = new byte[cipherBytes.Length];
                using var aesGcm = new AesGcm(keyBytes, 16);
                aesGcm.Decrypt(nonce, cipherBytes, tag, plaintextBytes);

                plaintext = Encoding.UTF8.GetString(plaintextBytes);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}

