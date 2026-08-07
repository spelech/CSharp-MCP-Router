using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Secrets
{
    public static class SymmetricEncryptionHelper
    {
        private static readonly object KeyLock = new object();
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("McpRouter_SymmetricEncryption_Salt_2026");
        private static (string secretString, byte[] key)? _cachedKey;

        private static byte[] GetEncryptionKey(IConfiguration config)
        {
            var secretString = config["ROUTER_SECRET"]
                ?? config["ROUTER_MASTER_KEY"]
                ?? DbKeyHelper.ResolveDbEncryptionKey(config);

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

        public static string Decrypt(string ciphertext, IConfiguration config)
        {
            if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

            var fullCipher = Convert.FromBase64String(ciphertext);
            if (fullCipher.Length < 28) // 12 nonce + 16 tag minimum
            {
                throw new CryptographicException("Ciphertext payload is invalid or truncated.");
            }

            var keyBytes = GetEncryptionKey(config);
            var nonce = new byte[12];
            var tag = new byte[16];
            var cipherBytes = new byte[fullCipher.Length - 28];

            Array.Copy(fullCipher, 0, nonce, 0, 12);
            Array.Copy(fullCipher, 12, tag, 0, 16);
            Array.Copy(fullCipher, 28, cipherBytes, 0, cipherBytes.Length);

            var plaintextBytes = new byte[cipherBytes.Length];
            using var aesGcm = new AesGcm(keyBytes, 16);
            aesGcm.Decrypt(nonce, cipherBytes, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}
