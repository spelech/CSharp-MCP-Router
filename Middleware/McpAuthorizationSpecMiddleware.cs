namespace ModelContextGateway.Middleware
{
    public class McpAuthorizationSpecMiddleware
    {
        private readonly RequestDelegate _next;

        public McpAuthorizationSpecMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                var host = context.Request.Host;
                var path = context.Request.Path.Value?.TrimStart('/') ?? "";
                var resourceMetadataUrl = string.IsNullOrEmpty(path)
                    ? $"{scheme}://{host}/.well-known/oauth-protected-resource"
                    : $"{scheme}://{host}/.well-known/oauth-protected-resource/{path}";

                if (context.Response.StatusCode == 401)
                {
                    context.Response.Headers["WWW-Authenticate"] = $"Bearer resource_metadata=\"{resourceMetadataUrl}\", scope=\"mcp_client\"";
                }
                else if (context.Response.StatusCode == 403)
                {
                    context.Response.Headers["WWW-Authenticate"] = $"Bearer error=\"insufficient_scope\", scope=\"mcp_client\", resource_metadata=\"{resourceMetadataUrl}\", error_description=\"Access denied: insufficient permissions or scope.\"";
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
