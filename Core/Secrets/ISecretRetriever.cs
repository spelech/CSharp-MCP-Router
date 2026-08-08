using System.Threading.Tasks;

namespace McpRouter.Core.Secrets
{
    /// <summary>
    /// Auto-generated XML documentation.
    /// </summary>
    public interface ISecretRetriever
    {
        string ProviderName { get; }
        Task<string?> GetSecretAsync(string secretPath, string keyName);
    }
}
