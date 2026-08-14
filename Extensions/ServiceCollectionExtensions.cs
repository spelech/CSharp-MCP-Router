using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using McpRouter.Models;
using McpRouter.Services;
using McpRouter.Core.Security;

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

            // Decorate Console and Debug providers with SanitizingLoggerProvider to sanitize all logs
            for (int i = 0; i < builder.Services.Count; i++)
            {
                var sd = builder.Services[i];
                if (sd.ServiceType == typeof(ILoggerProvider))
                {
                    var implType = sd.ImplementationType;
                    var implInstance = sd.ImplementationInstance;
                    var implFactory = sd.ImplementationFactory;

                    bool isInMemory = false;
                    if (implType != null && implType.Name.Contains("InMemoryLoggerProvider")) isInMemory = true;
                    if (implInstance != null && implInstance.GetType().Name.Contains("InMemoryLoggerProvider")) isInMemory = true;

                    if (isInMemory) continue;

                    if (implInstance != null)
                    {
                        var provider = (ILoggerProvider)implInstance;
                        builder.Services[i] = ServiceDescriptor.Singleton<ILoggerProvider>(sp => new McpRouter.Core.Logging.SanitizingLoggerProvider(provider));
                    }
                    else if (implType != null)
                    {
                        builder.Services[i] = ServiceDescriptor.Singleton<ILoggerProvider>(sp =>
                        {
                            var original = (ILoggerProvider)ActivatorUtilities.CreateInstance(sp, implType);
                            return new McpRouter.Core.Logging.SanitizingLoggerProvider(original);
                        });
                    }
                    else if (implFactory != null)
                    {
                        builder.Services[i] = ServiceDescriptor.Singleton<ILoggerProvider>(sp =>
                        {
                            var original = (ILoggerProvider)implFactory(sp);
                            return new McpRouter.Core.Logging.SanitizingLoggerProvider(original);
                        });
                    }
                }
            }

            // Register Multi-Database Provider Factory (Pure Dapper)
            builder.Services.AddSingleton<McpRouter.Core.Database.IDbConnectionFactory, McpRouter.Core.Database.DbConnectionFactory>();

            // Register Aligned Repositories (ISettingRepository, IServerRepository, IAppKeyRepository, ISecretProviderRepository, IAuthProviderRepository)
            builder.Services.AddSingleton<McpRouter.Core.Database.DatabaseRepository>();
            builder.Services.AddSingleton<McpRouter.Core.Database.ISettingRepository>(sp => sp.GetRequiredService<McpRouter.Core.Database.DatabaseRepository>());
            builder.Services.AddSingleton<McpRouter.Core.Database.IServerRepository>(sp => sp.GetRequiredService<McpRouter.Core.Database.DatabaseRepository>());
            builder.Services.AddSingleton<McpRouter.Core.Database.IAppKeyRepository>(sp => sp.GetRequiredService<McpRouter.Core.Database.DatabaseRepository>());
            builder.Services.AddSingleton<McpRouter.Core.Database.ISecretProviderRepository>(sp => sp.GetRequiredService<McpRouter.Core.Database.DatabaseRepository>());
            builder.Services.AddSingleton<McpRouter.Core.Database.IAuthProviderRepository>(sp => sp.GetRequiredService<McpRouter.Core.Database.DatabaseRepository>());

            // Register Credential Service
            builder.Services.AddSingleton<ICredentialService, CredentialService>();

            // Register Pluggable Identity Providers (Active Directory & Configurable Header Auth)
            builder.Services.AddSingleton<McpRouter.Core.Identity.ILdapService, McpRouter.Core.Identity.LdapActiveDirectoryService>();
            // App-key requests carry no AD/OIDC headers; resolve their owner+SID first so audit rows are attributable.
            builder.Services.AddSingleton<McpRouter.Core.Identity.IIdentityProvider, McpRouter.Core.Identity.AppKeyIdentityProvider>();
            builder.Services.AddSingleton<McpRouter.Core.Identity.IIdentityProvider, McpRouter.Core.Identity.ActiveDirectoryIdentityProvider>();
            builder.Services.AddSingleton<McpRouter.Core.Identity.IIdentityProvider, McpRouter.Core.Identity.HeaderIdentityProvider>();
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
            builder.Services.AddMcpOpenIddict(builder.Environment, builder.Configuration);
            builder.Services.AddControllers();

            builder.Services.AddHttpClient("McpClient");
            builder.Services.ConfigureAll<Microsoft.Extensions.Http.HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(b =>
                {
                    var configuration = b.Services.GetRequiredService<IConfiguration>();
                    var allowedIpRanges = configuration.GetSection("Security:AllowedIpRanges").Get<string[]>() ?? Array.Empty<string>();

                    b.PrimaryHandler = new SocketsHttpHandler
                    {
                        AllowAutoRedirect = false,
                        ConnectCallback = (context, cancellationToken) => SecurityValidationHelper.ValidatingConnectCallback(context, allowedIpRanges, cancellationToken)
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
                        if (builder.Environment.EnvironmentName == "Development" || builder.Environment.EnvironmentName == "Dev")
                        {
                            policy.WithOrigins("http://localhost:3000", "http://localhost:5000", "https://localhost:5001")
                                  .AllowAnyMethod()
                                  .AllowAnyHeader()
                                  .AllowCredentials();
                        }
                        else
                        {
                            policy.WithOrigins("https://invalid-origin.local");
                        }
                    }
                });
            });

        }
    }
}
