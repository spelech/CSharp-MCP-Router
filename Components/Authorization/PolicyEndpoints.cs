namespace McpRouter.Components.Authorization
{
    public static class PolicyEndpoints
    {
        public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
        {
            // Policy and Group Mapping routes (/api/permissions/policies, /api/permissions/mappings) are mapped via PermissionsController
            return app;
        }
    }
}
