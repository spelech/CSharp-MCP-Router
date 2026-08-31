using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ModelContextGateway.Tests
{
    public class CorsTests
    {
        private static Dictionary<string, string?> GetBaseConfig()
        {
            return new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            };
        }

        [Fact]
        [Requirement("GUARD-06", "GUARD", RequirementType.Positive, "CORS default fallback allows localhost origins in Development environment without wildcard permissions.")]
        public void Cors_DefaultFallback_Allows_LocalhostOrigins()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Development";

            var inMemoryConfig = GetBaseConfig();
            builder.Configuration.AddInMemoryCollection(inMemoryConfig);

            // Act
            builder.AddModelContextGatewayServices();
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
        [Requirement("GUARD-06", "GUARD", RequirementType.Negative, "CORS policy fails closed in Production and denies localhost origins by default.")]
        public void Cors_DefaultFallback_Denies_In_Production()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Production";

            var tempCertPath = Path.Combine(Path.GetTempPath(), $"cors_test_{Guid.NewGuid():N}.pfx");
            using (var rsa = System.Security.Cryptography.RSA.Create(2048))
            {
                var certReq = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=TestCert", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                var cert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
                File.WriteAllBytes(tempCertPath, cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx));
            }

            try
            {
                var inMemoryConfig = GetBaseConfig();
                inMemoryConfig["OpenIddict:CertificatePath"] = tempCertPath;
                builder.Configuration.AddInMemoryCollection(inMemoryConfig);

                // Act
                builder.AddModelContextGatewayServices();
                var app = builder.Build();

                // Assert
                var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
                var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

                defaultPolicy.Should().NotBeNull();
                defaultPolicy!.Origins.Should().NotContain("http://localhost:3000");
            }
            finally
            {
                if (File.Exists(tempCertPath))
                {
                    File.Delete(tempCertPath);
                }
            }
        }

        [Fact]
        [Requirement("GUARD-06", "GUARD", RequirementType.Positive, "CORS restricts access strictly to configured CORS_ALLOWED_ORIGINS list.")]
        public void Cors_WithConfiguredOrigins_RestrictsToConfigured()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Development";

            var inMemoryConfig = GetBaseConfig();
            inMemoryConfig["CORS_ALLOWED_ORIGINS"] = "https://my-domain.com, https://another.org";
            builder.Configuration.AddInMemoryCollection(inMemoryConfig);

            // Act
            builder.AddModelContextGatewayServices();
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
        [Requirement("GUARD-06", "GUARD", RequirementType.Positive, "CORS policy falls back to AllowedOrigins configuration key when CORS_ALLOWED_ORIGINS is unset.")]
        public void Cors_WithAllowedOriginsKeyFallback_RestrictsToConfigured()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Development";

            var inMemoryConfig = GetBaseConfig();
            inMemoryConfig["AllowedOrigins"] = "https://allowed-fallback.com";
            builder.Configuration.AddInMemoryCollection(inMemoryConfig);

            // Act
            builder.AddModelContextGatewayServices();
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
