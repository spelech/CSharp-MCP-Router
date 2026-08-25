namespace McpRouter.Components.Providers
{
    public static class ProviderEndpoints
    {
        public static IEndpointRouteBuilder MapProviderEndpoints(this IEndpointRouteBuilder app)
        {
            // Provider configuration routes (/api/providers*, /api/admin/providers) are mapped via ProvidersController
            return app;
        }
    }
}
