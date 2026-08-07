using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Secrets
{
    public static class SymmetricEncryptionHelper
    {
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("McpRouter_SymmetricEncryption_Salt_2026");
        private static (string secretString, byte[] key)? _cachedKey;

        private static byte[] GetEncryptionKey(IConfiguration config)
        {
            var secretString = config["ROUTER_SECRET"]
                ?? DbKeyHelper.ResolveDbEncryptionKey(config);

            if (_cachedKey.HasValue && _cachedKey.Value.secretString == secretString)
            {
                return _cachedKey.Value.key;
            }

            var secretBytes = Encoding.UTF8.GetBytes(secretString);
            var derivedKey = Rfc2898DeriveBytes.Pbkdf2(secretBytes, Salt, 600_000, HashAlgorithmName.SHA256, 32);
            _cachedKey = (secretString, derivedKey);

            return derivedKey;
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

            try
            {
                var fullCipher = Convert.FromBase64String(ciphertext);
                if (fullCipher.Length >= 28) // 12 nonce + 16 tag minimum
                {
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
            catch
            {
                // Fall back to DecryptLegacy if AES-GCM fails (e.g., legacy AES-CBC formatted values)
            }

            return DecryptLegacy(ciphertext, config);
        }

        public static string DecryptLegacy(string ciphertext, IConfiguration config)
        {
            if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

            try
            {
                var fullCipher = Convert.FromBase64String(ciphertext);
                if (fullCipher.Length < 16) return string.Empty;

                var secretString = config["ROUTER_SECRET"]
                    ?? DbKeyHelper.ResolveDbEncryptionKey(config);

                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretString));
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;

                    var iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
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
