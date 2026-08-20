using System.Threading.Tasks;

namespace McpRouter.Infrastructure.Secrets
{
    public interface IUserSecretStore
    {
        Task<string?> GetSecretAsync(string username, string serverId);
    }
}
