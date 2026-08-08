using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Core.Identity
{
    /// <summary>
    /// Represents the authenticated user's identity context, including username, auth type, and group SIDs.
    /// </summary>
    /// <param name="Username">The authenticated username or principal name.</param>
    /// <param name="AuthenticationType">The mechanism used for authentication (e.g. Kerberos, OIDC, Claims).</param>
    /// <param name="GroupNames">List of role names or group names associated with the user.</param>
    /// <param name="Sid">Primary security identifier (SID) if applicable.</param>
    /// <param name="Sids">List of all associated group SIDs.</param>
    public record UserIdentityContext(
        string Username,
        string AuthenticationType,
        List<string> GroupNames,
        string Sid = "",
        List<string>? Sids = null
    )
    {
        /// <summary>
        /// Gets all unique security identifiers (SIDs) for this identity context.
        /// </summary>
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
    /// Defines a pluggable identity provider interface for resolving user identities from incoming HTTP contexts.
    /// </summary>
    public interface IIdentityProvider
    {
        /// <summary>
        /// Gets the unique code name of this identity provider (e.g. ActiveDirectory, OIDC).
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Resolves the authenticated user identity context from the provided HTTP context.
        /// </summary>
        /// <param name="httpContext">The current HTTP context containing request headers or credentials.</param>
        /// <returns>A task returning the resolved <see cref="UserIdentityContext"/>.</returns>
        Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext);
    }
}
