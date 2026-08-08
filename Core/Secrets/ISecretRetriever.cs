using System.Threading.Tasks;

namespace McpRouter.Core.Secrets
{
    /// <summary>
    /// Defines a pluggable secret retriever interface for fetching downstream server credentials and API tokens.
    /// </summary>
    public interface ISecretRetriever
    {
        /// <summary>
        /// Gets the provider code name (e.g., Vault, WindowsRegistry, Environment).
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Asynchronously retrieves a secret value for the specified path and key.
        /// </summary>
        /// <param name="secretPath">The secret store path or environment variable name.</param>
        /// <param name="keyName">The specific field or property key within the secret payload.</param>
        /// <returns>The secret value if retrieved successfully; otherwise <c>null</c>.</returns>
        Task<string?> GetSecretAsync(string secretPath, string keyName);
    }
}
