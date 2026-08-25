namespace McpRouter.Infrastructure.Identity
{
    /// <summary>
    /// Pluggable identity provider that extracts authenticated user identities and roles from HTTP reverse proxy headers
    /// (e.g., Remote-User, X-Forwarded-User, Remote-Groups, sso_groups, X-Auth-Request-Groups).
    /// </summary>
    public class HeaderIdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration? _configuration;
        private readonly IAuthProviderRepository? _authRepo;
        private static readonly string[] DefaultUserHeaders = new[] { "Remote-User", "X-Forwarded-User", "X-Auth-Request-User", "X-User" };
        private static readonly string[] DefaultGroupHeaders = new[] { "Remote-Groups", "X-Forwarded-Groups", "X-Auth-Request-Groups", "sso_groups" };
        private static readonly string[] DefaultSidHeaders = new[] { "Remote-User-Sid", "X-Auth-Request-Sid" };

        public HeaderIdentityProvider(IConfiguration? configuration = null)
            : this(configuration, null)
        {
        }

        public HeaderIdentityProvider(IConfiguration? configuration, IAuthProviderRepository? authRepo)
        {
            _configuration = configuration;
            _authRepo = authRepo;
        }

        public string ProviderName => "HeaderAuth";

        public async Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            var config = _configuration ?? (httpContext.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration);
            var authRepo = _authRepo ?? (httpContext.RequestServices?.GetService(typeof(IAuthProviderRepository)) as IAuthProviderRepository);

            if (!TrustedProxyHelper.IsTrustedProxy(httpContext, config))
            {
                TrustedProxyHelper.StripUntrustedHeaders(httpContext);
                return new UserIdentityContext("guest", ProviderName, new List<string>());
            }

            var userHeadersList = new List<string>();
            var groupHeadersList = new List<string>();

            // Check database-backed configuration if present
            if (authRepo != null)
            {
                try
                {
                    var dbAuthProviders = await authRepo.GetAuthProvidersAsync();
                    var oidcDb = dbAuthProviders?.FirstOrDefault(p =>
                        string.Equals(p.ProviderName, "HeaderAuth", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProviderName, "Oidc", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProviderName, "OIDC_ReverseProxy", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProviderName, "OidcHeader", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProviderName, "SSO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProviderName, "PocketID_TinyAuth", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProviderName, "PocketID", StringComparison.OrdinalIgnoreCase));

                    if (oidcDb != null)
                    {
                        if (!oidcDb.IsEnabled)
                        {
                            return new UserIdentityContext("guest", ProviderName, new List<string>());
                        }

                        if (!string.IsNullOrWhiteSpace(oidcDb.UserHeader))
                        {
                            userHeadersList.Add(oidcDb.UserHeader.Trim());
                        }
                        if (!string.IsNullOrWhiteSpace(oidcDb.GroupsHeader))
                        {
                            groupHeadersList.Add(oidcDb.GroupsHeader.Trim());
                        }
                    }
                }
                catch
                {
                    // Fallback to static configuration
                }
            }

            var configuredUserHeaders = config?.GetSection("Identity:HeaderAuth:UserHeaders").Get<string[]>() ?? DefaultUserHeaders;
            var configuredGroupHeaders = config?.GetSection("Identity:HeaderAuth:GroupHeaders").Get<string[]>() ?? DefaultGroupHeaders;

            foreach (var h in configuredUserHeaders)
            {
                if (!userHeadersList.Contains(h, StringComparer.OrdinalIgnoreCase))
                {
                    userHeadersList.Add(h);
                }
            }
            foreach (var h in configuredGroupHeaders)
            {
                if (!groupHeadersList.Contains(h, StringComparer.OrdinalIgnoreCase))
                {
                    groupHeadersList.Add(h);
                }
            }

            string? user = null;
            foreach (var header in userHeadersList)
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
            foreach (var header in groupHeadersList)
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

            return new UserIdentityContext(user, ProviderName, groups.Distinct().ToList(), Sid: sid ?? "", Sids: sids);
        }
    }

    /// <summary>
    /// Backward-compatibility alias for OidcIdentityProvider.
    /// </summary>
    public class OidcIdentityProvider : HeaderIdentityProvider
    {
        public OidcIdentityProvider(IConfiguration? configuration = null) : base(configuration, null) { }
        public OidcIdentityProvider(IConfiguration? configuration, IAuthProviderRepository? authRepo) : base(configuration, authRepo) { }
    }
}
