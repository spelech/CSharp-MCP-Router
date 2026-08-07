using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Identity
{
    public class OidcIdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration? _configuration;

        public OidcIdentityProvider(IConfiguration? configuration = null)
        {
            _configuration = configuration;
        }

        public string ProviderName => "PocketID_TinyAuth";

        public Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            var config = _configuration ?? (httpContext.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration);

            if (!TrustedProxyHelper.IsTrustedProxy(httpContext, config))
            {
                TrustedProxyHelper.StripUntrustedHeaders(httpContext);
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

            return Task.FromResult(new UserIdentityContext(user, ProviderName, groups, Sid: "", Sids: new List<string>()));
        }
    }
}

