using Microsoft.Extensions.DependencyInjection;
using McpRouter.Models;
using System;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using McpRouter.Middleware;

namespace McpRouter.Extensions
{
    public static class OpenIddictExtensions
    {
        public static IServiceCollection AddMcpOpenIddict(this IServiceCollection services, Microsoft.Extensions.Hosting.IHostEnvironment env)
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
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx =>
                          {
                              var httpContext = ctx.Resource as Microsoft.AspNetCore.Http.HttpContext;
                              var config = httpContext?.RequestServices?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                              var adminSid = config?["Admin:GroupSid"] ?? "S-1-5-32-544";
                              return ctx.User.HasClaim("Sid", adminSid);
                          })
                          .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, "AppKey", "OidcHeader");
                });
            });

            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                           .UseDbContext<RouterDbContext>();
                })
                .AddServer(options =>
                {
                    options.SetTokenEndpointUris("/connect/token", "/oauth/token");
                    options.AllowClientCredentialsFlow();
                    
                    if (env.IsDevelopment() || env.EnvironmentName == "Dev" || env.EnvironmentName == "Development")
                    {
                        options.AddDevelopmentEncryptionCertificate()
                               .AddDevelopmentSigningCertificate();
                    }
                    else
                    {
                        options.AddEphemeralEncryptionKey()
                               .AddEphemeralSigningKey();
                    }

                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough();
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
