#pragma warning disable CA1416

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Core.Identity
{
    public class ActiveDirectoryIdentityProvider : IIdentityProvider
    {
        public string ProviderName => "ActiveDirectory";

        public Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            if (httpContext.User.Identity is WindowsIdentity winIdentity && winIdentity.IsAuthenticated)
            {
                var username = winIdentity.Name;
                var sid = winIdentity.User?.Value ?? "";
                var groups = winIdentity.Groups?
                    .Select(g => g.Value)
                    .ToList() ?? new List<string>();

                return Task.FromResult(new UserIdentityContext(username, ProviderName, groups, sid));
            }

            return Task.FromResult(new UserIdentityContext("anonymous", ProviderName, new List<string>()));
        }
    }
}
#pragma warning restore CA1416
