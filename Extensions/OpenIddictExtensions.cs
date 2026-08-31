using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

namespace ModelContextGateway.Extensions
{
    public static class OpenIddictExtensions
    {
        public static IServiceCollection AddMcpOpenIddict(
            this IServiceCollection services,
            Microsoft.Extensions.Hosting.IHostEnvironment env,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, AppKeyAuthenticationHandler>("AppKey", null)
            .AddScheme<AuthenticationSchemeOptions, OidcHeaderAuthenticationHandler>("OidcHeader", null);

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, "AppKey", "OidcHeader")
                    .Build();

                options.AddPolicy("AdminPolicy", policy =>
                {
                    policy.RequireAssertion(ctx =>
                    {
                        var httpContext = ctx.Resource as Microsoft.AspNetCore.Http.HttpContext;
                        var cfg = httpContext?.RequestServices?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

                        // Standalone network authorization (when no external IDP is configured)
                        if (httpContext != null && SecurityValidationHelper.IsStandaloneAdminNetwork(httpContext.Connection.RemoteIpAddress, cfg))
                        {
                            if (!SecurityValidationHelper.HasExternalIdp(cfg, httpContext))
                            {
                                return true;
                            }
                        }

                        if (ctx.User?.Identity?.IsAuthenticated != true)
                        {
                            return false;
                        }

                        if (ctx.User.IsInRole("Administrator") || ctx.User.HasClaim("Scope", "admin"))
                        {
                            return true;
                        }

                        var adminSid = cfg?["Admin:GroupSid"] ?? "S-1-5-32-544";

                        var configuredAdminGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var singleGroupName = cfg?["Admin:GroupName"];
                        if (!string.IsNullOrWhiteSpace(singleGroupName))
                        {
                            configuredAdminGroups.Add(singleGroupName.Trim());
                        }
                        else
                        {
                            configuredAdminGroups.Add("full_admin");
                            configuredAdminGroups.Add("Administrator");
                            configuredAdminGroups.Add("Administrators");
                        }

                        var adminGroupsSection = cfg?.GetSection("Admin:Groups")?.Get<string[]>();
                        if (adminGroupsSection != null)
                        {
                            foreach (var g in adminGroupsSection)
                            {
                                if (!string.IsNullOrWhiteSpace(g))
                                {
                                    configuredAdminGroups.Add(g.Trim());
                                }
                            }
                        }

                        return ctx.User.HasClaim("Sid", adminSid) ||
                               ctx.User.Claims.Any(c => c.Type == ClaimTypes.Role && configuredAdminGroups.Contains(c.Value));
                    })
                    .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, "AppKey", "OidcHeader");
                });
            });

            services.AddOpenIddict()
                .AddServer(options =>
                {
                    options.SetTokenEndpointUris("/connect/token", "/oauth/token");
                    options.SetAuthorizationEndpointUris("/connect/authorize", "/oauth/authorize");
                    options.AllowClientCredentialsFlow();
                    options.AllowAuthorizationCodeFlow();
                    options.AllowRefreshTokenFlow();
                    options.EnableDegradedMode();
                    options.RegisterScopes(
                        OpenIddict.Abstractions.OpenIddictConstants.Scopes.OpenId,
                        OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
                        OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
                        OpenIddict.Abstractions.OpenIddictConstants.Scopes.OfflineAccess,
                        "api",
                        "mcp_client",
                        "tools:execute",
                        "resources:read",
                        "prompts:read"
                    );
                    options.DisableScopeValidation();
                    options.DisableResourceValidation();

                    var certPath = config["MCG_JWT_CERT_PATH"]
                        ?? config["MCG_OPENIDDICT_CERT_PATH"]
                        ?? config["OpenIddict:CertificatePath"]
                        ?? Environment.GetEnvironmentVariable("MCG_JWT_CERT_PATH")
                        ?? Environment.GetEnvironmentVariable("MCG_OPENIDDICT_CERT_PATH")
                        ?? Environment.GetEnvironmentVariable("OPENIDDICT_CERT_PATH");
                    var certPass = config["MCG_JWT_CERT_PASSWORD"]
                        ?? config["MCG_OPENIDDICT_CERT_PASSWORD"]
                        ?? config["OpenIddict:CertificatePassword"]
                        ?? Environment.GetEnvironmentVariable("MCG_JWT_CERT_PASSWORD")
                        ?? Environment.GetEnvironmentVariable("MCG_OPENIDDICT_CERT_PASSWORD")
                        ?? Environment.GetEnvironmentVariable("OPENIDDICT_CERT_PASSWORD");

                    if (!string.IsNullOrEmpty(certPath) && System.IO.File.Exists(certPath))
                    {
                        var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                            certPath, certPass, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet);
                        options.AddSigningCertificate(cert).AddEncryptionCertificate(cert);
                    }
                    else if (env.EnvironmentName == Environments.Production)
                    {
                        throw new InvalidOperationException("OpenIddict:CertificatePath must be configured in Production environment.");
                    }
                    else
                    {
                        options.AddDevelopmentEncryptionCertificate()
                               .AddDevelopmentSigningCertificate();
                    }

                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough()
                           .EnableAuthorizationEndpointPassthrough()
                           .DisableTransportSecurityRequirement();

                    options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ValidateAuthorizationRequestContext>(builder =>
                        builder.UseInlineHandler(context => default));

                    options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ValidateTokenRequestContext>(builder =>
                        builder.UseInlineHandler(context => default));

                    options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ApplyAuthorizationResponseContext>(builder =>
                        builder.UseInlineHandler(context =>
                        {
                            var issuer = context.Options.Issuer?.AbsoluteUri.TrimEnd('/')
                                ?? ((string?)context.Response.GetParameter("issuer"))?.TrimEnd('/')
                                ?? ((string?)context.Response.GetParameter("iss"))?.TrimEnd('/');

                            if (!string.IsNullOrEmpty(issuer) && string.IsNullOrEmpty((string?)context.Response.GetParameter("iss")))
                            {
                                context.Response.SetParameter("iss", issuer);
                            }
                            return default;
                        }));

                    options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ApplyConfigurationResponseContext>(builder =>
                        builder.UseInlineHandler(context =>
                        {
                            var issuer = ((string?)context.Response.GetParameter("issuer"))?.TrimEnd('/') ?? "";
                            if (!string.IsNullOrEmpty(issuer))
                            {
                                context.Response.SetParameter("registration_endpoint", $"{issuer}/api/register");
                            }
                            return default;
                        }));
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });

            return services;
        }
    }
}
