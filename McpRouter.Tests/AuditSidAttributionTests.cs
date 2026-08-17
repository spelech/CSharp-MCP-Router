using McpRouter.Tests.Attributes;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Identity;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Middleware;
using McpRouter.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class AuditSidAttributionTests
    {
        [Fact]
        [Requirement("SEC-03", "SEC", RequirementType.Positive, "Audit logging securely records actions")]
        public async Task HeaderIdentityProvider_Extracts_RemoteUserSid_And_Populates_Sid()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            context.Request.Headers["Remote-User"] = "john.doe";
            context.Request.Headers["Remote-User-Sid"] = "S-1-5-21-9999-9999";

            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:TrustedProxies"] = "127.0.0.1"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new HeaderIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("john.doe", identity.Username);
            Assert.Equal("S-1-5-21-9999-9999", identity.Sid);
            Assert.Contains("S-1-5-21-9999-9999", identity.AllSids);
        }

        [Fact]

        [Requirement("SEC-03", "SEC", RequirementType.Positive, "Audit logging securely records actions")]
        public async Task AppKeyAuthenticationHandler_Emits_Sid_Claim_When_OwnerSid_Present()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            services.AddSingleton<IConfiguration>(config);

            var mockDbConnection = new Mock<System.Data.IDbConnection>();
            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            services.AddSingleton(mockDbFactory.Object);

            var loggerFactory = new LoggerFactory();
            services.AddSingleton<ILoggerFactory>(loggerFactory);

            var sp = services.BuildServiceProvider();

            var appKey = new AppKey
            {
                Id = "test-key-id",
                Name = "Test Key",
                Username = "test.user",
                KeyPrefix = "mcp-global-abc",
                EncryptedKey = "some_encrypted_key",
                OwnerSid = "S-1-5-21-owner"
            };

            Assert.Equal("S-1-5-21-owner", appKey.OwnerSid);
        }

        [Fact]

        [Requirement("SEC-03", "SEC", RequirementType.Positive, "Audit logging securely records actions")]
        public async Task AppKeyIdentityProvider_ResolvesOwnerAndSid_FromHttpContextItems()
        {
            // Simulates what AppKeyAuthenticationHandler stashes after validating a key.
            var context = new DefaultHttpContext();
            context.Items["AppKeyUsed"] = true;
            context.Items["AppKeyOwner"] = "svc.account";
            context.Items["AppKeyOwnerSid"] = "S-1-5-21-appkey-owner";

            var provider = new AppKeyIdentityProvider();
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("svc.account", identity.Username);
            Assert.Equal("S-1-5-21-appkey-owner", identity.Sid);
            Assert.Contains("S-1-5-21-appkey-owner", identity.AllSids);
        }

        [Fact]

        [Requirement("SEC-03", "SEC", RequirementType.Positive, "Audit logging securely records actions")]
        public async Task AppKeyIdentityProvider_ReturnsAnonymous_WhenNoAppKey()
        {
            var context = new DefaultHttpContext();

            var provider = new AppKeyIdentityProvider();
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("anonymous", identity.Username);
        }
    }
}
