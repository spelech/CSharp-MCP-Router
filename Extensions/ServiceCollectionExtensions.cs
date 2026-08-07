using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using McpRouter.Models;
using McpRouter.Services;

namespace McpRouter.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMcpRouterServices(this WebApplicationBuilder builder)
        {

        // Add logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.AddProvider(new InMemoryLoggerProvider());

        // Register Multi-Database Provider Factory & DbContext
        builder.Services.AddSingleton<McpRouter.Core.Database.IDbConnectionFactory, McpRouter.Core.Database.DbConnectionFactory>();
        builder.Services.AddDbContext<RouterDbContext>();

        // Register Identity Providers (Active Directory & PocketID/TinyAuth OIDC)
        builder.Services.AddSingleton<McpRouter.Core.Identity.IIdentityProvider, McpRouter.Core.Identity.ActiveDirectoryIdentityProvider>();
        builder.Services.AddSingleton<McpRouter.Core.Identity.IIdentityProvider, McpRouter.Core.Identity.OidcIdentityProvider>();
        builder.Services.AddSingleton<McpRouter.Core.Identity.CompositeIdentityProvider>();

        // Register Secret Retrievers (HashiCorp Vault, Windows Registry & Environment)
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<McpRouter.Core.Secrets.ISecretRetriever, McpRouter.Core.Secrets.VaultSecretRetriever>();
        builder.Services.AddSingleton<McpRouter.Core.Secrets.ISecretRetriever, McpRouter.Core.Secrets.WindowsRegistrySecretRetriever>();
        builder.Services.AddSingleton<McpRouter.Core.Secrets.ISecretRetriever, McpRouter.Core.Secrets.EnvironmentSecretRetriever>();
        builder.Services.AddSingleton<McpRouter.Core.Secrets.CompositeSecretRetriever>();

        // Register Observability & Audit Logger
        builder.Services.AddSingleton<McpRouter.Core.Logging.IAuditLogger, McpRouter.Core.Logging.AuditLogger>();

        // Register OpenIddict & Controllers
        builder.Services.AddMcpOpenIddict();
        builder.Services.AddControllers();

        builder.Services.AddHttpClient();
        builder.Services.Configure<Microsoft.Extensions.Http.HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(b =>
            {
                b.PrimaryHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };
            });
        });
        builder.Services.AddSingleton<SessionManager>();
        
        // Register Docker Auto-Discovery Service
        builder.Services.AddHostedService<DockerAutoDiscoveryService>();
        
        // Register Backend Health Check Service
        builder.Services.AddSingleton<BackendHealthCheckService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<BackendHealthCheckService>());
        
        // Register Dynamic Embedding Service (handles settings in encrypted DB)
        builder.Services.AddSingleton<DynamicEmbeddingService>();
        builder.Services.AddSingleton<IEmbeddingService>(sp => sp.GetRequiredService<DynamicEmbeddingService>());

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOriginsValue = builder.Configuration["CORS_ALLOWED_ORIGINS"]
                    ?? builder.Configuration["AllowedOrigins"];

                if (!string.IsNullOrWhiteSpace(allowedOriginsValue))
                {
                    var origins = allowedOriginsValue
                        .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.Trim())
                        .ToArray();

                    policy.WithOrigins(origins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:5000", "https://localhost:5001")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
            });
        });

        }
    }
}
