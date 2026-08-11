using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Core.Identity
{
    /// <summary>
    /// Resolves identity from a validated App Key. When <see cref="McpRouter.Middleware.AppKeyAuthenticationHandler"/>
    /// authenticates a request it stashes the key owner and the owner's SID in <c>HttpContext.Items</c>; this provider
    /// surfaces them as a <see cref="UserIdentityContext"/> so audit and invocation rows are attributable to the key
    /// owner instead of falling through to "anonymous". Registered ahead of the header/AD providers so that an
    /// app-key-authenticated request is attributed to the key owner.
    /// </summary>
    public class AppKeyIdentityProvider : IIdentityProvider
    {
        public string ProviderName => "AppKey";

        public Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue("AppKeyUsed", out var used) && used is bool usedFlag && usedFlag)
            {
                var owner = httpContext.Items.TryGetValue("AppKeyOwner", out var o) ? o as string : null;
                var ownerSid = httpContext.Items.TryGetValue("AppKeyOwnerSid", out var s) ? s as string : null;

                if (!string.IsNullOrEmpty(owner))
                {
                    var sids = !string.IsNullOrEmpty(ownerSid)
                        ? new List<string> { ownerSid! }
                        : new List<string>();
                    return Task.FromResult(new UserIdentityContext(owner!, ProviderName, new List<string>(), Sid: ownerSid ?? "", Sids: sids));
                }
            }

            return Task.FromResult(new UserIdentityContext("anonymous", ProviderName, new List<string>()));
        }
    }
}
