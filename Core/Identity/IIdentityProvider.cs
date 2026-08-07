using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Core.Identity
{
    public record UserIdentityContext(
        string Username,
        string AuthenticationType,
        List<string> GroupNames,
        string Sid = "",
        List<string>? Sids = null
    )
    {
        public List<string> AllSids
        {
            get
            {
                var list = new List<string>();
                if (!string.IsNullOrEmpty(Sid)) list.Add(Sid);
                if (Sids != null && Sids.Count > 0) list.AddRange(Sids);
                if (GroupNames != null) list.AddRange(GroupNames.Where(g => g.StartsWith("S-1-")));
                return list.Distinct().ToList();
            }
        }
    }

    public interface IIdentityProvider
    {
        string ProviderName { get; }
        Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext);
    }
}

