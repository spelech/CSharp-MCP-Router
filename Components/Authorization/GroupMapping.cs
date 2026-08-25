namespace ModelContextGateway.Components.Authorization
{
    public class GroupMapping
    {
        public string Id { get; set; } = string.Empty; // Guid or key
        public string ExternalId { get; set; } = string.Empty; // e.g., S-1-5-... or oidc_admins
        public string InternalGroup { get; set; } = string.Empty; // e.g., database_users
    }
}
