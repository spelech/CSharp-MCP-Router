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
                if (string.IsNullOrEmpty(address))
                {
                    _vaultClient = null;
                    return null;
                }

                if (!address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Vault Address must use the HTTPS scheme.");
                }

                var roleId = _config["Vault:RoleId"];
                var secretId = _config["Vault:SecretId"];
                if (string.IsNullOrEmpty(roleId) || string.IsNullOrEmpty(secretId))
                {
                    _vaultClient = null;
                    return null;
                }

                var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
                var settings = new VaultClientSettings(address, authMethod);
                _vaultClient = new VaultClient(settings);

                return _vaultClient;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            string mountPoint = "secret";
            string path = secretPath;

            if (secretPath.Contains(':'))
            {
                var parts = secretPath.Split(':', 2);
                mountPoint = parts[0];
                path = parts[1];
            }

            // 1. Check cache FIRST before client creation or network operations
            string cacheKey = $"vault:{mountPoint}:{path}:{keyName}";
            if (_cache.TryGetValue(cacheKey, out string? cached) && cached != null)
            {
                return cached;
            }

            var client = await EnsureVaultClientAsync();
            if (client == null) return null;

            // 2. Perform JIT renewal check on token TTL
            long ttlSeconds = 1200;
            try
            {
                var tokenInfoSecret = await client.V1.Auth.Token.LookupSelfAsync();
                if (tokenInfoSecret?.Data != null)
                {
                    ttlSeconds = tokenInfoSecret.Data.TimeToLive;
                }
                else
                {
                    ttlSeconds = 0; // Null token data -> force re-login / client recreation
                }
            }
            catch
            {
                ttlSeconds = 0; // Exception -> force re-login / client recreation
            }

            if (ttlSeconds < 300) // Under 5 minutes remaining (or force re-login on exception/0)
            {
                client = await EnsureVaultClientAsync(forceRecreate: true);
                if (client == null) return null;
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
            catch (Exception ex)
            {
                throw new System.Security.SecurityException($"Vault secret read failed for path '{secretPath}', key '{keyName}'.", ex);
            }
        }
    }
}
