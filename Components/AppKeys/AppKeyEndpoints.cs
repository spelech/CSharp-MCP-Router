namespace McpRouter.Components.AppKeys
{
    public static class AppKeyEndpoints
    {
        public static IEndpointRouteBuilder MapAppKeyEndpoints(this IEndpointRouteBuilder app)
        {
            // AppKey management routes are registered via AppKeysController
            return app;
        }
    }
}
