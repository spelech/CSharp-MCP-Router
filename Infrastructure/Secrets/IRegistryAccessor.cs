using System;
using Microsoft.Win32;

namespace McpRouter.Infrastructure.Secrets
{
    public interface IRegistryAccessor
    {
        object? GetValue(string subKeyPath, string valueName);
    }

    public class WindowsRegistryAccessor : IRegistryAccessor
    {
        public object? GetValue(string subKeyPath, string valueName)
        {
            if (!OperatingSystem.IsWindows()) return null;
#pragma warning disable CA1416
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var subKey = baseKey.OpenSubKey(subKeyPath);
                return subKey?.GetValue(valueName);
            }
            catch
            {
                return null;
            }
#pragma warning restore CA1416
        }
    }
}
