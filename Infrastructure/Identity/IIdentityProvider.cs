namespace ModelContextGateway.Infrastructure.Identity
{
    /// <summary>
    /// Represents the authenticated user's identity context, including username, auth type, and group SIDs.
    /// </summary>
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
                if (!string.IsNullOrEmpty(Sid))
                {
                    list.Add(Sid);
                }

                if (Sids != null)
                {
                    list.AddRange(Sids);
                }

                return list.Distinct().ToList();
            }
        }
    }

    /// <summary>
    /// Defines a pluggable identity provider interface for resolving user identities from incoming HTTP contexts.
    /// </summary>
    public interface IIdentityProvider
    {
        string ProviderName { get; }
        Task<UserIdentityContext> ResolveIdentityAsync(HttpContext httpContext);
    }
}
