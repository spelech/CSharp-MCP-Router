using System;
using System.Threading.Tasks;

namespace McpRouter.Core.Secrets
{
    public class EnvironmentSecretRetriever : ISecretRetriever
    {
        public string ProviderName => "Environment";

        public Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            try
            {
                // First try direct lookup by keyName
                var val = Environment.GetEnvironmentVariable(keyName);
                if (string.IsNullOrEmpty(val) && !string.IsNullOrEmpty(secretPath))
                {
                    // Or try looking up by secretPath if it contains the variable name
                    val = Environment.GetEnvironmentVariable(secretPath);
                }
                return Task.FromResult<string?>(val);
            }
            catch
            {
                // Safe fallback returning null
                return Task.FromResult<string?>(null);
            }
        }
    }
}
