using System.Collections.Generic;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using McpRouter.Core.Identity;
using McpRouter.Models;
using McpRouter.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class IdentityProviderTests
    {
        [Fact]
        public async Task OidcIdentityProvider_WithProxyValidation_Allows_TrustedProxy()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5");
            context.Request.Headers["Remote-User"] = "admin-user";
            context.Request.Headers["Remote-Groups"] = "Administrators";

            var configDict = new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "true" },
                { "Oidc:TrustedProxies", "10.0.0.5, 10.0.0.6" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("admin-user", identity.Username);
            Assert.Contains("Administrators", identity.GroupNames);
        }

        [Fact]
        public async Task OidcIdentityProvider_WithProxyValidation_Rejects_UntrustedProxy()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.50");
            context.Request.Headers["Remote-User"] = "admin-user";
            context.Request.Headers["Remote-Groups"] = "Administrators";

            var configDict = new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "true" },
                { "Oidc:TrustedProxies", "10.0.0.5, 10.0.0.6" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            // Since request is from an untrusted proxy, sso headers should be ignored, falling back to guest
            Assert.Equal("guest", identity.Username);
            Assert.Empty(identity.GroupNames);
        }

        [Fact]
        public async Task OidcIdentityProvider_Parses_Remote_User_And_Groups_Headers()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Remote-User"] = "steve";
            context.Request.Headers["Remote-Groups"] = "full_admin, house_member";

            var provider = new OidcIdentityProvider();
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("steve", identity.Username);
            Assert.Equal("PocketID_TinyAuth", identity.AuthenticationType);
            Assert.Equal(2, identity.GroupNames.Count);
            Assert.Contains("full_admin", identity.GroupNames);
            Assert.Contains("house_member", identity.GroupNames);
        }

        [Fact]
        public async Task CompositeIdentityProvider_Falls_Back_To_Oidc_When_AD_Not_Authenticated()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Remote-User"] = "alex";
            context.Request.Headers["sso_groups"] = "dev_team";

            var adProvider = new ActiveDirectoryIdentityProvider();
            var oidcProvider = new OidcIdentityProvider();
            var composite = new CompositeIdentityProvider(new IIdentityProvider[] { adProvider, oidcProvider });

            var identity = await composite.ResolveIdentityAsync(context);

            Assert.Equal("alex", identity.Username);
            Assert.Contains("dev_team", identity.GroupNames);
        }

        [Fact]
        public async Task ADProvider_ResolvesUserSids_AndAllowsAdminRole()
        {
            var mockLdap = new Mock<ILdapService>();
            mockLdap.Setup(l => l.ResolveUserSidsAsync("ad-admin"))
                .ReturnsAsync(new List<string> { "S-1-5-21-100-200-300-1001", "S-1-5-32-544" });

            var configDict = new Dictionary<string, string?>
            {
                { "Admin:GroupSid", "S-1-5-32-544" },
                { "Oidc:RequireTrustedProxy", "true" },
                { "Oidc:TrustedProxies", "10.0.0.5" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5");
            context.Request.Headers["Remote-User"] = "ad-admin";

            var provider = new ActiveDirectoryIdentityProvider(config, mockLdap.Object);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("ad-admin", identity.Username);
            Assert.Contains("S-1-5-32-544", identity.AllSids);

            // Verify authorization bypass in ClientSession
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            var composite = new CompositeIdentityProvider(new IIdentityProvider[] { provider });
            services.AddSingleton(composite);

            context.RequestServices = services.BuildServiceProvider();

            var httpClient = new HttpClient();
            var loggerMock = new Mock<ILogger>();
            var embeddingMock = new Mock<IEmbeddingService>();

            var session = new ClientSession("ad-test-session", context.Response, new List<McpServer>(), httpClient, embeddingMock.Object, loggerMock.Object);

            var authorized = await session.IsUserAuthorizedAsync("tools/call", "restricted_tool_id");
            Assert.True(authorized);
        }

        [Fact]
        public void LdapActiveDirectoryService_ConvertSidBytesToString_ReturnsCorrectSidString()
        {
            // S-1-5-32-544 binary bytes
            byte[] sidBytes = new byte[] { 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00 };
            var result = LdapActiveDirectoryService.ConvertSidBytesToString(sidBytes);
            Assert.Equal("S-1-5-32-544", result);
        }
    }
}

