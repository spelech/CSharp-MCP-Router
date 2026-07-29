using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Core.Identity
{
    public class OidcIdentityProvider : IIdentityProvider
    {
        public string ProviderName => "PocketID_TinyAuth";

        public Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext)
        {
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
