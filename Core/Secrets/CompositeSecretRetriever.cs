using System.Collections.Generic;
using System.Threading.Tasks;

namespace McpRouter.Core.Secrets
{
    public class CompositeSecretRetriever : ISecretRetriever
    {
        private readonly IEnumerable<ISecretRetriever> _retrievers;
        public string ProviderName => "Composite";

        public CompositeSecretRetriever(IEnumerable<ISecretRetriever> retrievers)
        {
            _retrievers = retrievers;
        }

        public async Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            foreach (var retriever in _retrievers)
            {
                var secret = await retriever.GetSecretAsync(secretPath, keyName);
                if (!string.IsNullOrEmpty(secret))
                {
                    return secret;
                }
            }

            return null;
        }
    }
}
