using System.Collections.Generic;
using System.Threading.Tasks;

namespace McpRouter.Infrastructure.Identity
{
    public interface ILdapService
    {
        Task<List<string>> ResolveUserSidsAsync(string username);
    }
}
