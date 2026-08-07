using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Identity
{
    public class OidcIdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration? _config;
        private readonly List<string> _trustedProxies = new();
        private readonly bool _requireTrustedProxy = false;

        public string ProviderName => "PocketID_TinyAuth";

        public OidcIdentityProvider()
        {
        }

        public OidcIdentityProvider(IConfiguration config)
        {
            _config = config;
            _requireTrustedProxy = config.GetValue<bool>("Oidc:RequireTrustedProxy", true);
            var proxiesStr = config["Oidc:TrustedProxies"] ?? "";
            _trustedProxies = proxiesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        public Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            var remoteIp = httpContext.Connection.RemoteIpAddress;
            bool isTrusted = false;

            if (remoteIp != null)
            {
                var remoteIpStr = remoteIp.ToString();

                // 1. Local loopback is trusted by default
                if (IPAddress.IsLoopback(remoteIp))
                {
                    isTrusted = true;
                }
                // 2. Check if remote IP is in the configured trusted proxies
                else if (_trustedProxies.Contains(remoteIpStr))
                {
                    isTrusted = true;
                }
            }
            else
            {
                // If remoteIp is null (e.g. testing context without remote IP set), we default to true if proxy check is not strictly required
                if (!_requireTrustedProxy && _trustedProxies.Count == 0)
                {
                    isTrusted = true;
                }
            }

            // If we require proxy validation, and the source is not trusted, we reject the headers and treat the request as "guest"
            if (!isTrusted && (_requireTrustedProxy || _trustedProxies.Count > 0))
            {
                return Task.FromResult(new UserIdentityContext("guest", ProviderName, new List<string>()));
            }

            var user = httpContext.Request.Headers["Remote-User"].FirstOrDefault()
                    ?? httpContext.Request.Headers["X-Forwarded-User"].FirstOrDefault()
                    ?? "guest";

            var groupsHeader = httpContext.Request.Headers["Remote-Groups"].FirstOrDefault()
                            ?? httpContext.Request.Headers["sso_groups"].FirstOrDefault()
                            ?? "";

            var groups = groupsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                     .ToList();

            return Task.FromResult(new UserIdentityContext(user, ProviderName, groups));
        }
    }
}
