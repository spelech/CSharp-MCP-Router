using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Identity
{
    public class ActiveDirectoryIdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration? _configuration;

        public ActiveDirectoryIdentityProvider(IConfiguration? configuration = null)
        {
            _configuration = configuration;
        }

        public string ProviderName => "ActiveDirectory";

        public Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            var config = _configuration ?? (httpContext.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration);

            if (!TrustedProxyHelper.IsTrustedProxy(httpContext, config))
            {
                TrustedProxyHelper.StripUntrustedHeaders(httpContext);
                return Task.FromResult(new UserIdentityContext("anonymous", ProviderName, new List<string>()));
            }

            if (httpContext.User.Identity is WindowsIdentity winIdentity && winIdentity.IsAuthenticated)
            {
                var username = winIdentity.Name;
                var sid = winIdentity.User?.Value ?? "";
                var groups = winIdentity.Groups?
                    .Select(g => g.Value)
                    .ToList() ?? new List<string>();

                var sids = new List<string>(groups);
                if (!string.IsNullOrEmpty(sid))
                {
                    sids.Add(sid);
                }

                return Task.FromResult(new UserIdentityContext(username, ProviderName, groups, Sid: sid, Sids: sids));
            }

            return Task.FromResult(new UserIdentityContext("anonymous", ProviderName, new List<string>()));
        }
    }
}
