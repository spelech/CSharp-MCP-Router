using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using McpRouter.Extensions;
using Xunit;
using FluentAssertions;

namespace McpRouter.Tests
{
    public class CorsTests
    {
        private static Dictionary<string, string?> GetBaseConfig()
        {
            return new Dictionary<string, string?>
            {
                { "Encryption:Key", "TestSecretKey1234567890123456789012" },
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            };
        }

        [Fact]
        public void Cors_DefaultFallback_Allows_LocalhostOrigins()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            var inMemoryConfig = GetBaseConfig();
            builder.Configuration.AddInMemoryCollection(inMemoryConfig);

            // Act
            builder.AddMcpRouterServices();
            var app = builder.Build();

            // Assert
            var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
            var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

            defaultPolicy.Should().NotBeNull();
            defaultPolicy!.IsOriginAllowed("*").Should().BeFalse(); // No wildcard
            defaultPolicy.Origins.Should().Contain("http://localhost:3000");
            defaultPolicy.Origins.Should().Contain("http://localhost:5000");
            defaultPolicy.Origins.Should().Contain("https://localhost:5001");
            defaultPolicy.AllowAnyOrigin.Should().BeFalse();
            defaultPolicy.AllowAnyHeader.Should().BeTrue();
            defaultPolicy.AllowAnyMethod.Should().BeTrue();
            defaultPolicy.SupportsCredentials.Should().BeTrue();
        }

        [Fact]
        public void Cors_WithConfiguredOrigins_RestrictsToConfigured()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            var inMemoryConfig = GetBaseConfig();
            inMemoryConfig["CORS_ALLOWED_ORIGINS"] = "https://my-domain.com, https://another.org";
            builder.Configuration.AddInMemoryCollection(inMemoryConfig);

            // Act
            builder.AddMcpRouterServices();
            var app = builder.Build();

            // Assert
            var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
            var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

            defaultPolicy.Should().NotBeNull();
            defaultPolicy!.IsOriginAllowed("*").Should().BeFalse(); // No wildcard
            defaultPolicy.Origins.Should().Contain("https://my-domain.com");
            defaultPolicy.Origins.Should().Contain("https://another.org");
            defaultPolicy.Origins.Should().NotContain("http://localhost:3000");
            defaultPolicy.AllowAnyOrigin.Should().BeFalse();
            defaultPolicy.SupportsCredentials.Should().BeTrue();
        }

        [Fact]
        public void Cors_WithAllowedOriginsKeyFallback_RestrictsToConfigured()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            var inMemoryConfig = GetBaseConfig();
            inMemoryConfig["AllowedOrigins"] = "https://allowed-fallback.com";
            builder.Configuration.AddInMemoryCollection(inMemoryConfig);

            // Act
            builder.AddMcpRouterServices();
            var app = builder.Build();

            // Assert
            var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
            var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

            defaultPolicy.Should().NotBeNull();
            defaultPolicy!.Origins.Should().Contain("https://allowed-fallback.com");
            defaultPolicy.Origins.Should().NotContain("http://localhost:3000");
        }
    }
}
