using System.Threading.Tasks;

namespace McpRouter.Core.Secrets
{
    public interface ISecretRetriever
    {
        string ProviderName { get; }
        Task<string?> GetSecretAsync(string secretPath, string keyName);
    }
}
