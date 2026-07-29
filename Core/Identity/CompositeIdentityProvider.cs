using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Core.Identity
{
    public class CompositeIdentityProvider : IIdentityProvider
    {
        private readonly IEnumerable<IIdentityProvider> _providers;
        public string ProviderName => "Composite";

        public CompositeIdentityProvider(IEnumerable<IIdentityProvider> providers)
        {
            _providers = providers;
        }

        public async Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            foreach (var provider in _providers)
            {
                var identity = await provider.ResolveIdentityAsync(httpContext);
                if (identity.Username != "anonymous" && identity.Username != "guest")
                {
                    return identity;
                }
            }

            // Fallback: try OIDC header resolution
            var oidcProvider = _providers.OfType<OidcIdentityProvider>().FirstOrDefault();
            if (oidcProvider != null)
            {
                return await oidcProvider.ResolveIdentityAsync(httpContext);
            }

            return new UserIdentityContext("anonymous", ProviderName, new List<string>());
        }
    }
}
