using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace McpRouter.Core.Secrets
{
    public class WindowsRegistrySecretRetriever : ISecretRetriever
    {
        public string ProviderName => "WindowsRegistry";

        public Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Task.FromResult<string?>(null);
            }

            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var subKey = baseKey.OpenSubKey(secretPath);
                if (subKey == null) return Task.FromResult<string?>(null);

                var val = subKey.GetValue(keyName);
                if (val is string strVal)
                {
                    return Task.FromResult<string?>(strVal);
                }
                else if (val is byte[] rawBytes)
                {
                    byte[] decrypted = ProtectedData.Unprotect(rawBytes, null, DataProtectionScope.LocalMachine);
                    return Task.FromResult<string?>(Encoding.UTF8.GetString(decrypted));
                }
            }
            catch
            {
                // Fallback / handling on unreadable key or non-Windows platform
            }

            return Task.FromResult<string?>(null);
        }
    }
}
