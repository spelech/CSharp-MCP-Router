using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Middleware
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
                if (context.Response.StatusCode == 401)
                {
                    var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                    var host = context.Request.Host;
                    var resourceMetadataUrl = $"{scheme}://{host}/.well-known/oauth-protected-resource";
                    context.Response.Headers["WWW-Authenticate"] = $"Bearer resource_metadata=\"{resourceMetadataUrl}\", scope=\"mcp_client\"";
                }
                else if (context.Response.StatusCode == 403)
                {
                    var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                    var host = context.Request.Host;
                    var resourceMetadataUrl = $"{scheme}://{host}/.well-known/oauth-protected-resource";
                    context.Response.Headers["WWW-Authenticate"] = $"Bearer error=\"insufficient_scope\", scope=\"mcp_client\", resource_metadata=\"{resourceMetadataUrl}\", error_description=\"Access denied: insufficient permissions or scope.\"";
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
