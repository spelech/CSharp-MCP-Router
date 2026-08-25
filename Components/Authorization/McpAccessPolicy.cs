namespace ModelContextGateway.Components.Authorization
{
    public class McpAccessPolicy
    {
        public string Id { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty; // e.g., "server:ha", "tool:docker__list_containers", "prompt:router__diagnose_failure", "resource:router://status"
        public string RequiredGroup { get; set; } = string.Empty;
        public bool IsAllowed { get; set; } = true;
    }
}
