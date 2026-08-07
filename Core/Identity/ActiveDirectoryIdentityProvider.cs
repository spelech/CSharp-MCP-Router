#pragma warning disable CA1416

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Identity
{
    public class ActiveDirectoryIdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration? _config;
        private readonly ILdapService? _ldapService;
        private readonly List<string> _trustedProxies = new();
        private readonly bool _requireTrustedProxy = false;

        public string ProviderName => "ActiveDirectory";

        public ActiveDirectoryIdentityProvider()
        {
        }

        public ActiveDirectoryIdentityProvider(IConfiguration? config = null, ILdapService? ldapService = null)
        {
            _config = config;
            _ldapService = ldapService;
            if (_config != null)
            {
                _requireTrustedProxy = _config.GetValue<bool>("Oidc:RequireTrustedProxy", true);
                var proxiesStr = _config["Oidc:TrustedProxies"] ?? "";
                _trustedProxies = proxiesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
        }

        public async Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            // 1. Windows Authentication (IIS / Negotiate)
            if (httpContext.User.Identity is WindowsIdentity winIdentity && winIdentity.IsAuthenticated)
            {
                var username = winIdentity.Name;
                var sid = winIdentity.User?.Value ?? "";
                var groups = winIdentity.Groups?
                    .Select(g => g.Value)
                    .ToList() ?? new List<string>();

                List<string>? ldapSids = null;
                if (_ldapService != null)
                {
                    ldapSids = await _ldapService.ResolveUserSidsAsync(username);
                }

                return new UserIdentityContext(username, ProviderName, groups, sid, ldapSids);
            }

            // 2. Header-based SSO with trusted proxy validation
            var remoteIp = httpContext.Connection.RemoteIpAddress;
            bool isTrusted = false;

            if (remoteIp != null)
            {
                var remoteIpStr = remoteIp.ToString();
                if (IPAddress.IsLoopback(remoteIp) || _trustedProxies.Contains(remoteIpStr))
                {
                    isTrusted = true;
                }
            }
            else
            {
                if (!_requireTrustedProxy && _trustedProxies.Count == 0)
                {
                    isTrusted = true;
                }
            }

            if (!isTrusted && (_requireTrustedProxy || _trustedProxies.Count > 0))
            {
                return new UserIdentityContext("anonymous", ProviderName, new List<string>());
            }

            var headerUser = httpContext.Request.Headers["Remote-User"].FirstOrDefault()
                          ?? httpContext.Request.Headers["X-Forwarded-User"].FirstOrDefault();

            if (!string.IsNullOrEmpty(headerUser) && headerUser != "guest" && headerUser != "anonymous" && _ldapService != null)
            {
                var resolvedSids = await _ldapService.ResolveUserSidsAsync(headerUser);
                if (resolvedSids != null && resolvedSids.Count > 0)
                {
                    return new UserIdentityContext(headerUser, ProviderName, new List<string>(), "", resolvedSids);
                }
            }

            return new UserIdentityContext("anonymous", ProviderName, new List<string>());
        }
    }
}
#pragma warning restore CA1416
