using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpRouter.Core.Identity;

namespace McpRouter.Middleware
{
    public class OidcHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly CompositeIdentityProvider _identityProvider;

        public OidcHeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            CompositeIdentityProvider identityProvider)
            : base(options, logger, encoder)
        {
            _identityProvider = identityProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                var identity = await _identityProvider.ResolveIdentityAsync(Context);
                if (identity == null || identity.Username == "anonymous" || identity.Username == "guest")
                {
                    return AuthenticateResult.NoResult();
                }

                var claims = new System.Collections.Generic.List<Claim>
                {
                    new Claim(ClaimTypes.Name, identity.Username),
                    new Claim("identity_provider", identity.AuthenticationType)
                };

                if (identity.GroupNames != null)
                {
                    foreach (var group in identity.GroupNames)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, group));
                    }
                }

                // Check if user should be mapped to the Administrator role
                bool isAdmin = identity.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                               identity.Username.Equals("system", StringComparison.OrdinalIgnoreCase) ||
                               (identity.GroupNames != null && (
                                   identity.GroupNames.Contains("Administrators") ||
                                   identity.GroupNames.Contains("full_admin")
                               ));

                if (isAdmin)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                }

                var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(claimsIdentity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                // Set Items["AuthenticatedUser"] for legacy REST compatibility
                Context.Items["AuthenticatedUser"] = identity.Username;

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error resolving identity from OIDC/AD headers.");
                return AuthenticateResult.Fail("Error resolving identity.");
            }
        }
    }
}
