using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpRouter.Core.Identity;

namespace McpRouter.Middleware
{
    public class OidcHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IIdentityProvider _identityProvider;
        private readonly IConfiguration? _configuration;

        public OidcHeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IIdentityProvider identityProvider,
            IConfiguration? configuration = null)
            : base(options, logger, encoder)
        {
            _identityProvider = identityProvider;
            _configuration = configuration;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var config = _configuration ?? Context.RequestServices?.GetService<IConfiguration>();
            var identityContext = await _identityProvider.ResolveIdentityAsync(Context);
            Logger.LogInformation("[DEBUG_AUTH] OidcHeaderHandler: user={User}, groups={Groups}, ip={IP}", identityContext?.Username, string.Join(",", identityContext?.GroupNames ?? new List<string>()), Context.Connection.RemoteIpAddress);
            if (identityContext == null || identityContext.Username == "guest" || identityContext.Username == "anonymous")
            {
                return AuthenticateResult.NoResult();
            }

            var adminGroupSid = config?["Admin:GroupSid"] ?? "S-1-5-32-544";
            var username = identityContext.Username;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, username)
            };

            if (identityContext.AllSids.Contains(adminGroupSid) || identityContext.GroupNames.Contains("full_admin") || identityContext.GroupNames.Contains("Administrator"))
            {
                claims.Add(new Claim("Sid", adminGroupSid));
            }

            foreach (var group in identityContext.GroupNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, group));
            }

            foreach (var sid in identityContext.AllSids)
            {
                claims.Add(new Claim(ClaimTypes.GroupSid, sid));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
