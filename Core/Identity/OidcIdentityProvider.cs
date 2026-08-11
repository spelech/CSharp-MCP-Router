using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Identity
{
    /// <summary>
    /// Pluggable identity provider that extracts authenticated user identities and roles from HTTP reverse proxy headers
    /// (e.g., Remote-User, X-Forwarded-User, Remote-Groups, sso_groups, X-Auth-Request-Groups).
    /// </summary>
    public class HeaderIdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration? _configuration;
        private static readonly string[] DefaultUserHeaders = new[] { "Remote-User", "X-Forwarded-User", "X-Auth-Request-User", "X-User" };
        private static readonly string[] DefaultGroupHeaders = new[] { "Remote-Groups", "X-Forwarded-Groups", "X-Auth-Request-Groups", "sso_groups" };
        private static readonly string[] DefaultSidHeaders = new[] { "Remote-User-Sid", "X-Auth-Request-Sid" };

        public HeaderIdentityProvider(IConfiguration? configuration = null)
        {
            _configuration = configuration;
        }

        public string ProviderName => "HeaderAuth";

        public Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            var config = _configuration ?? (httpContext.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration);

            if (!TrustedProxyHelper.IsTrustedProxy(httpContext, config))
            {
                TrustedProxyHelper.StripUntrustedHeaders(httpContext);
                return Task.FromResult(new UserIdentityContext("guest", ProviderName, new List<string>()));
            }

            var userHeaders = config?.GetSection("Identity:HeaderAuth:UserHeaders").Get<string[]>() ?? DefaultUserHeaders;
            var groupHeaders = config?.GetSection("Identity:HeaderAuth:GroupHeaders").Get<string[]>() ?? DefaultGroupHeaders;

            string? user = null;
            foreach (var header in userHeaders)
            {
                var val = httpContext.Request.Headers[header].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    user = val.Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(user))
            {
                user = "guest";
            }

            var groups = new List<string>();
            foreach (var header in groupHeaders)
            {
                var val = httpContext.Request.Headers[header].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var split = val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    groups.AddRange(split);
                }
            }

            var sidHeadersSection = config?.GetSection("Identity:HeaderAuth:SidHeaders");
            var sidHeaders = (sidHeadersSection != null && sidHeadersSection.Exists())
                ? sidHeadersSection.Get<string[]>()
                : null;
            sidHeaders ??= DefaultSidHeaders;

            string? sid = null;
            foreach (var header in sidHeaders)
            {
                var val = httpContext.Request.Headers[header].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    sid = val.Trim();
                    break;
                }
            }

            var sids = !string.IsNullOrEmpty(sid) ? new List<string> { sid } : new List<string>();

            return Task.FromResult(new UserIdentityContext(user, ProviderName, groups.Distinct().ToList(), Sid: sid ?? "", Sids: sids));
        }
    }

    /// <summary>
    /// Backward-compatibility alias for OidcIdentityProvider.
    /// </summary>
    public class OidcIdentityProvider : HeaderIdentityProvider
    {
        public OidcIdentityProvider(IConfiguration? configuration = null) : base(configuration) { }
    }
}
