using System.Data;

namespace McpRouter.Infrastructure.Persistence
{
    public interface ISqlDialectStrategy
    {
        string ProviderName { get; }
        Task SaveServerAsync(IDbConnection conn, McpServer server);
        Task<IEnumerable<AppKey>> GetAppKeysAsync(IDbConnection conn, string? usernameFilter, bool isAdmin, string? currentUser, string? keyType = null);
        Task SaveAppKeyAsync(IDbConnection conn, AppKey key);
        Task DeleteAppKeyAsync(IDbConnection conn, string id);
        Task SaveSecretProviderAsync(IDbConnection conn, SecretProviderDto dto, string? encryptedConfig);
        Task SaveAuthProviderAsync(IDbConnection conn, AuthProviderDto dto, string? encryptedConfig);
    }
}
