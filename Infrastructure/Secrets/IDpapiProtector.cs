using System.Security.Cryptography;

namespace ModelContextGateway.Infrastructure.Secrets
{
    public interface IDpapiProtector
    {
        byte[] Unprotect(byte[] encryptedData, byte[]? optionalEntropy, DataProtectionScope scope);
        byte[] Protect(byte[] userData, byte[]? optionalEntropy, DataProtectionScope scope);
    }

    public class WindowsDpapiProtector : IDpapiProtector
    {
        public byte[] Unprotect(byte[] encryptedData, byte[]? optionalEntropy, DataProtectionScope scope)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("DPAPI is only supported on Windows.");
            }
#pragma warning disable CA1416
            return ProtectedData.Unprotect(encryptedData, optionalEntropy, scope);
#pragma warning restore CA1416
        }

        public byte[] Protect(byte[] userData, byte[]? optionalEntropy, DataProtectionScope scope)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("DPAPI is only supported on Windows.");
            }
#pragma warning disable CA1416
            return ProtectedData.Protect(userData, optionalEntropy, scope);
#pragma warning restore CA1416
        }
    }
}
