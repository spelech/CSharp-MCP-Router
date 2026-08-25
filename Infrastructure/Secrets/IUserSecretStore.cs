namespace ModelContextGateway.Infrastructure.Secrets
{
    public interface IUserSecretStore
    {
        Task<string?> GetSecretAsync(string username, string serverId);
        Task SaveSecretAsync(string username, string serverId, string secretJson);
        Task DeleteSecretAsync(string username, string serverId);
        Task<System.Collections.Generic.IEnumerable<string>> GetServerIdsAsync(string username);
    }
}
