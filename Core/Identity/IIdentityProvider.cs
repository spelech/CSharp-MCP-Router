using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Core.Identity
{
    public record UserIdentityContext(
        string Username,
        string AuthenticationType,
        List<string> GroupNames,
        string Sid = ""
    );

    public interface IIdentityProvider
    {
        string ProviderName { get; }
        Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext);
    }
}
