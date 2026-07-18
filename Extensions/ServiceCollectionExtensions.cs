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

        // Register SQLite Database
        builder.Services.AddDbContext<RouterDbContext>();

        // Register OpenIddict & Controllers
        builder.Services.AddMcpOpenIddict();
        builder.Services.AddControllers();

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<SessionManager>();
        
        // Register Dynamic Embedding Service (handles settings in encrypted DB)
        builder.Services.AddSingleton<DynamicEmbeddingService>();
        builder.Services.AddSingleton<IEmbeddingService>(sp => sp.GetRequiredService<DynamicEmbeddingService>());

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        }
    }
}
