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
        private readonly ILdapService? _ldapService;

        public ActiveDirectoryIdentityProvider(IConfiguration? configuration = null, ILdapService? ldapService = null)
        {
            _configuration = configuration;
            _ldapService = ldapService;
        }

        public string ProviderName => "ActiveDirectory";

        public async Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            var config = _configuration ?? (httpContext.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration);
            var ldapService = _ldapService ?? (httpContext.RequestServices?.GetService(typeof(ILdapService)) as ILdapService);

            if (!TrustedProxyHelper.IsTrustedProxy(httpContext, config))
            {
                TrustedProxyHelper.StripUntrustedHeaders(httpContext);
                return new UserIdentityContext("anonymous", ProviderName, new List<string>());
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

                if (ldapService != null)
                {
                    var ldapSids = await ldapService.ResolveUserSidsAsync(username);
                    if (ldapSids != null)
                    {
                        sids.AddRange(ldapSids);
                    }
                }

                return new UserIdentityContext(username, ProviderName, groups, Sid: sid, Sids: sids.Distinct().ToList());
            }

            return new UserIdentityContext("anonymous", ProviderName, new List<string>());
        }
    }
}
