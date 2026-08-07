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

        // Register Multi-Database Provider Factory & DbContext
        builder.Services.AddSingleton<McpRouter.Core.Database.IDbConnectionFactory, McpRouter.Core.Database.DbConnectionFactory>();
        builder.Services.AddDbContext<RouterDbContext>();

        // Register Identity Providers (Active Directory & PocketID/TinyAuth OIDC)
        builder.Services.AddSingleton<McpRouter.Core.Identity.ILdapService, McpRouter.Core.Identity.LdapActiveDirectoryService>();
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
        builder.Services.AddMcpOpenIddict(builder.Environment);
        builder.Services.AddControllers();

        builder.Services.AddHttpClient();
        builder.Services.Configure<Microsoft.Extensions.Http.HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(b =>
            {
                var configuration = b.Services.GetRequiredService<IConfiguration>();
                var allowedIpRanges = configuration.GetSection("Security:AllowedIpRanges").Get<string[]>() ?? Array.Empty<string>();

                b.PrimaryHandler = new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        var host = context.DnsEndPoint.Host;
                        var port = context.DnsEndPoint.Port;

                        IPAddress[] ipAddresses;
                        if (IPAddress.TryParse(host, out var directIp))
                        {
                            ipAddresses = new[] { directIp };
                        }
                        else
                        {
                            ipAddresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
                        }

                        if (ipAddresses.Length == 0)
                        {
                            throw new HttpRequestException($"Unable to resolve host '{host}'.");
                        }

                        foreach (var ip in ipAddresses)
                        {
                            if (SecurityValidationHelper.IsBlockedIp(ip, allowedIpRanges))
                            {
                                throw new HttpRequestException($"Access to IP address '{ip}' for host '{host}' is blocked for security (SSRF protection).");
                            }
                        }

                        Socket? socket = null;
                        Exception? lastException = null;

                        foreach (var ip in ipAddresses)
                        {
                            var s = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                            {
                                NoDelay = true
                            };

                            try
                            {
                                await s.ConnectAsync(new IPEndPoint(ip, port), cancellationToken);
                                socket = s;
                                break;
                            }
                            catch (Exception ex)
                            {
                                s.Dispose();
                                lastException = ex;
                            }
                        }

                        if (socket == null)
                        {
                            throw new HttpRequestException($"Failed to connect to host '{host}' ({ipAddresses[0]}) on port {port}.", lastException);
                        }

                        return new NetworkStream(socket, ownsSocket: true);
                    }
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
