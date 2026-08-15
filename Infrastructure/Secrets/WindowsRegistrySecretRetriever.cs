using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace McpRouter.Infrastructure.Secrets
{
    public class WindowsRegistrySecretRetriever : ISecretRetriever
    {
        private readonly IRegistryAccessor _registryAccessor;
        private readonly IDpapiProtector _dpapiProtector;

        public WindowsRegistrySecretRetriever(IRegistryAccessor? registryAccessor = null, IDpapiProtector? dpapiProtector = null)
        {
            _registryAccessor = registryAccessor ?? new WindowsRegistryAccessor();
            _dpapiProtector = dpapiProtector ?? new WindowsDpapiProtector();
        }

        public string ProviderName => "WindowsRegistry";

        public Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            try
            {
                var val = _registryAccessor.GetValue(secretPath, keyName);
                if (val is string strVal)
                {
                    return Task.FromResult<string?>(strVal);
                }
                else if (val is byte[] rawBytes)
                {
#pragma warning disable CA1416
                    byte[] decrypted = _dpapiProtector.Unprotect(rawBytes, null, DataProtectionScope.LocalMachine);
#pragma warning restore CA1416
                    return Task.FromResult<string?>(Encoding.UTF8.GetString(decrypted));
                }
            }
            catch
            {
                // Fallback / handling on unreadable key or decryption failure
            }

            return Task.FromResult<string?>(null);
        }
    }
}
