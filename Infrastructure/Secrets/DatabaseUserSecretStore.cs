using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using McpRouter.Infrastructure.Persistence;

namespace McpRouter.Infrastructure.Secrets
{
    public class DatabaseUserSecretStore : IUserSecretStore
    {
        private readonly IUserCredentialRepository _repo;
        private readonly IConfiguration _config;

        public DatabaseUserSecretStore(IUserCredentialRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        public async Task<string?> GetSecretAsync(string username, string serverId)
        {
            var cred = await _repo.GetCredentialAsync(username, serverId);
            if (cred == null || string.IsNullOrEmpty(cred.EncryptedSecretJson))
            {
                return null;
            }

            if (SymmetricEncryptionHelper.TryDecrypt(cred.EncryptedSecretJson, _config, out var plaintext))
            {
                return plaintext;
            }

            return null;
        }

        public async Task SaveSecretAsync(string username, string serverId, string secretJson)
        {
            var encrypted = SymmetricEncryptionHelper.Encrypt(secretJson, _config);
            var dto = new UserCredentialDto
            {
                Id = $"{username}_{serverId}",
                Username = username,
                ServerId = serverId,
                EncryptedSecretJson = encrypted
            };

            await _repo.SaveCredentialAsync(dto);
        }

        public async Task DeleteSecretAsync(string username, string serverId)
        {
            await _repo.DeleteCredentialAsync(username, serverId);
        }

        public async Task<System.Collections.Generic.IEnumerable<string>> GetServerIdsAsync(string username)
        {
            return await _repo.GetServerIdsAsync(username);
        }
    }
}
