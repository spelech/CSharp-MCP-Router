using System.Threading.Tasks;

namespace McpRouter.Infrastructure.Secrets
{
    public interface ISecretRetriever
    {
        string ProviderName { get; }
        Task<string?> GetSecretAsync(string secretPath, string keyName);
    }
}
