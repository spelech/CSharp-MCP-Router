using Microsoft.Extensions.DependencyInjection;
using McpRouter.Models;
using System;
using System.Security.Claims;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using McpRouter.Middleware;
using Microsoft.Extensions.Hosting;

namespace McpRouter.Extensions
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
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx =>
                          {
                              var httpContext = ctx.Resource as Microsoft.AspNetCore.Http.HttpContext;
                              var cfg = httpContext?.RequestServices?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                              var adminSid = cfg?["Admin:GroupSid"] ?? "S-1-5-32-544";
                              return ctx.User.HasClaim("Sid", adminSid);
                          })
                          .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, "AppKey", "OidcHeader");
                });
            });

            services.AddOpenIddict()
                .AddServer(options =>
                {
                    options.SetTokenEndpointUris("/connect/token", "/oauth/token");
                    options.AllowClientCredentialsFlow();
                    
                    var certPath = config["OpenIddict:CertificatePath"] ?? Environment.GetEnvironmentVariable("OPENIDDICT_CERT_PATH");
                    var certPass = config["OpenIddict:CertificatePassword"] ?? Environment.GetEnvironmentVariable("OPENIDDICT_CERT_PASSWORD");

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
