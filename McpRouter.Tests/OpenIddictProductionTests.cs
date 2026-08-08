using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using McpRouter.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class OpenIddictProductionTests
    {
        [Fact]
        public void OpenIddict_Production_RefusesBoot_WithoutCertificate()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                services.AddMcpOpenIddict(mockEnv.Object, config);
            });

            Assert.Contains("FATAL: A persistent OpenIddict certificate is required outside Development", ex.Message);
        }

        [Fact]
        public void OpenIddict_Production_Boots_WithCertificate()
        {
            var tempPfxPath = Path.Combine(Path.GetTempPath(), $"test_cert_{Guid.NewGuid():N}.pfx");
            try
            {
                // Create self-signed test certificate
                using (var rsa = RSA.Create(2048))
                {
                    var req = new CertificateRequest("CN=TestOpenIddict", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
                    var pfxBytes = cert.Export(X509ContentType.Pfx, "password123");
                    File.WriteAllBytes(tempPfxPath, pfxBytes);
                }

                var services = new ServiceCollection();
                services.AddDbContext<McpRouter.Models.RouterDbContext>();
                var mockEnv = new Mock<IHostEnvironment>();
                mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                    { "OpenIddict:CertificatePath", tempPfxPath },
                    { "OpenIddict:CertificatePassword", "password123" }
                }).Build();

                services.AddMcpOpenIddict(mockEnv.Object, config);
                var provider = services.BuildServiceProvider();

                Assert.NotNull(provider);
            }
            finally
            {
                if (File.Exists(tempPfxPath))
                    File.Delete(tempPfxPath);
            }
        }
    }
}
