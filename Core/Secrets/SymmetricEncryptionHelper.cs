using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Secrets
{
    public static class SymmetricEncryptionHelper
    {
        private static byte[]? _cachedKey;

        private static byte[] GetEncryptionKey(IConfiguration config)
        {
            if (_cachedKey != null) return _cachedKey;

            var secretString = EncryptionKeyProvider.GetRouterSecret(config);

            // Hash with SHA-256 to ensure a solid 256-bit key
            using (var sha256 = SHA256.Create())
            {
                _cachedKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretString));
            }
            return _cachedKey;
        }

        public static string Encrypt(string plaintext, IConfiguration config)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;

            var keyBytes = GetEncryptionKey(config);
            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    // Write IV first
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs, Encoding.UTF8))
                    {
                        writer.Write(plaintext);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string ciphertext, IConfiguration config)
        {
            if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

            try
            {
                var fullCipher = Convert.FromBase64String(ciphertext);
                if (fullCipher.Length < 16) return string.Empty;

                var keyBytes = GetEncryptionKey(config);
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
                // Return empty or throw depending on design, returning empty is safe for invalid tokens
                return string.Empty;
            }
        }
    }
}
