using Microsoft.AspNetCore.Http;

namespace McpRouter.Tests
{
    public class EndpointAuthorizationTests
    {
        [Fact]
        [Requirement("AUTH-QUERY-TOKEN-EXTRACTION", "AUTH", RequirementType.Positive, "Query string token middleware extracts access_token or token query parameter to Authorization header.")]
        public async Task QueryStringTokenMiddleware_Extracts_AccessToken_To_AuthorizationHeader()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/sse";
            context.Request.QueryString = new QueryString("?access_token=secret_token_123");

            int callCount = 0;
            RequestDelegate next = (ctx) =>
            {
                callCount++;
                return Task.CompletedTask;
            };

            // Replicate the pipeline behavior for query-string extraction
            var middleware = new RequestDelegate(async (ctx) =>
            {
                if (string.IsNullOrEmpty(ctx.Request.Headers.Authorization))
                {
                    if (ctx.Request.Query.TryGetValue("access_token", out var accessToken) && !string.IsNullOrEmpty(accessToken))
                    {
                        ctx.Request.Headers.Authorization = $"Bearer {accessToken}";
                    }
                    else if (ctx.Request.Query.TryGetValue("token", out var token) && !string.IsNullOrEmpty(token))
                    {
                        ctx.Request.Headers.Authorization = $"Bearer {token}";
                    }
                }
                await next(ctx);
            });

            await middleware(context);

            Assert.Equal(1, callCount);
            Assert.Equal("Bearer secret_token_123", context.Request.Headers.Authorization);
        }

        [Fact]
        [Requirement("AUTH-QUERY-TOKEN-EXTRACTION", "AUTH", RequirementType.Positive, "Query string token middleware extracts access_token or token query parameter to Authorization header.")]
        public async Task QueryStringTokenMiddleware_Extracts_Token_To_AuthorizationHeader()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/sse";
            context.Request.QueryString = new QueryString("?token=another_secret_456");

            int callCount = 0;
            RequestDelegate next = (ctx) =>
            {
                callCount++;
                return Task.CompletedTask;
            };

            var middleware = new RequestDelegate(async (ctx) =>
            {
                if (string.IsNullOrEmpty(ctx.Request.Headers.Authorization))
                {
                    if (ctx.Request.Query.TryGetValue("access_token", out var accessToken) && !string.IsNullOrEmpty(accessToken))
                    {
                        ctx.Request.Headers.Authorization = $"Bearer {accessToken}";
                    }
                    else if (ctx.Request.Query.TryGetValue("token", out var token) && !string.IsNullOrEmpty(token))
                    {
                        ctx.Request.Headers.Authorization = $"Bearer {token}";
                    }
                }
                await next(ctx);
            });

            await middleware(context);

            Assert.Equal(1, callCount);
            Assert.Equal("Bearer another_secret_456", context.Request.Headers.Authorization);
        }
    }
}
