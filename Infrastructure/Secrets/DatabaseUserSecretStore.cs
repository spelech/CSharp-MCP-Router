using System.Threading.Tasks;

namespace McpRouter.Infrastructure.Secrets
{
    public class DatabaseUserSecretStore : IUserSecretStore
    {
        public Task<string?> GetSecretAsync(string username, string serverId)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
