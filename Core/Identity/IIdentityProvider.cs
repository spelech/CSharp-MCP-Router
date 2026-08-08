using System.Collections.Generic;
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
                if (Sids != null) list.AddRange(Sids);
                return list.Distinct().ToList();
            }
        }
    }

    /// <summary>
    /// Auto-generated XML documentation.
    /// </summary>
    public interface IIdentityProvider
    {
        string ProviderName { get; }
        Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext);
    }
}
