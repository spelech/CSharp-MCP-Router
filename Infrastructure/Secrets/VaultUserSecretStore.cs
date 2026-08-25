namespace ModelContextGateway.Infrastructure.Secrets
{
    public class VaultUserSecretStore : IUserSecretStore
    {
        private readonly VaultSecretRetriever _retriever;

        public VaultUserSecretStore(VaultSecretRetriever retriever)
        {
            _retriever = retriever;
        }

        public async Task<string?> GetSecretAsync(string username, string serverId)
        {
            return await _retriever.GetSecretAsync($"users/{username}/{serverId}", "secret");
        }

        public Task SaveSecretAsync(string username, string serverId, string secretJson)
        {
            throw new NotImplementedException("Vault save not implemented");
        }

        public Task DeleteSecretAsync(string username, string serverId)
        {
            throw new NotImplementedException("Vault delete not implemented");
        }

        public Task<System.Collections.Generic.IEnumerable<string>> GetServerIdsAsync(string username)
        {
            throw new NotImplementedException("Vault list not implemented");
        }
    }
}
