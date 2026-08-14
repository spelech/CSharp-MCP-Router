using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace McpRouter.Components.Clients
{
    public static class ClientEndpoints
    {
        public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
        {
            // Clients and OAuth endpoints are registered via Controllers (ClientsController, AuthorizationController)
            // and OpenIddict pipeline extensions.
            return app;
        }
    }
}
