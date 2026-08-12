using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

            if (httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                var username = httpContext.User.Identity.Name ?? "";
                string sid = "";
                var groups = new List<string>();

                if (httpContext.User.Identity is WindowsIdentity winIdentity)
                {
#pragma warning disable CA1416
                    sid = winIdentity.User?.Value ?? "";
                    groups = winIdentity.Groups?
                        .Select(g => g.Value)
                        .ToList() ?? new List<string>();
#pragma warning restore CA1416
                }
                else if (httpContext.User.Identity is ClaimsIdentity claimsIdentity)
                {
                    var primarySidClaim = claimsIdentity.FindFirst(ClaimTypes.PrimarySid);
                    if (primarySidClaim != null)
                    {
                        sid = primarySidClaim.Value;
                    }

                    groups = claimsIdentity.FindAll(ClaimTypes.GroupSid)
                        .Select(c => c.Value)
                        .ToList();
                }

                var sids = new List<string>(groups);
                if (!string.IsNullOrEmpty(sid))
                {
                    sids.Add(sid);
                }

                if (ldapService != null && !string.IsNullOrEmpty(username))
                {
                    try
                    {
                        var ldapSids = await ldapService.ResolveUserSidsAsync(username);
                        if (ldapSids != null && ldapSids.Count > 0)
                        {
                            sids.AddRange(ldapSids);
                            
                            // If primary SID is missing, take the first SID returned by LDAP
                            if (string.IsNullOrEmpty(sid))
                            {
                                sid = ldapSids.First();
                            }
                        }
                    }
                    catch (System.Exception exLdap)
                    {
                        // A transient LDAP failure must never demote an already-authenticated user to anonymous;
                        // keep the token-group SIDs collected above and continue.
                        var logger = httpContext.RequestServices?.GetService(typeof(Microsoft.Extensions.Logging.ILogger<ActiveDirectoryIdentityProvider>))
                            as Microsoft.Extensions.Logging.ILogger<ActiveDirectoryIdentityProvider>;
                        logger?.LogWarning(exLdap, "LDAP SID augmentation failed for {Username}; using token-group SIDs only.", username);
                    }
                }

                return new UserIdentityContext(username, ProviderName, groups, Sid: sid, Sids: sids.Distinct().ToList());
            }

            return new UserIdentityContext("anonymous", ProviderName, new List<string>());
        }
    }
}
