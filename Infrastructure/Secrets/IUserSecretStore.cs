using System.Threading.Tasks;

namespace McpRouter.Infrastructure.Secrets
{
    public interface IUserSecretStore
    {
        Task<string?> GetSecretAsync(string username, string serverId);
        Task SaveSecretAsync(string username, string serverId, string secretJson);
        Task DeleteSecretAsync(string username, string serverId);
    }
}
