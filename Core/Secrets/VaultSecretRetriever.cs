using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;

namespace McpRouter.Core.Secrets
{
    public class VaultSecretRetriever : ISecretRetriever
    {
        private readonly IVaultClient? _vaultClient;
        private readonly IMemoryCache _cache;

        public string ProviderName => "HashiCorpVault";

        public VaultSecretRetriever(IConfiguration config, IMemoryCache cache)
        {
            _cache = cache;
            try
            {
                var address = config["Vault:Address"];
                if (!string.IsNullOrEmpty(address))
                {
                    if (!address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Vault Address must use the HTTPS scheme.");
                    }
                }
                var roleId = config["Vault:RoleId"];
                var secretId = config["Vault:SecretId"];

                if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(roleId) && !string.IsNullOrEmpty(secretId))
                {
                    var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
                    var settings = new VaultClientSettings(address, authMethod);
                    _vaultClient = new VaultClient(settings);
                }
            }
            catch
            {
                // Suppress and protect configuration metadata details
                _vaultClient = null;
            }
        }

        public async Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            if (_vaultClient == null) return null;

            string cacheKey = $"vault:{secretPath}:{keyName}";
            if (_cache.TryGetValue(cacheKey, out string? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var secretData = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: secretPath);
                var value = secretData.Data.Data[keyName]?.ToString();
                if (value != null)
                {
                    _cache.Set(cacheKey, value, TimeSpan.FromMinutes(10));
                }
                return value;
            }
            catch
            {
                // Ensure no details/paths or connection details leak publicly
                return null;
            }
        }
    }
}
