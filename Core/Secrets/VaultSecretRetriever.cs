using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;

namespace McpRouter.Core.Secrets
{
    public class VaultSecretRetriever : ISecretRetriever
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly Func<IVaultClient>? _vaultClientFactory;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private IVaultClient? _vaultClient;

        public string ProviderName => "HashiCorpVault";

        public VaultSecretRetriever(IConfiguration config, IMemoryCache cache)
            : this(config, cache, null)
        {
        }

        public VaultSecretRetriever(IConfiguration config, IMemoryCache cache, Func<IVaultClient>? vaultClientFactory)
        {
            _config = config;
            _cache = cache;
            _vaultClientFactory = vaultClientFactory;
        }

        public async Task<IVaultClient?> EnsureVaultClientAsync(bool forceRecreate = false)
        {
            if (_vaultClient != null && !forceRecreate)
            {
                return _vaultClient;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (_vaultClient != null && !forceRecreate)
                {
                    return _vaultClient;
                }

                if (_vaultClientFactory != null)
                {
                    _vaultClient = _vaultClientFactory();
                    return _vaultClient;
                }

                var address = _config["Vault:Address"];
                var roleId = _config["Vault:RoleId"];
                var secretId = _config["Vault:SecretId"];

                if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(roleId) && !string.IsNullOrEmpty(secretId))
                {
                    var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
                    var settings = new VaultClientSettings(address, authMethod);
                    _vaultClient = new VaultClient(settings);
                }
                else
                {
                    _vaultClient = null;
                }

                return _vaultClient;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            var client = await EnsureVaultClientAsync();
            if (client == null) return null;

            // Perform JIT renewal check on token TTL
            long ttlSeconds = 1200; // Fallback to 20 minutes on failure/null
            try
            {
                var tokenInfoSecret = await client.V1.Auth.Token.LookupSelfAsync();
                if (tokenInfoSecret?.Data != null)
                {
                    ttlSeconds = tokenInfoSecret.Data.TimeToLive;
                }
            }
            catch
            {
                ttlSeconds = 1200; // Fallback to 20 mins on failure/null
            }

            if (ttlSeconds < 300) // Under 5 minutes remaining
            {
                client = await EnsureVaultClientAsync(forceRecreate: true);
                if (client == null) return null;
            }

            string mountPoint = "secret";
            string path = secretPath;

            if (secretPath.Contains(':'))
            {
                var parts = secretPath.Split(':', 2);
                mountPoint = parts[0];
                path = parts[1];
            }

            string cacheKey = $"vault:{mountPoint}:{path}:{keyName}";
            if (_cache.TryGetValue(cacheKey, out string? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var secretData = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: path, mountPoint: mountPoint);
                var value = secretData.Data.Data[keyName]?.ToString();
                if (value != null)
                {
                    _cache.Set(cacheKey, value, TimeSpan.FromMinutes(10));
                }
                return value;
            }
            catch
            {
                return null;
            }
        }
    }
}
