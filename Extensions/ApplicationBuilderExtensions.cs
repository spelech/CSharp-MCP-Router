using System.Reflection;

namespace McpRouter.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        private static readonly string AppVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.6";

        public static void ConfigureMcpRouterPipeline(this WebApplication app)
        {
            var config = app.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("McpRouter.Startup");

            var requireTrusted = config.GetValue<bool>("Oidc:RequireTrustedProxy", true);
            if (!requireTrusted)
            {
                logger.LogWarning("SECURITY WARNING: Oidc:RequireTrustedProxy is set to false! This is dangerous in production as headers like Remote-User can be spoofed by any internal or external client.");
            }

            var trustedProxies = config["Oidc:TrustedProxies"];
            if (string.IsNullOrWhiteSpace(trustedProxies))
            {
                logger.LogWarning("Notice: Oidc:TrustedProxies is unset. Header-based authentication (reverse proxy auth) will trust loopback-only (127.0.0.1 / ::1).");
            }

            if (!SecurityValidationHelper.HasExternalIdp(config))
            {
                var standaloneNetworks = config.GetSection("Admin:StandaloneAllowedNetworks").Get<string[]>();
                var networksList = standaloneNetworks != null && standaloneNetworks.Length > 0
                    ? string.Join(", ", standaloneNetworks)
                    : (config["Admin:StandaloneAllowedNetworks"] ?? "127.0.0.1, ::1");

                logger.LogInformation("Standalone Admin Mode active (no external IDP detected). Allowed administrative network CIDRs: {Networks}", networksList);
            }

            app.UseCors();

            // Extract token from query parameters for SSE / WebSocket bypass support
            app.Use(async (context, next) =>
            {
                if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
                {
                    if (context.Request.Query.TryGetValue("access_token", out var accessToken) && !string.IsNullOrEmpty(accessToken))
                    {
                        context.Request.Headers.Authorization = $"Bearer {accessToken}";
                    }
                    else if (context.Request.Query.TryGetValue("token", out var token) && !string.IsNullOrEmpty(token))
                    {
                        context.Request.Headers.Authorization = $"Bearer {token}";
                    }
                }
                await next();
            });

            app.UseMiddleware<McpRouter.Middleware.McpAuthorizationSpecMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<McpRouter.Middleware.McpDualSpecMiddleware>();
            app.MapControllers();

            // Request logging middleware (metadata only — never headers/body/query, which carry credentials)
            app.Use(async (context, next) =>
            {
                var reqLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                reqLogger.LogInformation("Incoming request: {Method} {Path} from {Ip}",
                    context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);
                await next();
            });

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                    ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                    ctx.Context.Response.Headers.Append("Expires", "0");
                }
            }); // Serves dashboard files from wwwroot with no-cache headers

            app.SeedDatabase();

            // ----------------------------------------------------
            // SYSTEM/HEALTH ENDPOINTS
            // ----------------------------------------------------
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "McpRouter", version = AppVersion }));
            app.MapGet("/api/config/branding", async (McpRouter.Infrastructure.Persistence.ISettingRepository settingsRepo) =>
            {
                var settings = await settingsRepo.GetSettingsAsync() ?? new McpRouter.Models.RouterSettings();
                return Results.Ok(new
                {
                    title = settings.DashboardTitle,
                    icon = settings.DashboardIcon
                });
            });
            app.MapGet("/api/config/branding/logo", () =>
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "data", "branding");
                if (!Directory.Exists(dir))
                {
                    return Results.NotFound();
                }

                var file = Directory.GetFiles(dir, "logo.*").FirstOrDefault();
                if (file == null)
                {
                    return Results.NotFound();
                }

                var ext = Path.GetExtension(file).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".svg" => "image/svg+xml",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    ".ico" => "image/x-icon",
                    _ => "application/octet-stream"
                };
                return Results.File(file, contentType, enableRangeProcessing: true);
            });

            // ----------------------------------------------------
            // OAUTH & OIDC DISCOVERY ENDPOINTS
            // ----------------------------------------------------
            app.MapGet("/.well-known/oauth-protected-resource", (HttpContext context) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    resource = $"{scheme}://{host}/mcp",
                    authorization_servers = new[] { $"{scheme}://{host}" },
                    bearer_methods_supported = new[] { "header" }
                });
            });

            app.MapGet("/.well-known/oauth-protected-resource/{**path}", (HttpContext context, string path) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    resource = $"{scheme}://{host}/{path}",
                    authorization_servers = new[] { $"{scheme}://{host}" },
                    bearer_methods_supported = new[] { "header" }
                });
            });

            app.MapGet("/.well-known/oauth-authorization-server", (HttpContext context) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    issuer = $"{scheme}://{host}",
                    authorization_endpoint = $"{scheme}://{host}/oauth/authorize",
                    token_endpoint = $"{scheme}://{host}/oauth/token",
                    registration_endpoint = $"{scheme}://{host}/api/register",
                    response_types_supported = new[] { "code" },
                    grant_types_supported = new[] { "authorization_code" },
                    token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" }
                });
            });

            app.MapGet("/.well-known/openid-configuration", (HttpContext context) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    issuer = $"{scheme}://{host}",
                    authorization_endpoint = $"{scheme}://{host}/oauth/authorize",
                    token_endpoint = $"{scheme}://{host}/oauth/token",
                    registration_endpoint = $"{scheme}://{host}/api/register",
                    response_types_supported = new[] { "code" },
                    grant_types_supported = new[] { "authorization_code" },
                    subject_types_supported = new[] { "public" },
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                });
            });

            // Feature-focused endpoint composition
            app.MapProxyEndpoints();
            app.MapAdminMcpEndpoints();
            app.MapCapabilityEndpoints();
            app.MapServerEndpoints();
            app.MapClientEndpoints();
            app.MapAppKeyEndpoints();
            app.MapProviderEndpoints();
            app.MapPolicyEndpoints();
            app.MapFallbackToFile("index.html");
        }

        // Backwards compatibility endpoint forwarder
        public static void MapAdminEndpoints(this WebApplication app)
        {
            app.MapAdminMcpEndpoints();
            app.MapCapabilityEndpoints();
            app.MapServerEndpoints();
            app.MapClientEndpoints();
            app.MapAppKeyEndpoints();
            app.MapProviderEndpoints();
            app.MapPolicyEndpoints();
        }
    }
}
